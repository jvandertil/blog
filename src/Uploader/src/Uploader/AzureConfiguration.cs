using Microsoft.Extensions.Configuration;

namespace Uploader
{
    public class AzureConfiguration
    {
        public string ConnectionString { get; }

        public AzureConfiguration(IConfiguration configuration)
        {
            ConnectionString = configuration.GetValue<string>("AZURE_CONNECTIONSTRING");
        }
    }
}
