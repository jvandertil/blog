using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlogComments.Functions.Validation
{
    public sealed class CloudflareTurnstileValidator : ITurnstileValidator
    {
        private const string SiteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        private readonly HttpClient _httpClient;
        private readonly TurnstileOptions _options;
        private readonly ILogger<CloudflareTurnstileValidator> _logger;

        public CloudflareTurnstileValidator(
            HttpClient httpClient,
            IOptions<TurnstileOptions> options,
            ILogger<CloudflareTurnstileValidator> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> IsValidAsync(string? token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    SiteverifyUrl,
                    new SiteverifyRequest(_options.SecretKey, token, Guid.NewGuid()),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Cloudflare Turnstile verification returned HTTP {StatusCode}.", (int)response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<SiteverifyResponse>(cancellationToken);
                return result?.Success == true;
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Cloudflare Turnstile verification could not be reached.");
                return false;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Cloudflare Turnstile verification returned an invalid response.");
                return false;
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Cloudflare Turnstile verification timed out.");
                return false;
            }
        }

        private sealed record SiteverifyRequest(
            [property: JsonPropertyName("secret")] string Secret,
            [property: JsonPropertyName("response")] string Response,
            [property: JsonPropertyName("idempotency_key")] Guid IdempotencyKey);

        private sealed record SiteverifyResponse(
            [property: JsonPropertyName("success")] bool Success);
    }
}
