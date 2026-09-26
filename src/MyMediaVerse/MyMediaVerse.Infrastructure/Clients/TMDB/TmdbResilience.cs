using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace MyMediaVerse.Infrastructure.Clients.TMDB
{
    /// <summary>
    /// Resilience configuration for the TMDB API HTTP client. Retries transient failures
    /// (HTTP 429 rate limiting and 5xx) with exponential backoff, honoring the
    /// <c>Retry-After</c> header TMDB sends when a client is asked to slow down.
    ///
    /// Other 4xx responses are not retried: a 401 means the key is wrong and a 404 means the
    /// id no longer exists, and neither changes on a second attempt.
    /// </summary>
    public static class TmdbResilience
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
