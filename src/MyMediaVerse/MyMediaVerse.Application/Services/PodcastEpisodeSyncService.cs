using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class PodcastEpisodeSyncService : IPodcastEpisodeSyncService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPodcastFeedReader _feedReader;
        private readonly ISyncStateService _syncState;
        private readonly PodcastSyncOptions _options;
        private readonly ILogger<PodcastEpisodeSyncService> _logger;

        public PodcastEpisodeSyncService(
            IApplicationDbContext context,
            IPodcastFeedReader feedReader,
            ISyncStateService syncState,
            IOptions<PodcastSyncOptions> options,
            ILogger<PodcastEpisodeSyncService> logger)
        {
            _context = context;
            _feedReader = feedReader;
            _syncState = syncState;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PodcastEpisodeSyncResultDto> SyncSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
        {
            var result = new PodcastEpisodeSyncResultDto { SeriesId = seriesId, StartedAt = DateTime.UtcNow };

            var series = await _context.PodcastSeries
                .Include(s => s.Topics)
                .Include(s => s.Genres)
                .FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken)
                ?? throw new KeyNotFoundException($"Podcast series with ID {seriesId} not found.");

            result.SeriesTitle = series.Title;

            if (!UrlNormalizer.IsValid(series.RssFeedUrl))
                throw new InvalidOperationException("This podcast series has no RSS feed URL to sync from.");

            PodcastFeed feed;
            try
            {
                feed = await _feedReader.ReadAsync(series.RssFeedUrl!, cancellationToken: cancellationToken);
            }
            catch (PodcastFeedException ex)
            {
                // A feed host being down is the remote side's problem, not an application error.
                _logger.LogWarning("Episode sync for {Title} ({Id}) could not read the feed ({Reason})",
                    series.Title, series.Id, ex.Reason);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }

            result.FeedItemCount = feed.TotalItemCount;

            var index = await PodcastEpisodeIdentityIndex.BuildAsync(_context.PodcastEpisodes, series.Id, cancellationToken);
            var newestStored = index.NewestReleaseDate;

            var candidates = new List<CreatePodcastEpisodeDto>();
            var guidFills = new Dictionary<Guid, string>();

            foreach (var item in feed.Episodes)
            {
                var dto = PodcastEpisodeFeedMapper.ToCreateDto(item, series);
                if (dto == null)
                {
                    result.IgnoredCount++;
                    continue;
                }

                var match = index.Find(dto.RssGuid, dto.AudioLink, dto.Title, dto.ReleaseDate);
                if (match != null)
                {
                    result.SkippedCount++;

                    // A stored episode (or an earlier duplicate in this feed) without a guid adopts the item's.
                    if (match.Id != Guid.Empty && match.RssGuid == null && dto.RssGuid != null
                        && !guidFills.ContainsKey(match.Id) && !index.GuidOwnedByOther(dto.RssGuid, match.Id))
                    {
                        guidFills[match.Id] = dto.RssGuid;
                        index.AssignGuid(match, dto.RssGuid);
                    }
                    continue;
                }

                // Registered with an empty id so a repeat of the same item later in the feed collapses onto it.
                index.Add(new PodcastEpisodeIdentityIndex.Entry(Guid.Empty, dto.RssGuid, dto.AudioLink, dto.Title, dto.ReleaseDate));
                candidates.Add(dto);
            }

            var toCreate = SelectEpisodesToCreate(candidates, newestStored, result);

            foreach (var dto in toCreate)
            {
                var episode = PodcastEpisodeFactory.Create(dto);
                foreach (var topic in series.Topics)
                    episode.Topics.Add(topic);
                foreach (var genre in series.Genres)
                    episode.Genres.Add(genre);
                _context.Add(episode);
            }

            if (guidFills.Count > 0)
            {
                var ids = guidFills.Keys.ToList();
                var stored = await _context.PodcastEpisodes.Where(e => ids.Contains(e.Id)).ToListAsync(cancellationToken);
                foreach (var episode in stored)
                    episode.RssGuid = guidFills[episode.Id];
                result.UpdatedCount = stored.Count;
            }

            var now = DateTime.UtcNow;
            series.LastSyncDate = now;
            if (feed.TotalItemCount > 0)
                series.TotalEpisodes = feed.TotalItemCount;
            await FillFeedGuidAsync(series, feed, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _context.ClearChangeTracker();
                _logger.LogError(ex, "Episode sync for {Title} ({Id}) could not save the new episodes", series.Title, series.Id);
                result.Success = false;
                result.CreatedCount = 0;
                result.UpdatedCount = 0;
                result.ErrorMessage = "Could not save the new episodes.";
                return result;
            }

            result.CreatedCount = toCreate.Count;
            result.LastSyncDate = now;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Episode sync for {Title} ({Id}): {Created} created, {Updated} updated, {Skipped} already stored, {Backlog} backlog",
                series.Title, series.Id, result.CreatedCount, result.UpdatedCount, result.SkippedCount, result.BacklogCount);

            return result;
        }

        /// <summary>
        /// A first sync (no stored episode has a release date) takes the newest FirstSyncEpisodeCount episodes.
        /// Later syncs take only episodes newer than the newest stored one; older or undated items are
        /// back catalog and left for one-at-a-time import.
        /// </summary>
        private List<CreatePodcastEpisodeDto> SelectEpisodesToCreate(
            List<CreatePodcastEpisodeDto> candidates, DateTime? newestStored, PodcastEpisodeSyncResultDto result)
        {
            var max = Math.Max(1, _options.MaxEpisodesPerSync);
            var newestFirst = candidates
                .OrderByDescending(c => c.ReleaseDate.HasValue)
                .ThenByDescending(c => c.ReleaseDate)
                .ToList();

            List<CreatePodcastEpisodeDto> selected;
            if (newestStored == null)
            {
                selected = newestFirst.Take(Math.Max(1, _options.FirstSyncEpisodeCount)).ToList();
            }
            else
            {
                var fresh = newestFirst
                    .Where(c => c.ReleaseDate.HasValue && DateTimeNormalizer.ToUtc(c.ReleaseDate.Value) > newestStored.Value)
                    .ToList();
                selected = fresh.Take(max).ToList();

                if (fresh.Count > max)
                {
                    result.WarningMessage =
                        $"{fresh.Count} new episodes were found, but one sync imports at most {max}. " +
                        $"The {fresh.Count - max} oldest of them were not imported; add them from the feed episode list.";
                }
            }

            result.BacklogCount = candidates.Count - selected.Count;
            return selected;
        }

        private async Task FillFeedGuidAsync(Domain.Entities.PodcastSeries series, PodcastFeed feed, CancellationToken cancellationToken)
        {
            var guid = feed.Series.PodcastGuid;
            if (!string.IsNullOrWhiteSpace(series.FeedGuid) || string.IsNullOrWhiteSpace(guid) || guid.Length > 100)
                return;

            if (!await _context.PodcastSeries.AnyAsync(s => s.Id != series.Id && s.FeedGuid == guid, cancellationToken))
                series.FeedGuid = guid;
        }

        public async Task<PodcastSyncAllResultDto> SyncSubscribedAsync(CancellationToken cancellationToken = default)
        {
            var result = new PodcastSyncAllResultDto { StartedAt = DateTime.UtcNow };

            List<(Guid Id, string Title, string FeedUrl)> due;
            try
            {
                due = (await _context.PodcastSeries
                        .AsNoTracking()
                        .Where(s => s.IsSubscribed && s.RssFeedUrl != null)
                        .OrderBy(s => s.LastSyncDate.HasValue)
                        .ThenBy(s => s.LastSyncDate)
                        .ThenBy(s => s.DateAdded)
                        .Select(s => new { s.Id, s.Title, s.RssFeedUrl })
                        .ToListAsync(cancellationToken))
                    .Select(s => (s.Id, s.Title, s.RssFeedUrl!))
                    .ToList();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                result.WasCancelled = true;
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Podcast sync-all could not load the subscribed series");
                result.Success = false;
                result.ErrorMessage = "Could not load the subscribed podcast series.";
                return result;
            }

            var deadline = _options.RunTimeBudgetSeconds > 0
                ? result.StartedAt.AddSeconds(_options.RunTimeBudgetSeconds)
                : (DateTime?)null;
            var lastReadByHost = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var truncatedSeries = 0;

            for (var i = 0; i < due.Count; i++)
            {
                var (id, title, feedUrl) = due[i];

                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    result.PendingSeriesCount = due.Count - i;
                    break;
                }

                if (deadline.HasValue && DateTime.UtcNow >= deadline.Value)
                {
                    result.TimeBudgetReached = true;
                    result.PendingSeriesCount = due.Count - i;
                    break;
                }

                var host = UrlNormalizer.ExtractDomain(feedUrl);
                try
                {
                    await WaitForHostAsync(host, lastReadByHost, cancellationToken);

                    var seriesResult = await SyncSeriesAsync(id, cancellationToken);
                    result.SeriesChecked++;
                    result.Series.Add(new PodcastSeriesSyncSummaryDto
                    {
                        SeriesId = id,
                        SeriesTitle = seriesResult.SeriesTitle,
                        Success = seriesResult.Success,
                        CreatedCount = seriesResult.CreatedCount,
                        WarningMessage = seriesResult.WarningMessage,
                        ErrorMessage = seriesResult.ErrorMessage
                    });

                    if (seriesResult.Success)
                    {
                        result.SeriesSucceeded++;
                        result.CreatedCount += seriesResult.CreatedCount;
                        result.UpdatedCount += seriesResult.UpdatedCount;
                        result.SkippedCount += seriesResult.SkippedCount;
                        if (seriesResult.WarningMessage != null)
                            truncatedSeries++;
                    }
                    else
                    {
                        RecordFailure(result, title, seriesResult.ErrorMessage ?? "Sync failed.");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    result.PendingSeriesCount = due.Count - i;
                    break;
                }
                catch (KeyNotFoundException)
                {
                    // Deleted after the run started; nothing to sync.
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Podcast sync-all failed for {Title} ({Id})", title, id);
                    result.SeriesChecked++;
                    result.Series.Add(new PodcastSeriesSyncSummaryDto
                    {
                        SeriesId = id, SeriesTitle = title, Success = false, ErrorMessage = "Sync failed."
                    });
                    RecordFailure(result, title, "Sync failed.");
                }
                finally
                {
                    if (!string.IsNullOrEmpty(host))
                        lastReadByHost[host] = DateTime.UtcNow;

                    // Each series is its own unit of work; do not carry tracked episodes into the next.
                    _context.ClearChangeTracker();
                }
            }

            result.WarningMessage = BuildWarning(result, truncatedSeries);

            if (!result.WasCancelled && !result.TimeBudgetReached && result.SeriesFailed == 0)
            {
                try
                {
                    await _syncState.MarkSyncSucceededAsync(PodcastSyncAllResultDto.SyncAllOperation, result.StartedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Podcast sync-all could not record its successful run");
                    result.WarningMessage = AppendSentence(result.WarningMessage, "The run finished but its sync record could not be saved.");
                }
            }

            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Podcast sync-all: {Checked} series checked, {Failed} failed, {Created} episodes created, {Pending} pending",
                result.SeriesChecked, result.SeriesFailed, result.CreatedCount, result.PendingSeriesCount);

            return result;
        }

        private async Task WaitForHostAsync(string host, Dictionary<string, DateTime> lastReadByHost, CancellationToken cancellationToken)
        {
            if (_options.HostDelayMs <= 0 || string.IsNullOrEmpty(host) || !lastReadByHost.TryGetValue(host, out var lastRead))
                return;

            var wait = TimeSpan.FromMilliseconds(_options.HostDelayMs) - (DateTime.UtcNow - lastRead);
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);
        }

        private static void RecordFailure(PodcastSyncAllResultDto result, string title, string reason)
        {
            result.SeriesFailed++;
            if (result.Errors.Count < PodcastSyncAllResultDto.MaxErrors)
                result.Errors.Add($"{title}: {reason}");
        }

        private static string? BuildWarning(PodcastSyncAllResultDto result, int truncatedSeries)
        {
            string? warning = null;
            if (result.SeriesFailed > 0)
                warning = AppendSentence(warning, $"{result.SeriesFailed} of {result.SeriesChecked} series could not be synced.");
            if (truncatedSeries > 0)
                warning = AppendSentence(warning, $"{truncatedSeries} series had more new episodes than one sync imports.");
            if (result.TimeBudgetReached)
                warning = AppendSentence(warning, $"The run stopped at its time limit; {result.PendingSeriesCount} series are left for the next run.");
            if (result.WasCancelled)
                warning = AppendSentence(warning, $"The run was cancelled; {result.PendingSeriesCount} series were not synced.");
            return warning;
        }

        private static string AppendSentence(string? existing, string sentence) =>
            string.IsNullOrEmpty(existing) ? sentence : $"{existing} {sentence}";
    }
}
