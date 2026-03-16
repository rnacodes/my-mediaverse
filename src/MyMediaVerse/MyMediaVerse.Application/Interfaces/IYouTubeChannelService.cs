using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for YouTube Channel operations
    /// </summary>
    public interface IYouTubeChannelService
    {
        /// <summary>
        /// Get all YouTube channels
        /// </summary>
        Task<IEnumerable<YouTubeChannel>> GetAllChannelsAsync();
        
        /// <summary>
        /// Get a YouTube channel by its database ID
        /// </summary>
        Task<YouTubeChannel?> GetChannelByIdAsync(Guid id);
        
        /// <summary>
        /// Get a YouTube channel by its external YouTube ID
        /// </summary>
        Task<YouTubeChannel?> GetChannelByExternalIdAsync(string externalId);
        
        /// <summary>
        /// Get all videos associated with a specific channel
        /// </summary>
        Task<List<Video>> GetChannelVideosAsync(Guid channelId);
        
        /// <summary>
        /// Create a new YouTube channel
        /// </summary>
        Task<YouTubeChannel> CreateChannelAsync(CreateYouTubeChannelDto dto);
        
        /// <summary>
        /// Update an existing YouTube channel
        /// </summary>
        Task<YouTubeChannel> UpdateChannelAsync(Guid id, UpdateYouTubeChannelDto dto);
        
        /// <summary>
        /// Delete a YouTube channel (videos remain but ChannelId becomes null)
        /// </summary>
        Task<bool> DeleteChannelAsync(Guid id);
        
        /// <summary>
        /// Import a YouTube channel from the YouTube API by channel ID
        /// </summary>
        Task<YouTubeChannel> ImportChannelFromYouTubeAsync(string channelId);
        
        /// <summary>
        /// Sync channel metadata from YouTube API (update subscriber count, video count, etc.)
        /// </summary>
        Task<YouTubeChannel> SyncChannelMetadataAsync(Guid channelId);
        
        /// <summary>
        /// Check if a channel exists by external ID
        /// </summary>
        Task<bool> ChannelExistsAsync(string externalId);
    }
}

