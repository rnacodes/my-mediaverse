using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Web.API.Extensions;

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
                var response = videos.Select(v => v.ToResponseDto()).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all videos");
                return StatusCode(500, new { error = "Failed to retrieve videos", details = ex.Message });
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

                return Ok(video.ToResponseDto());
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
                var response = videos.Select(v => v.ToResponseDto()).ToList();
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

                var result = await _videoService.CreateVideoAsync(dto);
                var response = result.Video.ToResponseDto();

                // A video already in the library is returned as it is stored, not created again.
                return result.Created
                    ? CreatedAtAction(nameof(GetVideo), new { id = result.Video.Id }, response)
                    : Ok(response);
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
                var response = video.ToResponseDto();
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Video not found for update: {Id}", id);
                return NotFound($"Video with ID {id} not found");
            }
            catch (VideoIdentityConflictException ex)
            {
                _logger.LogWarning(ex, "Video {Id} was given an id that belongs to another video", id);
                return Conflict(new { error = ex.Message });
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
    }
}
