using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Infrastructure.Data;
using ProjectLoopbreaker.DTOs;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ProjectLoopbreaker.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController : ControllerBase
    {
        private readonly MediaLibraryDbContext _context;

        public GenresController(MediaLibraryDbContext context)
        {
            _context = context;
        }

        // GET: api/genres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GenreResponseDto>>> GetAllGenres()
        {
            try
            {
                // Get all genres in a single query
                var genres = await _context.Genres
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                // Get all media item counts using LINQ navigation properties
                var genreCounts = await _context.Genres
                    .Select(g => new { GenreId = g.Id, Count = g.MediaItems.Count })
                    .ToListAsync();
                var countsByGenreId = genreCounts.ToDictionary(gc => gc.GenreId, gc => gc.Count);

                // Build response with counts (no need for individual queries)
                var response = genres.Select(genre => new GenreResponseDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    MediaItemIds = Array.Empty<Guid>(), // Not needed for list view
                    MediaItemCount = countsByGenreId.GetValueOrDefault(genre.Id, 0)
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllGenres: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve genres", details = ex.Message });
            }
        }

        // GET: api/genres/search?query={query}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<GenreResponseDto>>> SearchGenres([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllGenres();
            }

            try
            {
                var normalizedQuery = query.ToLowerInvariant();
                var genres = await _context.Genres
                    .AsNoTracking()
                    .Where(g => g.Name.Contains(normalizedQuery))
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                // Get counts for all genres using LINQ
                var genreCounts = await _context.Genres
                    .Select(g => new { GenreId = g.Id, Count = g.MediaItems.Count })
                    .ToListAsync();
                var countsByGenreId = genreCounts.ToDictionary(gc => gc.GenreId, gc => gc.Count);

                var response = genres.Select(genre => new GenreResponseDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    MediaItemIds = Array.Empty<Guid>(),
                    MediaItemCount = countsByGenreId.GetValueOrDefault(genre.Id, 0)
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchGenres: {ex.Message}");
                return StatusCode(500, new { error = "Failed to search genres", details = ex.Message });
            }
        }

        // GET: api/genres/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GenreResponseDto>> GetGenre(Guid id)
        {
            try
            {
                var genre = await _context.Genres
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (genre == null)
                {
                    return NotFound($"Genre with ID {id} not found.");
                }

                // Get media item IDs using LINQ navigation properties
                var mediaItemIds = await _context.MediaItems
                    .Where(m => m.Genres.Any(g => g.Id == id))
                    .Select(m => m.Id)
                    .ToListAsync();

                var response = new GenreResponseDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    MediaItemIds = mediaItemIds.ToArray(),
                    MediaItemCount = mediaItemIds.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGenre: {ex.Message}");
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

            var normalizedGenreName = dto.Name.Trim().ToLowerInvariant();

            // Check if genre already exists
            var existingGenre = await _context.Genres
                .Include(g => g.MediaItems)
                .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

            if (existingGenre != null)
            {
                var existingResponse = new GenreResponseDto
                {
                    Id = existingGenre.Id,
                    Name = existingGenre.Name,
                    MediaItemIds = existingGenre.MediaItems.Select(m => m.Id).ToArray(),
                    MediaItemCount = existingGenre.MediaItems.Count
                };
                return Ok(existingResponse);
            }

            var genre = new Genre { Name = normalizedGenreName };
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            var response = new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = 0
            };

            return CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, response);
        }

        // PUT: api/genres/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<GenreResponseDto>> UpdateGenre(Guid id, [FromBody] CreateGenreDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Genre name is required.");
            }

            var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == id);
            if (genre == null)
            {
                return NotFound($"Genre with ID {id} not found.");
            }

            var normalizedGenreName = dto.Name.Trim().ToLowerInvariant();

            // Check if another genre with the new name already exists
            var existingGenre = await _context.Genres
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == normalizedGenreName && g.Id != id);

            if (existingGenre != null)
            {
                return BadRequest($"A genre with the name '{dto.Name}' already exists.");
            }

            genre.Name = normalizedGenreName;
            await _context.SaveChangesAsync();

            // Get media item IDs using LINQ navigation properties
            var mediaItemIds = await _context.MediaItems
                .Where(m => m.Genres.Any(g => g.Id == id))
                .Select(m => m.Id)
                .ToListAsync();

            var response = new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                MediaItemIds = mediaItemIds.ToArray(),
                MediaItemCount = mediaItemIds.Count
            };

            return Ok(response);
        }

        // DELETE: api/genres/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(Guid id)
        {
            var genre = await _context.Genres
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genre == null)
            {
                return NotFound($"Genre with ID {id} not found.");
            }

            // The database is configured with cascade delete, so removing the genre
            // will automatically remove all associations in the MediaItemGenres join table
            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/genres/import/json
        [HttpPost("import/json")]
        public async Task<ActionResult<BulkImportResultDto>> ImportGenresFromJson([FromBody] List<CreateGenreDto> genres)
        {
            var result = new BulkImportResultDto();

            if (genres == null || !genres.Any())
            {
                return BadRequest("No genres provided for import.");
            }

            foreach (var genreDto in genres)
            {
                result.TotalProcessed++;

                try
                {
                    if (string.IsNullOrWhiteSpace(genreDto.Name))
                    {
                        result.Errors.Add($"Genre at index {result.TotalProcessed - 1}: Name is required");
                        result.ErrorCount++;
                        continue;
                    }

                    var normalizedGenreName = genreDto.Name.Trim().ToLowerInvariant();

                    // Check if genre already exists
                    var existingGenre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

                    if (existingGenre != null)
                    {
                        result.Skipped.Add($"Genre '{genreDto.Name}' already exists");
                        result.SkippedCount++;
                        continue;
                    }

                    var genre = new Genre { Name = normalizedGenreName };
                    _context.Genres.Add(genre);
                    await _context.SaveChangesAsync();

                    result.Imported.Add(new GenreResponseDto
                    {
                        Id = genre.Id,
                        Name = genre.Name,
                        MediaItemIds = Array.Empty<Guid>(),
                        MediaItemCount = 0
                    });
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Genre '{genreDto.Name}': {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return Ok(result);
        }

        // POST: api/genres/import/csv
        [HttpPost("import/csv")]
        public async Task<ActionResult<BulkImportResultDto>> ImportGenresFromCsv(IFormFile file)
        {
            var result = new BulkImportResultDto();

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
                using var reader = new StreamReader(file.OpenReadStream());
                var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()
                };
                using var csv = new CsvReader(reader, csvConfig);

                csv.Read();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                if (headers == null || !headers.Any(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("CSV file must have a 'Name' column");
                }

                while (csv.Read())
                {
                    result.TotalProcessed++;

                    try
                    {
                        var name = csv.GetField("name");

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.Errors.Add($"Row {csv.CurrentIndex}: Name is required");
                            result.ErrorCount++;
                            continue;
                        }

                        var normalizedGenreName = name.Trim().ToLowerInvariant();

                        // Check if genre already exists
                        var existingGenre = await _context.Genres
                            .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

                        if (existingGenre != null)
                        {
                            result.Skipped.Add($"Genre '{name}' already exists");
                            result.SkippedCount++;
                            continue;
                        }

                        var genre = new Genre { Name = normalizedGenreName };
                        _context.Genres.Add(genre);
                        await _context.SaveChangesAsync();

                        result.Imported.Add(new GenreResponseDto
                        {
                            Id = genre.Id,
                            Name = genre.Name,
                            MediaItemIds = Array.Empty<Guid>(),
                            MediaItemCount = 0
                        });
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {csv.CurrentIndex}: {ex.Message}");
                        result.ErrorCount++;
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing CSV file: {ex.Message}");
            }
        }
    }
}
