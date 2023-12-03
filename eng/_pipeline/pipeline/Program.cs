using System;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Vandertil.Blog.Pipeline.Azure;
using Vandertil.Blog.Pipeline.CloudFlare;

namespace Vandertil.Blog.Pipeline
{
    public class Program : NukeBuild, IClean, IBlogContentPipeline, IBlogCommentFunctionPipeline
    {
        private const string AzureLocation = "westeurope";

        /// Support plugins are available for:
        ///   - JetBrains ReSharper        https://nuke.build/resharper
        ///   - JetBrains Rider            https://nuke.build/rider
        ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
        ///   - Microsoft VSCode           https://nuke.build/vscode

        [Parameter("Deploy only - The environment that is being deployed.")]
        public Environment Environment { get; set; }

        [Parameter("Deploy only - The API key for CloudFlare.", Name = "CloudflareApiKey")]
        public string CloudFlareApiKey { get; set; }

        [Parameter("Deploy only - The zone id this blog is hosted on in CloudFlare.", Name = "CloudflareZoneId")]
        public string CloudFlareZoneId { get; set; }

        public static int Main() => Execute<Program>(x => x.Build);

        private AbsolutePath ArtifactsDirectory => (this as IProvideArtifactsDirectory).ArtifactsDirectory;
        private AbsolutePath BlogArtifact => ArtifactsDirectory / "blog.zip";
        private AbsolutePath CommentFunctionArtifact => ArtifactsDirectory / "blog-comments-function.zip";

        public Target Build => _ => _
            .Inherit<IBlogContentPipeline>(x => x.Build)
            .Inherit<IBlogCommentFunctionPipeline>(x => x.Build);

        private static AbsolutePath InfraDirectory => RootDirectory / "eng" / "infra";

        private string ResourceGroup => $"rg-jvandertil-blog-{Environment}";

        private string CustomDomain
        {
            get
            {
                if (Environment == Environment.Prd)
                {
                    return "www.jvandertil.nl";
                }
                else
                {
                    return $"{Environment}.jvandertil.nl";
                }
            }
        }

        public Target Deploy => _ => _
            .Requires(() => Environment)
            .Requires(() => CloudFlareApiKey)
            .Requires(() => CloudFlareZoneId)
            .Requires(() => BlogArtifact.FileExists())
            .Requires(() => CommentFunctionArtifact.FileExists())
            .Executes(async () =>
            {
                AzCli.Az($"group create --name {ResourceGroup} --location {AzureLocation}");
                Bicep.Deployments.Blog deployment = await DeployInfrastructureToAzure();

                await CreateOrUpdateCNameRecordAsync($"asverify.{CustomDomain}", $"asverify.{new Uri(deployment.StorageAccountWebEndpoint).Host}", false);
                await CreateOrUpdateCNameRecordAsync($"{CustomDomain}", $"{new Uri(deployment.StorageAccountWebEndpoint).Host}", true);

                await UploadBlogContentAsync(deployment);
                EnableStaticWebsite();

                using var client = new CloudFlareClient(CloudFlareApiKey);
                await client.PurgeZoneCache(CloudFlareZoneId);

                string ipAddress = await GetCurrentIpAddressAsync();
                using (AzFunctionApp.CreateTemporaryScmFirewallRule(ResourceGroup, deployment.FunctionAppName, ipAddress))
                {
                    AzFunctionApp.DeployZipPackage(CommentFunctionArtifact, ResourceGroup, deployment.FunctionAppName);
                }
            });

        private async Task UploadBlogContentAsync(Bicep.Deployments.Blog deployment)
        {
            var contentPath = ArtifactsDirectory / "blog-content";
            BlogArtifact.UncompressTo(contentPath);

            const int MaxAttempts = 5;
            int attempt = 0;
            bool uploaded = false;
            while (!uploaded)
            {
                attempt++;

                try
                {
                    await AzStorage.SyncFolderToContainerAsync(contentPath / Environment, ResourceGroup, deployment.StorageAccountName, "$web");
                    uploaded = true;
                }
                catch
                {
                    Serilog.Log.Information("Error while syncing content, retrying.");

                    if (attempt >= MaxAttempts)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
        }

        private async Task CreateOrUpdateCNameRecordAsync(string name, string content, bool proxied)
        {
            using var client = new CloudFlareClient(CloudFlareApiKey);
            var records = await client.ListDnsRecords(CloudFlareZoneId);

            if (records.Success)
            {
                var record = records.Result.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (record is null)
                {
                    await client.CreateDnsRecord(CloudFlareZoneId, DnsRecordType.CNAME, name, content, proxied);
                }
                else
                {
                    await client.UpdateDnsRecord(CloudFlareZoneId, record.Id, DnsRecordType.CNAME, name, content, proxied);
                }
            }
        }

        private async Task<Bicep.Deployments.Blog> DeployInfrastructureToAzure()
        {
            await HttpTasks.HttpDownloadFileAsync("https://api.cloudflare.com/client/v4/ips", InfraDirectory / "cloudflare-ips.txt");

            var deployment = AzCli.DeployTemplate<Bicep.Deployments.Blog>(InfraDirectory / "blog.bicep", ResourceGroup, new Bicep.Parameters.BlogParameters
            {
                env = Environment,
                location = AzureLocation,
            });

            AzCli.Az($"functionapp cors add --resource-group {ResourceGroup} --name {deployment.FunctionAppName} --allowed-origins https://{CustomDomain}");

            return deployment;
        }

        private static async Task<string> GetCurrentIpAddressAsync()
        {
            string ipAddress = await HttpTasks.HttpDownloadStringAsync("http://ipv4.icanhazip.com/");

            return ipAddress.Trim();
        }

        private void EnableStaticWebsite()
        {
            var deployment = new Bicep.Deployments.Blog(ResourceGroup);

            AzCli.Az($"storage account update --name {deployment.StorageAccountName} --custom-domain {CustomDomain} --use-subdomain true --default-action Deny");
            AzCli.Az($"storage blob service-properties update --auth-mode login --account-name {deployment.StorageAccountName} --static-website true --404-document 404.html --index-document index.html");
        }
    }
}
