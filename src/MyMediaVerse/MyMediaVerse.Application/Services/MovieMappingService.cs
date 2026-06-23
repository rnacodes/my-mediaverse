using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.Application.Services
{
    public class MovieMappingService : IMovieMappingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<MovieMappingService> _logger;

        public MovieMappingService(IApplicationDbContext context, ILogger<MovieMappingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Movie> MapFromDtoAsync(CreateMovieDto dto)
        {
            var movie = new Movie
            {
                Title = dto.Title,
                MediaType = MediaType.Movie,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                Director = dto.Director,
                Cast = dto.Cast,
                ReleaseYear = dto.ReleaseYear,
                RuntimeMinutes = dto.RuntimeMinutes,
                MpaaRating = dto.MpaaRating,
                ImdbId = dto.ImdbId,
                TmdbId = dto.TmdbId,
                TmdbRating = dto.TmdbRating,
                TmdbBackdropPath = dto.TmdbBackdropPath,
                Tagline = dto.Tagline,
                Homepage = dto.Homepage,
                OriginalLanguage = dto.OriginalLanguage,
                OriginalTitle = dto.OriginalTitle
            };

            // Handle Topics array conversion
            if (dto.Topics?.Length > 0)
            {
                foreach (var topicName in dto.Topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                    var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                    if (existingTopic != null)
                    {
                        movie.Topics.Add(existingTopic);
                    }
                    else
                    {
                        movie.Topics.Add(new Topic { Name = normalizedTopicName });
                    }
                }
            }

            // Handle Genres array conversion
            if (dto.Genres?.Length > 0)
            {
                foreach (var genreName in dto.Genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                    var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                    if (existingGenre != null)
                    {
                        movie.Genres.Add(existingGenre);
                    }
                    else
                    {
                        movie.Genres.Add(new Genre { Name = normalizedGenreName });
                    }
                }
            }

            return movie;
        }

        public Task<MovieResponseDto> MapToResponseDtoAsync(Movie movie)
        {
            return Task.FromResult(new MovieResponseDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                MediaType = movie.MediaType,
                Status = movie.Status,
                DateAdded = movie.DateAdded,
                Link = movie.Link,
                Thumbnail = movie.Thumbnail,
                Rating = movie.Rating,
                OwnershipStatus = movie.OwnershipStatus,
                DateCompleted = movie.DateCompleted,
                Notes = movie.Notes,
                RelatedNotes = movie.RelatedNotes,
                Topics = movie.Topics.Select(t => t.Name).ToArray(),
                Genres = movie.Genres.Select(g => g.Name).ToArray(),
                Director = movie.Director,
                Cast = movie.Cast,
                ReleaseYear = movie.ReleaseYear,
                RuntimeMinutes = movie.RuntimeMinutes,
                MpaaRating = movie.MpaaRating,
                ImdbId = movie.ImdbId,
                TmdbId = movie.TmdbId,
                TmdbRating = movie.TmdbRating,
                TmdbBackdropPath = movie.TmdbBackdropPath,
                Tagline = movie.Tagline,
                Homepage = movie.Homepage,
                OriginalLanguage = movie.OriginalLanguage,
                OriginalTitle = movie.OriginalTitle,
                TmdbBackdropUrl = movie.GetTmdbBackdropUrl(),
                FormattedRuntime = FormatRuntime(movie.RuntimeMinutes)
            });
        }

        public Task<Movie> MapFromTmdbAsync(TmdbMovieDto tmdbMovie)
        {
            var movie = new Movie
            {
                Title = tmdbMovie.Title ?? "Unknown Title",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Description = tmdbMovie.Overview,
                Thumbnail = !string.IsNullOrEmpty(tmdbMovie.PosterPath) 
                    ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}" 
                    : null,
                TmdbId = tmdbMovie.Id.ToString(),
                TmdbRating = tmdbMovie.VoteAverage,
                TmdbBackdropPath = tmdbMovie.BackdropPath,
                Tagline = tmdbMovie.Tagline,
                Homepage = tmdbMovie.Homepage,
                OriginalLanguage = tmdbMovie.OriginalLanguage,
                OriginalTitle = tmdbMovie.OriginalTitle,
                ImdbId = tmdbMovie.ImdbId,
                RuntimeMinutes = tmdbMovie.Runtime
            };

            // Parse release date to get year
            if (!string.IsNullOrEmpty(tmdbMovie.ReleaseDate) && 
                DateTime.TryParse(tmdbMovie.ReleaseDate, out var releaseDate))
            {
                movie.ReleaseYear = releaseDate.Year;
            }

            // Handle genres from TMDB genre IDs (simplified - would need genre mapping service)
            // For now, we'll skip this as it would require a TMDB genre mapping service

            return Task.FromResult(movie);
        }

        public Task<MovieSearchResultDto> MapToSearchResultDtoAsync(TmdbMovieDto tmdbMovie)
        {
            return Task.FromResult(new MovieSearchResultDto
            {
                Id = tmdbMovie.Id,
                Title = tmdbMovie.Title,
                Overview = tmdbMovie.Overview,
                PosterPath = tmdbMovie.PosterPath,
                BackdropPath = tmdbMovie.BackdropPath,
                ReleaseDate = tmdbMovie.ReleaseDate,
                VoteAverage = tmdbMovie.VoteAverage,
                Popularity = tmdbMovie.Popularity,
                OriginalLanguage = tmdbMovie.OriginalLanguage,
                OriginalTitle = tmdbMovie.OriginalTitle,
                GenreIds = tmdbMovie.GenreIds,
                Runtime = tmdbMovie.Runtime,
                Tagline = tmdbMovie.Tagline,
                Homepage = tmdbMovie.Homepage,
                ImdbId = tmdbMovie.ImdbId,
                PosterUrl = !string.IsNullOrEmpty(tmdbMovie.PosterPath) 
                    ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}" 
                    : null,
                BackdropUrl = !string.IsNullOrEmpty(tmdbMovie.BackdropPath)
                    ? $"https://image.tmdb.org/t/p/w1280{tmdbMovie.BackdropPath}"
                    : null
            });
        }

        private static string? FormatRuntime(int? runtimeMinutes)
        {
            if (!runtimeMinutes.HasValue) return null;
            
            var hours = runtimeMinutes.Value / 60;
            var minutes = runtimeMinutes.Value % 60;
            
            if (hours > 0)
            {
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }
            
            return $"{minutes}m";
        }
    }
}
