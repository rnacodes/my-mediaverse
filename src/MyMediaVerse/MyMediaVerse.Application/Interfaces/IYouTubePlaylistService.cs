using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Interfaces
{
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
        /// <returns>The imported YouTubePlaylist entity</returns>
        Task<YouTubePlaylist> ImportPlaylistFromYouTubeAsync(string playlistExternalId);
        
        /// <summary>
        /// Add a video to a playlist
        /// </summary>
        Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid videoId, int? position = null);
        
        /// <summary>
        /// Remove a video from a playlist
        /// </summary>
        Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid videoId);
        
        /// <summary>
        /// Sync playlist videos from YouTube API (add/remove videos to match YouTube)
        /// </summary>
        Task<YouTubePlaylist> SyncPlaylistVideosAsync(Guid playlistId);
        
        /// <summary>
        /// Save or update a YouTube playlist
        /// </summary>
        Task<YouTubePlaylist> SavePlaylistAsync(YouTubePlaylist playlist, bool updateIfExists = false);
        
        /// <summary>
        /// Delete a YouTube playlist
        /// </summary>
        Task<bool> DeletePlaylistAsync(Guid id);
    }
}

