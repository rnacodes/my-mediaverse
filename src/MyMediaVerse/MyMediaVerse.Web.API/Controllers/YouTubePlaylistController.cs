using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YouTubePlaylistController : ControllerBase
    {
        private readonly IYouTubePlaylistService _playlistService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<YouTubePlaylistController> _logger;

        public YouTubePlaylistController(
            IYouTubePlaylistService playlistService,
            IImportReindexService importReindexService,
            ILogger<YouTubePlaylistController> logger)
        {
            _playlistService = playlistService;
            _importReindexService = importReindexService;
            _logger = logger;
        }

        /// <summary>
        /// Get all YouTube playlists
        /// </summary>
        /// <returns>List of all YouTube playlists</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<YouTubePlaylistResponseDto>>> GetAllPlaylists()
        {
            try
            {
                var playlists = await _playlistService.GetAllPlaylistsAsync();
                var response = playlists.Select(p => p.ToResponseDto()).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all YouTube playlists");
                return StatusCode(500, new { error = "Failed to retrieve YouTube playlists", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a YouTube playlist by database ID
        /// </summary>
        /// <param name="id">Playlist database ID</param>
        /// <param name="includeVideos">Include playlist videos in response</param>
        /// <returns>YouTube playlist details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<YouTubePlaylistResponseDto>> GetPlaylist(Guid id, [FromQuery] bool includeVideos = false)
        {
            try
            {
                var playlist = await _playlistService.GetPlaylistByIdAsync(id, includeVideos);

                if (playlist == null)
                {
                    return NotFound($"YouTube playlist with ID {id} not found.");
                }

                return Ok(playlist.ToResponseDto(includeVideos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving YouTube playlist with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve YouTube playlist", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a YouTube playlist by external YouTube playlist ID
        /// </summary>
        /// <param name="externalId">YouTube playlist ID</param>
        /// <param name="includeVideos">Include playlist videos in response</param>
        /// <returns>YouTube playlist details</returns>
        [HttpGet("by-external/{externalId}")]
        public async Task<ActionResult<YouTubePlaylistResponseDto>> GetPlaylistByExternalId(string externalId, [FromQuery] bool includeVideos = false)
        {
            try
            {
                var playlist = await _playlistService.GetPlaylistByExternalIdAsync(externalId, includeVideos);

                if (playlist == null)
                {
                    return NotFound($"YouTube playlist with external ID {externalId} not found.");
                }

                return Ok(playlist.ToResponseDto(includeVideos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving YouTube playlist with external ID {ExternalId}", externalId);
                return StatusCode(500, new { error = "Failed to retrieve YouTube playlist", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all videos in a YouTube playlist
        /// </summary>
        /// <param name="id">Playlist database ID</param>
        /// <returns>List of videos in the playlist</returns>
        [HttpGet("{id}/videos")]
        public async Task<ActionResult<IEnumerable<VideoInfoDto>>> GetPlaylistVideos(Guid id)
        {
            try
            {
                var videos = await _playlistService.GetPlaylistVideosAsync(id);
                var response = videos.Select(v => new VideoInfoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Thumbnail = v.GetEffectiveThumbnail(),
                    LengthInSeconds = v.LengthInSeconds,
                    ExternalId = v.ExternalId
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving videos for playlist {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve playlist videos", details = ex.Message });
            }
        }

        /// <summary>
        /// Import a YouTube playlist from YouTube API
        /// </summary>
        /// <param name="externalId">YouTube playlist ID</param>
        /// <returns>The playlist: 201 when it was added, 200 when it was already in the library</returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies outbound YouTube calls per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("import/{externalId}")]
        public async Task<ActionResult<YouTubePlaylistResponseDto>> ImportPlaylist(string externalId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(externalId))
                {
                    return BadRequest(new { error = "Playlist external ID is required" });
                }

                var result = await _playlistService.ImportPlaylistFromYouTubeAsync(externalId);
                var response = result.Playlist.ToResponseDto();
                if (!result.Created)
                {
                    return Ok(response);
                }

                // Search reindex comes last, and only for a playlist this import added.
                await _importReindexService.ReindexItemAfterImportAsync(result.Playlist.Id, "YouTube playlist import");
                return CreatedAtAction(nameof(GetPlaylist), new { id = result.Playlist.Id }, response);
            }
            catch (YouTubeResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Playlist not found for import: {ExternalId}", externalId);
                return NotFound(new { error = ex.Message });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while importing playlist {ExternalId}", externalId);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing YouTube playlist: {ExternalId}", externalId);
                return StatusCode(500, new { error = "An error occurred while importing the playlist", details = ex.Message });
            }
        }

        /// <summary>
        /// Sync a playlist's videos with YouTube
        /// </summary>
        /// <param name="id">Playlist database ID</param>
        /// <returns>The updated playlist</returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // rewrites stored data and proxies outbound YouTube calls per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("{id}/sync")]
        public async Task<ActionResult<YouTubePlaylistResponseDto>> SyncPlaylist(Guid id)
        {
            try
            {
                var result = await _playlistService.SyncPlaylistVideosAsync(id);

                // Search reindex comes last. Videos new to the library need the full pass;
                // otherwise only the playlist's own document can have changed.
                if (result.VideosCreated > 0)
                {
                    await _importReindexService.ReindexAfterImportAsync(result.VideosCreated, "YouTube playlist sync");
                }
                else
                {
                    await _importReindexService.ReindexItemAfterImportAsync(result.Playlist.Id, "YouTube playlist sync");
                }

                return Ok(result.Playlist.ToResponseDto());
            }
            catch (YouTubeResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Playlist {Id} is no longer on YouTube", id);
                return NotFound(new { error = ex.Message });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while syncing playlist {Id}", id);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Playlist not found for sync: {Id}", id);
                return NotFound(new { error = $"Playlist with ID {id} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing playlist: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while syncing the playlist", details = ex.Message });
            }
        }

        /// <summary>
        /// Add a video to a playlist
        /// </summary>
        /// <param name="id">Playlist database ID</param>
        /// <param name="videoId">Video database ID</param>
        /// <param name="position">Optional position in playlist</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/videos/{videoId}")]
        public async Task<ActionResult> AddVideoToPlaylist(Guid id, Guid videoId, [FromQuery] int? position = null)
        {
            try
            {
                var success = await _playlistService.AddVideoToPlaylistAsync(id, videoId, position);
                
                if (!success)
                {
                    return NotFound("Playlist or video not found");
                }

                return Ok(new { message = "Video added to playlist successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding video {VideoId} to playlist {PlaylistId}", videoId, id);
                return StatusCode(500, new { error = "An error occurred while adding the video to the playlist" });
            }
        }

        /// <summary>
        /// Remove a video from a playlist
        /// </summary>
        /// <param name="id">Playlist database ID</param>
        /// <param name="videoId">Video database ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}/videos/{videoId}")]
        public async Task<ActionResult> RemoveVideoFromPlaylist(Guid id, Guid videoId)
        {
            try
            {
                var success = await _playlistService.RemoveVideoFromPlaylistAsync(id, videoId);
                
                if (!success)
                {
                    return NotFound("Playlist video association not found");
                }

                return Ok(new { message = "Video removed from playlist successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing video {VideoId} from playlist {PlaylistId}", videoId, id);
                return StatusCode(500, new { error = "An error occurred while removing the video from the playlist" });
            }
        }

        /// <summary>
        /// Delete a YouTube playlist
        /// </summary>
        /// <param name="id">Playlist database ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlaylist(Guid id)
        {
            try
            {
                var success = await _playlistService.DeletePlaylistAsync(id);
                
                if (!success)
                {
                    return NotFound($"Playlist with ID {id} not found");
                }

                return Ok(new { message = "Playlist deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting playlist: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the playlist" });
            }
        }
    }
}

