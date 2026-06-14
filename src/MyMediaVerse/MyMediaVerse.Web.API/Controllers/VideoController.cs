using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService _videoService;
        private readonly ILogger<VideoController> _logger;

        public VideoController(IVideoService videoService, ILogger<VideoController> logger)
        {
            _videoService = videoService;
            _logger = logger;
        }

        // GET: api/video
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VideoResponseDto>>> GetAllVideos()
        {
            try
            {
                var videos = await _videoService.GetAllVideosAsync();
                var response = videos.Select(MapToResponseDto).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all videos");
                return StatusCode(500, new { error = "Failed to retrieve videos", details = ex.Message });
            }
        }

        // GET: api/video/series
        [HttpGet("series")]
        public async Task<ActionResult<IEnumerable<VideoResponseDto>>> GetVideoSeries()
        {
            try
            {
                var series = await _videoService.GetVideoSeriesAsync();
                var response = series.Select(MapToResponseDto).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving video series");
                return StatusCode(500, new { error = "Failed to retrieve video series", details = ex.Message });
            }
        }

        // GET: api/video/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VideoResponseDto>> GetVideo(Guid id)
        {
            try
            {
                var video = await _videoService.GetVideoByIdAsync(id);

                if (video == null)
                {
                    return NotFound($"Video with ID {id} not found.");
                }

                return Ok(MapToResponseDto(video));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving video with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve video", details = ex.Message });
            }
        }

        // GET: api/video/{id}/playlists
        [HttpGet("{id}/playlists")]
        public async Task<ActionResult<IEnumerable<VideoPlaylistInfoDto>>> GetPlaylistsForVideo(Guid id)
        {
            try
            {
                var playlists = await _videoService.GetPlaylistsForVideoAsync(id);
                var response = playlists.Select(p => new VideoPlaylistInfoDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Thumbnail = p.Thumbnail,
                    PlaylistExternalId = p.PlaylistExternalId,
                    VideoCount = p.VideoCount
                }).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving playlists for video {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve playlists for video", details = ex.Message });
            }
        }

        // GET: api/video/channel/{channelId}
        [HttpGet("channel/{channelId}")]
        public async Task<ActionResult<IEnumerable<VideoResponseDto>>> GetVideosByChannel(Guid channelId)
        {
            try
            {
                var videos = await _videoService.GetVideosByChannelAsync(channelId);
                var response = videos.Select(MapToResponseDto).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving videos by channel {ChannelId}", channelId);
                return StatusCode(500, new { error = "Failed to retrieve videos by channel", details = ex.Message });
            }
        }

        // POST: api/video
        [HttpPost]
        public async Task<ActionResult<VideoResponseDto>> CreateVideo([FromBody] CreateVideoDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var video = await _videoService.CreateVideoAsync(dto);
                var response = MapToResponseDto(video);
                return CreatedAtAction(nameof(GetVideo), new { id = video.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating video");
                return StatusCode(500, new { error = "Failed to create video", details = ex.Message });
            }
        }

        // PUT: api/video/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<VideoResponseDto>> UpdateVideo(Guid id, [FromBody] CreateVideoDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var video = await _videoService.UpdateVideoAsync(id, dto);
                var response = MapToResponseDto(video);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Video not found for update: {Id}", id);
                return NotFound($"Video with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating video with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update video", details = ex.Message });
            }
        }

        // DELETE: api/video/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVideo(Guid id)
        {
            try
            {
                var result = await _videoService.DeleteVideoAsync(id);
                
                if (!result)
                {
                    return NotFound($"Video with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting video with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete video", details = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to map Video entity to VideoResponseDto
        /// </summary>
        private VideoResponseDto MapToResponseDto(Video video)
        {
            return new VideoResponseDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                MediaType = video.MediaType,
                Status = video.Status,
                DateAdded = video.DateAdded,
                Link = video.Link,
                Thumbnail = video.GetEffectiveThumbnail(),
                VideoType = video.VideoType,
                ParentVideoId = video.ParentVideoId,
                Platform = video.Platform,
                ChannelId = video.ChannelId,
                Channel = video.Channel != null ? new YouTubeChannelInfoDto
                {
                    Id = video.Channel.Id,
                    Title = video.Channel.Title,
                    Thumbnail = video.Channel.Thumbnail,
                    ChannelExternalId = video.Channel.ChannelExternalId,
                    CustomUrl = video.Channel.CustomUrl,
                    SubscriberCount = video.Channel.SubscriberCount
                } : null,
                LengthInSeconds = video.LengthInSeconds,
                ExternalId = video.ExternalId,
                Rating = video.Rating,
                OwnershipStatus = video.OwnershipStatus,
                DateCompleted = video.DateCompleted,
                Notes = video.Notes,
                RelatedNotes = video.RelatedNotes,
                Topics = video.Topics.Select(t => t.Name).ToArray(),
                Genres = video.Genres.Select(g => g.Name).ToArray()
            };
        }
    }
}
