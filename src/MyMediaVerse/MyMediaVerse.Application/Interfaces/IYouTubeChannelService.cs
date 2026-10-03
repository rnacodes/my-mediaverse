using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// The outcome of importing a channel: the stored row, and whether this call created it
    /// or found it already in the library.
    /// </summary>
    public record YouTubeChannelCreationResult(YouTubeChannel Channel, bool Created);

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
        /// Import a YouTube channel from the YouTube API by channel ID. A channel already in
        /// the library is returned as it is, without calling YouTube.
        /// </summary>
        Task<YouTubeChannelCreationResult> ImportChannelFromYouTubeAsync(string channelId);
        
        /// <summary>
        /// Refresh the channel's metadata from YouTube (title, counts, thumbnail, ...) and report
        /// how many uploads the library does not hold yet.
        /// </summary>
        Task<YouTubeChannelSyncResultDto> SyncChannelMetadataAsync(Guid channelId);

        /// <summary>
        /// Import the channel's newest uploads: stored videos are linked to the channel when they
        /// have none, the rest are created under it. <paramref name="count"/> is clamped to one
        /// YouTube page. Nothing is saved when YouTube's quota runs out mid-run.
        /// </summary>
        Task<YouTubeChannelImportResultDto> ImportLatestUploadsAsync(Guid channelId, int count);
    }
}

