using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using System.Text.Json;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PodcastController : ControllerBase
    {
        private readonly IPodcastService _podcastService;
        private readonly IPodcastMappingService _podcastMappingService;
        private readonly IListenNotesService _listenNotesService;
        private readonly IPodcastOpmlImportService _opmlImportService;
        private readonly ILogger<PodcastController> _logger;

        public PodcastController(
            IPodcastService podcastService,
            IPodcastMappingService podcastMappingService,
            IListenNotesService listenNotesService,
            IPodcastOpmlImportService opmlImportService,
            ILogger<PodcastController> logger)
        {
            _podcastService = podcastService;
            _podcastMappingService = podcastMappingService;
            _listenNotesService = listenNotesService;
            _opmlImportService = opmlImportService;
            _logger = logger;
        }

        // Helper method to map PodcastSeries to PodcastSeriesResponseDto
        private PodcastSeriesResponseDto MapToResponseDto(PodcastSeries series)
        {
            return new PodcastSeriesResponseDto
            {
                Id = series.Id,
                Title = series.Title,
                Description = series.Description,
                MediaType = series.MediaType,
                Status = series.Status,
                DateAdded = series.DateAdded,
                DateCompleted = series.DateCompleted,
                Rating = series.Rating,
                Link = series.Link,
                Thumbnail = series.Thumbnail,
                Publisher = series.Publisher,
                ExternalId = series.ExternalId,
                RssFeedUrl = series.RssFeedUrl,
                ApplePodcastsId = series.ApplePodcastsId,
                IsSubscribed = series.IsSubscribed,
                LastSyncDate = series.LastSyncDate,
                TotalEpisodes = series.TotalEpisodes,
                EpisodeCount = series.EpisodeCount,
                Topics = series.Topics?.Select(t => t.Name).ToList() ?? new List<string>(),
                Genres = series.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                MixlistIds = series.Mixlists?.Select(m => m.Id).ToList() ?? new List<Guid>(),
                PodcastType = "Series"
            };
        }

        // Helper method to map PodcastEpisode to PodcastEpisodeResponseDto
        private PodcastEpisodeResponseDto MapToResponseDto(PodcastEpisode episode)
        {
            return new PodcastEpisodeResponseDto
            {
                Id = episode.Id,
                Title = episode.Title,
                Description = episode.Description,
                MediaType = episode.MediaType,
                Status = episode.Status,
                DateAdded = episode.DateAdded,
                DateCompleted = episode.DateCompleted,
                Rating = episode.Rating,
                Link = episode.Link,
                Thumbnail = episode.GetEffectiveThumbnail(),
                SeriesId = episode.SeriesId,
                SeriesTitle = episode.Series?.Title,
                AudioLink = episode.AudioLink,
                ReleaseDate = episode.ReleaseDate,
                DurationInSeconds = episode.DurationInSeconds,
                EpisodeNumber = episode.EpisodeNumber,
                SeasonNumber = episode.SeasonNumber,
                ExternalId = episode.ExternalId,
                Publisher = episode.Publisher,
                Topics = episode.Topics?.Select(t => t.Name).ToList() ?? new List<string>(),
                Genres = episode.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                PodcastType = "Episode"
            };
        }

        // ============ PODCAST SERIES ENDPOINTS ============

        // GET: api/podcast/series
        [HttpGet("series")]
        public async Task<ActionResult<IEnumerable<PodcastSeriesResponseDto>>> GetPodcastSeries()
        {
            try
            {
                var series = await _podcastService.GetAllPodcastSeriesAsync();
                var response = series.Select(s => MapToResponseDto(s)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving podcast series");
                return StatusCode(500, new { error = "Failed to retrieve podcast series", details = ex.Message });
            }
        }

        // GET: api/podcast/series/search
        [HttpGet("series/search")]
        public async Task<ActionResult<IEnumerable<PodcastSeriesResponseDto>>> SearchPodcastSeries([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                var series = await _podcastService.SearchPodcastSeriesAsync(query);
                var response = series.Select(s => MapToResponseDto(s)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching podcast series with query: {Query}", query);
                return StatusCode(500, new { error = "Failed to search podcast series", details = ex.Message });
            }
        }

        // GET: api/podcast/series/{id}
        [HttpGet("series/{id}")]
        public async Task<ActionResult<PodcastSeriesResponseDto>> GetPodcastSeries(Guid id)
        {
            try
            {
                var series = await _podcastService.GetPodcastSeriesByIdAsync(id);

                if (series == null)
                {
                    return NotFound($"Podcast series with ID {id} not found.");
                }

                var response = MapToResponseDto(series);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving podcast series with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve podcast series", details = ex.Message });
            }
        }

        // POST: api/podcast/series
        [HttpPost("series")]
        public async Task<IActionResult> CreatePodcastSeries([FromBody] CreatePodcastSeriesDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Podcast series data is required");
                }

                var series = await _podcastService.CreatePodcastSeriesAsync(dto);
                var response = MapToResponseDto(series);
                return CreatedAtAction(nameof(GetPodcastSeries), new { id = series.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating podcast series");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating podcast series");
                return StatusCode(500, new { error = "Failed to create podcast series", details = ex.Message });
            }
        }

        // PUT: api/podcast/series/{id}
        [HttpPut("series/{id}")]
        public async Task<IActionResult> UpdatePodcastSeries(Guid id, [FromBody] CreatePodcastSeriesDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Podcast series data is required");
                }

                var series = await _podcastService.UpdatePodcastSeriesAsync(id, dto);
                var response = MapToResponseDto(series);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound($"Podcast series with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating podcast series with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update podcast series", details = ex.Message });
            }
        }

        // DELETE: api/podcast/series/{id}
        [HttpDelete("series/{id}")]
        public async Task<IActionResult> DeletePodcastSeries(Guid id)
        {
            try
            {
                var deleted = await _podcastService.DeletePodcastSeriesAsync(id);
                
                if (!deleted)
                {
                    return NotFound($"Podcast series with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting podcast series with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete podcast series", details = ex.Message });
            }
        }

        // POST: api/podcast/series/{seriesId}/subscribe
        [HttpPost("series/{seriesId}/subscribe")]
        public async Task<IActionResult> SubscribeToPodcastSeries(Guid seriesId)
        {
            try
            {
                var series = await _podcastService.SubscribeToPodcastSeriesAsync(seriesId);
                
                if (series == null)
                {
                    return NotFound($"Podcast series with ID {seriesId} not found.");
                }

                _logger.LogInformation("Subscribed to podcast series: {Title} (ID: {SeriesId})", series.Title, seriesId);

                return Ok(new { message = "Successfully subscribed to podcast series", seriesId, isSubscribed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while subscribing to podcast series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to subscribe to podcast series", details = ex.Message });
            }
        }

        // POST: api/podcast/series/{seriesId}/unsubscribe
        [HttpPost("series/{seriesId}/unsubscribe")]
        public async Task<IActionResult> UnsubscribeFromPodcastSeries(Guid seriesId)
        {
            try
            {
                var series = await _podcastService.UnsubscribeFromPodcastSeriesAsync(seriesId);
                
                if (series == null)
                {
                    return NotFound($"Podcast series with ID {seriesId} not found.");
                }

                _logger.LogInformation("Unsubscribed from podcast series: {Title} (ID: {SeriesId})", series.Title, seriesId);

                return Ok(new { message = "Successfully unsubscribed from podcast series", seriesId, isSubscribed = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while unsubscribing from podcast series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to unsubscribe from podcast series", details = ex.Message });
            }
        }

        // GET: api/podcast/series/subscriptions
        [HttpGet("series/subscriptions")]
        public async Task<ActionResult<IEnumerable<PodcastSeriesResponseDto>>> GetSubscribedPodcastSeries()
        {
            try
            {
                var subscribedSeries = await _podcastService.GetSubscribedPodcastSeriesAsync();
                var response = subscribedSeries.Select(s => MapToResponseDto(s)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving subscribed podcast series");
                return StatusCode(500, new { error = "Failed to retrieve subscribed podcast series", details = ex.Message });
            }
        }

        // POST: api/podcast/series/{seriesId}/sync
        [HttpPost("series/{seriesId}/sync")]
        public async Task<IActionResult> SyncPodcastSeriesEpisodes(Guid seriesId)
        {
            try
            {
                var result = await _podcastService.SyncPodcastSeriesEpisodesAsync(seriesId);
                
                if (result == null)
                {
                    return NotFound($"Podcast series with ID {seriesId} not found or has no external ID.");
                }

                _logger.LogInformation("Synced episodes for podcast series: {Title} (ID: {SeriesId}). New episodes: {NewEpisodesCount}", 
                    result.SeriesTitle, seriesId, result.NewEpisodesCount);

                return Ok(new { 
                    message = "Successfully synced podcast series episodes", 
                    seriesId, 
                    seriesTitle = result.SeriesTitle,
                    newEpisodesCount = result.NewEpisodesCount,
                    totalEpisodesCount = result.TotalEpisodesCount,
                    lastSyncDate = result.LastSyncDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing episodes for podcast series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to sync podcast series episodes", details = ex.Message });
            }
        }

        // POST: api/podcast/series/from-api/{podcastId}
        [HttpPost("series/from-api/{podcastId}")]
        public async Task<IActionResult> ImportPodcastSeriesFromApi(string podcastId)
        {
            try
            {
                _logger.LogInformation("Starting podcast series import from API for ID: {PodcastId}", podcastId);

                var series = await _listenNotesService.ImportPodcastSeriesAsync(podcastId);
                
                _logger.LogInformation("Successfully imported podcast series: {Title} with ID: {Id}", series.Title, series.Id);
                var response = MapToResponseDto(series);
                return CreatedAtAction(nameof(GetPodcastSeries), new { id = series.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast series from API with ID: {PodcastId}", podcastId);
                return StatusCode(500, new { error = "Failed to import podcast series from API", details = ex.Message });
            }
        }

        // POST: api/podcast/series/from-api/by-name
        [HttpPost("series/from-api/by-name")]
        public async Task<IActionResult> ImportPodcastSeriesByName([FromBody] ImportPodcastByNameDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.PodcastName))
                {
                    return BadRequest("Podcast name is required");
                }

                _logger.LogInformation("Searching for podcast series by name: {PodcastName}", dto.PodcastName);

                var series = await _listenNotesService.ImportPodcastSeriesByNameAsync(dto.PodcastName);
                if (series == null)
                {
                    return NotFound($"No podcast series found with name: {dto.PodcastName}");
                }

                _logger.LogInformation("Successfully imported podcast series: {Title} with ID: {Id}", series.Title, series.Id);
                var response = MapToResponseDto(series);
                return CreatedAtAction(nameof(GetPodcastSeries), new { id = series.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast series by name: {PodcastName}", dto?.PodcastName);
                return StatusCode(500, new { error = "Failed to import podcast series by name", details = ex.Message });
            }
        }

        // POST: api/podcast/import-opml
        [HttpPost("import-opml")]
        public async Task<IActionResult> ImportPodcastsFromOpml(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".opml", StringComparison.OrdinalIgnoreCase) &&
                    !file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "File must be an OPML export (.opml or .xml)" });
                }

                _logger.LogInformation("Processing podcast OPML import: {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                var result = await _opmlImportService.ImportFromOpmlAsync(stream);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing podcast OPML import");
                return StatusCode(500, new { error = "Failed to process podcast OPML import", details = ex.Message });
            }
        }

        // ============ PODCAST EPISODE ENDPOINTS ============

        // GET: api/podcast/series/{seriesId}/episodes
        [HttpGet("series/{seriesId}/episodes")]
        public async Task<ActionResult<IEnumerable<PodcastEpisodeResponseDto>>> GetEpisodesBySeries(Guid seriesId)
        {
            try
            {
                var episodes = await _podcastService.GetEpisodesBySeriesIdAsync(seriesId);

                var response = episodes.Select(MapToResponseDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving episodes for series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to retrieve episodes", details = ex.Message });
            }
        }

        // GET: api/podcast/episodes/{id}
        [HttpGet("episodes/{id}")]
        public async Task<ActionResult<PodcastEpisodeResponseDto>> GetPodcastEpisode(Guid id)
        {
            try
            {
                var episode = await _podcastService.GetPodcastEpisodeByIdAsync(id);

                if (episode == null)
                {
                    return NotFound($"Podcast episode with ID {id} not found.");
                }

                var response = MapToResponseDto(episode);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving podcast episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve podcast episode", details = ex.Message });
            }
        }

        // POST: api/podcast/episodes
        [HttpPost("episodes")]
        public async Task<IActionResult> CreatePodcastEpisode([FromBody] CreatePodcastEpisodeDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Podcast episode data is required");
                }

                var episode = await _podcastService.CreatePodcastEpisodeAsync(dto);

                var response = MapToResponseDto(episode);

                return CreatedAtAction(nameof(GetPodcastEpisode), new { id = episode.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating podcast episode");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating podcast episode");
                return StatusCode(500, new { error = "Failed to create podcast episode", details = ex.Message });
            }
        }

        // PUT: api/podcast/episodes/{id}
        [HttpPut("episodes/{id}")]
        public async Task<IActionResult> UpdatePodcastEpisode(Guid id, [FromBody] CreatePodcastEpisodeDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Podcast episode data is required");
                }

                var episode = await _podcastService.UpdatePodcastEpisodeAsync(id, dto);

                var response = MapToResponseDto(episode);

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound($"Podcast episode with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating podcast episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update podcast episode", details = ex.Message });
            }
        }

        // DELETE: api/podcast/episodes/{id}
        [HttpDelete("episodes/{id}")]
        public async Task<IActionResult> DeletePodcastEpisode(Guid id)
        {
            try
            {
                var deleted = await _podcastService.DeletePodcastEpisodeAsync(id);
                
                if (!deleted)
                {
                    return NotFound($"Podcast episode with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting podcast episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete podcast episode", details = ex.Message });
            }
        }

        // GET: api/podcast/episodes
        [HttpGet("episodes")]
        public async Task<ActionResult<IEnumerable<PodcastEpisodeResponseDto>>> GetAllPodcastEpisodes()
        {
            try
            {
                var episodes = await _podcastService.GetAllPodcastEpisodesAsync();

                var response = episodes.Select(MapToResponseDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all podcast episodes");
                return StatusCode(500, new { error = "Failed to retrieve podcast episodes", details = ex.Message });
            }
        }

        // POST: api/podcast/episodes/from-api/{episodeId}
        [HttpPost("episodes/from-api/{episodeId}")]
        public async Task<IActionResult> ImportPodcastEpisodeFromApi(string episodeId, [FromQuery] Guid seriesId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(episodeId))
                {
                    return BadRequest("Episode ID is required");
                }

                if (seriesId == Guid.Empty)
                {
                    return BadRequest("Series ID is required");
                }

                _logger.LogInformation("Importing podcast episode from API - Episode ID: {EpisodeId}, Series ID: {SeriesId}", 
                    episodeId, seriesId);

                var episode = await _listenNotesService.ImportPodcastEpisodeAsync(episodeId, seriesId);

                var response = MapToResponseDto(episode);

                _logger.LogInformation("Successfully imported podcast episode: {Title} (ID: {Id})", episode.Title, episode.Id);

                return CreatedAtAction(nameof(GetPodcastEpisode), new { id = episode.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast episode from API - Episode ID: {EpisodeId}, Series ID: {SeriesId}", 
                    episodeId, seriesId);
                return StatusCode(500, new { error = "Failed to import podcast episode from API", details = ex.Message });
            }
        }
    }
}
