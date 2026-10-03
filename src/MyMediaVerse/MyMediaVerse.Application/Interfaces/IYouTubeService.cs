using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// The outcome of a YouTube import: the stored video, channel, or playlist, whether this
    /// call created it, and the channel a video import brought in along the way (null when
    /// the channel was already stored or the import was not a video).
    /// </summary>
    public record YouTubeImportResult(BaseMediaItem Item, bool Created, YouTubeChannel? AutoImportedChannel = null);

    public interface IYouTubeService
    {
        Task<YouTubeSearchResultDto> SearchAsync(string query, string type = "video", int maxResults = 25, string? pageToken = null, string? channelId = null);
        Task<YouTubeVideoDto?> GetVideoDetailsAsync(string videoId);
        Task<List<YouTubeVideoDto>> GetVideosAsync(List<string> videoIds);
        Task<YouTubePlaylistDto?> GetPlaylistDetailsAsync(string playlistId);
        Task<YouTubePlaylistItemListResponseDto> GetPlaylistItemsAsync(string playlistId, int maxResults = 50, string? pageToken = null);
        Task<List<YouTubePlaylistItemDto>> GetAllPlaylistItemsAsync(string playlistId);
        Task<YouTubeChannelDto?> GetChannelDetailsAsync(string channelId);
        Task<YouTubeChannelDto?> GetChannelByUsernameAsync(string username);
        Task<YouTubeChannelDto?> GetChannelByHandleAsync(string handle);
        Task<YouTubePlaylistItemListResponseDto> GetChannelUploadsAsync(string channelId, int maxResults = 25, string? pageToken = null);
        
        // Import methods
        Task<YouTubeImportResult> ImportVideoAsync(string videoId);
        Task<YouTubeImportResult> ImportFromUrlAsync(string url);
    }
}
