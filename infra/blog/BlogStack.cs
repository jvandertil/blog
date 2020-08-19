using System.Collections.Generic;
using Pulumi;
using Azure = Pulumi.Azure;
using Cloudflare = Pulumi.Cloudflare;

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
        var clientConfig = Output.Create(Azure.Core.GetClientConfig.InvokeAsync());
        var config = new Configuration();

        var cloudflareIpRanges = Output.Create(Cloudflare.GetIpRanges.InvokeAsync()).Apply(x => x.Ipv4CidrBlocks);

        var storageAccountName = config.ContentStorageAccountName;
        var storageAccountHost = storageAccountName.Apply(x => x + ".z6.web.core.windows.net");

        var verificationDnsRecord = new Cloudflare.Record(
            "static-site-verification-dns-record",
            new Cloudflare.RecordArgs
            {
                ZoneId = config.CloudFlareZoneId,
                Type = "CNAME",
                Name = "asverify." + config.SubdomainName,
                Value = storageAccountHost.Apply(x => "asverify." + x),
            });

        var verificationIsIndirect = verificationDnsRecord.Name.Apply(x => x.StartsWith("asverify"));

        var customDomainDnsRecord = new Cloudflare.Record("static-site-dns-record", new Cloudflare.RecordArgs
        {
            ZoneId = config.CloudFlareZoneId,
            Type = "CNAME",
            Name = config.SubdomainName,
            Value = storageAccountHost,
            Proxied = true
        });

        // Create an Azure Resource Group
        var resourceGroup = new Azure.Core.ResourceGroup("resource-group", new Azure.Core.ResourceGroupArgs
        {
            Name = config.ResourceGroupName,
        });

        var fullDomainName = config.SubdomainName + "." + config.DomainName;

        // Create an Azure Storage Account
        var storageAccount = new Azure.Storage.Account("content-storage-account", new Azure.Storage.AccountArgs
        {
            Name = config.ContentStorageAccountName,
            ResourceGroupName = resourceGroup.Name,

            AccountKind = "StorageV2",
            AccountReplicationType = "LRS",
            AccountTier = "Standard",
            AccessTier = "Hot",

            StaticWebsite = new Azure.Storage.Inputs.AccountStaticWebsiteArgs
            {
                Error404Document = "404.html",
                IndexDocument = "index.html",
            },

            CustomDomain = new Azure.Storage.Inputs.AccountCustomDomainArgs
            {
                Name = fullDomainName,
                UseSubdomain = verificationIsIndirect,
            },

            EnableHttpsTrafficOnly = true,
            NetworkRules = new Azure.Storage.Inputs.AccountNetworkRulesArgs
            {
                Bypasses = "None",
                DefaultAction = "Deny",
                IpRules = cloudflareIpRanges
            },
        }, new CustomResourceOptions
        {
            Aliases = new List<Input<Alias>> { new Alias() { Name = "storage-account" } },
            DependsOn = verificationDnsRecord,
        });

        // Create Azure Function App
        var loggingStorageAccount = new Azure.Storage.Account("logging-storage-account", new Azure.Storage.AccountArgs
        {
            Name = config.LoggingStorageAccountName,
            ResourceGroupName = resourceGroup.Name,

            AccountKind = "StorageV2",
            AccountReplicationType = "LRS",
            AccountTier = "Standard",
        });

        var functionAppPlan = new Azure.AppService.Plan("function-app-plan", new Azure.AppService.PlanArgs
        {
            Name = config.FunctionAppServiceName,
            ResourceGroupName = resourceGroup.Name,

            Kind = "FunctionApp",

            Sku = new Azure.AppService.Inputs.PlanSkuArgs
            {
                Tier = "Dynamic",
                Size = "Y1",
            },
        });

        var keyVault = new Azure.KeyVault.KeyVault("key-vault", new Azure.KeyVault.KeyVaultArgs
        {
            Name = config.KeyVaultName,
            ResourceGroupName = resourceGroup.Name,

            SoftDeleteEnabled = true,
            SkuName = "standard",

            TenantId = clientConfig.Apply(x => x.TenantId),
        });

        var applicationInsights = new Azure.AppInsights.Insights("function-app-insights", new Azure.AppInsights.InsightsArgs
        {
            Name = config.AppInsightsName,
            ResourceGroupName = resourceGroup.Name,
            
            DailyDataCapInGb = 1,
            ApplicationType = "web",
            DailyDataCapNotificationsDisabled = true,
        });

        var azureFunctionApp = new Azure.AppService.FunctionApp("function-app", new Azure.AppService.FunctionAppArgs
        {
            Name = config.FunctionAppName,
            AppServicePlanId = functionAppPlan.Id,

            ResourceGroupName = resourceGroup.Name,

            Identity = new Azure.AppService.Inputs.FunctionAppIdentityArgs
            {
                Type = "SystemAssigned"
            },

            StorageAccountName = loggingStorageAccount.Name,
            StorageAccountAccessKey = loggingStorageAccount.PrimaryAccessKey,

            HttpsOnly = true,

            SiteConfig = new Azure.AppService.Inputs.FunctionAppSiteConfigArgs
            {
                FtpsState = "Disabled",
                Http2Enabled = true,
                Cors = new Azure.AppService.Inputs.FunctionAppSiteConfigCorsArgs
                {
                    AllowedOrigins = new InputList<string>
                    {
                        "https://" + fullDomainName,
                    }
                }
            },

            AppSettings = new InputMap<string>
            {
                ["runtime"] = "dotnet",
                ["FUNCTIONS_WORKER_RUNTIME"] = "dotnet",

                ["GitHub__ApplicationId"] = "76324",
                ["GitHub__Username"] = "jvandertil",
                ["GitHub__Repository"] = "blog",
                ["GitHub__EnablePullRequestCreation"] = config.EnableCommentPullRequest.ToString(),

                ["KeyVault__Url"] = keyVault.VaultUri,
                ["KeyVault__KeyName"] = "jvandertil-blog-bot",

                ["APPINSIGHTS_INSTRUMENTATIONKEY"] = applicationInsights.InstrumentationKey,
            },

            Version = "~3",
        }, new CustomResourceOptions
        {
            Parent = functionAppPlan,
        });

        var policy = new Azure.KeyVault.AccessPolicy("kv-allow-asp-sign-policy", new Azure.KeyVault.AccessPolicyArgs
        {
            KeyVaultId = keyVault.Id,

            // Workaround for https://github.com/pulumi/pulumi-azure/issues/192
            ObjectId = azureFunctionApp.Identity.Apply(x => x.PrincipalId ?? "11111111-1111-1111-1111-111111111111"), // Is not null if managed service identity
            KeyPermissions = new InputList<string> { "get", "sign" },
            TenantId = clientConfig.Apply(x => x.TenantId),
        });

        ConnectionString = storageAccount.PrimaryConnectionString.Apply(Output.CreateSecret);
        StorageAccountName = storageAccountName;
        FullDomainName = Output.Create(fullDomainName);
    }
}
