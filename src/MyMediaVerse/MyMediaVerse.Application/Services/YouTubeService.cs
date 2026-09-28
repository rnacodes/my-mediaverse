using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class YouTubeService : IYouTubeService
    {
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IYouTubeMappingService _mappingService;
        private readonly IVideoService _videoService;
        private readonly IYouTubeChannelService _channelService;
        private readonly IYouTubePlaylistService _playlistService;
        private readonly ILogger<YouTubeService> _logger;

        public YouTubeService(
            IYouTubeApiClient youTubeApiClient,
            IYouTubeMappingService mappingService,
            IVideoService videoService,
            IYouTubeChannelService channelService,
            IYouTubePlaylistService playlistService,
            ILogger<YouTubeService> logger)
        {
            _youTubeApiClient = youTubeApiClient;
            _mappingService = mappingService;
            _videoService = videoService;
            _channelService = channelService;
            _playlistService = playlistService;
            _logger = logger;
        }

        public async Task<YouTubeSearchResultDto> SearchAsync(string query, string type = "video", int maxResults = 25, string? pageToken = null, string? channelId = null)
        {
            return await _youTubeApiClient.SearchAsync(query, type, maxResults, pageToken, channelId);
        }

        public async Task<YouTubeVideoDto?> GetVideoDetailsAsync(string videoId)
        {
            return await _youTubeApiClient.GetVideoDetailsAsync(videoId);
        }

        public async Task<List<YouTubeVideoDto>> GetVideosAsync(List<string> videoIds)
        {
            return await _youTubeApiClient.GetVideosAsync(videoIds);
        }

        public async Task<YouTubePlaylistDto?> GetPlaylistDetailsAsync(string playlistId)
        {
            return await _youTubeApiClient.GetPlaylistDetailsAsync(playlistId);
        }

        public async Task<List<YouTubePlaylistItemDto>> GetPlaylistItemsAsync(string playlistId, int maxResults = 50, string? pageToken = null)
        {
            return await _youTubeApiClient.GetPlaylistItemsAsync(playlistId, maxResults, pageToken);
        }

        public async Task<List<YouTubePlaylistItemDto>> GetAllPlaylistItemsAsync(string playlistId)
        {
            return await _youTubeApiClient.GetAllPlaylistItemsAsync(playlistId);
        }

        public async Task<YouTubeChannelDto?> GetChannelDetailsAsync(string channelId)
        {
            return await _youTubeApiClient.GetChannelDetailsAsync(channelId);
        }

        public async Task<YouTubeChannelDto?> GetChannelByUsernameAsync(string username)
        {
            return await _youTubeApiClient.GetChannelByUsernameAsync(username);
        }

        public async Task<YouTubeChannelDto?> GetChannelByHandleAsync(string handle)
        {
            return await _youTubeApiClient.GetChannelByHandleAsync(handle);
        }

        public async Task<List<YouTubePlaylistItemDto>> GetChannelUploadsAsync(string channelId, int maxResults = 25, string? pageToken = null)
        {
            return await _youTubeApiClient.GetChannelUploadsAsync(channelId, maxResults, pageToken);
        }

        public async Task<YouTubeImportResult> ImportVideoAsync(string videoId)
        {
            try
            {
                _logger.LogInformation($"Importing YouTube video: {videoId}");

                // A video already stored under this id is returned as it is, without
                // spending a YouTube call.
                var storedVideo = await _videoService.GetVideoByExternalIdAsync(VideoDuplicateFinder.YouTubePlatform, videoId);
                if (storedVideo != null)
                {
                    _logger.LogInformation($"YouTube video {videoId} is already in the library");
                    return new YouTubeImportResult(storedVideo, false);
                }

                var videoDto = await _youTubeApiClient.GetVideoDetailsAsync(videoId);
                if (videoDto == null)
                {
                    throw new YouTubeResourceNotFoundException("video", videoId);
                }

                // Auto-import/link channel if available
                Guid? channelId = null;
                YouTubeChannel? autoImportedChannel = null;
                if (!string.IsNullOrEmpty(videoDto.Snippet?.ChannelId))
                {
                    try
                    {
                        _logger.LogInformation($"Checking for channel: {videoDto.Snippet.ChannelId}");
                        
                        // Check if channel already exists
                        var existingChannel = await _channelService.GetChannelByExternalIdAsync(videoDto.Snippet.ChannelId);
                        
                        if (existingChannel != null)
                        {
                            _logger.LogInformation($"Channel already exists: {existingChannel.Title}");
                            channelId = existingChannel.Id;
                        }
                        else
                        {
                            // Import the channel
                            _logger.LogInformation($"Auto-importing channel: {videoDto.Snippet.ChannelTitle}");
                            var importedChannel = await _channelService.ImportChannelFromYouTubeAsync(videoDto.Snippet.ChannelId);
                            channelId = importedChannel.Channel.Id;
                            if (importedChannel.Created)
                            {
                                autoImportedChannel = importedChannel.Channel;
                            }
                            _logger.LogInformation($"Successfully auto-imported channel: {importedChannel.Channel.Title}");
                        }
                    }
                    // A used-up daily quota stops the whole import; any other channel failure
                    // only costs the link.
                    catch (Exception channelEx) when (channelEx is not YouTubeQuotaExceededException)
                    {
                        _logger.LogWarning(channelEx, $"Failed to import channel for video {videoId}, continuing without channel link");
                        // Continue without channel - don't fail the video import
                    }
                }

                var video = _mappingService.MapVideoToEntity(videoDto);
                if (channelId.HasValue)
                {
                    video.ChannelId = channelId.Value;
                }
                var saved = await _videoService.SaveVideoAsync(video);

                _logger.LogInformation($"Successfully imported YouTube video: {video.Title}" + 
                    (channelId.HasValue ? $" (linked to channel)" : ""));
                return new YouTubeImportResult(saved.Video, saved.Created, autoImportedChannel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing YouTube video: {videoId}");
                throw;
            }
        }

        public async Task<YouTubeImportResult> ImportFromUrlAsync(string url)
        {
            try
            {
                _logger.LogInformation($"Importing from YouTube URL: {url}");

                // Try to extract video ID first
                var videoId = YouTubeHelper.ExtractVideoIdFromUrl(url);
                if (!string.IsNullOrEmpty(videoId))
                {
                    return await ImportVideoAsync(videoId);
                }

                // Try to extract playlist ID — import as a first-class playlist container
                var playlistId = YouTubeHelper.ExtractPlaylistIdFromUrl(url);
                if (!string.IsNullOrEmpty(playlistId))
                {
                    var playlist = await _playlistService.ImportPlaylistFromYouTubeAsync(playlistId);
                    return new YouTubeImportResult(playlist.Playlist, playlist.Created);
                }

                // Try to extract channel identifier (could be ID, handle, or username)
                var channelIdentifier = YouTubeHelper.ExtractChannelIdFromUrl(url);
                if (!string.IsNullOrEmpty(channelIdentifier))
                {
                    var resolvedChannelId = await ResolveChannelIdAsync(channelIdentifier);
                    var channel = await _channelService.ImportChannelFromYouTubeAsync(resolvedChannelId);
                    return new YouTubeImportResult(channel.Channel, channel.Created);
                }

                throw new ArgumentException($"Unable to extract valid YouTube ID from URL: {url}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing from YouTube URL: {url}");
                throw;
            }
        }

        /// <summary>
        /// Resolves a channel identifier (handle, username, custom URL, or channel ID) to a real YouTube channel ID.
        /// Real channel IDs start with "UC" and are used directly. Other identifiers are resolved via the YouTube API.
        /// </summary>
        private async Task<string> ResolveChannelIdAsync(string identifier)
        {
            // Real channel IDs start with "UC"
            if (identifier.StartsWith("UC"))
                return identifier;

            _logger.LogInformation("Resolving channel identifier: {Identifier}", identifier);

            // Try as handle (for /@handle URLs)
            var channelDto = await _youTubeApiClient.GetChannelByHandleAsync(identifier);
            if (channelDto?.Id != null)
            {
                _logger.LogInformation("Resolved handle @{Handle} to channel ID: {ChannelId}", identifier, channelDto.Id);
                return channelDto.Id;
            }

            // Try as username (for /user/ URLs)
            channelDto = await _youTubeApiClient.GetChannelByUsernameAsync(identifier);
            if (channelDto?.Id != null)
            {
                _logger.LogInformation("Resolved username {Username} to channel ID: {ChannelId}", identifier, channelDto.Id);
                return channelDto.Id;
            }

            throw new YouTubeResourceNotFoundException("channel", identifier);
        }
    }
}
