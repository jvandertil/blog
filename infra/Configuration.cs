using Pulumi;
using Config = Pulumi.Config;

internal class Configuration
{
    private readonly Config _config;

    public Configuration()
    {
        _config = new Config();
    }

    public Output<string> ResourceGroupName => _config.RequireSecret("azure.resourceGroupName");

    public Output<string> StorageAccountName => _config.RequireSecret("azure.storageAccountName");

    public Output<string> CloudFlareZoneId => _config.RequireSecret("cloudflare.zoneId");

    public string DomainName => _config.Require("domainName");

    public string SubdomainName => _config.Require("subDomainName");
}
