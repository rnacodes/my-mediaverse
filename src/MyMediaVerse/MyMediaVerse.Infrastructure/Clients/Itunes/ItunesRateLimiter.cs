using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Configuration;

namespace MyMediaVerse.Infrastructure.Clients.Itunes
{
    /// <summary>
    /// The one token bucket every outbound Apple iTunes call draws from, so the whole process stays under
    /// Apple's published rate however many requests and enrichment runs are in flight. Register as a singleton.
    /// </summary>
    public sealed class ItunesRateLimiter : IDisposable
    {
        public ItunesRateLimiter(IOptions<PodcastDirectoryOptions> options)
            : this(Create(options.Value))
        {
        }

        public ItunesRateLimiter(RateLimiter limiter)
        {
            Limiter = limiter;
        }

        public RateLimiter Limiter { get; }

        private static TokenBucketRateLimiter Create(PodcastDirectoryOptions options) => new(new TokenBucketRateLimiterOptions
        {
            TokenLimit = Math.Max(1, options.AppleBurstLimit),
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(Math.Max(1, options.AppleSecondsPerCall)),
            QueueLimit = Math.Max(0, options.AppleQueueLimit),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

        public void Dispose() => Limiter.Dispose();
    }
}
