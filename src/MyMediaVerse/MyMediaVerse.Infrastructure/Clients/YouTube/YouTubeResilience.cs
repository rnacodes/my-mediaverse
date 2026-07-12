using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;

namespace MyMediaVerse.Infrastructure.Clients.YouTube
{
    /// <summary>
    /// Shared resilience configuration for the YouTube Data API HTTP client.
    /// Handles transient failures (HTTP 429 rate limiting and 5xx) with exponential
    /// backoff, honoring the <c>Retry-After</c> header when present.
    ///
    /// It deliberately does NOT handle HTTP 403: YouTube overloads 403 for daily-quota
    /// exhaustion (fatal — must not be retried) and, occasionally, per-user rate limiting.
    /// Distinguishing those requires reading the response body, which is handled in
    /// <see cref="YouTubeApiClient"/> so this predicate can stay status-code only.
    /// </summary>
    public static class YouTubeResilience
    {
        private static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromSeconds(1);
        private const int DefaultMaxRetryAttempts = 5;

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
                // Honor Retry-After on 429/503 when the server sends it (default, made explicit).
                ShouldRetryAfterHeader = true,
                // Retry transient rate limiting (429) and server errors (5xx) only.
                // 403 is intentionally excluded so quota exhaustion surfaces to the client.
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
