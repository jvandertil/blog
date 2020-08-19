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

    public Output<string> ContentStorageAccountName => _config.RequireSecret("azure.contentStorageAccountName");

    public string LoggingStorageAccountName => _config.Require("azure.loggingStorageAccountName");

    public string FunctionAppName => _config.Require("azure.functionAppName");

    public string FunctionAppServiceName => _config.Require("azure.functionAppServiceName");

    public string KeyVaultName => _config.Require("azure.keyvaultName");

    public string AppInsightsName => _config.Require("azure.appInsightsName");

    public Output<string> CloudFlareZoneId => _config.RequireSecret("cloudflare.zoneId");

    public string DomainName => _config.Require("domainName");

    public string SubdomainName => _config.Require("subDomainName");

    public bool EnableCommentPullRequest => _config.RequireBoolean("github.enablePullRequestCreation");
}

