using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// The outcome of importing a playlist: the stored row, and whether this call created it
    /// or found it already in the library.
    /// </summary>
    public record YouTubePlaylistCreationResult(YouTubePlaylist Playlist, bool Created);

    /// <summary>
    /// The outcome of syncing a playlist with YouTube: the stored row as it stands afterwards,
    /// and what the sync changed. <paramref name="VideosCreated"/> counts videos that were new
    /// to the library, <paramref name="VideosLinked"/> videos already in the library that joined
    /// the playlist, <paramref name="VideosUnlinked"/> videos that left it (their rows stay), and
    /// <paramref name="PositionsUpdated"/> videos whose place in the playlist moved.
    /// </summary>
    public record YouTubePlaylistSyncResult(
        YouTubePlaylist Playlist,
        int VideosCreated,
        int VideosLinked,
        int VideosUnlinked,
        int PositionsUpdated);

    public interface IYouTubePlaylistService
    {
        /// <summary>
        /// Get a YouTube playlist by its internal ID
        /// </summary>
        Task<YouTubePlaylist?> GetPlaylistByIdAsync(Guid id, bool includeVideos = false);
        
        /// <summary>
        /// Get a YouTube playlist by its external YouTube playlist ID
        /// </summary>
        Task<YouTubePlaylist?> GetPlaylistByExternalIdAsync(string externalId, bool includeVideos = false);
        
        /// <summary>
        /// Get all YouTube playlists
        /// </summary>
        Task<List<YouTubePlaylist>> GetAllPlaylistsAsync();
        
        /// <summary>
        /// Get all videos in a playlist, ordered by position
        /// </summary>
        Task<List<Video>> GetPlaylistVideosAsync(Guid playlistId);
        
        /// <summary>
        /// Import a YouTube playlist from the YouTube API as a first-class YouTubePlaylist entity
        /// </summary>
        /// <param name="playlistExternalId">YouTube playlist ID</param>
        /// <returns>The stored playlist, and whether this call created it</returns>
        Task<YouTubePlaylistCreationResult> ImportPlaylistFromYouTubeAsync(string playlistExternalId);
        
        /// <summary>
        /// Add a video to a playlist
        /// </summary>
        Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid videoId, int? position = null);
        
        /// <summary>
        /// Remove a video from a playlist
        /// </summary>
        Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid videoId);
        
        /// <summary>
        /// Makes the stored playlist mirror YouTube: refreshes the playlist's own details, adds
        /// the videos that joined it (reusing any already in the library), unlinks the ones that
        /// left without deleting them, and follows YouTube's order. Nothing is saved unless every
        /// YouTube request succeeds.
        /// </summary>
        Task<YouTubePlaylistSyncResult> SyncPlaylistVideosAsync(Guid playlistId);
        
        /// <summary>
        /// Save or update a YouTube playlist
        /// </summary>
        Task<YouTubePlaylist> SavePlaylistAsync(YouTubePlaylist playlist, bool updateIfExists = false);
        
        /// <summary>
        /// Delete a YouTube playlist. Its videos stay in the library.
        /// </summary>
        Task<bool> DeletePlaylistAsync(Guid id);
    }
}

