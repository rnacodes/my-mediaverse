using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YouTubeController : ControllerBase
    {
        private readonly IYouTubeService _youTubeService;
        private readonly ILogger<YouTubeController> _logger;

        public YouTubeController(IYouTubeService youTubeService, ILogger<YouTubeController> logger)
        {
            _youTubeService = youTubeService;
            _logger = logger;
        }

        /// <summary>
        /// Search for videos, channels, and playlists on YouTube
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="type">Type of content to search for (video, channel, playlist)</param>
        /// <param name="maxResults">Maximum number of results (default: 25)</param>
        /// <param name="pageToken">Page token for pagination</param>
        /// <param name="channelId">Channel ID to search within (optional)</param>
        /// <returns>YouTube search results</returns>
        [HttpGet("search")]
        public async Task<ActionResult<YouTubeSearchResultDto>> Search(
            [FromQuery] string query,
            [FromQuery] string type = "video",
            [FromQuery] int maxResults = 25,
            [FromQuery] string? pageToken = null,
            [FromQuery] string? channelId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                var result = await _youTubeService.SearchAsync(query, type, maxResults, pageToken, channelId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching YouTube with query: {Query}", query);
                return StatusCode(500, "An error occurred while searching YouTube");
            }
        }

        /// <summary>
        /// Get detailed information about a specific video
        /// </summary>
        /// <param name="videoId">YouTube video ID</param>
        /// <returns>Detailed video information</returns>
        [HttpGet("videos/{videoId}")]
        public async Task<ActionResult<YouTubeVideoDto>> GetVideoDetails(string videoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(videoId))
                {
                    return BadRequest("Video ID is required");
                }

                var result = await _youTubeService.GetVideoDetailsAsync(videoId);
                if (result == null)
                {
                    return NotFound($"Video with ID {videoId} not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube video details for ID: {VideoId}", videoId);
                return StatusCode(500, "An error occurred while getting video details");
            }
        }

        /// <summary>
        /// Get multiple videos by their IDs
        /// </summary>
        /// <param name="videoIds">Comma-separated list of video IDs</param>
        /// <returns>List of video details</returns>
        [HttpGet("videos")]
        public async Task<ActionResult<List<YouTubeVideoDto>>> GetVideos([FromQuery] string videoIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(videoIds))
                {
                    return BadRequest("Video IDs are required");
                }

                var idList = videoIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                var result = await _youTubeService.GetVideosAsync(idList);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube videos for IDs: {VideoIds}", videoIds);
                return StatusCode(500, "An error occurred while getting videos");
            }
        }

        /// <summary>
        /// Get detailed information about a specific playlist
        /// </summary>
        /// <param name="playlistId">YouTube playlist ID</param>
        /// <returns>Detailed playlist information</returns>
        [HttpGet("playlists/{playlistId}")]
        public async Task<ActionResult<YouTubePlaylistDto>> GetPlaylistDetails(string playlistId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playlistId))
                {
                    return BadRequest("Playlist ID is required");
                }

                var result = await _youTubeService.GetPlaylistDetailsAsync(playlistId);
                if (result == null)
                {
                    return NotFound($"Playlist with ID {playlistId} not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube playlist details for ID: {PlaylistId}", playlistId);
                return StatusCode(500, "An error occurred while getting playlist details");
            }
        }

        /// <summary>
        /// Get videos from a specific playlist
        /// </summary>
        /// <param name="playlistId">YouTube playlist ID</param>
        /// <param name="maxResults">Maximum number of results (default: 50)</param>
        /// <param name="pageToken">Page token for pagination</param>
        /// <returns>List of playlist items</returns>
        [HttpGet("playlists/{playlistId}/items")]
        public async Task<ActionResult<List<YouTubePlaylistItemDto>>> GetPlaylistItems(
            string playlistId,
            [FromQuery] int maxResults = 50,
            [FromQuery] string? pageToken = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playlistId))
                {
                    return BadRequest("Playlist ID is required");
                }

                var result = await _youTubeService.GetPlaylistItemsAsync(playlistId, maxResults, pageToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube playlist items for ID: {PlaylistId}", playlistId);
                return StatusCode(500, "An error occurred while getting playlist items");
            }
        }

        /// <summary>
        /// Get all videos from a playlist (handles pagination automatically)
        /// </summary>
        /// <param name="playlistId">YouTube playlist ID</param>
        /// <returns>All playlist items</returns>
        [HttpGet("playlists/{playlistId}/all-items")]
        public async Task<ActionResult<List<YouTubePlaylistItemDto>>> GetAllPlaylistItems(string playlistId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playlistId))
                {
                    return BadRequest("Playlist ID is required");
                }

                var result = await _youTubeService.GetAllPlaylistItemsAsync(playlistId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all YouTube playlist items for ID: {PlaylistId}", playlistId);
                return StatusCode(500, "An error occurred while getting all playlist items");
            }
        }

        /// <summary>
        /// Get detailed information about a specific channel
        /// </summary>
        /// <param name="channelId">YouTube channel ID</param>
        /// <returns>Detailed channel information</returns>
        [HttpGet("channels/{channelId}")]
        public async Task<ActionResult<YouTubeChannelDto>> GetChannelDetails(string channelId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    return BadRequest("Channel ID is required");
                }

                var result = await _youTubeService.GetChannelDetailsAsync(channelId);
                if (result == null)
                {
                    return NotFound($"Channel with ID {channelId} not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube channel details for ID: {ChannelId}", channelId);
                return StatusCode(500, "An error occurred while getting channel details");
            }
        }

        /// <summary>
        /// Get channel details by username/handle
        /// </summary>
        /// <param name="username">YouTube channel username</param>
        /// <returns>Detailed channel information</returns>
        [HttpGet("channels/by-username/{username}")]
        public async Task<ActionResult<YouTubeChannelDto>> GetChannelByUsername(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest("Username is required");
                }

                var result = await _youTubeService.GetChannelByUsernameAsync(username);
                if (result == null)
                {
                    return NotFound($"Channel with username {username} not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube channel details for username: {Username}", username);
                return StatusCode(500, "An error occurred while getting channel details");
            }
        }

        /// <summary>
        /// Get videos from a channel's uploads
        /// </summary>
        /// <param name="channelId">YouTube channel ID</param>
        /// <param name="maxResults">Maximum number of results (default: 25)</param>
        /// <param name="pageToken">Page token for pagination</param>
        /// <returns>List of channel upload items</returns>
        [HttpGet("channels/{channelId}/uploads")]
        public async Task<ActionResult<List<YouTubePlaylistItemDto>>> GetChannelUploads(
            string channelId,
            [FromQuery] int maxResults = 25,
            [FromQuery] string? pageToken = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    return BadRequest("Channel ID is required");
                }

                var result = await _youTubeService.GetChannelUploadsAsync(channelId, maxResults, pageToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube channel uploads for ID: {ChannelId}", channelId);
                return StatusCode(500, "An error occurred while getting channel uploads");
            }
        }

        /// <summary>
        /// Import a YouTube video into the media library
        /// </summary>
        /// <param name="videoId">YouTube video ID</param>
        /// <returns>Imported video entity</returns>
        [HttpPost("import/video/{videoId}")]
        public async Task<ActionResult<Video>> ImportVideo(string videoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(videoId))
                {
                    return BadRequest("Video ID is required");
                }

                var result = await _youTubeService.ImportVideoAsync(videoId);
                return CreatedAtAction(nameof(GetVideoDetails), new { videoId = result.ExternalId }, result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Video not found for import: {VideoId}", videoId);
                return NotFound($"Video with ID {videoId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing YouTube video: {VideoId}", videoId);
                return StatusCode(500, "An error occurred while importing the video");
            }
        }

        /// <summary>
        /// Import a YouTube playlist into the media library
        /// </summary>
        /// <param name="playlistId">YouTube playlist ID</param>
        /// <param name="importAsChannel">Whether to import as a channel/series with episodes</param>
        /// <returns>List of imported video entities</returns>
        [HttpPost("import/playlist/{playlistId}")]
        public async Task<ActionResult<List<Video>>> ImportPlaylist(
            string playlistId,
            [FromQuery] bool importAsChannel = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playlistId))
                {
                    return BadRequest("Playlist ID is required");
                }

                var result = await _youTubeService.ImportPlaylistAsync(playlistId, importAsChannel);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Playlist not found for import: {PlaylistId}", playlistId);
                return NotFound($"Playlist with ID {playlistId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing YouTube playlist: {PlaylistId}", playlistId);
                return StatusCode(500, "An error occurred while importing the playlist");
            }
        }

        /// <summary>
        /// Import a YouTube channel into the media library
        /// </summary>
        /// <param name="channelId">YouTube channel ID</param>
        /// <returns>Imported channel entity</returns>
        [HttpPost("import/channel/{channelId}")]
        public async Task<ActionResult<Video>> ImportChannel(string channelId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    return BadRequest("Channel ID is required");
                }

                var result = await _youTubeService.ImportChannelAsync(channelId);
                return Created($"/api/YouTube/channel/{channelId}", result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Channel not found for import: {ChannelId}", channelId);
                return NotFound($"Channel with ID {channelId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing YouTube channel: {ChannelId}", channelId);
                return StatusCode(500, "An error occurred while importing the channel");
            }
        }

        /// <summary>
        /// Import from a YouTube URL (auto-detects video, playlist, or channel)
        /// </summary>
        /// <param name="request">Import request containing the YouTube URL</param>
        /// <returns>Imported entity</returns>
        [HttpPost("import/url")]
        public async Task<ActionResult<Video>> ImportFromUrl([FromBody] ImportUrlRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Url))
                {
                    return BadRequest("URL is required");
                }

                var result = await _youTubeService.ImportFromUrlAsync(request.Url);
                if (result is Video video)
                {
                    return CreatedAtAction(nameof(GetVideoDetails), new { videoId = video.ExternalId }, result);
                }
                return Created(string.Empty, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid YouTube URL: {Url}", request?.Url);
                return BadRequest($"Invalid YouTube URL: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Content not found for URL: {Url}", request?.Url);
                return NotFound($"Content not found for the provided URL");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from YouTube URL: {Url}", request?.Url);
                return StatusCode(500, "An error occurred while importing from the URL");
            }
        }
    }

    public class ImportUrlRequest
    {
        public string? Url { get; set; }
    }
}
