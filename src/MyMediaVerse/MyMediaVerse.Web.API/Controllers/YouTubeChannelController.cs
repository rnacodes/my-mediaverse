using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YouTubeChannelController : ControllerBase
    {
        private readonly IYouTubeChannelService _channelService;
        private readonly ILogger<YouTubeChannelController> _logger;

        public YouTubeChannelController(IYouTubeChannelService channelService, ILogger<YouTubeChannelController> logger)
        {
            _channelService = channelService;
            _logger = logger;
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
                var response = channels.Select(MapToResponseDto).ToList();
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

                return Ok(MapToResponseDto(channel));
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

                return Ok(MapToResponseDto(channel));
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
                var response = videos.Select(v => new VideoResponseDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Description = v.Description,
                    MediaType = v.MediaType,
                    Status = v.Status,
                    DateAdded = v.DateAdded,
                    Link = v.Link,
                    Thumbnail = v.GetEffectiveThumbnail(),
                    Platform = v.Platform,
                    ChannelId = v.ChannelId,
                    LengthInSeconds = v.LengthInSeconds,
                    ExternalId = v.ExternalId,
                    Rating = v.Rating,
                    OwnershipStatus = v.OwnershipStatus,
                    DateCompleted = v.DateCompleted,
                    Notes = v.Notes,
                    RelatedNotes = v.RelatedNotes,
                    Topics = v.Topics.Select(t => t.Name).ToArray(),
                    Genres = v.Genres.Select(g => g.Name).ToArray()
                }).ToList();

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
                var response = MapToResponseDto(channel);
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
                var response = MapToResponseDto(channel);
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
        /// <returns>Imported YouTube channel</returns>
        [HttpPost("import/{channelId}")]
        public async Task<ActionResult<YouTubeChannelResponseDto>> ImportChannel(string channelId)
        {
            try
            {
                var channel = await _channelService.ImportChannelFromYouTubeAsync(channelId);
                var response = MapToResponseDto(channel);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error importing channel: {ChannelId}", channelId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing YouTube channel {ChannelId}", channelId);
                return StatusCode(500, new { error = "Failed to import YouTube channel", details = ex.Message });
            }
        }

        /// <summary>
        /// Sync channel metadata from YouTube API (update subscriber count, video count, etc.)
        /// </summary>
        /// <param name="id">Channel database ID</param>
        /// <returns>Updated YouTube channel</returns>
        [HttpPost("{id}/sync")]
        public async Task<ActionResult<YouTubeChannelResponseDto>> SyncChannelMetadata(Guid id)
        {
            try
            {
                var channel = await _channelService.SyncChannelMetadataAsync(id);
                var response = MapToResponseDto(channel);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "YouTube channel not found for sync: {Id}", id);
                return NotFound($"YouTube channel with ID {id} not found");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error syncing channel: {Id}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing YouTube channel metadata for {Id}", id);
                return StatusCode(500, new { error = "Failed to sync YouTube channel metadata", details = ex.Message });
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

        /// <summary>
        /// Helper method to map YouTubeChannel entity to YouTubeChannelResponseDto
        /// </summary>
        private YouTubeChannelResponseDto MapToResponseDto(YouTubeChannel channel)
        {
            return new YouTubeChannelResponseDto
            {
                Id = channel.Id,
                Title = channel.Title,
                Description = channel.Description,
                Link = channel.Link,
                Thumbnail = channel.Thumbnail,
                ChannelExternalId = channel.ChannelExternalId,
                CustomUrl = channel.CustomUrl,
                SubscriberCount = channel.SubscriberCount,
                VideoCount = channel.VideoCount,
                ViewCount = channel.ViewCount,
                UploadsPlaylistId = channel.UploadsPlaylistId,
                Country = channel.Country,
                PublishedAt = channel.PublishedAt,
                LastSyncedAt = channel.LastSyncedAt,
                MediaType = channel.MediaType,
                Status = channel.Status,
                DateAdded = channel.DateAdded,
                DateCompleted = channel.DateCompleted,
                Rating = channel.Rating,
                Notes = channel.Notes,
                RelatedNotes = channel.RelatedNotes,
                Topics = channel.Topics.Select(t => t.Name).ToArray(),
                Genres = channel.Genres.Select(g => g.Name).ToArray(),
                MixlistIds = channel.Mixlists.Select(m => m.Id).ToArray(),
                VideoCountInDb = channel.Videos.Count
            };
        }
    }
}

