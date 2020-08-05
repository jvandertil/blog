using Pulumi;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;
using Pulumi.Azure.Storage.Inputs;
using Pulumi.Cloudflare;

public class BlogStack : Stack
{
    [Output]
    public Output<string> ConnectionString { get; set; }

    [Output]
    public Output<string> StorageAccountName { get; set; }

    [Output]
    public Output<string> FullDomainName { get; set; }

    public BlogStack()
    {
        var config = new Configuration();

        var cloudflareIpRanges = Output.Create(GetIpRanges.InvokeAsync()).Apply(x => x.Ipv4CidrBlocks);

        var storageAccountName = config.StorageAccountName;
        var storageAccountHost = storageAccountName.Apply(x => x + ".z6.web.core.windows.net");

        var verificationDnsRecord = new Record(
            "static-site-verification-dns-record",
            new RecordArgs
            {
                ZoneId = config.CloudFlareZoneId,
                Type = "CNAME",
                Name = "asverify." + config.SubdomainName,
                Value = storageAccountHost.Apply(x => "asverify." + x),
            });

        var verificationIsIndirect = verificationDnsRecord.Name.Apply(x => x.StartsWith("asverify"));

        var customDomainDnsRecord = new Record("static-site-dns-record", new RecordArgs
        {
            ZoneId = config.CloudFlareZoneId,
            Type = "CNAME",
            Name = config.SubdomainName,
            Value = storageAccountHost,
            Proxied = true
        });

        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup("resource-group", new ResourceGroupArgs
        {
            Name = config.ResourceGroupName,
        });

        var fullDomainName = config.SubdomainName + "." + config.DomainName;

        // Create an Azure Storage Account
        var storageAccount = new Account("storage-account", new AccountArgs
        {
            Name = config.StorageAccountName,
            ResourceGroupName = resourceGroup.Name,

            AccountKind = "StorageV2",
            AccountReplicationType = "LRS",
            AccountTier = "Standard",
            AccessTier = "Hot",

            StaticWebsite = new AccountStaticWebsiteArgs
            {
                Error404Document = "404.html",
                IndexDocument = "index.html",
            },

            CustomDomain = new AccountCustomDomainArgs
            {
                Name = fullDomainName,
                UseSubdomain = verificationIsIndirect,
            },

            EnableHttpsTrafficOnly = true,
            NetworkRules = new Pulumi.Azure.Storage.Inputs.AccountNetworkRulesArgs
            {
                Bypasses = "None",
                DefaultAction = "Deny",
                IpRules = cloudflareIpRanges
            },
        });

        ConnectionString = storageAccount.PrimaryConnectionString.Apply(Output.CreateSecret);
        StorageAccountName = storageAccountName;
        FullDomainName = Output.Create(fullDomainName);
    }
}
