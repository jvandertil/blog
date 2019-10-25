using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Uploader
{
    public class Program
    {
        public static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            return builder.Build();
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddLogging(x =>
                {
                    x.AddConsole();
                    x.SetMinimumLevel(LogLevel.Debug);
                })
                .AddSingleton(LoadConfiguration())
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
            using var serviceProvider = ConfigureServices();

            using var scope = serviceProvider.CreateScope();
            var syncer = scope.ServiceProvider.GetRequiredService<ContentSyncer>();
            var cachePurger = scope.ServiceProvider.GetRequiredService<CloudFlareCachePurger>();

            var processedFiles = await syncer.SynchronizeFilesAsync();

            await cachePurger.PurgeFilesAsync(processedFiles);

            return 0;
        }
    }
}
