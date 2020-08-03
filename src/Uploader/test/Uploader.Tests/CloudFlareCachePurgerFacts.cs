using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Uploader.Tests.Stubs;
using Xunit;
using Xunit.Abstractions;

namespace Uploader.Tests
{
    public class CloudFlareCachePurgerFacts
    {
        private const string ZoneId = "12345678900000";
        private const string ApiKey = "API_KEY_:D";

        private static readonly Uri UrlRoot = new Uri("https://www.example.org/");

        private readonly CloudFlareCachePurger _purger;
        private readonly StubHttpMessageHandler _httpStub;

        public CloudFlareCachePurgerFacts(ITestOutputHelper outputHelper)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["CF_PURGE_ENABLED"] = bool.TrueString,
                    ["CF_API_KEY"] = ApiKey,
                    ["CF_ZONE_ID"] = ZoneId,
                    ["CF_ZONE_URL_ROOT"] = UrlRoot.ToString(),

                })
                .Build();

            _httpStub = new StubHttpMessageHandler();

            _purger = new CloudFlareCachePurger(
                new HttpClient(_httpStub),
                new CloudFlareConfiguration(configuration),
                new XunitLogger<CloudFlareCachePurger>(outputHelper));
        }

        [Fact]
        public async Task PurgeFilesAsync_SendsRequestToCorrectZoneId()
        {
            await _purger.PurgeFilesAsync(new[] { "/test.html" });

            var performedRequest = _httpStub.PerformedRequests[0].request;

            Assert.Equal(new Uri("https://api.cloudflare.com/client/v4/zones/" + ZoneId + "/purge_cache"), performedRequest.RequestUri);
        }

        [Fact]
        public async Task PurgeFilesAsync_SendsFullUrisInBody()
        {
            var urlsToPurge = new[] { "/test.html" };

            await _purger.PurgeFilesAsync(urlsToPurge);

            AssertAllUrlsPurged(urlsToPurge);
        }

        [Fact]
        public async Task PurgeFilesAsync_SendsApiKeyInAuthenticationHeader()
        {
            var urlsToPurge = new[] { "/test.html" };

            await _purger.PurgeFilesAsync(urlsToPurge);

            var auth = _httpStub.PerformedRequests[0].request.Headers.Authorization;
            Assert.Equal("Bearer", auth.Scheme);
            Assert.Equal(ApiKey, auth.Parameter);
        }

        [Fact]
        public async Task PurgeFilesAsync_ChunksRequestsIntoBlocksOf30()
        {
            var urlsToPurge = new List<string>();

            for (int i = 0; i < 100; ++i)
            {
                urlsToPurge.Add(Guid.NewGuid().ToString());
            }

            await _purger.PurgeFilesAsync(urlsToPurge);

            for (int i = 0; i < 3; ++i)
            {
                int count = Deserialize(_httpStub.PerformedRequests[i].body).Files.Count();

                Assert.Equal(30, count);
            }

            AssertAllUrlsPurged(urlsToPurge);
        }

        [Fact]
        public async Task PurgeFilesAsync_PurgesFolderPathIfIndexIsPurged()
        {
            var urlsToPurge = new[] { "/bla/index.html" };

            await _purger.PurgeFilesAsync(urlsToPurge);

            AssertAllUrlsPurged(urlsToPurge.Append("/bla/"));
        }

        private void AssertAllUrlsPurged(IEnumerable<string> relativePaths)
        {
            var allFiles = new List<string>();

            foreach (var request in _httpStub.PerformedRequests)
            {
                var content = Deserialize(request.body);

                allFiles.AddRange(content.Files);
            }

            foreach (var url in relativePaths)
            {
                Assert.Contains(new Uri(UrlRoot, url).ToString(), allFiles);
            }
        }

        private UrlsToPurgeContent Deserialize(string body)
        {
            return JsonSerializer.Deserialize<UrlsToPurgeContent>(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}
