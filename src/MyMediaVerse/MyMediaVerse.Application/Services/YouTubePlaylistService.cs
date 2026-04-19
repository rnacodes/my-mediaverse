using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class YouTubePlaylistService : IYouTubePlaylistService
    {
        private readonly IApplicationDbContext _context;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IYouTubeMappingService _mappingService;
        private readonly IVideoService _videoService;
        private readonly ILogger<YouTubePlaylistService> _logger;

        public YouTubePlaylistService(
            IApplicationDbContext context,
            IYouTubeApiClient youTubeApiClient,
            IYouTubeMappingService mappingService,
            IVideoService videoService,
            ILogger<YouTubePlaylistService> logger)
        {
            _context = context;
            _youTubeApiClient = youTubeApiClient;
            _mappingService = mappingService;
            _videoService = videoService;
            _logger = logger;
        }

        public async Task<YouTubePlaylist?> GetPlaylistByIdAsync(Guid id, bool includeVideos = false)
        {
            var query = _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .AsQueryable();

            if (includeVideos)
            {
                query = query
                    .Include(p => p.PlaylistVideos)
                    .ThenInclude(pv => pv.Video);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<YouTubePlaylist?> GetPlaylistByExternalIdAsync(string externalId, bool includeVideos = false)
        {
            var query = _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .AsQueryable();

            if (includeVideos)
            {
                query = query
                    .Include(p => p.PlaylistVideos)
                    .ThenInclude(pv => pv.Video);
            }

            return await query.FirstOrDefaultAsync(p => p.PlaylistExternalId == externalId);
        }

        public async Task<List<YouTubePlaylist>> GetAllPlaylistsAsync()
        {
            return await _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .ToListAsync();
        }

        public async Task<List<Video>> GetPlaylistVideosAsync(Guid playlistId)
        {
            var playlist = await _context.YouTubePlaylists
                .Include(p => p.PlaylistVideos)
                .ThenInclude(pv => pv.Video)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return new List<Video>();

            return playlist.PlaylistVideos
                .OrderBy(pv => pv.Position)
                .Select(pv => pv.Video)
                .ToList();
        }

        public async Task<YouTubePlaylist> ImportPlaylistFromYouTubeAsync(string playlistExternalId)
        {
            try
            {
                _logger.LogInformation($"Importing YouTube playlist: {playlistExternalId}");

                // Check if playlist already exists
                var existingPlaylist = await GetPlaylistByExternalIdAsync(playlistExternalId, includeVideos: false);
                if (existingPlaylist != null)
                {
                    _logger.LogInformation($"Playlist {playlistExternalId} already exists, returning existing playlist");
                    return existingPlaylist;
                }

                // Get playlist details from YouTube API
                var playlistDto = await _youTubeApiClient.GetPlaylistDetailsAsync(playlistExternalId);
                if (playlistDto == null)
                {
                    throw new InvalidOperationException($"Playlist with ID {playlistExternalId} not found on YouTube");
                }

                // Create playlist entity
                var playlist = _mappingService.MapPlaylistToYouTubePlaylistEntity(playlistDto);

                // Try to link to YouTubeChannel if it exists
                if (!string.IsNullOrEmpty(playlist.ChannelExternalId))
                {
                    var linkedChannel = await _context.YouTubeChannels
                        .FirstOrDefaultAsync(c => c.ChannelExternalId == playlist.ChannelExternalId);

                    if (linkedChannel != null)
                    {
                        playlist.LinkedYouTubeChannelId = linkedChannel.Id;
                        _logger.LogInformation($"Linked playlist to existing channel: {linkedChannel.Title}");
                    }
                }

                // Save playlist (without importing videos - similar to podcast series)
                var savedPlaylist = await SavePlaylistAsync(playlist, updateIfExists: false);

                _logger.LogInformation($"Successfully imported playlist {savedPlaylist.Title} (videos not auto-imported - use selective import)");

                return savedPlaylist;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing YouTube playlist: {playlistExternalId}");
                throw;
            }
        }

        public async Task<YouTubePlaylist> SyncPlaylistVideosAsync(Guid playlistId)
        {
            var playlist = await GetPlaylistByIdAsync(playlistId, includeVideos: true);
            if (playlist == null)
                throw new InvalidOperationException($"Playlist with ID {playlistId} not found");

            _logger.LogInformation($"Syncing playlist: {playlist.Title}");

            // Get current videos from YouTube
            var playlistItems = await _youTubeApiClient.GetAllPlaylistItemsAsync(playlist.PlaylistExternalId);

            // Filter out deleted and private videos
            var availablePlaylistItems = playlistItems
                .Where(item => !IsDeletedOrPrivateVideo(item))
                .ToList();

            var filteredCount = playlistItems.Count - availablePlaylistItems.Count;
            if (filteredCount > 0)
            {
                _logger.LogInformation($"Filtered out {filteredCount} deleted/private videos from playlist");
            }

            var currentVideoExternalIds = availablePlaylistItems
                .Select(item => item.Snippet?.ResourceId?.VideoId ?? item.ContentDetails?.VideoId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            // Get existing videos in our database
            var existingPlaylistVideos = playlist.PlaylistVideos?.ToList() ?? new List<YouTubePlaylistVideo>();

            // Remove playlist-video associations for videos no longer in the YouTube playlist
            // (but keep the video entities themselves in case they're used elsewhere)
            var videosToRemove = existingPlaylistVideos
                .Where(pv => pv.Video != null && !currentVideoExternalIds.Contains(pv.Video.ExternalId))
                .ToList();

            foreach (var pv in videosToRemove)
            {
                _context.Remove(pv);
                _logger.LogInformation($"Removed video '{pv.Video?.Title}' from playlist (no longer in YouTube playlist)");
            }

            // Update playlist metadata (video count from available videos only)
            playlist.LastSyncedAt = DateTime.UtcNow;
            playlist.VideoCount = availablePlaylistItems.Count;
            _context.Update(playlist);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Synced playlist {playlist.Title}: Removed {videosToRemove.Count} associations, {availablePlaylistItems.Count} available videos on YouTube");

            return await GetPlaylistByIdAsync(playlistId, includeVideos: true) ?? playlist;
        }

        /// <summary>
        /// Helper method to check if a playlist item represents a deleted or private video
        /// </summary>
        private static bool IsDeletedOrPrivateVideo(YouTubePlaylistItemDto item)
        {
            var title = item.Snippet?.Title ?? string.Empty;
            var titleLower = title.ToLowerInvariant();

            // Check for common deleted/private video indicators
            if (titleLower == "deleted video" ||
                titleLower == "private video" ||
                titleLower == "[deleted video]" ||
                titleLower == "[private video]")
            {
                return true;
            }

            // Check if the video has no channel info (often indicates deleted)
            var channelTitle = item.Snippet?.ChannelTitle ?? string.Empty;
            var videoId = item.Snippet?.ResourceId?.VideoId ?? item.ContentDetails?.VideoId;

            if (string.IsNullOrEmpty(channelTitle) && string.IsNullOrEmpty(videoId))
            {
                return true;
            }

            return false;
        }

        public async Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid videoId, int? position = null)
        {
            var playlist = await GetPlaylistByIdAsync(playlistId);
            if (playlist == null)
                return false;

            var video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
                return false;

            // Check if already exists - we need to query through the playlist's navigation property
            var playlistWithVideos = await _context.YouTubePlaylists
                .Include(p => p.PlaylistVideos)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
            
            if (playlistWithVideos == null)
                return false;

            var existing = playlistWithVideos.PlaylistVideos
                .FirstOrDefault(pv => pv.VideoId == videoId);

            if (existing != null)
                return true; // Already exists

            var playlistVideo = new YouTubePlaylistVideo
            {
                YouTubePlaylistId = playlistId,
                VideoId = videoId,
                Position = position
            };

            _context.Add(playlistVideo);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid videoId)
        {
            var playlistWithVideos = await _context.YouTubePlaylists
                .Include(p => p.PlaylistVideos)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
            
            if (playlistWithVideos == null)
                return false;

            var playlistVideo = playlistWithVideos.PlaylistVideos
                .FirstOrDefault(pv => pv.VideoId == videoId);

            if (playlistVideo == null)
                return false;

            _context.Remove(playlistVideo);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<YouTubePlaylist> SavePlaylistAsync(YouTubePlaylist playlist, bool updateIfExists = false)
        {
            var existingPlaylist = await _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .FirstOrDefaultAsync(p => p.PlaylistExternalId == playlist.PlaylistExternalId);

            if (existingPlaylist != null)
            {
                if (updateIfExists)
                {
                    // Update existing playlist
                    existingPlaylist.Title = playlist.Title;
                    existingPlaylist.Description = playlist.Description;
                    existingPlaylist.Link = playlist.Link;
                    existingPlaylist.Thumbnail = playlist.Thumbnail;
                    existingPlaylist.VideoCount = playlist.VideoCount;
                    existingPlaylist.PrivacyStatus = playlist.PrivacyStatus;
                    existingPlaylist.LastSyncedAt = DateTime.UtcNow;

                    _context.Update(existingPlaylist);
                    await _context.SaveChangesAsync();

                    return existingPlaylist;
                }
                else
                {
                    return existingPlaylist;
                }
            }

            // Topics and Genres will be handled via the navigation properties
            // EF Core will automatically track and manage these relationships
            _context.Add(playlist);
            await _context.SaveChangesAsync();

            return playlist;
        }

        public async Task<bool> DeletePlaylistAsync(Guid id)
        {
            var playlist = await _context.YouTubePlaylists.FirstOrDefaultAsync(p => p.Id == id);
            if (playlist == null)
                return false;

            _context.Remove(playlist);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task AutoLinkChannelToVideo(Video video, List<YouTubeVideoDto> videoDetails)
        {
            var videoDto = videoDetails.FirstOrDefault(v => v.Id == video.ExternalId);
            if (videoDto?.Snippet?.ChannelId == null)
                return;

            var channelExternalId = videoDto.Snippet.ChannelId;
            var linkedChannel = await _context.YouTubeChannels
                .FirstOrDefaultAsync(c => c.ChannelExternalId == channelExternalId);

            if (linkedChannel != null)
            {
                video.ChannelId = linkedChannel.Id;
                _logger.LogDebug($"Linked video '{video.Title}' to channel '{linkedChannel.Title}'");
            }
        }
    }
}

