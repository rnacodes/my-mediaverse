using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Service for enriching podcast series from ListenNotes API.
    /// Processes podcasts in batches with rate limiting to respect API guidelines.
    /// </summary>
    public class PodcastEnrichmentService : IPodcastEnrichmentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IListenNotesApiClient _listenNotesClient;
        private readonly IItunesLookupClient _itunesLookupClient;
        private readonly ILogger<PodcastEnrichmentService> _logger;

        public PodcastEnrichmentService(
            IApplicationDbContext context,
            IListenNotesApiClient listenNotesClient,
            IItunesLookupClient itunesLookupClient,
            ILogger<PodcastEnrichmentService> logger)
        {
            _context = context;
            _listenNotesClient = listenNotesClient;
            _itunesLookupClient = itunesLookupClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> GetPodcastsNeedingEnrichmentCountAsync()
        {
            return await _context.PodcastSeries
                .Where(p => p.ExternalId == null || p.ExternalId == "")
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<PodcastEnrichmentResult> EnrichPodcastsWithoutListenNotesDataAsync(
            int batchSize = 25,
            int delayBetweenCallsMs = 1500,
            CancellationToken cancellationToken = default)
        {
            var result = new PodcastEnrichmentResult();

            try
            {
                // Get podcasts that need enrichment: have no ExternalId (ListenNotes ID)
                var podcastsToEnrich = await _context.PodcastSeries
                    .Where(p => p.ExternalId == null || p.ExternalId == "")
                    .OrderBy(p => p.DateAdded) // Process oldest first
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = podcastsToEnrich.Count;

                if (podcastsToEnrich.Count == 0)
                {
                    _logger.LogInformation("No podcasts found needing ListenNotes enrichment");
                    return result;
                }

                _logger.LogInformation("Starting ListenNotes enrichment for {Count} podcasts", podcastsToEnrich.Count);

                foreach (var podcast in podcastsToEnrich)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Podcast ListenNotes enrichment cancelled");
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        PodcastSeriesDto? podcastDetails = null;
                        PodcastSearchDto? searchMatch = null;

                        if (!string.IsNullOrWhiteSpace(podcast.ApplePodcastsId))
                        {
                            try
                            {
                                _logger.LogDebug("Looking up Apple Podcasts id {AppleId} for podcast: {Title}",
                                    podcast.ApplePodcastsId, podcast.Title);

                                var itunes = await _itunesLookupClient.GetPodcastByCollectionIdAsync(
                                    podcast.ApplePodcastsId, cancellationToken);

                                if (itunes != null)
                                {
                                    ApplyItunesMetadata(podcast, itunes);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex,
                                    "Apple iTunes lookup unavailable for {Title}; proceeding with title/RSS search",
                                    podcast.Title);
                            }
                        }

                        // 2 & 3. Search ListenNotes by title, then disambiguate the results by RSS
                        //        feed url (backfilled from Apple above when available) before the
                        //        title/publisher heuristic.
                        _logger.LogDebug("Searching ListenNotes for podcast: {Title}", podcast.Title);

                        var searchResult = await _listenNotesClient.SearchAsync(
                            query: podcast.Title,
                            type: "podcast");

                        if (searchResult?.Results != null && searchResult.Results.Count > 0)
                        {
                            searchMatch = FindBestPodcastMatch(
                                searchResult.Results, podcast.Title, podcast.Publisher, podcast.RssFeedUrl);

                            if (searchMatch != null)
                            {
                                // Fetch full podcast details for the chosen search result
                                podcastDetails = await _listenNotesClient.GetPodcastByIdAsync(searchMatch.Id);
                            }
                        }

                        if (podcastDetails == null || string.IsNullOrEmpty(podcastDetails.Id))
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("No suitable ListenNotes match found for podcast: {Title}", podcast.Title);
                            continue;
                        }

                        // Map ListenNotes data to entity
                        MapListenNotesToEntity(podcast, podcastDetails, searchMatch);
                        _context.Update(podcast);
                        result.EnrichedCount++;

                        _logger.LogDebug("Successfully enriched podcast: {Title} (ListenNotes ID: {ExternalId})",
                            podcast.Title, podcast.ExternalId);
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to enrich podcast '{podcast.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to enrich podcast: {Title}", podcast.Title);
                    }

                    // Rate limiting: delay between API calls (ListenNotes has stricter limits)
                    if (delayBetweenCallsMs > 0)
                    {
                        await Task.Delay(delayBetweenCallsMs, cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Podcast ListenNotes enrichment complete. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                _logger.LogInformation("Podcast ListenNotes enrichment was cancelled");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Podcast enrichment run failed: {ex.Message}");
                _logger.LogError(ex, "Podcast ListenNotes enrichment run failed");
            }

            return result;
        }

        /// <summary>
        /// Finds the best matching podcast from search results.
        /// Prefers an exact RSS feed url match (the strongest signal in a search result),
        /// then exact/partial title matches with an optional publisher tie-break.
        /// </summary>
        private PodcastSearchDto? FindBestPodcastMatch(
            List<PodcastSearchDto> results,
            string title,
            string? publisher,
            string? rssFeedUrl = null)
        {
            if (results.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(rssFeedUrl))
            {
                var feedMatch = results.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.Rss) && RssUrlsMatch(r.Rss!, rssFeedUrl));

                if (feedMatch != null)
                    return feedMatch;
            }

            var normalizedTitle = NormalizeForComparison(title);
            var normalizedPublisher = publisher != null ? NormalizeForComparison(publisher) : null;

            // First, try to find exact title match
            var exactMatch = results.FirstOrDefault(r =>
                NormalizeForComparison(r.TitleOriginal ?? "") == normalizedTitle);

            if (exactMatch != null)
                return exactMatch;

            // If we have publisher info, try to find a match that includes both title and publisher
            if (!string.IsNullOrEmpty(normalizedPublisher))
            {
                var publisherMatch = results.FirstOrDefault(r =>
                    NormalizeForComparison(r.TitleOriginal ?? "").Contains(normalizedTitle) &&
                    NormalizeForComparison(r.PublisherOriginal ?? "").Contains(normalizedPublisher));

                if (publisherMatch != null)
                    return publisherMatch;
            }

            // Try to find a partial title match (title contains our search term)
            var partialMatch = results.FirstOrDefault(r =>
                NormalizeForComparison(r.TitleOriginal ?? "").Contains(normalizedTitle));

            if (partialMatch != null)
                return partialMatch;

            // Fall back to first result (highest relevance from search)
            return results[0];
        }

        /// <summary>
        /// Backfills a stub from Apple iTunes Lookup data, only filling fields that are null/empty.
        /// The RSS feed url is the most valuable field.
        /// </summary>
        private static void ApplyItunesMetadata(PodcastSeries podcast, ItunesPodcastDto itunes)
        {
            if (string.IsNullOrWhiteSpace(podcast.RssFeedUrl) && !string.IsNullOrWhiteSpace(itunes.FeedUrl))
            {
                podcast.RssFeedUrl = itunes.FeedUrl;
            }

            if (string.IsNullOrEmpty(podcast.Publisher) && !string.IsNullOrEmpty(itunes.ArtistName))
            {
                podcast.Publisher = itunes.ArtistName;
            }

            if (string.IsNullOrEmpty(podcast.Thumbnail) && !string.IsNullOrEmpty(itunes.ArtworkUrl600))
            {
                podcast.Thumbnail = itunes.ArtworkUrl600;
            }

            if (podcast.TotalEpisodes == 0 && itunes.TrackCount is int trackCount && trackCount > 0)
            {
                podcast.TotalEpisodes = trackCount;
            }

            if (string.IsNullOrEmpty(podcast.Link) && !string.IsNullOrEmpty(itunes.CollectionViewUrl))
            {
                podcast.Link = itunes.CollectionViewUrl;
            }
        }

        /// <summary>
        /// Compares two RSS feed urls for equality, tolerating trailing-slash and case differences.
        /// </summary>
        private static bool RssUrlsMatch(string a, string b)
        {
            return string.Equals(
                a.Trim().TrimEnd('/'),
                b.Trim().TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Normalizes a string for comparison by converting to lowercase and removing special characters.
        /// </summary>
        private string NormalizeForComparison(string input)
        {
            return input.ToLowerInvariant()
                .Replace("the ", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace(":", "")
                .Replace("-", " ")
                .Trim();
        }

        /// <summary>
        /// Maps ListenNotes podcast details to the PodcastSeries entity, only updating fields that are null/empty.
        /// <paramref name="searchResult"/> is optional supplemental data from a title search; it is null
        /// when the match was resolved directly by Apple Podcasts id.
        /// </summary>
        private void MapListenNotesToEntity(
            PodcastSeries podcast,
            PodcastSeriesDto details,
            PodcastSearchDto? searchResult)
        {
            // Always set ExternalId as this is the primary enrichment identifier
            podcast.ExternalId = details.Id;

            // Set description if not already set
            if (string.IsNullOrEmpty(podcast.Description) && !string.IsNullOrEmpty(details.Description))
            {
                podcast.Description = details.Description;
            }

            // Set publisher if not already set
            if (string.IsNullOrEmpty(podcast.Publisher) && !string.IsNullOrEmpty(details.Publisher))
            {
                podcast.Publisher = details.Publisher;
            }

            // Set thumbnail if not already set
            if (string.IsNullOrEmpty(podcast.Thumbnail))
            {
                // Prefer the thumbnail from details, fall back to search result
                var thumbnailUrl = !string.IsNullOrEmpty(details.Thumbnail)
                    ? details.Thumbnail
                    : searchResult?.Thumbnail;

                if (!string.IsNullOrEmpty(thumbnailUrl))
                {
                    podcast.Thumbnail = thumbnailUrl;
                }
            }

            // Set total episodes, preferring the details count and falling back to the search result
            if (podcast.TotalEpisodes == 0)
            {
                if (details.TotalEpisodes > 0)
                {
                    podcast.TotalEpisodes = details.TotalEpisodes;
                }
                else if (searchResult?.TotalEpisodes is int totalEpisodes)
                {
                    podcast.TotalEpisodes = totalEpisodes;
                }
            }

            // Set website link if not already set
            if (string.IsNullOrEmpty(podcast.Link))
            {
                var website = details.Website ?? searchResult?.Website;
                if (!string.IsNullOrEmpty(website))
                {
                    podcast.Link = website;
                }
            }
        }
    }
}
