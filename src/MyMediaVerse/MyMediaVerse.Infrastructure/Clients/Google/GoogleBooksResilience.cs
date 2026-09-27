using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace MyMediaVerse.Infrastructure.Clients.Google
{
    /// <summary>
    /// Resilience configuration for the Google Books API HTTP client. Retries transient
    /// failures (HTTP 429 rate limiting and 5xx) with exponential backoff, honoring the
    /// <c>Retry-After</c> header when present.
    ///
    /// An exhausted daily quota or HTTP 403 are not not retried.
    /// </summary>
    public static class GoogleBooksResilience
    {
        private static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromSeconds(1);
        private const int DefaultMaxRetryAttempts = 3;

        /// <summary>
        /// Builds the retry strategy options. <paramref name="baseDelay"/> is exposed so tests
        /// can compress the backoff to near-zero and exercise the retry path without real waits.
        /// </summary>
        public static HttpRetryStrategyOptions CreateRetryOptions(
            TimeSpan? baseDelay = null,
            int maxRetryAttempts = DefaultMaxRetryAttempts)
        {
            return new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = baseDelay ?? DefaultBaseDelay,
                ShouldRetryAfterHeader = true,
                ShouldHandle = async args => args.Outcome switch
                {
                    { Result: { } response } =>
                        (int)response.StatusCode >= 500
                        || (response.StatusCode == HttpStatusCode.TooManyRequests
                            && !await IsDailyQuotaExhaustedAsync(response, args.Context.CancellationToken)),
                    { Exception: HttpRequestException } => true,
                    _ => false
                }
            };
        }

        /// <summary>
        /// True when a 429 body says a per-day quota ran out. An unreadable or unrecognized
        /// body counts as an ordinary throttle, so it is still retried.
        /// </summary>
        private static async Task<bool> IsDailyQuotaExhaustedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                // Buffer first so the caller can still read the body after this check.
                await response.Content.LoadIntoBufferAsync(cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(body))
                {
                    return false;
                }

                using var document = JsonDocument.Parse(body);

                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("error", out var error)
                    || error.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (error.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                {
                    foreach (var detail in details.EnumerateArray())
                    {
                        if (detail.ValueKind != JsonValueKind.Object
                            || !detail.TryGetProperty("metadata", out var metadata)
                            || metadata.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        // quota_unit looks like "1/d/{project}" for a daily limit, "1/min/..." for a per-minute one.
                        if (GetString(metadata, "quota_unit")?.StartsWith("1/d/", StringComparison.OrdinalIgnoreCase) == true
                            || GetString(metadata, "quota_limit")?.Contains("PerDay", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return true;
                        }
                    }
                }

                return GetString(error, "message")?.Contains("per day", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}
