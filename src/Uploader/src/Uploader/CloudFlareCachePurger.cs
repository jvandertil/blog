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

        private readonly string _uri;

        public CloudFlareCachePurger(HttpClient httpClient, CloudFlareConfiguration configuration, ILogger<CloudFlareCachePurger> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _uri = "https://api.cloudflare.com/client/v4/zones/" + _configuration.ZoneId + "/purge_cache";
        }

        public async Task PurgeFilesAsync(IReadOnlyCollection<string> relativeUrls)
        {
            if (!_configuration.CachePurgeEnabled)
            {
                return;
            }

            var locationUrls = new List<string>(relativeUrls);

            foreach (var url in relativeUrls)
            {
                if (url.Contains("index.htm", StringComparison.OrdinalIgnoreCase))
                {
                    var path = new Uri(ToFullUri(url), ".").LocalPath;

                    locationUrls.Add(path);

                    _logger.LogInformation("Purging location {path} because index.html was found.", path);
                }
            }

            await PurgeUrls(locationUrls);
        }

        private async Task PurgeUrls(IReadOnlyCollection<string> urls)
        {
            int toProcess = urls.Count;
            int processed = 0;
            int blockSize = _configuration.MaxUrlsToPurgePerBlock;

            while (processed < toProcess)
            {
                await PurgeUrlsBlock(urls.Skip(processed).Take(blockSize).Select(x => ToFullUri(x).ToString()));
                processed += blockSize;
            }
        }

        private async Task PurgeUrlsBlock(IEnumerable<string> urls)
        {
            _logger.LogInformation("Purging urls: {0}", string.Join(Environment.NewLine, urls));

            using var request = GetRequestMessage();
            request.Content = new JsonHttpContent(new UrlsToPurgeContent(urls));

            using var result = await _httpClient.SendAsync(request);

            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError(await result.Content.ReadAsStringAsync());
            }
        }

        private HttpRequestMessage GetRequestMessage()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _uri);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration.ApiKey);

            return request;
        }

        private Uri ToFullUri(string relativePath)
        {
            var builder = new UriBuilder(_configuration.ZoneUrlRoot!)
            {
                Path = relativePath
            };

            return builder.Uri;
        }
    }
}
