using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Brings podcast episodes in from each series' RSS feed. Sync imports new episodes only: a first
    /// sync takes the newest episodes up to the per-sync limit, later syncs take episodes newer than the
    /// newest one stored, and the older back catalog is left for one-at-a-time import.
    /// </summary>
    public interface IPodcastEpisodeSyncService
    {
        /// <summary>
        /// Syncs one series. Returns a failed result (not an exception) when the feed cannot be read or
        /// the new episodes cannot be saved. Throws <see cref="KeyNotFoundException"/> when the series
        /// does not exist and <see cref="InvalidOperationException"/> when it has no feed URL.
        /// </summary>
        Task<PodcastEpisodeSyncResultDto> SyncSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Syncs every subscribed series that has a feed, least recently synced first. One series failing
        /// does not stop the run.
        /// </summary>
        Task<PodcastSyncAllResultDto> SyncSubscribedAsync(CancellationToken cancellationToken = default);
    }
}
