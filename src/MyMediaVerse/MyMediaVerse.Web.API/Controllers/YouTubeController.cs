using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;
using MyMediaVerse.Shared.Exceptions;

namespace MyMediaVerse.Web.API.Controllers
{
    [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
    [ApiController]
    [Route("api/[controller]")]
    public class YouTubeController : ControllerBase
    {
        private readonly IYouTubeService _youTubeService;
        private readonly IYouTubeRefreshService _refreshService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<YouTubeController> _logger;

        public YouTubeController(
            IYouTubeService youTubeService,
            IYouTubeRefreshService refreshService,
            IImportReindexService importReindexService,
            ILogger<YouTubeController> logger)
        {
            _youTubeService = youTubeService;
            _refreshService = refreshService;
            _importReindexService = importReindexService;
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
        // An action-level policy replaces the controller-level one for this action.
        [EnableRateLimiting(RateLimitingExtensions.YouTubeSearchPolicy)]
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
        /// <returns>One page of playlist items with the token for the next page</returns>
        [HttpGet("playlists/{playlistId}/items")]
        public async Task<ActionResult<YouTubePlaylistItemListResponseDto>> GetPlaylistItems(
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
        /// <returns>One page of channel uploads with the token for the next page</returns>
        [HttpGet("channels/{channelId}/uploads")]
        public async Task<ActionResult<YouTubePlaylistItemListResponseDto>> GetChannelUploads(
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
        /// <returns>The video: 201 when it was added, 200 when it was already in the library</returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies outbound YouTube calls per request.
        [Authorize]
        [HttpPost("import/video/{videoId}")]
        public async Task<ActionResult<VideoResponseDto>> ImportVideo(string videoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(videoId))
                {
                    return BadRequest(new { error = "Video ID is required" });
                }

                if (!YouTubeHelper.IsValidVideoId(videoId))
                {
                    return BadRequest(new { error = $"'{videoId}' is not a YouTube video ID. A video ID is 11 letters, digits, hyphens or underscores." });
                }

                var result = await _youTubeService.ImportVideoAsync(videoId);
                return await ImportedAsync(result);
            }
            catch (YouTubeResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Video not found for import: {VideoId}", videoId);
                return NotFound(new { error = ex.Message });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while importing video {VideoId}", videoId);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing YouTube video: {VideoId}", videoId);
                return StatusCode(500, new { error = "An error occurred while importing the video", details = ex.Message });
            }
        }

        /// <summary>
        /// Import from a YouTube URL (auto-detects video, playlist, or channel)
        /// </summary>
        /// <param name="request">Import request containing the YouTube URL</param>
        /// <returns>
        /// The video, channel or playlist the URL points at (its <c>mediaType</c> says which):
        /// 201 when it was added, 200 when it was already in the library
        /// </returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies outbound YouTube calls per request.
        [Authorize]
        [HttpPost("import/url")]
        public async Task<ActionResult> ImportFromUrl([FromBody] ImportUrlRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Url))
                {
                    return BadRequest(new { error = "URL is required" });
                }

                var result = await _youTubeService.ImportFromUrlAsync(request.Url);
                return await ImportedAsync(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid YouTube URL: {Url}", request?.Url);
                return BadRequest(new { error = $"Invalid YouTube URL: {ex.Message}" });
            }
            catch (YouTubeResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Content not found for URL: {Url}", request?.Url);
                return NotFound(new { error = ex.Message });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while importing from URL {Url}", request?.Url);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from YouTube URL: {Url}", request?.Url);
                return StatusCode(500, new { error = "An error occurred while importing from the URL", details = ex.Message });
            }
        }

        // Builds the response for a finished import. Only what the import added is sent to
        // the search index, and that comes last; the reindex never throws.
        private async Task<ActionResult> ImportedAsync(YouTubeImportResult result)
        {
            if (result.AutoImportedChannel != null)
            {
                await _importReindexService.ReindexItemAfterImportAsync(result.AutoImportedChannel.Id, "YouTube channel import");
            }

            var response = result.Item.ToImportResponseDto();
            if (!result.Created)
            {
                return Ok(response);
            }

            await _importReindexService.ReindexItemAfterImportAsync(result.Item.Id, "YouTube import");

            return result.Item switch
            {
                Video => CreatedAtAction(nameof(VideoController.GetVideo), "Video", new { id = result.Item.Id }, response),
                YouTubeChannel => CreatedAtAction(nameof(YouTubeChannelController.GetChannel), "YouTubeChannel", new { id = result.Item.Id }, response),
                _ => CreatedAtAction(nameof(YouTubePlaylistController.GetPlaylist), "YouTubePlaylist", new { id = result.Item.Id }, response)
            };
        }

        /// <summary>
        /// Refreshes stored channels, playlists and videos whose YouTube data is older than the
        /// given age (YouTube's terms allow stored data to be kept for at most 30 days). Safe to
        /// call on a schedule: a run with nothing stale reports zero processed.
        /// </summary>
        /// <param name="limit">Maximum items to refresh in this run, across all three kinds (1-1000, default: 200)</param>
        /// <param name="olderThanDays">Age at which stored YouTube data counts as stale (0-3650, default: 30)</param>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // rewrites stored metadata across the library and spends YouTube quota on every call.
        [Authorize]
        [HttpPost("refresh-stale")]
        public async Task<ActionResult<YouTubeRefreshResultDto>> RefreshStale(
            [FromQuery] int limit = 200,
            [FromQuery] int olderThanDays = 30)
        {
            if (limit < 1 || limit > 1000)
            {
                return BadRequest(new { error = "limit must be between 1 and 1000" });
            }

            if (olderThanDays < 0 || olderThanDays > 3650)
            {
                return BadRequest(new { error = "olderThanDays must be between 0 and 3650" });
            }

            YouTubeRefreshResultDto result;
            try
            {
                result = await _refreshService.RefreshStaleAsync(
                    limit: limit,
                    olderThanDays: olderThanDays,
                    cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running YouTube refresh");
                result = new YouTubeRefreshResultDto
                {
                    Success = false,
                    StartedAt = DateTime.UtcNow,
                    ErrorMessage = $"YouTube refresh run failed: {ex.Message}"
                };
            }

            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            // Search reindex comes last, and only when stored data actually changed.
            if (result.UpdatedCount > 0)
            {
                try
                {
                    await _importReindexService.ReindexAfterImportAsync(result.UpdatedCount, "YouTube refresh");
                    result.ReindexTriggered = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Search reindex after YouTube refresh failed");
                    result.Warnings.Add("The refresh was saved, but the search reindex failed; search results may lag until the next reindex.");
                }
            }

            return Ok(result);
        }
    }

    public class ImportUrlRequest
    {
        public string? Url { get; set; }
    }
}
