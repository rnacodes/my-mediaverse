using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YouTubeChannelController : ControllerBase
    {
        private readonly IYouTubeChannelService _channelService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<YouTubeChannelController> _logger;
        private readonly YouTubeSyncOptions _syncOptions;

        public YouTubeChannelController(
            IYouTubeChannelService channelService,
            IImportReindexService importReindexService,
            ILogger<YouTubeChannelController> logger,
            IOptions<YouTubeSyncOptions> syncOptions)
        {
            _channelService = channelService;
            _importReindexService = importReindexService;
            _logger = logger;
            _syncOptions = syncOptions.Value;
        }

        /// <summary>
        /// Get all YouTube channels
        /// </summary>
        /// <returns>List of all YouTube channels</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<YouTubeChannelResponseDto>>> GetAllChannels()
        {
            try
            {
                var channels = await _channelService.GetAllChannelsAsync();
                var response = channels.Select(c => c.ToResponseDto()).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all YouTube channels");
                return StatusCode(500, new { error = "Failed to retrieve YouTube channels", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a YouTube channel by database ID
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <returns>YouTube channel details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<YouTubeChannelResponseDto>> GetChannel(Guid id)
        {
            try
            {
                var channel = await _channelService.GetChannelByIdAsync(id);

                if (channel == null)
                {
                    return NotFound($"YouTube channel with ID {id} not found.");
                }

                return Ok(channel.ToResponseDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving YouTube channel with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a YouTube channel by external YouTube channel ID
        /// </summary>
        /// <param name="externalId">YouTube channel ID (e.g., UCxxxxxx)</param>
        /// <returns>YouTube channel details</returns>
        [HttpGet("by-external/{externalId}")]
        public async Task<ActionResult<YouTubeChannelResponseDto>> GetChannelByExternalId(string externalId)
        {
            try
            {
                var channel = await _channelService.GetChannelByExternalIdAsync(externalId);

                if (channel == null)
                {
                    return NotFound($"YouTube channel with external ID {externalId} not found.");
                }

                return Ok(channel.ToResponseDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving YouTube channel with external ID {ExternalId}", externalId);
                return StatusCode(500, new { error = "Failed to retrieve YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all videos associated with a YouTube channel
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <returns>List of videos from this channel</returns>
        [HttpGet("{id}/videos")]
        public async Task<ActionResult<IEnumerable<VideoResponseDto>>> GetChannelVideos(Guid id)
        {
            try
            {
                var videos = await _channelService.GetChannelVideosAsync(id);
                var response = videos.Select(v => v.ToResponseDto()).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving videos for channel {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve channel videos", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new YouTube channel manually
        /// </summary>
        /// <param name="dto">Channel creation data</param>
        /// <returns>Created YouTube channel</returns>
        [HttpPost]
        public async Task<ActionResult<YouTubeChannelResponseDto>> CreateChannel([FromBody] CreateYouTubeChannelDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var channel = await _channelService.CreateChannelAsync(dto);
                var response = channel.ToResponseDto();
                return CreatedAtAction(nameof(GetChannel), new { id = channel.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Channel already exists: {ExternalId}", dto.ChannelExternalId);
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating YouTube channel");
                return StatusCode(500, new { error = "Failed to create YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing YouTube channel
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <param name="dto">Channel update data</param>
        /// <returns>Updated YouTube channel</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<YouTubeChannelResponseDto>> UpdateChannel(Guid id, [FromBody] UpdateYouTubeChannelDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var channel = await _channelService.UpdateChannelAsync(id, dto);
                var response = channel.ToResponseDto();
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "YouTube channel not found for update: {Id}", id);
                return NotFound($"YouTube channel with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating YouTube channel with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a YouTube channel (videos remain but ChannelId becomes null)
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteChannel(Guid id)
        {
            try
            {
                var result = await _channelService.DeleteChannelAsync(id);
                
                if (!result)
                {
                    return NotFound($"YouTube channel with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting YouTube channel with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Import a YouTube channel from the YouTube API
        /// </summary>
        /// <param name="channelId">YouTube channel ID (e.g., UCxxxxxx)</param>
        /// <returns>The channel: 201 when it was added, 200 when it was already in the library</returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies outbound YouTube calls per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("import/{channelId}")]
        public async Task<ActionResult<YouTubeChannelResponseDto>> ImportChannel(string channelId)
        {
            try
            {
                var result = await _channelService.ImportChannelFromYouTubeAsync(channelId);
                var response = result.Channel.ToResponseDto();
                if (!result.Created)
                {
                    return Ok(response);
                }

                // Search reindex comes last, and only for a channel this import added.
                await _importReindexService.ReindexItemAfterImportAsync(result.Channel.Id, "YouTube channel import");
                return CreatedAtAction(nameof(GetChannel), new { id = result.Channel.Id }, response);
            }
            catch (YouTubeResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Channel not found for import: {ChannelId}", channelId);
                return NotFound(new { error = ex.Message });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while importing channel {ChannelId}", channelId);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing YouTube channel {ChannelId}", channelId);
                return StatusCode(500, new { error = "Failed to import YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Refresh the channel's metadata from YouTube and report how many uploads the library
        /// does not hold yet.
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <returns>The sync result (reporting contract shape)</returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // rewrites stored metadata and proxies an outbound YouTube call per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("{id}/sync")]
        public async Task<ActionResult<YouTubeChannelSyncResultDto>> SyncChannelMetadata(Guid id)
        {
            try
            {
                var result = await _channelService.SyncChannelMetadataAsync(id);

                // Search reindex comes last; only the channel's own document can have changed.
                if (result.UpdatedCount > 0)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(id, "YouTube channel sync");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "YouTube channel not found for sync: {Id}", id);
                return NotFound(new { error = $"YouTube channel with ID {id} not found" });
            }
            catch (YouTubeResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Channel {Id} is no longer on YouTube", id);
                return NotFound(new { error = ex.Message });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while syncing channel {Id}", id);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing YouTube channel metadata for {Id}", id);
                return StatusCode(500, new { error = "Failed to sync YouTube channel metadata", details = ex.Message });
            }
        }

        /// <summary>
        /// Import the channel's newest uploads into the library.
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <param name="count">How many of the newest uploads to bring in; defaults to the configured count, capped at one YouTube page</param>
        /// <returns>The import result (reporting contract shape)</returns>
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies outbound YouTube calls per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("{id}/import-latest")]
        public async Task<ActionResult<YouTubeChannelImportResultDto>> ImportLatestUploads(Guid id, [FromQuery] int? count = null)
        {
            try
            {
                var result = await _channelService.ImportLatestUploadsAsync(id, count ?? _syncOptions.LatestUploadsCount);

                // Search reindex comes last. New and newly linked videos both change documents.
                var changed = result.CreatedCount + result.LinkedCount;
                if (changed > 0)
                {
                    await _importReindexService.ReindexAfterImportAsync(changed, "YouTube channel import-latest");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "YouTube channel not found for import-latest: {Id}", id);
                return NotFound(new { error = $"YouTube channel with ID {id} not found" });
            }
            catch (YouTubeQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "YouTube quota used up while importing latest uploads for channel {Id}", id);
                return StatusCode(503, new { error = "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing latest uploads for channel {Id}", id);
                return StatusCode(500, new YouTubeChannelImportResultDto
                {
                    Success = false,
                    ChannelId = id,
                    ErrorMessage = "Failed to import the channel's latest uploads",
                    StartedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Check if a channel exists by external YouTube ID
        /// </summary>
        /// <param name="externalId">YouTube channel ID</param>
        /// <returns>Boolean indicating if channel exists</returns>
        [HttpGet("exists/{externalId}")]
        public async Task<ActionResult<bool>> CheckChannelExists(string externalId)
        {
            try
            {
                var exists = await _channelService.ChannelExistsAsync(externalId);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if channel exists: {ExternalId}", externalId);
                return StatusCode(500, new { error = "Failed to check channel existence", details = ex.Message });
            }
        }
    }
}

