using System;
using System.Collections.Generic;
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
        private readonly HashSet<string> _expectedHostnames;
        private readonly ILogger<CloudflareTurnstileValidator> _logger;

        public CloudflareTurnstileValidator(
            HttpClient httpClient,
            IOptions<TurnstileOptions> options,
            ILogger<CloudflareTurnstileValidator> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _expectedHostnames = new HashSet<string>(_options.ExpectedHostnames, StringComparer.OrdinalIgnoreCase);
            _logger = logger;
        }

        public async Task<bool> IsValidAsync(string? token, string expectedAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 2048 || string.IsNullOrWhiteSpace(expectedAction))
            {
                return false;
            }

            try
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = _options.SecretKey,
                    ["response"] = token,
                    ["idempotency_key"] = Guid.NewGuid().ToString()
                });
                using var response = await _httpClient.PostAsync(SiteverifyUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Cloudflare Turnstile verification returned HTTP {StatusCode}.", (int)response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<SiteverifyResponse>(cancellationToken);
                return result?.Success == true
                    && string.Equals(result.Action, expectedAction, StringComparison.Ordinal)
                    && result.Hostname is not null
                    && _expectedHostnames.Contains(result.Hostname);
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

        private sealed record SiteverifyResponse(
            [property: JsonPropertyName("success")] bool Success,
            [property: JsonPropertyName("action")] string? Action,
            [property: JsonPropertyName("hostname")] string? Hostname);
    }
}
