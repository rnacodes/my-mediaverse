using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Services
{
    public class YouTubeMappingService : IYouTubeMappingService
    {
        public Video MapVideoToEntity(YouTubeVideoDto videoDto)
        {
            if (videoDto?.Snippet == null)
                throw new ArgumentException("Video DTO or snippet cannot be null", nameof(videoDto));

            var video = new Video
            {
                Title = videoDto.Snippet.Title ?? "Unknown Title",
                Description = videoDto.Snippet.Description,
                Link = $"https://www.youtube.com/watch?v={videoDto.Id}",
                Platform = "YouTube",
                ExternalId = videoDto.Id,
                MediaType = MediaType.Video,
                VideoType = VideoType.Episode, // Individual videos are episodes
                Thumbnail = GetBestThumbnailUrl(videoDto.Snippet.Thumbnails),
                LengthInSeconds = YouTubeHelper.ParseDurationToSeconds(videoDto.ContentDetails?.Duration),
                DateAdded = DateTime.UtcNow
            };

            return video;
        }

        public Video MapPlaylistToEntity(YouTubePlaylistDto playlistDto)
        {
            if (playlistDto?.Snippet == null)
                throw new ArgumentException("Playlist DTO or snippet cannot be null", nameof(playlistDto));

            var playlist = new Video
            {
                Title = playlistDto.Snippet.Title ?? "Unknown Playlist",
                Description = playlistDto.Snippet.Description,
                Link = $"https://www.youtube.com/playlist?list={playlistDto.Id}",
                Platform = "YouTube",
                ExternalId = playlistDto.Id,
                MediaType = MediaType.Video,
                VideoType = VideoType.Series, // Playlists are series
                Thumbnail = GetBestThumbnailUrl(playlistDto.Snippet.Thumbnails),
                DateAdded = DateTime.UtcNow
            };

            return playlist;
        }

        public Video MapChannelToEntity(YouTubeChannelDto channelDto)
        {
            // NOTE: This method is deprecated and will be removed in a future version
            // Use MapChannelToYouTubeChannelEntity instead
            if (channelDto?.Snippet == null)
                throw new ArgumentException("Channel DTO or snippet cannot be null", nameof(channelDto));

            var channel = new Video
            {
                Title = channelDto.Snippet.Title ?? "Unknown Channel",
                Description = channelDto.Snippet.Description,
                Link = $"https://www.youtube.com/channel/{channelDto.Id}",
                Platform = "YouTube",
                ExternalId = channelDto.Id,
                MediaType = MediaType.Video,
                VideoType = VideoType.Channel, // Channels are channels
                Thumbnail = GetBestThumbnailUrl(channelDto.Snippet.Thumbnails),
                DateAdded = DateTime.UtcNow
            };

            return channel;
        }

        public YouTubeChannel MapChannelToYouTubeChannelEntity(YouTubeChannelDto channelDto)
        {
            if (channelDto?.Snippet == null)
                throw new ArgumentException("Channel DTO or snippet cannot be null", nameof(channelDto));

            var channel = new YouTubeChannel
            {
                Title = channelDto.Snippet.Title ?? "Unknown Channel",
                Description = channelDto.Snippet.Description,
                Link = $"https://www.youtube.com/channel/{channelDto.Id}",
                ChannelExternalId = channelDto.Id ?? throw new ArgumentException("Channel ID cannot be null"),
                CustomUrl = channelDto.Snippet.CustomUrl,
                MediaType = MediaType.Channel,
                Thumbnail = GetBestThumbnailUrl(channelDto.Snippet.Thumbnails),
                Country = channelDto.Snippet.Country,
                PublishedAt = channelDto.Snippet.PublishedAt,
                DateAdded = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            // Add statistics if available
            if (channelDto.Statistics != null)
            {
                if (long.TryParse(channelDto.Statistics.SubscriberCount, out var subscriberCount))
                    channel.SubscriberCount = subscriberCount;
                    
                if (long.TryParse(channelDto.Statistics.VideoCount, out var videoCount))
                    channel.VideoCount = videoCount;
                    
                if (long.TryParse(channelDto.Statistics.ViewCount, out var viewCount))
                    channel.ViewCount = viewCount;
            }

            // Add uploads playlist ID if available
            if (channelDto.ContentDetails?.RelatedPlaylists?.Uploads != null)
            {
                channel.UploadsPlaylistId = channelDto.ContentDetails.RelatedPlaylists.Uploads;
            }

            return channel;
        }

        public YouTubePlaylist MapPlaylistToYouTubePlaylistEntity(YouTubePlaylistDto playlistDto)
        {
            if (playlistDto?.Snippet == null)
                throw new ArgumentException("Playlist DTO or snippet cannot be null", nameof(playlistDto));

            var playlist = new YouTubePlaylist
            {
                Title = playlistDto.Snippet.Title ?? "Unknown Playlist",
                Description = playlistDto.Snippet.Description,
                Link = $"https://www.youtube.com/playlist?list={playlistDto.Id}",
                PlaylistExternalId = playlistDto.Id ?? throw new ArgumentException("Playlist ID cannot be null"),
                ChannelExternalId = playlistDto.Snippet.ChannelId,
                MediaType = MediaType.Playlist,
                Thumbnail = GetBestThumbnailUrl(playlistDto.Snippet.Thumbnails),
                PublishedAt = playlistDto.Snippet.PublishedAt,
                DateAdded = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            // Add content details if available
            if (playlistDto.ContentDetails != null)
            {
                playlist.VideoCount = playlistDto.ContentDetails.ItemCount;
            }

            // Add status if available
            if (playlistDto.Status != null)
            {
                playlist.PrivacyStatus = playlistDto.Status.PrivacyStatus;
            }

            return playlist;
        }

        public Video MapPlaylistItemToVideoEntity(YouTubePlaylistItemDto playlistItemDto, YouTubeVideoDto? videoDetails = null)
        {
            if (playlistItemDto?.Snippet == null)
                throw new ArgumentException("Playlist item DTO or snippet cannot be null", nameof(playlistItemDto));

            var videoId = playlistItemDto.Snippet.ResourceId?.VideoId ?? playlistItemDto.ContentDetails?.VideoId;
            
            var video = new Video
            {
                Title = playlistItemDto.Snippet.Title ?? "Unknown Video",
                Description = playlistItemDto.Snippet.Description,
                Link = $"https://www.youtube.com/watch?v={videoId}",
                Platform = "YouTube",
                ExternalId = videoId,
                MediaType = MediaType.Video,
                VideoType = VideoType.Episode, // Playlist items are episodes
                Thumbnail = GetBestThumbnailUrl(playlistItemDto.Snippet.Thumbnails),
                DateAdded = DateTime.UtcNow
            };

            // If we have detailed video information, use it to enhance the entity
            if (videoDetails?.ContentDetails != null)
            {
                video.LengthInSeconds = YouTubeHelper.ParseDurationToSeconds(videoDetails.ContentDetails.Duration);
            }

            if (videoDetails?.Snippet != null)
            {
                // Use more detailed information from video details if available
                video.Description = videoDetails.Snippet.Description ?? video.Description;
                video.Thumbnail = GetBestThumbnailUrl(videoDetails.Snippet.Thumbnails) ?? video.Thumbnail;
            }

            return video;
        }

        public List<Video> MapPlaylistItemsToVideoEntities(List<YouTubePlaylistItemDto> playlistItems, List<YouTubeVideoDto>? videoDetails = null)
        {
            var videos = new List<Video>();
            
            foreach (var item in playlistItems)
            {
                var videoId = item.Snippet?.ResourceId?.VideoId ?? item.ContentDetails?.VideoId;
                var details = videoDetails?.FirstOrDefault(v => v.Id == videoId);
                
                try
                {
                    var video = MapPlaylistItemToVideoEntity(item, details);
                    videos.Add(video);
                }
                catch (Exception)
                {
                    // Skip items that can't be mapped (e.g., deleted videos)
                    continue;
                }
            }

            return videos;
        }

        private static string? GetBestThumbnailUrl(YouTubeThumbnailsDto? thumbnails)
        {
            if (thumbnails == null)
                return null;

            // Prefer higher quality thumbnails
            return thumbnails.Maxres?.Url ??
                   thumbnails.Standard?.Url ??
                   thumbnails.High?.Url ??
                   thumbnails.Medium?.Url ??
                   thumbnails.Default?.Url;
        }
    }
}
