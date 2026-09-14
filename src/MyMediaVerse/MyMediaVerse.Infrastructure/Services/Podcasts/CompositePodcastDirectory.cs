using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Podcasts
{
    /// <summary>
    /// The directory the application uses: a primary (Apple) with an optional fallback (Podcast Index,
    /// present only when its key is configured). Search uses the primary and turns to the fallback only when
    /// the primary fails. Lookups also ask the fallback when the primary found no show or no feed URL. A
    /// fallback failure is logged and never hides a primary result.
    /// </summary>
    public class CompositePodcastDirectory : IPodcastDirectory
    {
        private readonly IPodcastDirectory _primary;
        private readonly IPodcastDirectory? _fallback;
        private readonly ILogger<CompositePodcastDirectory> _logger;

        public CompositePodcastDirectory(IPodcastDirectory primary, IPodcastDirectory? fallback, ILogger<CompositePodcastDirectory> logger)
        {
            _primary = primary;
            _fallback = fallback;
            _logger = logger;
        }

        public async Task<IReadOnlyList<DirectoryPodcast>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _primary.SearchAsync(term, limit, cancellationToken);
            }
            catch (HttpRequestException ex) when (_fallback != null)
            {
                _logger.LogWarning(ex, "Primary podcast directory search failed; using the fallback directory");
                return await _fallback.SearchAsync(term, limit, cancellationToken);
            }
        }

        public async Task<DirectoryPodcast?> LookupByAppleIdAsync(string applePodcastsId, CancellationToken cancellationToken = default)
        {
            DirectoryPodcast? primaryHit = null;
            ExceptionDispatchInfo? primaryError = null;
            try
            {
                primaryHit = await _primary.LookupByAppleIdAsync(applePodcastsId, cancellationToken);
            }
            catch (HttpRequestException ex) when (_fallback != null)
            {
                primaryError = ExceptionDispatchInfo.Capture(ex);
            }

            if (_fallback == null || primaryHit?.FeedUrl != null)
                return primaryHit;

            var fallbackHit = await TryFallbackAsync(
                () => _fallback.LookupByAppleIdAsync(applePodcastsId, cancellationToken), "Apple id lookup");

            if (primaryHit == null)
            {
                if (fallbackHit == null)
                    primaryError?.Throw();
                return fallbackHit;
            }

            return fallbackHit == null
                ? primaryHit
                : primaryHit with
                {
                    FeedUrl = primaryHit.FeedUrl ?? fallbackHit.FeedUrl,
                    PodcastIndexId = primaryHit.PodcastIndexId ?? fallbackHit.PodcastIndexId
                };
        }

        public async Task<DirectoryPodcast?> LookupByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken = default)
        {
            var primaryHit = await _primary.LookupByFeedUrlAsync(feedUrl, cancellationToken);
            if (primaryHit != null || _fallback == null)
                return primaryHit;

            return await TryFallbackAsync(() => _fallback.LookupByFeedUrlAsync(feedUrl, cancellationToken), "feed URL lookup");
        }

        private async Task<DirectoryPodcast?> TryFallbackAsync(Func<Task<DirectoryPodcast?>> lookup, string operation)
        {
            try
            {
                return await lookup();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Fallback podcast directory {Operation} failed", operation);
                return null;
            }
        }
    }
}
