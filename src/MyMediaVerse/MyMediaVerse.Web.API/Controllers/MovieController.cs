using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.DTOs;
using MyMediaVerse.Domain.Entities;
using System.Text.Json;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IMovieMappingService _movieMappingService;
        private readonly ILogger<MovieController> _logger;
        private readonly ITmdbApiClient _tmdbClient;

        public MovieController(
            IMovieService movieService,
            IMovieMappingService movieMappingService,
            ILogger<MovieController> logger,
            ITmdbApiClient tmdbClient)
        {
            _movieService = movieService;
            _movieMappingService = movieMappingService;
            _logger = logger;
            _tmdbClient = tmdbClient;
        }

        // GET: api/movie
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetAllMovies()
        {
            try
            {
                var movies = await _movieService.GetAllMoviesAsync();
                var response = await Task.WhenAll(movies.Select(m => _movieMappingService.MapToResponseDtoAsync(m)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all movies");
                return StatusCode(500, new { error = "Failed to retrieve movies", details = ex.Message });
            }
        }

        // GET: api/movie/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MovieResponseDto>> GetMovie(Guid id)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                {
                    return NotFound($"Movie with ID {id} not found.");
                }

                var response = await _movieMappingService.MapToResponseDtoAsync(movie);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movie with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve movie", details = ex.Message });
            }
        }

        // GET: api/movie/by-director/{director}
        [HttpGet("by-director/{director}")]
        public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetMoviesByDirector(string director)
        {
            try
            {
                var movies = await _movieService.GetMoviesByDirectorAsync(director);
                var response = await Task.WhenAll(movies.Select(m => _movieMappingService.MapToResponseDtoAsync(m)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movies by director: {Director}", director);
                return StatusCode(500, new { error = "Failed to retrieve movies by director", details = ex.Message });
            }
        }

        // GET: api/movie/by-year/{year}
        [HttpGet("by-year/{year}")]
        public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetMoviesByYear(int year)
        {
            try
            {
                var movies = await _movieService.GetMoviesByYearAsync(year);
                var response = await Task.WhenAll(movies.Select(m => _movieMappingService.MapToResponseDtoAsync(m)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movies by year: {Year}", year);
                return StatusCode(500, new { error = "Failed to retrieve movies by year", details = ex.Message });
            }
        }

        // POST: api/movie
        [HttpPost]
        public async Task<IActionResult> CreateMovie([FromBody] CreateMovieDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Movie data is required");
                }

                var movie = await _movieService.CreateMovieAsync(dto);
                var response = await _movieMappingService.MapToResponseDtoAsync(movie);

                return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating movie");
                return StatusCode(500, new { error = "Failed to create movie", details = ex.Message });
            }
        }

        // PUT: api/movie/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovie(Guid id, [FromBody] CreateMovieDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Movie data is required");
                }

                var movie = await _movieService.UpdateMovieAsync(id, dto);
                var response = await _movieMappingService.MapToResponseDtoAsync(movie);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Movie with ID {Id} not found for update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating movie with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update movie", details = ex.Message });
            }
        }

        // DELETE: api/movie/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(Guid id)
        {
            try
            {
                var deleted = await _movieService.DeleteMovieAsync(id);
                if (!deleted)
                {
                    return NotFound($"Movie with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting movie with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete movie", details = ex.Message });
            }
        }

        // POST: api/movie/from-tmdb/{movieId}
        [HttpPost("from-tmdb/{movieId}")]
        public async Task<IActionResult> ImportMovieFromTmdb(int movieId)
        {
            try
            {
                _logger.LogInformation("Starting movie import from TMDB for ID: {MovieId}", movieId);

                // Get movie data from TMDB API
                var tmdbMovie = await _tmdbClient.GetMovieDetailsAsync(movieId);
                _logger.LogInformation("Retrieved movie data from TMDB for ID: {MovieId}", movieId);

                // Map TMDB data to domain entity
                var movie = await _movieMappingService.MapFromTmdbAsync(tmdbMovie);
                _logger.LogInformation("Mapped movie data for: {Title}", movie.Title);

                // Save to database (keeping TMDB thumbnail URL directly instead of re-uploading)
                var createdMovie = await _movieService.CreateMovieAsync(new CreateMovieDto
                {
                    Title = movie.Title,
                    Description = movie.Description,
                    Thumbnail = movie.Thumbnail,
                    Link = $"https://www.themoviedb.org/movie/{movie.TmdbId}",
                    TmdbId = movie.TmdbId,
                    TmdbRating = movie.TmdbRating,
                    TmdbBackdropPath = movie.TmdbBackdropPath,
                    Tagline = movie.Tagline,
                    Homepage = movie.Homepage,
                    OriginalLanguage = movie.OriginalLanguage,
                    OriginalTitle = movie.OriginalTitle,
                    ImdbId = movie.ImdbId,
                    ReleaseYear = movie.ReleaseYear,
                    RuntimeMinutes = movie.RuntimeMinutes,
                    Status = Status.Uncharted,
                    MediaType = MediaType.Movie
                });

                var response = await _movieMappingService.MapToResponseDtoAsync(createdMovie);
                _logger.LogInformation("Successfully imported movie: {Title} with ID: {Id}", createdMovie.Title, createdMovie.Id);

                return CreatedAtAction(nameof(GetMovie), new { id = createdMovie.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing movie from TMDB with ID: {MovieId}", movieId);
                return StatusCode(500, new { error = "Failed to import movie from TMDB", details = ex.Message });
            }
        }

        // GET: api/movie/search-tmdb
        [HttpGet("search-tmdb")]
        public async Task<ActionResult<IEnumerable<MovieSearchResultDto>>> SearchTmdbMovies([FromQuery] string query, [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query is required");
                }

                _logger.LogInformation("Searching TMDB for movies with query: {Query}", query);

                var searchResults = await _tmdbClient.SearchMoviesAsync(query, page);
                var response = await Task.WhenAll(searchResults.Results.Select(m => _movieMappingService.MapToSearchResultDtoAsync(m)));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching TMDB movies with query: {Query}", query);
                return StatusCode(500, new { error = "Failed to search TMDB movies", details = ex.Message });
            }
        }
    }
}
