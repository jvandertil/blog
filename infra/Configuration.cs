using Pulumi;
using Config = Pulumi.Config;

internal class Configuration
{
    private readonly Config _config;

    public Configuration()
    {
        _config = new Config();
    }

    public string ResourceGroupName => _config.Require("azure.resourceGroupName");

    public string StorageAccountName => _config.Require("azure.storageAccountName");

    public Output<string> CloudFlareZoneId => _config.RequireSecret("cloudflare.zoneId");

    public string DomainName => _config.Require("domainName");

    public string SubdomainName => _config.Require("subDomainName");
}
