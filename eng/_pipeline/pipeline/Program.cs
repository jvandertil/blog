using System;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Vandertil.Blog.Pipeline.Azure;
using Vandertil.Blog.Pipeline.CloudFlare;

namespace Vandertil.Blog.Pipeline
{
    public class Program : NukeBuild, IClean, IBlogContentPipeline, IBlogCommentFunctionPipeline, IBlogUploaderPipeline
    {
        /// Support plugins are available for:
        ///   - JetBrains ReSharper        https://nuke.build/resharper
        ///   - JetBrains Rider            https://nuke.build/rider
        ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
        ///   - Microsoft VSCode           https://nuke.build/vscode

        [Parameter("Deploy only - The environment that is being deployed (tst, prd)")]
        public string Environment { get; set; }

        public static int Main() => Execute<Program>(x => (x as IClean).Clean, x => x.Build);

        private AbsolutePath ArtifactsDirectory => ((this) as IProvideArtifactsDirectory).ArtifactsDirectory;

        public Target Build => _ => _
            .Inherit<IBlogContentPipeline>(x => x.Build)
            .Inherit<IBlogCommentFunctionPipeline>(x => x.Build)
            .Inherit<IBlogUploaderPipeline>(x => x.Build);

        private AbsolutePath InfraDirectory => RootDirectory / "eng" / "infra";

        private string ResourceGroup => $"rg-newinfra-{Environment}";

        private string CustomDomain => Environment switch
        {
            "prd" => "www.jvandertil.nl",
            _ => $"{Environment}.jvandertil.nl"
        };

        public Target Deploy => _ => _
            .Executes(async () =>
            {
                AzCli.Az($"group create --name {ResourceGroup} --location westeurope");
                var deployment = await DeployInfrastructureToAzure();

                await UploadBlogContentAsync(deployment);

                await CreateOrUpdateCNameRecordAsync($"asverify.{CustomDomain}", $"asverify.{new Uri(deployment.StorageAccountWebEndpoint).Host}", false);
                await CreateOrUpdateCNameRecordAsync($"{CustomDomain}", $"{new Uri(deployment.StorageAccountWebEndpoint).Host}", true);
                EnableStaticWebsite();

                var ipAddress = await HttpTasks.HttpDownloadStringAsync("http://ipv4.icanhazip.com/");
                using (AzFunctionApp.CreateTemporaryScmFirewallRule(ResourceGroup, deployment.FunctionAppName, ipAddress))
                {
                    AzFunctionApp.DeployZipPackage(ArtifactsDirectory / "blog-comments-function.zip", ResourceGroup, deployment.FunctionAppName);
                }
            });

        private async Task UploadBlogContentAsync(Bicep.Deployments.Blog deployment)
        {
            CompressionTasks.UncompressZip(ArtifactsDirectory / "blog.zip", ArtifactsDirectory / "blog-content");

            var ipAddress = await HttpTasks.HttpDownloadStringAsync("http://ipv4.icanhazip.com/");
            using (AzStorage.AllowIpAddressTemporary(ResourceGroup, deployment.StorageAccountName, ipAddress))
            {
                await AzStorage.SyncFolderToContainerAsync(ArtifactsDirectory / "blog-content", ResourceGroup, deployment.StorageAccountName, "$web");
            }
        }

        private async Task CreateOrUpdateCNameRecordAsync(string name, string content, bool proxied)
        {
            var client = new CloudFlareClient(CloudFlareApiKey);
            var records = await client.ListDnsRecords(ZoneId);

            if (records.Success)
            {
                var record = records.Result.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (record is null)
                {
                    await client.CreateDnsRecord(ZoneId, DnsRecordType.CNAME, name, content, proxied);
                }
                else
                {
                    await client.UpdateDnsRecord(ZoneId, record.Id, DnsRecordType.CNAME, name, content, proxied);
                }
            }
        }

        private async Task<Bicep.Deployments.Blog> DeployInfrastructureToAzure()
        {
            await HttpTasks.HttpDownloadFileAsync("https://api.cloudflare.com/client/v4/ips", InfraDirectory / "cloudflare-ips.txt");

            return AzCli.DeployTemplate<Bicep.Deployments.Blog>(InfraDirectory / "blog.bicep", ResourceGroup, new Bicep.Parameters.BlogParameters
            {
                env = "tst"
            });
        }

        private void EnableStaticWebsite()
        {
            var deployment = new Bicep.Deployments.Blog(ResourceGroup);

            AzCli.Az($"storage account update --name {deployment.StorageAccountName} --custom-domain {CustomDomain} --use-subdomain true");
            AzCli.Az($"storage blob service-properties update --auth-mode login --account-name {deployment.StorageAccountName} --static-website true --404-document 404.html --index-document index.html");
        }
    }
}
