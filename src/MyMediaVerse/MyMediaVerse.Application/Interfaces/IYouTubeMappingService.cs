using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IYouTubeMappingService
    {
        Video MapVideoToEntity(YouTubeVideoDto videoDto);
        YouTubeChannel MapChannelToYouTubeChannelEntity(YouTubeChannelDto channelDto);
        YouTubePlaylist MapPlaylistToYouTubePlaylistEntity(YouTubePlaylistDto playlistDto);
        Video MapPlaylistItemToVideoEntity(YouTubePlaylistItemDto playlistItemDto, YouTubeVideoDto? videoDetails = null);
        List<Video> MapPlaylistItemsToVideoEntities(List<YouTubePlaylistItemDto> playlistItems, List<YouTubeVideoDto>? videoDetails = null);
    }
}
