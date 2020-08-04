using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Uploader
{
    public class Program
    {
        public class CommandLineOptions
        {
            [Option('d', "destination")]
            public string? AzureConnectionString { get; set; }

            [Option('s', "source")]
            public string? ContentPath { get; set; }

            [Option("no-purge", Required = false)]
            public bool NoCachePurge { get; set; }

            [Option("cf-apikey")]
            public string? CloudFlareApiKey { get; set; }

            [Option("cf-zoneid")]
            public string? CloudFlareZoneId { get; set; }

            [Option("cf-urlroot")]
            public string? CloudFlareUrlRoot { get; set; }
        }

        public static IConfiguration BuildConfiguration(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);

            if (result is Parsed<CommandLineOptions> parsed)
            {
                var options = parsed.Value;

                var argsConfig = new Dictionary<string, string>();

                if (options.AzureConnectionString != null)
                    argsConfig["AZURE_CONNECTIONSTRING"] = options.AzureConnectionString;

                if (options.ContentPath != null)
                    argsConfig["AZURE_CONTENT_PATH"] = options.ContentPath;

                if (options.CloudFlareApiKey != null)
                    argsConfig["CF_API_KEY"] = options.CloudFlareApiKey;

                if (options.CloudFlareZoneId != null)
                    argsConfig["CF_ZONE_ID"] = options.CloudFlareZoneId;

                if (options.CloudFlareUrlRoot != null)
                    argsConfig["CF_ZONE_URL_ROOT"] = options.CloudFlareUrlRoot;

                argsConfig["CF_PURGE_ENABLED"] = (!options.NoCachePurge).ToString();

                builder.AddInMemoryCollection(argsConfig);
            }

            return builder.Build();
        }

        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            return new ServiceCollection()
                .AddLogging(x =>
                {
                    x.AddConsole();
                    x.SetMinimumLevel(LogLevel.Debug);
                })
                .AddSingleton(configuration)
                .AddTransient<CloudFlareConfiguration>()
                .AddTransient<AzureConfiguration>()
                .AddTransient<DiskContentSourceConfiguration>()
                .AddTransient<IContentDestination, AzureContentDestination>()
                .AddTransient<IContentSource, DiskContentSource>()
                .AddTransient<ContentSyncer>()
                .AddTransient<CloudFlareCachePurger>()
                .AddTransient<HttpClient>()
                .BuildServiceProvider();
        }

        public static async Task<int> Main(string[] args)
        {
            var configuration = BuildConfiguration(args);

            using var serviceProvider = ConfigureServices(configuration);
            using (var scope = serviceProvider.CreateScope())
            {

                var syncer = scope.ServiceProvider.GetRequiredService<ContentSyncer>();
                var cachePurger = scope.ServiceProvider.GetRequiredService<CloudFlareCachePurger>();

                var processedFiles = await syncer.SynchronizeFilesAsync();

                await cachePurger.PurgeFilesAsync(processedFiles);
            }

            return 0;
        }
    }
}
