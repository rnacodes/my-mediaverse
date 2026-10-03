using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Shared.Interfaces
{
    public interface IYouTubeApiClient
    {
        Task<YouTubeSearchResultDto> SearchAsync(string query, string type = "video", int maxResults = 25, string? pageToken = null, string? channelId = null);
        Task<YouTubeVideoDto?> GetVideoDetailsAsync(string videoId);
        Task<List<YouTubeVideoDto>> GetVideosAsync(List<string> videoIds);
        Task<YouTubePlaylistDto?> GetPlaylistDetailsAsync(string playlistId);
        /// <summary>One page of a playlist's items, with the token for the next page.</summary>
        Task<YouTubePlaylistItemListResponseDto> GetPlaylistItemsAsync(string playlistId, int maxResults = 50, string? pageToken = null);
        Task<List<YouTubePlaylistItemDto>> GetAllPlaylistItemsAsync(string playlistId);
        Task<YouTubeChannelDto?> GetChannelDetailsAsync(string channelId);
        Task<YouTubeChannelDto?> GetChannelByUsernameAsync(string username);
        Task<YouTubeChannelDto?> GetChannelByHandleAsync(string handle);
        /// <summary>One page of a channel's uploads (newest first), with the token for the next page.</summary>
        Task<YouTubePlaylistItemListResponseDto> GetChannelUploadsAsync(string channelId, int maxResults = 25, string? pageToken = null);
    }
}
