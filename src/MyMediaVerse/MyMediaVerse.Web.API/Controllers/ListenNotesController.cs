using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    // Every action proxies the metered ListenNotes API on the app's key, so the
    // whole controller is rate limited per client IP.
    [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
    [ApiController]
    [Route("api/[controller]")]
    public class ListenNotesController : ControllerBase
    {
        private readonly IListenNotesService _listenNotesService;
        private readonly ILogger<ListenNotesController> _logger;

        public ListenNotesController(IListenNotesService listenNotesService, ILogger<ListenNotesController> logger)
        {
            _listenNotesService = listenNotesService;
            _logger = logger;
        }

        /// <summary>
        /// Get detailed information about a specific podcast
        /// </summary>
        /// <param name="id">ListenNotes podcast ID</param>
        /// <returns>Detailed podcast information</returns>
        [HttpGet("podcasts/{id}")]
        public async Task<ActionResult<PodcastSeriesDto>> GetPodcast(string id, [FromQuery] string? next_episode_pub_date = null)
        {
            try
            {
                var result = await _listenNotesService.GetPodcastByIdAsync(id, next_episode_pub_date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving podcast with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the podcast");
            }
        }

        /// <summary>
        /// Get playlists
        /// </summary>
        /// <returns>List of playlists</returns>
        [HttpGet("playlists")]
        public async Task<ActionResult<ListenNotesPlaylistsDto>> GetPlaylists()
        {
            try
            {
                var result = await _listenNotesService.GetPlaylistsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving playlists");
                return StatusCode(500, "An error occurred while retrieving playlists");
            }
        }

        /// <summary>
        /// Get detailed information about a specific playlist
        /// </summary>
        /// <param name="id">ListenNotes playlist ID</param>
        /// <returns>Detailed playlist information</returns>
        [HttpGet("playlists/{id}")]
        public async Task<ActionResult<ListenNotesPlaylistDto>> GetPlaylist(string id)
        {
            try
            {
                var result = await _listenNotesService.GetPlaylistByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving playlist with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the playlist");
            }
        }

        /// <summary>
        /// Search for podcasts or episodes
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="type">The type of search which could be "episode" or "podcast"</param>
        /// <param name="offset">Offset for pagination</param>
        /// <param name="lenMin">Minimum length in minutes</param>
        /// <param name="lenMax">Maximum length in minutes</param>
        /// <param name="genreIds">Comma-separated genre IDs</param>
        /// <param name="publishedBefore">Published before date</param>
        /// <param name="publishedAfter">Published after date</param>
        /// <param name="onlyIn">Only search in specific fields</param>
        /// <param name="language">Language code</param>
        /// <param name="region">Region code</param>
        /// <param name="sortByDate">Sort by date</param>
        /// <param name="safeMode">Safe mode</param>
        /// <param name="uniquePodcasts">Unique podcasts</param>
        /// <returns>Search results</returns>
        [HttpGet("search")]
        public async Task<ActionResult<SearchResultDto>> Search(
            [FromQuery] string query,
            [FromQuery] string? type = null,
            [FromQuery] int? offset = null,
            [FromQuery] int? lenMin = null,
            [FromQuery] int? lenMax = null,
            [FromQuery] string? genreIds = null,
            [FromQuery] string? publishedBefore = null,
            [FromQuery] string? publishedAfter = null,
            [FromQuery] string? onlyIn = null,
            [FromQuery] string? language = null,
            [FromQuery] string? region = null,
            [FromQuery] string? sortByDate = null,
            [FromQuery] string? safeMode = null,
            [FromQuery] string? uniquePodcasts = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                _logger.LogInformation("Starting search with query: {Query}", query);
                
                var result = await _listenNotesService.SearchAsync(
                    query, type, offset, lenMin, lenMax, genreIds,
                    publishedBefore, publishedAfter, onlyIn, language,
                    region, sortByDate, safeMode, uniquePodcasts);

                _logger.LogInformation("Search completed successfully for query: {Query}", query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching with query {Query}", query);
                return StatusCode(500, "An error occurred while performing the search");
            }
        }

        /// <summary>
        /// Get available genres
        /// </summary>
        /// <returns>List of genres</returns>
        [HttpGet("genres")]
        public async Task<ActionResult<ListenNotesGenresDto>> GetGenres()
        {
            try
            {
                var result = await _listenNotesService.GetGenresAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genres");
                return StatusCode(500, "An error occurred while retrieving genres");
            }
        }

        /// <summary>
        /// Get detailed information about a specific episode
        /// </summary>
        /// <param name="id">ListenNotes episode ID</param>
        /// <returns>Detailed episode information</returns>
        [HttpGet("episodes/{id}")]
        public async Task<ActionResult<PodcastEpisodeDto>> GetEpisode(string id)
        {
            try
            {
                var result = await _listenNotesService.GetEpisodeByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving episode with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the episode");
            }
        }

        /// <summary>
        /// Get best podcasts
        /// </summary>
        /// <param name="genreId">Genre ID filter</param>
        /// <param name="page">Page number</param>
        /// <param name="region">Region filter</param>
        /// <param name="sortByDate">Sort by date</param>
        /// <param name="safeMode">Safe mode</param>
        /// <returns>Best podcasts</returns>
        [HttpGet("best-podcasts")]
        public async Task<ActionResult<ListenNotesBestPodcastsDto>> GetBestPodcasts(
            [FromQuery] int? genreId = null,
            [FromQuery] int? page = null,
            [FromQuery] string? region = null,
            [FromQuery] string? sortByDate = null,
            [FromQuery] bool? safeMode = null)
        {
            try
            {
                var result = await _listenNotesService.GetBestPodcastsAsync(genreId, page, region, sortByDate, safeMode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving best podcasts");
                return StatusCode(500, "An error occurred while retrieving best podcasts");
            }
        }

        /// <summary>
        /// Get curated podcasts
        /// </summary>
        /// <param name="page">Page number</param>
        /// <returns>Curated podcasts</returns>
        [HttpGet("curated-podcasts")]
        public async Task<ActionResult<ListenNotesCuratedPodcastsDto>> GetCuratedPodcasts([FromQuery] int? page = null)
        {
            try
            {
                var result = await _listenNotesService.GetCuratedPodcastsAsync(page);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving curated podcasts");
                return StatusCode(500, "An error occurred while retrieving curated podcasts");
            }
        }

        /// <summary>
        /// Get detailed information about a specific curated podcast
        /// </summary>
        /// <param name="id">ListenNotes curated podcast ID</param>
        /// <returns>Detailed curated podcast information</returns>
        [HttpGet("curated-podcasts/{id}")]
        public async Task<ActionResult<ListenNotesCuratedPodcastDto>> GetCuratedPodcast(string id)
        {
            try
            {
                var result = await _listenNotesService.GetCuratedPodcastByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving curated podcast with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the curated podcast");
            }
        }

        /// <summary>
        /// Get podcast recommendations
        /// </summary>
        /// <param name="id">ListenNotes podcast ID</param>
        /// <param name="safeMode">Safe mode</param>
        /// <returns>Podcast recommendations</returns>
        [HttpGet("podcasts/{id}/recommendations")]
        public async Task<ActionResult<ListenNotesRecommendationsDto>> GetPodcastRecommendations(string id, [FromQuery] bool? safeMode = null)
        {
            try
            {
                var result = await _listenNotesService.GetPodcastRecommendationsAsync(id, safeMode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommendations for podcast with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving podcast recommendations");
            }
        }

        /// <summary>
        /// Get episode recommendations
        /// </summary>
        /// <param name="id">ListenNotes episode ID</param>
        /// <param name="safeMode">Safe mode</param>
        /// <returns>Episode recommendations</returns>
        [HttpGet("episodes/{id}/recommendations")]
        public async Task<ActionResult<ListenNotesRecommendationsDto>> GetEpisodeRecommendations(string id, [FromQuery] bool? safeMode = null)
        {
            try
            {
                var result = await _listenNotesService.GetEpisodeRecommendationsAsync(id, safeMode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommendations for episode with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving episode recommendations");
            }
        }

        /// <summary>
        /// Import a podcast series from ListenNotes into the media library
        /// </summary>
        /// <param name="podcastId">ListenNotes podcast ID</param>
        /// <returns>Imported podcast series entity</returns>
        [HttpPost("import/podcast/{podcastId}")]
        public async Task<ActionResult<PodcastSeries>> ImportPodcast(string podcastId)
        {
            try
            {
                var result = await _listenNotesService.ImportPodcastSeriesAsync(podcastId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Podcast not found for import: {PodcastId}", podcastId);
                return NotFound($"Podcast with ID {podcastId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast: {PodcastId}", podcastId);
                return StatusCode(500, "An error occurred while importing the podcast");
            }
        }

        /// <summary>
        /// Import a podcast episode from ListenNotes into the media library
        /// </summary>
        /// <param name="episodeId">ListenNotes episode ID</param>
        /// <param name="seriesId">ID of the parent podcast series</param>
        /// <returns>Imported podcast episode entity</returns>
        [HttpPost("import/episode/{episodeId}")]
        public async Task<ActionResult<PodcastEpisode>> ImportPodcastEpisode(string episodeId, [FromQuery] Guid seriesId)
        {
            try
            {
                var result = await _listenNotesService.ImportPodcastEpisodeAsync(episodeId, seriesId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Episode not found for import: {EpisodeId}", episodeId);
                return NotFound($"Episode with ID {episodeId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing episode: {EpisodeId}", episodeId);
                return StatusCode(500, "An error occurred while importing the episode");
            }
        }
    }
}
