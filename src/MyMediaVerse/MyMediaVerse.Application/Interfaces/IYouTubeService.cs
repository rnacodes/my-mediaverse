using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IYouTubeService
    {
        Task<YouTubeSearchResultDto> SearchAsync(string query, string type = "video", int maxResults = 25, string? pageToken = null, string? channelId = null);
        Task<YouTubeVideoDto?> GetVideoDetailsAsync(string videoId);
        Task<List<YouTubeVideoDto>> GetVideosAsync(List<string> videoIds);
        Task<YouTubePlaylistDto?> GetPlaylistDetailsAsync(string playlistId);
        Task<List<YouTubePlaylistItemDto>> GetPlaylistItemsAsync(string playlistId, int maxResults = 50, string? pageToken = null);
        Task<List<YouTubePlaylistItemDto>> GetAllPlaylistItemsAsync(string playlistId);
        Task<YouTubeChannelDto?> GetChannelDetailsAsync(string channelId);
        Task<YouTubeChannelDto?> GetChannelByUsernameAsync(string username);
        Task<YouTubeChannelDto?> GetChannelByHandleAsync(string handle);
        Task<List<YouTubePlaylistItemDto>> GetChannelUploadsAsync(string channelId, int maxResults = 25, string? pageToken = null);
        
        // Import methods
        Task<Video> ImportVideoAsync(string videoId);
        Task<BaseMediaItem> ImportFromUrlAsync(string url);
    }
}
