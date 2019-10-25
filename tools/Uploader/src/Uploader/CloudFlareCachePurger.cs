using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uploader
{
    public class CloudFlareCachePurger
    {
        private readonly HttpClient _httpClient;
        private readonly CloudFlareConfiguration _configuration;
        private readonly ILogger<CloudFlareCachePurger> _logger;

        public CloudFlareCachePurger(HttpClient httpClient, CloudFlareConfiguration configuration, ILogger<CloudFlareCachePurger> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task PurgeFilesAsync(IReadOnlyCollection<string> relativeUrls)
        {
            var request = GetRequestMessage();

            int toProcess = relativeUrls.Count;
            int processed = 0;
            while (processed < toProcess)
            {
                await PurgeUrls(relativeUrls.Skip(processed).Take(25).Select(x => ToFullUri(x).ToString()).ToArray());
                processed += 25;
            }

            foreach (var url in relativeUrls)
            {
                if (url.Contains("index.htm", StringComparison.OrdinalIgnoreCase))
                {
                    var path = new Uri(ToFullUri(url), ".");

                    await PurgeUrls(new[] { path.ToString() });
                }
            }
        }

        private async Task PurgeUrls(string[] urls)
        {
            _logger.LogInformation("Purging urls: {0}", string.Join(Environment.NewLine, urls));

            using var request = GetRequestMessage();
            request.Content = new JsonHttpContent(new UrlsToPurgeContent { Files = urls });

            using var result = await _httpClient.SendAsync(request);

            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError(await result.Content.ReadAsStringAsync());
            }
        }

        private HttpRequestMessage GetRequestMessage()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(_configuration.ZoneId));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration.ApiKey);

            return request;
        }

        private Uri ToFullUri(string relativePath)
        {
            var builder = new UriBuilder(_configuration.ZoneUrlRoot)
            {
                Path = relativePath
            };

            return builder.Uri;
        }

        private static string GetUrl(string zoneId)
        {
            return $"https://api.cloudflare.com/client/v4/zones/{zoneId}/purge_cache";
        }
    }
}
