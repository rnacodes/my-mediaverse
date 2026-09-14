using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Fills gaps in podcast series that have not been enriched yet. Today that is the Apple
    /// Podcasts backfill for series with an Apple id (feed URL, publisher, artwork, episode count);
    /// filling series from their own feeds comes next. Processes series in batches with a delay
    /// between directory calls.
    /// </summary>
    public class PodcastEnrichmentService : IPodcastEnrichmentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IItunesLookupClient _itunesLookupClient;
        private readonly ILogger<PodcastEnrichmentService> _logger;

        public PodcastEnrichmentService(
            IApplicationDbContext context,
            IItunesLookupClient itunesLookupClient,
            ILogger<PodcastEnrichmentService> logger)
        {
            _context = context;
            _itunesLookupClient = itunesLookupClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> GetPodcastsNeedingEnrichmentCountAsync()
        {
            return await _context.PodcastSeries
                .Where(p => p.EnrichedAt == null)
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<PodcastEnrichmentResult> EnrichPendingPodcastsAsync(
            int batchSize = 25,
            int delayBetweenCallsMs = 1500,
            CancellationToken cancellationToken = default)
        {
            var result = new PodcastEnrichmentResult();

            try
            {
                // Never-attempted series first, then the longest-waiting, so series that cannot be
                // enriched yet do not hold the front of the queue on every run.
                var podcastsToEnrich = await _context.PodcastSeries
                    .Where(p => p.EnrichedAt == null)
                    .OrderBy(p => p.LastEnrichmentAttemptAt.HasValue)
                    .ThenBy(p => p.LastEnrichmentAttemptAt)
                    .ThenBy(p => p.DateAdded)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = podcastsToEnrich.Count;

                if (podcastsToEnrich.Count == 0)
                {
                    _logger.LogInformation("No podcasts found needing enrichment");
                    return result;
                }

                _logger.LogInformation("Starting podcast enrichment for {Count} podcasts", podcastsToEnrich.Count);

                foreach (var podcast in podcastsToEnrich)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Podcast enrichment cancelled");
                        result.WasCancelled = true;
                        break;
                    }

                    podcast.LastEnrichmentAttemptAt = DateTime.UtcNow;

                    if (string.IsNullOrWhiteSpace(podcast.ApplePodcastsId))
                    {
                        result.NotFoundCount++;
                        _logger.LogDebug("Podcast {Title} has no Apple Podcasts id; nothing to enrich from yet", podcast.Title);
                        continue;
                    }

                    try
                    {
                        _logger.LogDebug("Looking up Apple Podcasts id {AppleId} for podcast: {Title}",
                            podcast.ApplePodcastsId, podcast.Title);

                        var itunes = await _itunesLookupClient.GetPodcastByCollectionIdAsync(
                            podcast.ApplePodcastsId, cancellationToken);

                        if (itunes != null && ApplyItunesMetadata(podcast, itunes))
                        {
                            podcast.EnrichedAt = DateTime.UtcNow;
                            result.EnrichedCount++;
                            _logger.LogDebug("Enriched podcast {Title} from Apple Podcasts", podcast.Title);
                        }
                        else
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("Apple Podcasts had nothing to add for podcast: {Title}", podcast.Title);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to enrich podcast '{podcast.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to enrich podcast: {Title}", podcast.Title);
                    }

                    // Space out directory calls.
                    if (delayBetweenCallsMs > 0)
                    {
                        await Task.Delay(delayBetweenCallsMs, cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Podcast enrichment complete. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                _logger.LogInformation("Podcast enrichment was cancelled");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Podcast enrichment run failed: {ex.Message}");
                _logger.LogError(ex, "Podcast enrichment run failed");
            }

            return result;
        }

        /// <summary>
        /// Backfills a stub from Apple iTunes Lookup data, only filling fields that are null/empty.
        /// The RSS feed url is the most valuable field.
        /// Returns true if at least one previously-empty field was filled.
        /// </summary>
        private static bool ApplyItunesMetadata(PodcastSeries podcast, ItunesPodcastDto itunes)
        {
            var applied = false;

            if (string.IsNullOrWhiteSpace(podcast.RssFeedUrl) && !string.IsNullOrWhiteSpace(itunes.FeedUrl))
            {
                podcast.RssFeedUrl = itunes.FeedUrl;
                applied = true;
            }

            if (string.IsNullOrEmpty(podcast.Publisher) && !string.IsNullOrEmpty(itunes.ArtistName))
            {
                podcast.Publisher = itunes.ArtistName;
                applied = true;
            }

            if (string.IsNullOrEmpty(podcast.Thumbnail) && !string.IsNullOrEmpty(itunes.ArtworkUrl600))
            {
                podcast.Thumbnail = itunes.ArtworkUrl600;
                applied = true;
            }

            if (podcast.TotalEpisodes == 0 && itunes.TrackCount is int trackCount && trackCount > 0)
            {
                podcast.TotalEpisodes = trackCount;
                applied = true;
            }

            if (string.IsNullOrEmpty(podcast.Link) && !string.IsNullOrEmpty(itunes.CollectionViewUrl))
            {
                podcast.Link = itunes.CollectionViewUrl;
                applied = true;
            }

            return applied;
        }
    }
}
