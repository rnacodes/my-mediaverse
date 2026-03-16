using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TmdbController : ControllerBase
    {
        private readonly ITmdbService _tmdbService;
        private readonly ILogger<TmdbController> _logger;

        public TmdbController(ITmdbService tmdbService, ILogger<TmdbController> logger)
        {
            _tmdbService = tmdbService;
            _logger = logger;
        }

        /// <summary>
        /// Search for movies using TMDB API
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Search results for movies</returns>
        [HttpGet("search/movies")]
        public async Task<ActionResult<TmdbMovieSearchResultDto>> SearchMovies(
            [FromQuery] string query, 
            [FromQuery] int page = 1, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                var result = await _tmdbService.SearchMoviesAsync(query, page, language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching movies with query: {Query}", query);
                return StatusCode(500, "An error occurred while searching movies");
            }
        }

        /// <summary>
        /// Search for TV shows using TMDB API
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Search results for TV shows</returns>
        [HttpGet("search/tv")]
        public async Task<ActionResult<TmdbTvSearchResultDto>> SearchTvShows(
            [FromQuery] string query, 
            [FromQuery] int page = 1, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                var result = await _tmdbService.SearchTvShowsAsync(query, page, language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching TV shows with query: {Query}", query);
                return StatusCode(500, "An error occurred while searching TV shows");
            }
        }

        /// <summary>
        /// Search for movies and TV shows using TMDB API
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Search results for movies and TV shows</returns>
        [HttpGet("search/multi")]
        public async Task<ActionResult<TmdbMultiSearchResultDto>> SearchMulti(
            [FromQuery] string query, 
            [FromQuery] int page = 1, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                var result = await _tmdbService.SearchMultiAsync(query, page, language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching multi with query: {Query}", query);
                return StatusCode(500, "An error occurred while searching");
            }
        }

        /// <summary>
        /// Get detailed information about a specific movie
        /// </summary>
        /// <param name="movieId">TMDB movie ID</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Detailed movie information</returns>
        [HttpGet("movie/{movieId}")]
        public async Task<ActionResult<TmdbMovieDto>> GetMovieDetails(
            int movieId, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.GetMovieDetailsAsync(movieId, language);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Movie not found with ID: {MovieId}", movieId);
                return NotFound($"Movie with ID {movieId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie details for ID: {MovieId}", movieId);
                return StatusCode(500, "An error occurred while getting movie details");
            }
        }

        /// <summary>
        /// Get detailed information about a specific TV show
        /// </summary>
        /// <param name="tvShowId">TMDB TV show ID</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Detailed TV show information</returns>
        [HttpGet("tv/{tvShowId}")]
        public async Task<ActionResult<TmdbTvShowDto>> GetTvShowDetails(
            int tvShowId, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.GetTvShowDetailsAsync(tvShowId, language);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "TV show not found with ID: {TvShowId}", tvShowId);
                return NotFound($"TV show with ID {tvShowId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TV show details for ID: {TvShowId}", tvShowId);
                return StatusCode(500, "An error occurred while getting TV show details");
            }
        }

        /// <summary>
        /// Get popular movies
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Popular movies</returns>
        [HttpGet("movies/popular")]
        public async Task<ActionResult<TmdbMovieSearchResultDto>> GetPopularMovies(
            [FromQuery] int page = 1, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.GetPopularMoviesAsync(page, language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular movies");
                return StatusCode(500, "An error occurred while getting popular movies");
            }
        }

        /// <summary>
        /// Get popular TV shows
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Popular TV shows</returns>
        [HttpGet("tv/popular")]
        public async Task<ActionResult<TmdbTvSearchResultDto>> GetPopularTvShows(
            [FromQuery] int page = 1, 
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.GetPopularTvShowsAsync(page, language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular TV shows");
                return StatusCode(500, "An error occurred while getting popular TV shows");
            }
        }

        /// <summary>
        /// Get movie genres
        /// </summary>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>List of movie genres</returns>
        [HttpGet("genres/movies")]
        public async Task<ActionResult<TmdbGenreListDto>> GetMovieGenres(
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.GetMovieGenresAsync(language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie genres");
                return StatusCode(500, "An error occurred while getting movie genres");
            }
        }

        /// <summary>
        /// Get TV show genres
        /// </summary>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>List of TV show genres</returns>
        [HttpGet("genres/tv")]
        public async Task<ActionResult<TmdbGenreListDto>> GetTvGenres(
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.GetTvGenresAsync(language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TV genres");
                return StatusCode(500, "An error occurred while getting TV genres");
            }
        }

        /// <summary>
        /// Get image URL for TMDB images
        /// </summary>
        /// <param name="imagePath">Image path from TMDB</param>
        /// <param name="size">Image size (default: w500)</param>
        /// <returns>Full image URL</returns>
        [HttpGet("image")]
        public ActionResult<string> GetImageUrl(
            [FromQuery] string? imagePath, 
            [FromQuery] string size = "w500")
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return BadRequest("Image path is required");
            }

            var imageUrl = _tmdbService.GetImageUrl(imagePath, size);
            return Ok(imageUrl);
        }

        /// <summary>
        /// Import a movie from TMDB into the media library
        /// </summary>
        /// <param name="movieId">TMDB movie ID</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Imported movie entity</returns>
        [HttpPost("import/movie/{movieId}")]
        public async Task<ActionResult<Movie>> ImportMovie(
            int movieId,
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.ImportMovieAsync(movieId, language);
                return Created(string.Empty, result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Movie not found for import: {MovieId}", movieId);
                return NotFound($"Movie with ID {movieId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing movie: {MovieId}", movieId);
                return StatusCode(500, "An error occurred while importing the movie");
            }
        }

        /// <summary>
        /// Import a TV show from TMDB into the media library
        /// </summary>
        /// <param name="tvShowId">TMDB TV show ID</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Imported TV show entity</returns>
        [HttpPost("import/tv/{tvShowId}")]
        public async Task<ActionResult<TvShow>> ImportTvShow(
            int tvShowId,
            [FromQuery] string language = "en-US")
        {
            try
            {
                var result = await _tmdbService.ImportTvShowAsync(tvShowId, language);
                return Created(string.Empty, result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "TV show not found for import: {TvShowId}", tvShowId);
                return NotFound($"TV show with ID {tvShowId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing TV show: {TvShowId}", tvShowId);
                return StatusCode(500, "An error occurred while importing the TV show");
            }
        }
    }
}
