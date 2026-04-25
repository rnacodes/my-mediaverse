using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController : ControllerBase
    {
        private readonly IGenresService _genresService;
        private readonly ILogger<GenresController> _logger;

        public GenresController(IGenresService genresService, ILogger<GenresController> logger)
        {
            _genresService = genresService;
            _logger = logger;
        }

        // GET: api/genres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GenreResponseDto>>> GetAllGenres()
        {
            try
            {
                var genres = await _genresService.GetAllGenresAsync();
                return Ok(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genres");
                return StatusCode(500, new { error = "Failed to retrieve genres", details = ex.Message });
            }
        }

        // GET: api/genres/search?query={query}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<GenreResponseDto>>> SearchGenres([FromQuery] string query)
        {
            try
            {
                var genres = await _genresService.SearchGenresAsync(query);
                return Ok(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching genres");
                return StatusCode(500, new { error = "Failed to search genres", details = ex.Message });
            }
        }

        // GET: api/genres/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GenreResponseDto>> GetGenre(Guid id)
        {
            try
            {
                var genre = await _genresService.GetGenreAsync(id);
                if (genre == null)
                {
                    return NotFound($"Genre with ID {id} not found.");
                }
                return Ok(genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genre {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve genre", details = ex.Message });
            }
        }

        // POST: api/genres
        [HttpPost]
        public async Task<ActionResult<GenreResponseDto>> CreateGenre([FromBody] CreateGenreDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Genre name is required.");
            }

            var (genre, created) = await _genresService.CreateGenreAsync(dto);
            return created
                ? CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, genre)
                : Ok(genre);
        }

        // PUT: api/genres/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<GenreResponseDto>> UpdateGenre(Guid id, [FromBody] CreateGenreDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Genre name is required.");
            }

            try
            {
                var genre = await _genresService.UpdateGenreAsync(id, dto);
                if (genre == null)
                {
                    return NotFound($"Genre with ID {id} not found.");
                }
                return Ok(genre);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/genres/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(Guid id)
        {
            var deleted = await _genresService.DeleteGenreAsync(id);
            if (!deleted)
            {
                return NotFound($"Genre with ID {id} not found.");
            }
            return NoContent();
        }

        // POST: api/genres/import/json
        [HttpPost("import/json")]
        public async Task<ActionResult<BulkImportResultDto>> ImportGenresFromJson([FromBody] List<CreateGenreDto> genres)
        {
            if (genres == null || !genres.Any())
            {
                return BadRequest("No genres provided for import.");
            }

            var result = await _genresService.ImportGenresFromJsonAsync(genres);
            return Ok(result);
        }

        // POST: api/genres/import/csv
        [HttpPost("import/csv")]
        public async Task<ActionResult<BulkImportResultDto>> ImportGenresFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("File must be a CSV");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _genresService.ImportGenresFromCsvAsync(stream);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV file");
                return StatusCode(500, $"Error processing CSV file: {ex.Message}");
            }
        }
    }
}
