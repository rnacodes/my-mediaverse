using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PodcastController : ControllerBase
    {
        private readonly IPodcastService _podcastService;
        private readonly IPodcastOpmlImportService _opmlImportService;
        private readonly IPodcastFeedImportService _feedImportService;
        private readonly IPodcastEpisodeSyncService _episodeSyncService;
        private readonly IPodcastFeedBrowserService _feedBrowserService;
        private readonly IPodcastEnrichmentService _enrichmentService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<PodcastController> _logger;

        private const long MaxOpmlFileBytes = 10 * 1024 * 1024;

        public PodcastController(
            IPodcastService podcastService,
            IPodcastOpmlImportService opmlImportService,
            IPodcastFeedImportService feedImportService,
            IPodcastEpisodeSyncService episodeSyncService,
            IPodcastFeedBrowserService feedBrowserService,
            IPodcastEnrichmentService enrichmentService,
            IImportReindexService importReindexService,
            ILogger<PodcastController> logger)
        {
            _podcastService = podcastService;
            _opmlImportService = opmlImportService;
            _feedImportService = feedImportService;
            _episodeSyncService = episodeSyncService;
            _feedBrowserService = feedBrowserService;
            _enrichmentService = enrichmentService;
            _importReindexService = importReindexService;
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
                FeedGuid = series.FeedGuid,
                PodcastIndexId = series.PodcastIndexId,
                MetadataSource = series.MetadataSource,
                Language = series.Language,
                EnrichedAt = series.EnrichedAt,
                LastEnrichmentAttemptAt = series.LastEnrichmentAttemptAt,
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
                RssGuid = episode.RssGuid,
                Publisher = episode.Publisher,
                Topics = episode.Topics?.Select(t => t.Name).ToList() ?? new List<string>(),
                Genres = episode.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                PodcastType = "Episode"
            };
        }

        // 201 with a Location header when the series was inserted; 200 when an existing row with the
        // same identity was returned instead.
        private IActionResult CreatedOrExisting(PodcastSeriesCreationResult result)
        {
            var response = MapToResponseDto(result.Series);
            return result.Created
                ? CreatedAtAction(nameof(GetPodcastSeries), new { id = result.Series.Id }, response)
                : Ok(response);
        }

        private IActionResult CreatedOrExisting(PodcastEpisodeCreationResult result)
        {
            var response = MapToResponseDto(result.Episode);
            return result.Created
                ? CreatedAtAction(nameof(GetPodcastEpisode), new { id = result.Episode.Id }, response)
                : Ok(response);
        }

        // 503 when our own Apple rate limit refused the call (try again shortly); 502 when a directory failed.
        private ObjectResult DirectoryUnavailable(HttpRequestException ex) =>
            ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                ? StatusCode(503, new { error = "The podcast directory is busy; try again in a minute." })
                : StatusCode(502, new { error = "The podcast directory could not be reached." });

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
                return StatusCode(500, new { error = "Failed to retrieve podcast series" });
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
                    return BadRequest(new { error = "Query parameter is required" });
                }

                var series = await _podcastService.SearchPodcastSeriesAsync(query);
                var response = series.Select(s => MapToResponseDto(s)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching podcast series with query: {Query}", query);
                return StatusCode(500, new { error = "Failed to search podcast series" });
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
                    return NotFound(new { error = $"Podcast series with ID {id} not found." });
                }

                var response = MapToResponseDto(series);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving podcast series with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve podcast series" });
            }
        }

        // POST: api/podcast/series
        // Returns 201 for a new series, 200 with the existing row when the feed URL, a directory id,
        // or title + publisher already identify one.
        [HttpPost("series")]
        public async Task<IActionResult> CreatePodcastSeries([FromBody] CreatePodcastSeriesDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Podcast series data is required" });
                }

                var result = await _podcastService.CreatePodcastSeriesAsync(dto);
                return CreatedOrExisting(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating podcast series");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating podcast series");
                return StatusCode(500, new { error = "Failed to create podcast series" });
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
                    return BadRequest(new { error = "Podcast series data is required" });
                }

                var series = await _podcastService.UpdatePodcastSeriesAsync(id, dto);
                var response = MapToResponseDto(series);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Podcast series with ID {id} not found." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // The new feed URL or Apple id already belongs to another series.
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating podcast series with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update podcast series" });
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
                    return NotFound(new { error = $"Podcast series with ID {id} not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting podcast series with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete podcast series" });
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
                    return NotFound(new { error = $"Podcast series with ID {seriesId} not found." });
                }

                _logger.LogInformation("Subscribed to podcast series: {Title} (ID: {SeriesId})", series.Title, seriesId);

                return Ok(new { message = "Successfully subscribed to podcast series", seriesId, isSubscribed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while subscribing to podcast series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to subscribe to podcast series" });
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
                    return NotFound(new { error = $"Podcast series with ID {seriesId} not found." });
                }

                _logger.LogInformation("Unsubscribed from podcast series: {Title} (ID: {SeriesId})", series.Title, seriesId);

                return Ok(new { message = "Successfully unsubscribed from podcast series", seriesId, isSubscribed = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while unsubscribing from podcast series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to unsubscribe from podcast series" });
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
                return StatusCode(500, new { error = "Failed to retrieve subscribed podcast series" });
            }
        }

        // POST: api/podcast/series/{seriesId}/sync
        // Imports new episodes from the series' RSS feed. 200 with the reporting-contract result when the
        // run completed (warnings included); 500 with the same body when it aborted.
        // Explicit [Authorize]: this endpoint writes to the library and fetches the feed per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("series/{seriesId}/sync")]
        public async Task<ActionResult<PodcastEpisodeSyncResultDto>> SyncPodcastSeriesEpisodes(Guid seriesId)
        {
            try
            {
                var result = await _episodeSyncService.SyncSeriesAsync(seriesId, HttpContext.RequestAborted);
                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                var changed = result.CreatedCount + result.UpdatedCount;
                if (changed > 0)
                {
                    await _importReindexService.ReindexAfterImportAsync(changed, "podcast episode sync");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Podcast series with ID {seriesId} not found." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing episodes for podcast series {SeriesId}", seriesId);
                return StatusCode(500, new PodcastEpisodeSyncResultDto
                {
                    SeriesId = seriesId,
                    Success = false,
                    StartedAt = DateTime.UtcNow,
                    ErrorMessage = "Episode sync failed."
                });
            }
        }

        // POST: api/podcast/series/sync-all
        // Syncs every subscribed series with a feed (the daily N8N job). One series failing is reported
        // in the body, not as an error status; 500 only when the run itself aborted.
        [Authorize]
        [HttpPost("series/sync-all")]
        public async Task<ActionResult<PodcastSyncAllResultDto>> SyncAllSubscribedSeries()
        {
            try
            {
                var result = await _episodeSyncService.SyncSubscribedAsync(HttpContext.RequestAborted);
                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                var changed = result.CreatedCount + result.UpdatedCount;
                if (changed > 0)
                {
                    await _importReindexService.ReindexAfterImportAsync(changed, "podcast sync-all");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing all subscribed podcast series");
                return StatusCode(500, new PodcastSyncAllResultDto
                {
                    Success = false,
                    StartedAt = DateTime.UtcNow,
                    ErrorMessage = "Podcast sync-all failed."
                });
            }
        }

        // POST: api/podcast/series/{seriesId}/enrich?force=
        // Fills one series from its feed. Without force an already-filled series is left alone; with it
        // the feed's values replace what is stored, which is how a series with stale data is repaired.
        // Explicit [Authorize]: this endpoint writes to the library and fetches the feed per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("series/{seriesId}/enrich")]
        public async Task<ActionResult<PodcastEnrichmentResult>> EnrichSeries(Guid seriesId, [FromQuery] bool force = false)
        {
            try
            {
                var result = await _enrichmentService.EnrichSeriesAsync(seriesId, force, HttpContext.RequestAborted);
                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                if (result.EnrichedCount > 0)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(seriesId, "podcast enrichment");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Podcast series with ID {seriesId} not found." });
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while filling podcast series {SeriesId} from its feed", seriesId);
                return StatusCode(500, new PodcastEnrichmentResult
                {
                    Success = false,
                    StartedAt = DateTime.UtcNow,
                    TotalProcessed = 1,
                    ErrorMessage = "Podcast enrichment failed."
                });
            }
        }

        // POST: api/podcast/series/from-feed  { feedUrl?, applePodcastsId? }
        // The directory resolves an Apple id to its feed, the feed fills the series. 201 for a new series
        // (a stub with warningMessage when the feed could not be read), 200 when one already matches.
        // Explicit [Authorize]: this endpoint writes to the library and fetches external sources per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("series/from-feed")]
        public async Task<IActionResult> ImportSeriesFromFeed([FromBody] ImportPodcastFromFeedDto dto)
        {
            try
            {
                var result = await _feedImportService.ImportSeriesFromFeedAsync(
                    dto?.FeedUrl, dto?.ApplePodcastsId, HttpContext.RequestAborted);

                if (result.Created)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(result.Series.Id, "podcast feed import");
                }

                var response = new PodcastFeedImportResultDto
                {
                    Series = MapToResponseDto(result.Series),
                    Created = result.Created,
                    FeedRead = result.FeedRead,
                    WarningMessage = result.WarningMessage
                };

                return result.Created
                    ? CreatedAtAction(nameof(GetPodcastSeries), new { id = result.Series.Id }, response)
                    : Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Podcast directory lookup failed for Apple id {AppleId}", dto?.ApplePodcastsId);
                return DirectoryUnavailable(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast series from feed {FeedUrl}", dto?.FeedUrl);
                return StatusCode(500, new { error = "Failed to import podcast series from feed" });
            }
        }

        // GET: api/podcast/directory/search?term=&limit=
        // Searches Apple Podcasts. Each result carries existingSeriesId when the show is already in the library.
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpGet("directory/search")]
        public async Task<ActionResult<IEnumerable<PodcastDirectorySearchResultDto>>> SearchDirectory(
            [FromQuery] string? term, [FromQuery] int limit = PodcastFeedImportService.DefaultSearchLimit)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest(new { error = "The term parameter is required." });
            }

            try
            {
                var hits = await _feedImportService.SearchDirectoryAsync(term, limit, HttpContext.RequestAborted);
                return Ok(hits.Select(hit => new PodcastDirectorySearchResultDto
                {
                    Title = hit.Podcast.Title,
                    Publisher = hit.Podcast.Publisher,
                    FeedUrl = hit.Podcast.FeedUrl,
                    ApplePodcastsId = hit.Podcast.ApplePodcastsId,
                    PodcastIndexId = hit.Podcast.PodcastIndexId,
                    ArtworkUrl = hit.Podcast.ArtworkUrl,
                    Genres = hit.Podcast.Genres.ToList(),
                    EpisodeCount = hit.Podcast.EpisodeCount,
                    StoreUrl = hit.Podcast.StoreUrl,
                    LatestReleaseDate = hit.Podcast.LatestReleaseDate,
                    Source = hit.Podcast.Source,
                    ExistingSeriesId = hit.ExistingSeriesId
                }).ToList());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Podcast directory search failed for {Term}", term);
                return DirectoryUnavailable(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching the podcast directory for {Term}", term);
                return StatusCode(500, new { error = "Failed to search the podcast directory" });
            }
        }

        // POST: api/podcast/series/from-opml
        // Stubs land with feed URL + Apple id only (no external calls); duplicates are skipped.
        [Authorize]
        [HttpPost("series/from-opml")]
        public async Task<IActionResult> ImportPodcastsFromOpml(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (file.Length > MaxOpmlFileBytes)
                {
                    return BadRequest(new { error = "The OPML file must be 10 MB or smaller." });
                }

                if (!file.FileName.EndsWith(".opml", StringComparison.OrdinalIgnoreCase) &&
                    !file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "File must be an OPML export (.opml or .xml)" });
                }

                _logger.LogInformation("Processing podcast OPML import: {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                var result = await _opmlImportService.ImportFromOpmlAsync(stream);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                if (result.CreatedCount > 0)
                {
                    await _importReindexService.ReindexAfterImportAsync(result.CreatedCount, "podcast OPML");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing podcast OPML import");
                return StatusCode(500, new { error = "Failed to process podcast OPML import" });
            }
        }

        // ============ PODCAST EPISODE ENDPOINTS ============

        // GET: api/podcast/series/{seriesId}/feed-episodes?offset=&limit=&refresh=
        // Pages through the series' live feed (cached briefly), marking items already in the library.
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpGet("series/{seriesId}/feed-episodes")]
        public async Task<ActionResult<PodcastFeedEpisodesPageDto>> GetFeedEpisodes(
            Guid seriesId, [FromQuery] int offset = 0, [FromQuery] int limit = 20, [FromQuery] bool refresh = false)
        {
            try
            {
                return Ok(await _feedBrowserService.GetFeedEpisodesAsync(seriesId, offset, limit, refresh, HttpContext.RequestAborted));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Podcast series with ID {seriesId} not found." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (PodcastFeedException ex)
            {
                return StatusCode(502, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading the feed episodes for podcast series {SeriesId}", seriesId);
                return StatusCode(500, new { error = "Failed to read the podcast feed" });
            }
        }

        // POST: api/podcast/episodes/from-feed  { seriesId, guid?, audioUrl? }
        // Imports one item of the series' feed. 201 for a new episode, 200 when it is already in the library.
        // Explicit [Authorize]: this endpoint writes to the library and fetches the feed per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("episodes/from-feed")]
        public async Task<IActionResult> ImportEpisodeFromFeed([FromBody] ImportPodcastEpisodeFromFeedDto dto)
        {
            try
            {
                if (dto == null || dto.SeriesId == Guid.Empty)
                {
                    return BadRequest(new { error = "A series id is required." });
                }

                var result = await _feedBrowserService.ImportEpisodeFromFeedAsync(
                    dto.SeriesId, dto.Guid, dto.AudioUrl, HttpContext.RequestAborted);

                if (result.Created)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(result.Episode.Id, "podcast episode import");
                }

                return CreatedOrExisting(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (PodcastFeedException ex)
            {
                return StatusCode(502, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing an episode from the feed of podcast series {SeriesId}", dto?.SeriesId);
                return StatusCode(500, new { error = "Failed to import the podcast episode" });
            }
        }

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
                return StatusCode(500, new { error = "Failed to retrieve episodes" });
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
                    return NotFound(new { error = $"Podcast episode with ID {id} not found." });
                }

                var response = MapToResponseDto(episode);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving podcast episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve podcast episode" });
            }
        }

        // POST: api/podcast/episodes
        // Returns 201 for a new episode, 200 with the existing row when the feed guid, external id,
        // audio URL, or title + release date already identify one in the series.
        [HttpPost("episodes")]
        public async Task<IActionResult> CreatePodcastEpisode([FromBody] CreatePodcastEpisodeDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Podcast episode data is required" });
                }

                var result = await _podcastService.CreatePodcastEpisodeAsync(dto);
                return CreatedOrExisting(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating podcast episode");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating podcast episode");
                return StatusCode(500, new { error = "Failed to create podcast episode" });
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
                    return BadRequest(new { error = "Podcast episode data is required" });
                }

                var episode = await _podcastService.UpdatePodcastEpisodeAsync(id, dto);

                var response = MapToResponseDto(episode);

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = $"Podcast episode with ID {id} not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating podcast episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update podcast episode" });
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
                    return NotFound(new { error = $"Podcast episode with ID {id} not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting podcast episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete podcast episode" });
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
                return StatusCode(500, new { error = "Failed to retrieve podcast episodes" });
            }
        }
    }
}
