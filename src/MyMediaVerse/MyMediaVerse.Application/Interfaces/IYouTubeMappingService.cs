using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IYouTubeMappingService
    {
        Video MapVideoToEntity(YouTubeVideoDto videoDto);
        Video MapPlaylistToEntity(YouTubePlaylistDto playlistDto); // Deprecated - use MapPlaylistToYouTubePlaylistEntity
        Video MapChannelToEntity(YouTubeChannelDto channelDto); // Deprecated - use MapChannelToYouTubeChannelEntity
        YouTubeChannel MapChannelToYouTubeChannelEntity(YouTubeChannelDto channelDto);
        YouTubePlaylist MapPlaylistToYouTubePlaylistEntity(YouTubePlaylistDto playlistDto);
        Video MapPlaylistItemToVideoEntity(YouTubePlaylistItemDto playlistItemDto, YouTubeVideoDto? videoDetails = null);
        List<Video> MapPlaylistItemsToVideoEntities(List<YouTubePlaylistItemDto> playlistItems, List<YouTubeVideoDto>? videoDetails = null);
    }
}
