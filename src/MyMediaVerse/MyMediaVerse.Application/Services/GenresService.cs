using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    public class GenresService : IGenresService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GenresService> _logger;

        public GenresService(IApplicationDbContext context, ILogger<GenresService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<GenreResponseDto>> GetAllGenresAsync()
        {
            var genres = await _context.Genres
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    Count = g.MediaItems.Count
                })
                .ToListAsync();

            return genres.Select(g => new GenreResponseDto
            {
                Id = g.Id,
                Name = g.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = g.Count
            }).ToList();
        }

        public async Task<IReadOnlyList<GenreResponseDto>> SearchGenresAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllGenresAsync();
            }

            var normalizedQuery = query.ToLowerInvariant();
            var genres = await _context.Genres
                .AsNoTracking()
                .Where(g => g.Name.Contains(normalizedQuery))
                .OrderBy(g => g.Name)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    Count = g.MediaItems.Count
                })
                .ToListAsync();

            return genres.Select(g => new GenreResponseDto
            {
                Id = g.Id,
                Name = g.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = g.Count
            }).ToList();
        }

        public async Task<GenreResponseDto?> GetGenreAsync(Guid id)
        {
            var genre = await _context.Genres
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genre == null)
            {
                return null;
            }

            var mediaItemIds = await _context.MediaItems
                .Where(m => m.Genres.Any(g => g.Id == id))
                .Select(m => m.Id)
                .ToListAsync();

            return new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                MediaItemIds = mediaItemIds.ToArray(),
                MediaItemCount = mediaItemIds.Count
            };
        }

        public async Task<(GenreResponseDto Genre, bool Created)> CreateGenreAsync(CreateGenreDto dto)
        {
            var normalizedGenreName = dto.Name.Trim().ToLowerInvariant();

            var existingGenre = await _context.Genres
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

            if (existingGenre != null)
            {
                var mediaItemIds = await _context.MediaItems
                    .Where(m => m.Genres.Any(g => g.Id == existingGenre.Id))
                    .Select(m => m.Id)
                    .ToListAsync();

                return (new GenreResponseDto
                {
                    Id = existingGenre.Id,
                    Name = existingGenre.Name,
                    MediaItemIds = mediaItemIds.ToArray(),
                    MediaItemCount = mediaItemIds.Count
                }, false);
            }

            var genre = new Genre { Name = normalizedGenreName };
            _context.Add(genre);
            await _context.SaveChangesAsync();

            return (new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = 0
            }, true);
        }

        public async Task<GenreResponseDto?> UpdateGenreAsync(Guid id, CreateGenreDto dto)
        {
            var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == id);
            if (genre == null)
            {
                return null;
            }

            var normalizedGenreName = dto.Name.Trim().ToLowerInvariant();
            var conflict = await _context.Genres
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == normalizedGenreName && g.Id != id);

            if (conflict != null)
            {
                throw new InvalidOperationException($"A genre with the name '{dto.Name}' already exists.");
            }

            genre.Name = normalizedGenreName;
            await _context.SaveChangesAsync();

            var mediaItemIds = await _context.MediaItems
                .Where(m => m.Genres.Any(g => g.Id == id))
                .Select(m => m.Id)
                .ToListAsync();

            return new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                MediaItemIds = mediaItemIds.ToArray(),
                MediaItemCount = mediaItemIds.Count
            };
        }

        public async Task<bool> DeleteGenreAsync(Guid id)
        {
            var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == id);
            if (genre == null)
            {
                return false;
            }

            // Cascade delete in the database removes the join-table associations.
            _context.Remove(genre);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BulkImportResultDto> ImportGenresFromJsonAsync(IReadOnlyList<CreateGenreDto> genres)
        {
            var result = new BulkImportResultDto();
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

                    var imported = await ImportSingleGenreAsync(genreDto.Name, result);
                    if (imported != null)
                    {
                        result.Imported.Add(imported);
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing genre '{Name}'", genreDto.Name);
                    result.Errors.Add($"Genre '{genreDto.Name}': {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return result;
        }

        public async Task<BulkImportResultDto> ImportGenresFromCsvAsync(Stream csvStream)
        {
            var result = new BulkImportResultDto();
            using var reader = new StreamReader(csvStream);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()
            };
            using var csv = new CsvReader(reader, csvConfig);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;
            if (headers == null || !headers.Any(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("CSV file must have a 'Name' column");
            }

            while (csv.Read())
            {
                result.TotalProcessed++;
                var name = csv.GetField("name");

                try
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.Errors.Add($"Row {csv.CurrentIndex}: Name is required");
                        result.ErrorCount++;
                        continue;
                    }

                    var imported = await ImportSingleGenreAsync(name, result);
                    if (imported != null)
                    {
                        result.Imported.Add(imported);
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing genre from CSV row {Row}", csv.CurrentIndex);
                    result.Errors.Add($"Row {csv.CurrentIndex}: {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return result;
        }

        private async Task<GenreResponseDto?> ImportSingleGenreAsync(string name, BulkImportResultDto result)
        {
            var normalizedGenreName = name.Trim().ToLowerInvariant();
            var existingGenre = await _context.Genres
                .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

            if (existingGenre != null)
            {
                result.Skipped.Add($"Genre '{name}' already exists");
                result.SkippedCount++;
                return null;
            }

            var genre = new Genre { Name = normalizedGenreName };
            _context.Add(genre);
            await _context.SaveChangesAsync();

            return new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = 0
            };
        }
    }
}
