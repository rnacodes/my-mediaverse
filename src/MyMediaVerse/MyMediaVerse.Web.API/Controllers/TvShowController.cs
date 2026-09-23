using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.DTOs;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;
using System.Text.Json;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TvShowController : ControllerBase
    {
        private readonly ITvShowService _tvShowService;
        private readonly ITvShowMappingService _tvShowMappingService;
        private readonly ILogger<TvShowController> _logger;
        private readonly ITmdbService _tmdbService;
        private readonly IImportReindexService _importReindexService;
        private readonly ITvEpisodeImportService _episodeImportService;

        public TvShowController(
            ITvShowService tvShowService,
            ITvShowMappingService tvShowMappingService,
            ILogger<TvShowController> logger,
            ITmdbService tmdbService,
            IImportReindexService importReindexService,
            ITvEpisodeImportService episodeImportService)
        {
            _importReindexService = importReindexService;
            _tvShowService = tvShowService;
            _tvShowMappingService = tvShowMappingService;
            _logger = logger;
            _tmdbService = tmdbService;
            _episodeImportService = episodeImportService;
        }

        // GET: api/tvshow
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TvShowResponseDto>>> GetAllTvShows()
        {
            try
            {
                var tvShows = await _tvShowService.GetAllTvShowsAsync();
                var response = await Task.WhenAll(tvShows.Select(t => _tvShowMappingService.MapToResponseDtoAsync(t)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all TV shows");
                return StatusCode(500, new { error = "Failed to retrieve TV shows", details = ex.Message });
            }
        }

        // GET: api/tvshow/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TvShowResponseDto>> GetTvShow(Guid id)
        {
            try
            {
                var tvShow = await _tvShowService.GetTvShowByIdAsync(id);
                if (tvShow == null)
                {
                    return NotFound($"TV show with ID {id} not found.");
                }

                var response = await _tvShowMappingService.MapToResponseDtoAsync(tvShow);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve TV show", details = ex.Message });
            }
        }

        // GET: api/tvshow/by-creator/{creator}
        [HttpGet("by-creator/{creator}")]
        public async Task<ActionResult<IEnumerable<TvShowResponseDto>>> GetTvShowsByCreator(string creator)
        {
            try
            {
                var tvShows = await _tvShowService.GetTvShowsByCreatorAsync(creator);
                var response = await Task.WhenAll(tvShows.Select(t => _tvShowMappingService.MapToResponseDtoAsync(t)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV shows by creator: {Creator}", creator);
                return StatusCode(500, new { error = "Failed to retrieve TV shows by creator", details = ex.Message });
            }
        }

        // GET: api/tvshow/by-year/{year}
        [HttpGet("by-year/{year}")]
        public async Task<ActionResult<IEnumerable<TvShowResponseDto>>> GetTvShowsByYear(int year)
        {
            try
            {
                var tvShows = await _tvShowService.GetTvShowsByYearAsync(year);
                var response = await Task.WhenAll(tvShows.Select(t => _tvShowMappingService.MapToResponseDtoAsync(t)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV shows by year: {Year}", year);
                return StatusCode(500, new { error = "Failed to retrieve TV shows by year", details = ex.Message });
            }
        }

        // POST: api/tvshow
        [HttpPost]
        public async Task<IActionResult> CreateTvShow([FromBody] CreateTvShowDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("TV show data is required");
                }

                var result = await _tvShowService.CreateTvShowAsync(dto);
                return await CreatedOrExistingAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TV show");
                return StatusCode(500, new { error = "Failed to create TV show", details = ex.Message });
            }
        }

        // PUT: api/tvshow/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTvShow(Guid id, [FromBody] CreateTvShowDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("TV show data is required");
                }

                var tvShow = await _tvShowService.UpdateTvShowAsync(id, dto);
                var response = await _tvShowMappingService.MapToResponseDtoAsync(tvShow);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "TV show with ID {Id} not found for update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating TV show with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update TV show", details = ex.Message });
            }
        }

        // DELETE: api/tvshow/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTvShow(Guid id)
        {
            try
            {
                var deleted = await _tvShowService.DeleteTvShowAsync(id);
                if (!deleted)
                {
                    return NotFound($"TV show with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting TV show with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete TV show", details = ex.Message });
            }
        }

        // POST: api/tvshow/from-tmdb/{tvShowId}
        // 201 when the TV show is imported; 200 with the stored TV show when its TMDB id is already
        // in the library (no TMDB request is made).
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies an outbound TMDB call per request.
        [Authorize]
        [HttpPost("from-tmdb/{tvShowId}")]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        public async Task<IActionResult> ImportTvShowFromTmdb(int tvShowId)
        {
            try
            {
                _logger.LogInformation("Starting TV show import from TMDB for ID: {TvShowId}", tvShowId);

                var stored = await _tvShowService.GetTvShowByTmdbIdAsync(tvShowId.ToString());
                if (stored != null)
                {
                    return Ok(await _tvShowMappingService.MapToResponseDtoAsync(stored));
                }

                // Get TV show data from TMDB API
                var tmdbTvShow = await _tmdbService.GetTvShowDetailsAsync(tvShowId);
                _logger.LogInformation("Retrieved TV show data from TMDB for ID: {TvShowId}", tvShowId);

                // Map TMDB data to domain entity
                var tvShow = await _tvShowMappingService.MapFromTmdbAsync(tmdbTvShow);
                _logger.LogInformation("Mapped TV show data for: {Title}", tvShow.Title);

                // Save to database (keeping TMDB thumbnail URL directly instead of re-uploading)
                var result = await _tvShowService.CreateTvShowAsync(new CreateTvShowDto
                {
                    Title = tvShow.Title,
                    Description = tvShow.Description,
                    Thumbnail = tvShow.Thumbnail,
                    Link = $"https://www.themoviedb.org/tv/{tvShow.TmdbId}",
                    TmdbId = tvShow.TmdbId,
                    TmdbRating = tvShow.TmdbRating,
                    TmdbPosterPath = tvShow.TmdbPosterPath,
                    Tagline = tvShow.Tagline,
                    Homepage = tvShow.Homepage,
                    OriginalLanguage = tvShow.OriginalLanguage,
                    OriginalName = tvShow.OriginalName,
                    FirstAirYear = tvShow.FirstAirYear,
                    LastAirYear = tvShow.LastAirYear,
                    NumberOfSeasons = tvShow.NumberOfSeasons,
                    NumberOfEpisodes = tvShow.NumberOfEpisodes,
                    Creator = tvShow.Creator,
                    Cast = tvShow.Cast,
                    ContentRating = tvShow.ContentRating,
                    Genres = tvShow.Genres.Select(g => g.Name).ToArray(),
                    Status = Status.Uncharted,
                    MediaType = MediaType.TVShow
                }, fromTmdb: true);

                if (result.Created)
                {
                    _logger.LogInformation("Successfully imported TV show: {Title} with ID: {Id}", result.TvShow.Title, result.TvShow.Id);
                    await ReindexAsync(result.TvShow.Id, "TMDB TV show import");
                }

                return await CreatedOrExistingAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing TV show from TMDB with ID: {TvShowId}", tvShowId);
                return StatusCode(500, new { error = "Failed to import TV show from TMDB", details = ex.Message });
            }
        }

        // POST: api/tvshow/{id}/episodes/from-tmdb
        // Imports the show's episodes from TMDB, one request per season. 200 with the run result
        // when the run completed (season failures included), 500 with the same body when it aborted,
        // 404 when the show id is unknown. Search is reindexed once at the end when rows changed.
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and proxies a burst of outbound TMDB calls per request.
        [Authorize]
        [HttpPost("{id}/episodes/from-tmdb")]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        public async Task<ActionResult<TvEpisodeImportResultDto>> ImportEpisodesFromTmdb(Guid id)
        {
            if (await _tvShowService.GetTvShowByIdAsync(id) == null)
            {
                return NotFound($"TV show with ID {id} not found.");
            }

            TvEpisodeImportResultDto result;
            try
            {
                result = await _episodeImportService.ImportFromTmdbAsync(id, cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing episodes from TMDB for TV show {Id}", id);
                result = new TvEpisodeImportResultDto
                {
                    Success = false,
                    ShowId = id,
                    StartedAt = DateTime.UtcNow,
                    ErrorMessage = $"TMDB episode import failed: {ex.Message}"
                };
            }

            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            // Search reindex comes last, and only when stored data actually changed.
            var changedCount = result.CreatedCount + result.UpdatedCount;
            if (changedCount > 0)
            {
                try
                {
                    await _importReindexService.ReindexAfterImportAsync(changedCount, "TMDB episode import");
                    result.ReindexTriggered = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Search reindex after TMDB episode import failed");
                    result.Warnings.Add("The episodes were saved, but the search reindex failed; search results may lag until the next reindex.");
                }
            }

            return Ok(result);
        }

        // GET: api/tvshow/{showId}/episodes
        [HttpGet("{showId}/episodes")]
        public async Task<ActionResult<IEnumerable<TvShowEpisodeResponseDto>>> GetEpisodesByShowId(Guid showId)
        {
            try
            {
                var episodes = await _tvShowService.GetEpisodesByShowIdAsync(showId);

                var response = episodes.Select(e => MapEpisodeToResponseDto(e));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving episodes for TV show {ShowId}", showId);
                return StatusCode(500, new { error = "Failed to retrieve TV show episodes", details = ex.Message });
            }
        }

        // GET: api/tvshow/episodes/{id}
        [HttpGet("episodes/{id}")]
        public async Task<ActionResult<TvShowEpisodeResponseDto>> GetTvShowEpisode(Guid id)
        {
            try
            {
                var episode = await _tvShowService.GetTvShowEpisodeByIdAsync(id);

                if (episode == null)
                {
                    return NotFound($"TV show episode with ID {id} not found.");
                }

                return Ok(MapEpisodeToResponseDto(episode));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve TV show episode", details = ex.Message });
            }
        }

        // POST: api/tvshow/episodes
        [HttpPost("episodes")]
        public async Task<IActionResult> CreateTvShowEpisode([FromBody] CreateTvShowEpisodeDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("TV show episode data is required");
                }

                var episode = await _tvShowService.CreateTvShowEpisodeAsync(dto);

                return CreatedAtAction(nameof(GetTvShowEpisode), new { id = episode.Id }, MapEpisodeToResponseDto(episode));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TV show episode");
                return StatusCode(500, new { error = "Failed to create TV show episode", details = ex.Message });
            }
        }

        // DELETE: api/tvshow/episodes/{id}
        [HttpDelete("episodes/{id}")]
        public async Task<IActionResult> DeleteTvShowEpisode(Guid id)
        {
            try
            {
                var deleted = await _tvShowService.DeleteTvShowEpisodeAsync(id);

                if (!deleted)
                {
                    return NotFound($"TV show episode with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting TV show episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete TV show episode", details = ex.Message });
            }
        }

        private static TvShowEpisodeResponseDto MapEpisodeToResponseDto(TvShowEpisode episode)
        {
            return new TvShowEpisodeResponseDto
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
                ShowId = episode.ShowId,
                ShowTitle = episode.Show?.Title,
                SeasonNumber = episode.SeasonNumber,
                EpisodeNumber = episode.EpisodeNumber,
                AirDate = episode.AirDate,
                DurationInMinutes = episode.DurationInMinutes,
                TmdbEpisodeId = episode.TmdbEpisodeId,
                TraktEpisodeId = episode.TraktEpisodeId,
                StillPath = episode.StillPath,
                TraktPlays = episode.TraktPlays,
                TraktLastWatchedAt = episode.TraktLastWatchedAt,
                EpisodeIdentifier = episode.GetEpisodeIdentifier()
            };
        }

        // GET: api/tvshow/search-tmdb
        [HttpGet("search-tmdb")]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        public async Task<ActionResult<IEnumerable<TvShowSearchResultDto>>> SearchTmdbTvShows([FromQuery] string query, [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query is required");
                }

                _logger.LogInformation("Searching TMDB for TV shows with query: {Query}", query);

                var searchResults = await _tmdbService.SearchTvShowsAsync(query, page);
                var response = await Task.WhenAll(searchResults.Results.Select(t => _tvShowMappingService.MapToSearchResultDtoAsync(t)));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching TMDB TV shows with query: {Query}", query);
                return StatusCode(500, new { error = "Failed to search TMDB TV shows", details = ex.Message });
            }
        }

        private async Task<IActionResult> CreatedOrExistingAsync(TvShowCreationResult result)
        {
            var response = await _tvShowMappingService.MapToResponseDtoAsync(result.TvShow);
            return result.Created
                ? CreatedAtAction(nameof(GetTvShow), new { id = result.TvShow.Id }, response)
                : Ok(response);
        }

        private async Task ReindexAsync(Guid id, string label)
        {
            try
            {
                await _importReindexService.ReindexItemAfterImportAsync(id, label);
            }
            catch (Exception ex)
            {
                // The import itself is already saved; a reindex failure must not turn it into a 500.
                _logger.LogError(ex, "Search reindex after {Label} failed", label);
            }
        }
    }
}
