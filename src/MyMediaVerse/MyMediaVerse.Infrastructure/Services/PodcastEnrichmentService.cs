using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Interfaces;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Service for enriching podcast series from ListenNotes API.
    /// Processes podcasts in batches with rate limiting to respect API guidelines.
    /// </summary>
    public class PodcastEnrichmentService : IPodcastEnrichmentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IListenNotesApiClient _listenNotesClient;
        private readonly ILogger<PodcastEnrichmentService> _logger;

        public PodcastEnrichmentService(
            IApplicationDbContext context,
            IListenNotesApiClient listenNotesClient,
            ILogger<PodcastEnrichmentService> logger)
        {
            _context = context;
            _listenNotesClient = listenNotesClient;
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
                        _logger.LogDebug("Searching ListenNotes for podcast: {Title}", podcast.Title);

                        // Search ListenNotes by title, specifying type as "podcast" to filter results
                        var searchResult = await _listenNotesClient.SearchAsync(
                            query: podcast.Title,
                            type: "podcast");

                        if (searchResult == null || searchResult.Results == null || searchResult.Results.Count == 0)
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("No ListenNotes match found for podcast: {Title}", podcast.Title);
                            continue;
                        }

                        // Find best match - prefer exact title match, otherwise use first result
                        var match = FindBestPodcastMatch(searchResult.Results, podcast.Title, podcast.Publisher);

                        if (match == null)
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("No suitable ListenNotes match found for podcast: {Title}", podcast.Title);
                            continue;
                        }

                        // Fetch full podcast details
                        var podcastDetails = await _listenNotesClient.GetPodcastByIdAsync(match.Id);

                        if (podcastDetails == null)
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("Failed to fetch podcast details for: {Title}", podcast.Title);
                            continue;
                        }

                        // Map ListenNotes data to entity
                        MapListenNotesToEntity(podcast, podcastDetails, match);
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

                // Save all changes
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
        /// Prefers exact title matches, then considers publisher matches.
        /// </summary>
        private PodcastSearchDto? FindBestPodcastMatch(
            List<PodcastSearchDto> results,
            string title,
            string? publisher)
        {
            if (results.Count == 0)
                return null;

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
        /// </summary>
        private void MapListenNotesToEntity(
            PodcastSeries podcast,
            PodcastSeriesDto details,
            PodcastSearchDto searchResult)
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
                    : searchResult.Thumbnail;

                if (!string.IsNullOrEmpty(thumbnailUrl))
                {
                    podcast.Thumbnail = thumbnailUrl;
                }
            }

            // Set total episodes from search result (more accurate than details.Episodes.Count)
            if (podcast.TotalEpisodes == 0 && searchResult.TotalEpisodes.HasValue)
            {
                podcast.TotalEpisodes = searchResult.TotalEpisodes.Value;
            }

            // Set website link if not already set
            if (string.IsNullOrEmpty(podcast.Link))
            {
                var website = details.Website ?? searchResult.Website;
                if (!string.IsNullOrEmpty(website))
                {
                    podcast.Link = website;
                }
            }
        }
    }
}
