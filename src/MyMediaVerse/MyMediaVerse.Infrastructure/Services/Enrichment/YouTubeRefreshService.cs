using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Refreshes stored YouTube channels, playlists and videos from the YouTube Data API.
    /// What a refresh may change is decided by <see cref="YouTubeMetadataApplier"/>.
    /// </summary>
    public class YouTubeRefreshService : IYouTubeRefreshService
    {
        // YouTube accepts at most 50 ids per videos request.
        public const int VideoBatchSize = 50;

        private const string YouTubePlatformLower = "youtube";

        private readonly IApplicationDbContext _context;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly ILogger<YouTubeRefreshService> _logger;

        public YouTubeRefreshService(
            IApplicationDbContext context,
            IYouTubeApiClient youTubeApiClient,
            ILogger<YouTubeRefreshService> logger)
        {
            _context = context;
            _youTubeApiClient = youTubeApiClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<YouTubeRefreshResultDto> RefreshStaleAsync(
            int limit = 200,
            int olderThanDays = 30,
            CancellationToken cancellationToken = default)
        {
            var result = new YouTubeRefreshResultDto { StartedAt = DateTime.UtcNow };

            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

                // The limit is shared: channels take from it first, then playlists, then videos.
                // Never-refreshed rows sort first explicitly: PostgreSQL puts NULLs last by default.
                var channels = await StaleChannels(cutoff)
                    .OrderBy(c => c.LastSyncedAt != null)
                    .ThenBy(c => c.LastSyncedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);
                var playlists = await StalePlaylists(cutoff)
                    .OrderBy(p => p.LastSyncedAt != null)
                    .ThenBy(p => p.LastSyncedAt)
                    .Take(limit - channels.Count)
                    .ToListAsync(cancellationToken);
                var videos = await StaleVideos(cutoff)
                    .OrderBy(v => v.YouTubeRefreshedAt != null)
                    .ThenBy(v => v.YouTubeRefreshedAt)
                    .Take(limit - channels.Count - playlists.Count)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Starting YouTube refresh for {Channels} channels, {Playlists} playlists and {Videos} videos older than {Days} days",
                    channels.Count, playlists.Count, videos.Count, olderThanDays);

                var keepGoing = await RefreshChannelsAsync(channels, result, cancellationToken);
                keepGoing = keepGoing && await RefreshPlaylistsAsync(playlists, result, cancellationToken);
                if (keepGoing)
                {
                    await RefreshVideosAsync(videos, result, cancellationToken);
                }

                // Whatever was refreshed before a stop is still saved.
                await _context.SaveChangesAsync(CancellationToken.None);

                result.RemainingCount =
                    await StaleChannels(cutoff).CountAsync(CancellationToken.None)
                    + await StalePlaylists(cutoff).CountAsync(CancellationToken.None)
                    + await StaleVideos(cutoff).CountAsync(CancellationToken.None);
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "YouTube refresh complete. Updated: {Updated}, unchanged: {Unchanged}, skipped: {Skipped}, failed: {Failed}, remaining: {Remaining}, quota exceeded: {QuotaExceeded}",
                    result.UpdatedCount, result.UnchangedCount, result.SkippedCount, result.FailedCount,
                    result.RemainingCount, result.QuotaExceeded);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"YouTube refresh run failed: {ex.Message}";
                _logger.LogError(ex, "YouTube refresh run failed");
            }

            return result;
        }

        private IQueryable<YouTubeChannel> StaleChannels(DateTime cutoff) => _context.YouTubeChannels
            .Where(c => c.ChannelExternalId != "")
            .Where(c => c.LastSyncedAt == null || c.LastSyncedAt < cutoff);

        private IQueryable<YouTubePlaylist> StalePlaylists(DateTime cutoff) => _context.YouTubePlaylists
            .Where(p => p.PlaylistExternalId != "")
            .Where(p => p.LastSyncedAt == null || p.LastSyncedAt < cutoff);

        private IQueryable<Video> StaleVideos(DateTime cutoff) => _context.Videos
            .Where(v => v.Platform.ToLower() == YouTubePlatformLower)
            .Where(v => v.ExternalId != null && v.ExternalId != "")
            .Where(v => v.YouTubeRefreshedAt == null || v.YouTubeRefreshedAt < cutoff);

        // Each of the three loops returns false when the run has to stop (canceled, or the
        // daily quota is used up), so the kinds after it are not started.

        private async Task<bool> RefreshChannelsAsync(
            List<YouTubeChannel> channels, YouTubeRefreshResultDto result, CancellationToken cancellationToken)
        {
            foreach (var channel in channels)
            {
                if (StopIfCancelled(result, cancellationToken)) return false;

                try
                {
                    var youTube = await _youTubeApiClient.GetChannelDetailsAsync(channel.ChannelExternalId);
                    if (youTube == null)
                    {
                        Skip(result, "channel", channel.Title, channel.ChannelExternalId);
                    }
                    else
                    {
                        Count(result, YouTubeMetadataApplier.Apply(channel, youTube));
                    }

                    channel.LastSyncedAt = DateTime.UtcNow;
                }
                catch (YouTubeQuotaExceededException)
                {
                    StopForQuota(result);
                    return false;
                }
                catch (Exception ex) when (ex is not YouTubeNotConfiguredException)
                {
                    Fail(result, ex, "channel", channel.Title, channel.ChannelExternalId);
                }

                result.ChannelsProcessed++;
            }

            return true;
        }

        private async Task<bool> RefreshPlaylistsAsync(
            List<YouTubePlaylist> playlists, YouTubeRefreshResultDto result, CancellationToken cancellationToken)
        {
            foreach (var playlist in playlists)
            {
                if (StopIfCancelled(result, cancellationToken)) return false;

                try
                {
                    var youTube = await _youTubeApiClient.GetPlaylistDetailsAsync(playlist.PlaylistExternalId);
                    if (youTube == null)
                    {
                        Skip(result, "playlist", playlist.Title, playlist.PlaylistExternalId);
                    }
                    else
                    {
                        Count(result, YouTubeMetadataApplier.Apply(playlist, youTube));
                    }

                    playlist.LastSyncedAt = DateTime.UtcNow;
                }
                catch (YouTubeQuotaExceededException)
                {
                    StopForQuota(result);
                    return false;
                }
                catch (Exception ex) when (ex is not YouTubeNotConfiguredException)
                {
                    Fail(result, ex, "playlist", playlist.Title, playlist.PlaylistExternalId);
                }

                result.PlaylistsProcessed++;
            }

            return true;
        }

        private async Task<bool> RefreshVideosAsync(
            List<Video> videos, YouTubeRefreshResultDto result, CancellationToken cancellationToken)
        {
            if (videos.Count == 0) return true;

            // Only videos without a channel can gain one, and only from channels already stored.
            var storedChannelIds = videos.Any(v => v.ChannelId == null)
                ? await LoadStoredChannelIdsAsync(cancellationToken)
                : new Dictionary<string, Guid>();

            foreach (var batch in videos.Chunk(VideoBatchSize))
            {
                if (StopIfCancelled(result, cancellationToken)) return false;

                List<YouTubeVideoDto> found;
                try
                {
                    found = await _youTubeApiClient.GetVideosAsync(batch.Select(v => v.ExternalId!).ToList());
                }
                catch (YouTubeQuotaExceededException)
                {
                    StopForQuota(result);
                    return false;
                }
                catch (Exception ex) when (ex is not YouTubeNotConfiguredException)
                {
                    result.FailedCount += batch.Length;
                    result.VideosProcessed += batch.Length;
                    result.AddError(
                        $"Failed to refresh a batch of {batch.Length} videos starting with '{batch[0].Title}': {ex.Message}");
                    _logger.LogWarning(ex, "Failed to refresh a batch of {Count} videos", batch.Length);
                    continue;
                }

                // YouTube leaves out what it no longer has and promises no order, so answers
                // are matched to stored videos by id.
                var byId = new Dictionary<string, YouTubeVideoDto>(StringComparer.Ordinal);
                foreach (var youTube in found.Where(v => !string.IsNullOrEmpty(v.Id)))
                {
                    byId.TryAdd(youTube.Id!, youTube);
                }

                foreach (var video in batch)
                {
                    if (byId.TryGetValue(video.ExternalId!, out var youTube))
                    {
                        var channelExternalId = youTube.Snippet?.ChannelId;
                        Guid? storedChannelId =
                            channelExternalId != null && storedChannelIds.TryGetValue(channelExternalId, out var channelId)
                                ? channelId
                                : null;

                        Count(result, YouTubeMetadataApplier.Apply(video, youTube, storedChannelId));
                    }
                    else
                    {
                        Skip(result, "video", video.Title, video.ExternalId!);
                    }

                    video.YouTubeRefreshedAt = DateTime.UtcNow;
                    result.VideosProcessed++;
                }
            }

            return true;
        }

        private async Task<Dictionary<string, Guid>> LoadStoredChannelIdsAsync(CancellationToken cancellationToken)
        {
            var stored = await _context.YouTubeChannels
                .Where(c => c.ChannelExternalId != "")
                .Select(c => new { c.ChannelExternalId, c.Id })
                .ToListAsync(cancellationToken);

            var byExternalId = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var channel in stored)
            {
                byExternalId.TryAdd(channel.ChannelExternalId, channel.Id);
            }

            return byExternalId;
        }

        private static void Count(YouTubeRefreshResultDto result, bool changed)
        {
            if (changed) result.UpdatedCount++;
            else result.UnchangedCount++;
        }

        // The item is kept and still stamped by the caller: left unstamped it would stay at the
        // front of every run and crowd out items that can be refreshed.
        private void Skip(YouTubeRefreshResultDto result, string kind, string title, string externalId)
        {
            result.SkippedCount++;
            result.Warnings.Add(
                $"YouTube no longer returns the {kind} '{title}' (id {externalId}); it was deleted or made private. The stored {kind} was kept.");
            _logger.LogWarning("YouTube no longer returns the {Kind} {Title} ({ExternalId})", kind, title, externalId);
        }

        private void Fail(YouTubeRefreshResultDto result, Exception ex, string kind, string title, string externalId)
        {
            result.FailedCount++;
            result.AddError($"Failed to refresh the {kind} '{title}': {ex.Message}");
            _logger.LogWarning(ex, "Failed to refresh the {Kind} {Title} ({ExternalId})", kind, title, externalId);
        }

        private void StopForQuota(YouTubeRefreshResultDto result)
        {
            result.QuotaExceeded = true;
            result.Warnings.Add(
                "YouTube's daily quota ran out before every stale item was refreshed. What was refreshed so far is saved; run again after the quota resets.");
            _logger.LogWarning("YouTube refresh stopped: the daily quota is used up");
        }

        private static bool StopIfCancelled(YouTubeRefreshResultDto result, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested) return false;

            result.Warnings.Add("The run was canceled before every stale item was refreshed.");
            return true;
        }
    }
}
