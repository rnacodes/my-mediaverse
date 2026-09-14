using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace MyMediaVerse.Infrastructure.Clients.Google
{
    /// <summary>
    /// Resilience configuration for the Google Books API HTTP client. Retries transient
    /// failures (HTTP 429 rate limiting and 5xx) with exponential backoff, honoring the
    /// <c>Retry-After</c> header when present.
    ///
    /// HTTP 403 is not retried: Google returns it when the daily quota is exhausted, and
    /// retrying would only burn more requests against a limit that won't reset for hours.
    /// Fewer attempts than the batch-oriented clients, because book search is interactive.
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
                ShouldHandle = args => ValueTask.FromResult(args.Outcome switch
                {
                    { Result: { } response } =>
                        response.StatusCode == HttpStatusCode.TooManyRequests
                        || (int)response.StatusCode >= 500,
                    { Exception: HttpRequestException } => true,
                    _ => false
                })
            };
        }
    }
}
