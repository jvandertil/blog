using Microsoft.Extensions.Configuration;

namespace Uploader
{
    public class CloudFlareConfiguration
    {
        public bool CachePurgeEnabled { get; }

        public string? ApiKey { get; }

        public string? ZoneId { get; }

        public string? ZoneUrlRoot { get; }

        public int MaxUrlsToPurgePerBlock => 30;

        public CloudFlareConfiguration(IConfiguration configuration)
        {
            CachePurgeEnabled = configuration.GetValue<bool>("CF_PURGE_ENABLED");

            if (CachePurgeEnabled)
            {
                ApiKey = configuration.GetValue<string>("CF_API_KEY");
                ZoneId = configuration.GetValue<string>("CF_ZONE_ID");
                ZoneUrlRoot = configuration.GetValue<string>("CF_ZONE_URL_ROOT");
            }
        }
    }
}
