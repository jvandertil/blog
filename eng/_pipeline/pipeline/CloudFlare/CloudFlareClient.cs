using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Vandertil.Blog.Pipeline.CloudFlare
{
    public class CloudFlareClient : IDisposable
    {
        private readonly HttpClient _client;

        public CloudFlareClient(string bearerToken)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
            };

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }

        public async Task<CloudFlareResponse<DnsRecord[]>> ListDnsRecords(string zoneId)
        {
            using var response = await _client.GetAsync($"zones/{zoneId}/dns_records").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await ReadResponse<DnsRecord[]>(response);
        }

        public async Task<CloudFlareResponse<DnsRecord>> CreateDnsRecord(string zoneId, DnsRecordType type, string name, string content, bool proxied)
        {
            var body = new
            {
                type = type.ToString(),
                name = name,
                content = content,
                ttl = 1,
                proxied = proxied,
            };

            using var response = await _client.PostAsJsonAsync($"zones/{zoneId}/dns_records", body);
            response.EnsureSuccessStatusCode();

            return await ReadResponse<DnsRecord>(response);
        }

        public async Task<CloudFlareResponse<DnsRecord>> UpdateDnsRecord(string zoneId, string recordId, DnsRecordType type, string name, string content, bool proxied)
        {
            var body = new
            {
                type = type.ToString(),
                name = name,
                content = content,
                ttl = 1,
                proxied = proxied,
            };

            using var response = await _client.PutAsJsonAsync($"zones/{zoneId}/dns_records/{recordId}", body);
            response.EnsureSuccessStatusCode();

            return await ReadResponse<DnsRecord>(response);
        }

        public async Task PurgeZoneCache(string zoneId)
        {
            var body = new
            {
                purge_everything = true
            };

            using var response = await _client.PostAsJsonAsync($"zones/{zoneId}/purge_cache", body);
            response.EnsureSuccessStatusCode();
        }

        private async Task<CloudFlareResponse<T>> ReadResponse<T>(HttpResponseMessage response)
        {
            return await response.Content.ReadFromJsonAsync<CloudFlareResponse<T>>(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }).ConfigureAwait(false);
        }

        public void Dispose() => _client.Dispose();
    }
}
