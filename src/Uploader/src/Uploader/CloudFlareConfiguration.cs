using Microsoft.Extensions.Configuration;

namespace Uploader
{
    public class CloudFlareConfiguration
    {
        public string ApiKey { get; }

        public string ZoneId { get; }

        public string ZoneUrlRoot { get; }

        public int MaxUrlsToPurgePerBlock => 30;

        public CloudFlareConfiguration(IConfiguration configuration)
        {
            ApiKey = configuration.GetValue<string>("CF_API_KEY");
            ZoneId = configuration.GetValue<string>("CF_ZONE_ID");
            ZoneUrlRoot = configuration.GetValue<string>("CF_ZONE_URL_ROOT");
        }
    }
}
