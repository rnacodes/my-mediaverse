using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Keeps stored YouTube data current.
    /// </summary>
    public interface IYouTubeRefreshService
    {
        /// <summary>
        /// Refreshes stored channels, playlists and YouTube videos whose data has no refresh
        /// timestamp or one older than <paramref name="olderThanDays"/>. Channels run first, then
        /// playlists, then videos, each oldest first. Values YouTube owns are overwritten; status,
        /// rating, notes, topics, genres, dates, links and mixlists are never touched. An item
        /// YouTube no longer returns is kept and reported as skipped.
        /// </summary>
        /// <param name="limit">Maximum items to refresh in this run, across all three kinds (default: 200)</param>
        /// <param name="olderThanDays">Age at which stored YouTube data counts as stale (default: 30)</param>
        Task<YouTubeRefreshResultDto> RefreshStaleAsync(
            int limit = 200,
            int olderThanDays = 30,
            CancellationToken cancellationToken = default);
    }
}
