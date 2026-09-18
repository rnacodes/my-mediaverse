using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Fills podcast series from their own RSS feed. A series with no feed URL is resolved through the
    /// directory first, so a show added by hand can still be filled. Each series is its own unit of
    /// work: one unreachable feed does not cost the rest of the batch its results.
    /// </summary>
    public class PodcastEnrichmentService : IPodcastEnrichmentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPodcastDirectory _directory;
        private readonly IPodcastFeedReader _feedReader;
        private readonly PodcastEnrichmentOptions _options;
        private readonly ILogger<PodcastEnrichmentService> _logger;

        public PodcastEnrichmentService(
            IApplicationDbContext context,
            IPodcastDirectory directory,
            IPodcastFeedReader feedReader,
            IOptions<PodcastEnrichmentOptions> options,
            ILogger<PodcastEnrichmentService> logger)
        {
            _context = context;
            _directory = directory;
            _feedReader = feedReader;
            _options = options.Value;
            _logger = logger;
        }

        private IQueryable<PodcastSeries> TrackedSeriesWithGenres => _context.PodcastSeries.Include(p => p.Genres);

        /// <summary>
        /// Never filled, and either never attempted or last attempted long enough ago to be worth
        /// another try. Without the retry window a permanently dead feed would be reattempted on
        /// every run, holding the front of the queue.
        /// </summary>
        private static IQueryable<PodcastSeries> Eligible(IQueryable<PodcastSeries> series, DateTime retryCutoff) =>
            series.Where(p => p.EnrichedAt == null
                && (p.LastEnrichmentAttemptAt == null || p.LastEnrichmentAttemptAt < retryCutoff));

        private DateTime RetryCutoff(DateTime now) =>
            now - TimeSpan.FromDays(Math.Max(0, _options.RetryAfterDays));

        public async Task<int> GetPodcastsNeedingEnrichmentCountAsync() =>
            await Eligible(_context.PodcastSeries, RetryCutoff(DateTime.UtcNow)).CountAsync();

        public async Task<PodcastEnrichmentResult> EnrichPendingPodcastsAsync(
            int? batchSize = null,
            int? delayBetweenCallsMs = null,
            CancellationToken cancellationToken = default)
        {
            var result = new PodcastEnrichmentResult { StartedAt = DateTime.UtcNow };
            var size = Math.Max(1, batchSize ?? _options.BatchSize);
            var delayMs = delayBetweenCallsMs ?? _options.DelayBetweenCallsMs;

            try
            {
                // Ids only: each series is loaded inside its own unit of work below. Holding a tracked
                // list here would not survive the change-tracker reset a failed series performs.
                var due = await Eligible(_context.PodcastSeries, RetryCutoff(result.StartedAt))
                    .AsNoTracking()
                    .OrderBy(p => p.LastEnrichmentAttemptAt.HasValue)
                    .ThenBy(p => p.LastEnrichmentAttemptAt)
                    .ThenBy(p => p.DateAdded)
                    .Take(size)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                if (due.Count == 0)
                {
                    _logger.LogInformation("No podcast series are waiting to be filled from their feed");
                    return Finish(result);
                }

                _logger.LogInformation("Filling {Count} podcast series from their feeds", due.Count);

                for (var i = 0; i < due.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    await FillByIdAsync(due[i], force: false, result, cancellationToken);

                    if (delayMs > 0 && i < due.Count - 1)
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }

                _logger.LogInformation(
                    "Podcast feed-fill complete. Filled: {Enriched}, unchanged: {Unchanged}, no feed found: {NotFound}, failed: {Failed}",
                    result.EnrichedCount, result.UnchangedCount, result.NotFoundCount, result.FailedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                _logger.LogInformation("Podcast feed-fill was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Podcast feed-fill run failed");
                result.Success = false;
                result.ErrorMessage = "Podcast enrichment failed.";
            }

            return await FinishWithPendingAsync(result, cancellationToken);
        }

        public async Task<PodcastEnrichmentResult> EnrichSeriesAsync(
            Guid seriesId,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            var result = new PodcastEnrichmentResult { StartedAt = DateTime.UtcNow, TotalProcessed = 1 };

            var series = await TrackedSeriesWithGenres.FirstOrDefaultAsync(p => p.Id == seriesId, cancellationToken)
                ?? throw new KeyNotFoundException($"Podcast series with ID {seriesId} not found.");

            if (!force && series.EnrichedAt != null)
            {
                result.SkippedCount = 1;
                result.WarningMessage = "This series has already been filled from its feed. Use force to fill it again.";
                return await FinishWithPendingAsync(result, cancellationToken);
            }

            // A forced re-fill is the tool for a series whose stored data is wrong, so the feed
            // overwrites rather than only filling gaps.
            await FillAsync(series, force, result, cancellationToken);

            return await FinishWithPendingAsync(result, cancellationToken);
        }

        /// <summary>
        /// Loads one series and fills it, then leaves the change tracker clean for the next one. A
        /// series deleted since the run started is simply skipped.
        /// </summary>
        private async Task FillByIdAsync(
            Guid seriesId, bool force, PodcastEnrichmentResult result, CancellationToken cancellationToken)
        {
            try
            {
                var series = await TrackedSeriesWithGenres
                    .FirstOrDefaultAsync(p => p.Id == seriesId, cancellationToken);
                if (series == null)
                {
                    return;
                }

                result.TotalProcessed++;
                await FillAsync(series, force, result, cancellationToken);
            }
            finally
            {
                _context.ClearChangeTracker();
            }
        }

        /// <summary>
        /// One series, one save. Records the attempt even when nothing could be filled, so the retry
        /// window applies to shows whose feed is down.
        /// </summary>
        private async Task FillAsync(
            PodcastSeries series, bool force, PodcastEnrichmentResult result, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            series.LastEnrichmentAttemptAt = now;
            var before = Snapshot(series);

            DirectoryPodcast? directoryHit = null;
            try
            {
                if (!UrlNormalizer.IsValid(series.RssFeedUrl))
                {
                    directoryHit = await ResolveThroughDirectoryAsync(series, cancellationToken);
                    ApplyResolvedIdentity(series, directoryHit);
                }

                if (!UrlNormalizer.IsValid(series.RssFeedUrl))
                {
                    // Nothing to read. Directory details are still worth keeping, and the series stays
                    // eligible so a later run can try again once it has a feed URL.
                    await AddGenresAsync(series, PodcastFeedMapper.ApplyDirectory(series, directoryHit));
                    if (directoryHit != null && series.MetadataSource == PodcastMetadataSources.Manual)
                    {
                        series.MetadataSource = SourceFor(directoryHit);
                    }

                    result.NotFoundCount++;
                    _logger.LogDebug("No feed could be found for podcast series {Title} ({Id})", series.Title, series.Id);
                    await SaveAsync(series, result, cancellationToken);
                    return;
                }

                var feed = await _feedReader.ReadAsync(series.RssFeedUrl!, maxItems: 0, cancellationToken);

                var genres = PodcastFeedMapper.ApplyToSeries(series, feed, directoryHit, fillOnly: !force);
                var genresAdded = await AddGenresAsync(series, genres);
                series.MetadataSource = PodcastMetadataSources.Rss;

                // The feed answered, so the series is filled even when it had nothing new to say.
                // Leaving EnrichedAt null would put the row back in the queue forever.
                series.EnrichedAt = now;

                if (Snapshot(series) != before || genresAdded > 0)
                {
                    result.EnrichedCount++;
                    _logger.LogDebug("Filled podcast series {Title} ({Id}) from its feed", series.Title, series.Id);
                }
                else
                {
                    result.UnchangedCount++;
                }

                await SaveAsync(series, result, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (PodcastFeedException ex)
            {
                // The feed host being unreachable is the remote side's problem, not an application error.
                _logger.LogWarning("Could not read the feed for podcast series {Title} ({Id}): {Reason}",
                    series.Title, series.Id, ex.Reason);
                await RecordFailureAsync(series, result, ex.Message, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "The podcast directory could not be reached while filling {Title} ({Id})",
                    series.Title, series.Id);
                await RecordFailureAsync(series, result, "The podcast directory could not be reached.", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fill podcast series {Title} ({Id})", series.Title, series.Id);
                await RecordFailureAsync(series, result, "Could not be filled from its feed.", cancellationToken);
            }
        }

        private async Task RecordFailureAsync(
            PodcastSeries series, PodcastEnrichmentResult result, string reason, CancellationToken cancellationToken)
        {
            result.FailedCount++;
            result.Errors.Add($"{series.Title}: {reason}");

            // Drop whatever this series put in the tracker, then stamp the attempt on its own, so the
            // retry window still applies to a series whose fill failed halfway through.
            _context.ClearChangeTracker();
            try
            {
                var stamped = await _context.PodcastSeries.FirstOrDefaultAsync(p => p.Id == series.Id, cancellationToken);
                if (stamped != null)
                {
                    stamped.LastEnrichmentAttemptAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not record the enrichment attempt for {Id}", series.Id);
                _context.ClearChangeTracker();
            }
        }

        private async Task SaveAsync(
            PodcastSeries series, PodcastEnrichmentResult result, CancellationToken cancellationToken)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Could not save the filled podcast series {Id}", series.Id);
                _context.ClearChangeTracker();
                result.FailedCount++;
                result.Errors.Add($"{series.Title}: could not be saved.");

                if (result.EnrichedCount > 0)
                {
                    result.EnrichedCount--;
                }
            }
        }

        /// <summary>
        /// Finds the show in the directory: by its Apple id when it has one, otherwise by title. Only an
        /// unambiguous title match is accepted — attaching the wrong feed to a series is worse than
        /// leaving it unfilled.
        /// </summary>
        private async Task<DirectoryPodcast?> ResolveThroughDirectoryAsync(
            PodcastSeries series, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(series.ApplePodcastsId))
            {
                return await _directory.LookupByAppleIdAsync(series.ApplePodcastsId, cancellationToken);
            }

            if (PodcastFeedMapper.IsPlaceholderTitle(series.Title))
            {
                return null;
            }

            var results = await _directory.SearchAsync(
                series.Title.Trim(), Math.Max(1, _options.DirectorySearchLimit), cancellationToken);

            return BestMatch(results, series);
        }

        internal static DirectoryPodcast? BestMatch(IReadOnlyList<DirectoryPodcast> results, PodcastSeries series)
        {
            var sameTitle = results
                .Where(r => string.Equals(r.Title?.Trim(), series.Title.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sameTitle.Count <= 1)
            {
                return sameTitle.FirstOrDefault();
            }

            // Several shows share the title; the publisher has to break the tie.
            if (string.IsNullOrWhiteSpace(series.Publisher))
            {
                return null;
            }

            var samePublisher = sameTitle
                .Where(r => string.Equals(r.Publisher?.Trim(), series.Publisher.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            return samePublisher.Count == 1 ? samePublisher[0] : null;
        }

        private static void ApplyResolvedIdentity(PodcastSeries series, DirectoryPodcast? directoryHit)
        {
            if (directoryHit == null)
            {
                return;
            }

            if (UrlNormalizer.IsValid(directoryHit.FeedUrl))
            {
                series.RssFeedUrl = directoryHit.FeedUrl!.Trim();
                series.FeedUrlKey = UrlNormalizer.GetComparisonKey(series.RssFeedUrl);
            }

            if (string.IsNullOrWhiteSpace(series.ApplePodcastsId) && !string.IsNullOrWhiteSpace(directoryHit.ApplePodcastsId))
            {
                series.ApplePodcastsId = directoryHit.ApplePodcastsId;
            }

            series.PodcastIndexId ??= directoryHit.PodcastIndexId;
        }

        private static string SourceFor(DirectoryPodcast directoryHit) =>
            directoryHit.Source == DirectoryPodcast.PodcastIndexSource
                ? PodcastMetadataSources.PodcastIndex
                : PodcastMetadataSources.Apple;

        private async Task<int> AddGenresAsync(PodcastSeries series, IEnumerable<string> genreNames)
        {
            var resolver = new GenreResolver(_context);
            var added = 0;

            foreach (var name in genreNames)
            {
                var genre = await resolver.GetOrCreateAsync(name);
                if (genre != null && series.Genres.All(g => g.Name != genre.Name))
                {
                    series.Genres.Add(genre);
                    added++;
                }
            }

            return added;
        }

        /// <summary>The fields a fill can write, so "did anything change?" is one comparison.</summary>
        private static (string?, string?, string?, string?, string?, string?, string?, int, string?, string?, long?, string) Snapshot(
            PodcastSeries series) =>
            (series.Title, series.Description, series.Publisher, series.Thumbnail, series.Link, series.Language,
                series.FeedGuid, series.TotalEpisodes, series.RssFeedUrl, series.ApplePodcastsId,
                series.PodcastIndexId, series.MetadataSource);

        private static PodcastEnrichmentResult Finish(PodcastEnrichmentResult result)
        {
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        private async Task<PodcastEnrichmentResult> FinishWithPendingAsync(
            PodcastEnrichmentResult result, CancellationToken cancellationToken)
        {
            try
            {
                result.PendingCount = await Eligible(_context.PodcastSeries, RetryCutoff(DateTime.UtcNow))
                    .CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not count the podcast series still waiting to be filled");
            }

            result.WarningMessage = BuildWarning(result);
            return Finish(result);
        }

        private static string? BuildWarning(PodcastEnrichmentResult result)
        {
            var sentences = new List<string>();
            if (result.WarningMessage != null)
            {
                sentences.Add(result.WarningMessage);
            }
            if (result.FailedCount > 0)
            {
                sentences.Add($"{result.FailedCount} series could not be filled and will be retried later.");
            }
            if (result.NotFoundCount > 0)
            {
                sentences.Add($"{result.NotFoundCount} series have no feed URL the directory could resolve.");
            }
            if (result.WasCancelled)
            {
                sentences.Add("The run was cancelled.");
            }

            return sentences.Count == 0 ? null : string.Join(" ", sentences);
        }
    }
}
