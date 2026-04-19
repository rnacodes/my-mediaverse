using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class TvShowMappingService : ITvShowMappingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TvShowMappingService> _logger;

        public TvShowMappingService(IApplicationDbContext context, ILogger<TvShowMappingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TvShow> MapFromDtoAsync(CreateTvShowDto dto)
        {
            var tvShow = new TvShow
            {
                Title = dto.Title,
                MediaType = MediaType.TVShow,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted,
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                Creator = dto.Creator,
                Cast = dto.Cast,
                FirstAirYear = dto.FirstAirYear,
                LastAirYear = dto.LastAirYear,
                NumberOfSeasons = dto.NumberOfSeasons,
                NumberOfEpisodes = dto.NumberOfEpisodes,
                ContentRating = dto.ContentRating,
                TmdbId = dto.TmdbId,
                TmdbRating = dto.TmdbRating,
                TmdbPosterPath = dto.TmdbPosterPath,
                Tagline = dto.Tagline,
                Homepage = dto.Homepage,
                OriginalLanguage = dto.OriginalLanguage,
                OriginalName = dto.OriginalName
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
                        tvShow.Topics.Add(existingTopic);
                    }
                    else
                    {
                        tvShow.Topics.Add(new Topic { Name = normalizedTopicName });
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
                        tvShow.Genres.Add(existingGenre);
                    }
                    else
                    {
                        tvShow.Genres.Add(new Genre { Name = normalizedGenreName });
                    }
                }
            }

            return tvShow;
        }

        public Task<TvShowResponseDto> MapToResponseDtoAsync(TvShow tvShow)
        {
            return Task.FromResult(new TvShowResponseDto
            {
                Id = tvShow.Id,
                Title = tvShow.Title,
                Description = tvShow.Description,
                MediaType = tvShow.MediaType,
                Status = tvShow.Status,
                DateAdded = tvShow.DateAdded,
                Link = tvShow.Link,
                Thumbnail = tvShow.Thumbnail,
                Rating = tvShow.Rating,
                OwnershipStatus = tvShow.OwnershipStatus,
                DateCompleted = tvShow.DateCompleted,
                Notes = tvShow.Notes,
                RelatedNotes = tvShow.RelatedNotes,
                Topics = tvShow.Topics.Select(t => t.Name).ToArray(),
                Genres = tvShow.Genres.Select(g => g.Name).ToArray(),
                Creator = tvShow.Creator,
                Cast = tvShow.Cast,
                FirstAirYear = tvShow.FirstAirYear,
                LastAirYear = tvShow.LastAirYear,
                NumberOfSeasons = tvShow.NumberOfSeasons,
                NumberOfEpisodes = tvShow.NumberOfEpisodes,
                ContentRating = tvShow.ContentRating,
                TmdbId = tvShow.TmdbId,
                TmdbRating = tvShow.TmdbRating,
                TmdbPosterPath = tvShow.TmdbPosterPath,
                Tagline = tvShow.Tagline,
                Homepage = tvShow.Homepage,
                OriginalLanguage = tvShow.OriginalLanguage,
                OriginalName = tvShow.OriginalName,
                TmdbPosterUrl = tvShow.GetTmdbPosterUrl(),
                AirYears = tvShow.GetAirYears(),
                EpisodeCount = tvShow.GetEpisodeCount()
            });
        }

        public Task<TvShow> MapFromTmdbAsync(TmdbTvShowDto tmdbTvShow)
        {
            var tvShow = new TvShow
            {
                Title = tmdbTvShow.Name ?? "Unknown Title",
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Description = tmdbTvShow.Overview,
                Thumbnail = !string.IsNullOrEmpty(tmdbTvShow.PosterPath) 
                    ? $"https://image.tmdb.org/t/p/w500{tmdbTvShow.PosterPath}" 
                    : null,
                TmdbId = tmdbTvShow.Id.ToString(),
                TmdbRating = tmdbTvShow.VoteAverage,
                TmdbPosterPath = tmdbTvShow.PosterPath,
                Tagline = tmdbTvShow.Tagline,
                Homepage = tmdbTvShow.Homepage,
                OriginalLanguage = tmdbTvShow.OriginalLanguage,
                OriginalName = tmdbTvShow.OriginalName,
                NumberOfSeasons = tmdbTvShow.NumberOfSeasons,
                NumberOfEpisodes = tmdbTvShow.NumberOfEpisodes
            };

            // Parse air dates to get years
            if (!string.IsNullOrEmpty(tmdbTvShow.FirstAirDate) && 
                DateTime.TryParse(tmdbTvShow.FirstAirDate, out var firstAirDate))
            {
                tvShow.FirstAirYear = firstAirDate.Year;
            }

            if (!string.IsNullOrEmpty(tmdbTvShow.LastAirDate) && 
                DateTime.TryParse(tmdbTvShow.LastAirDate, out var lastAirDate))
            {
                tvShow.LastAirYear = lastAirDate.Year;
            }


            // Handle genres from TMDB genre IDs (simplified - would need genre mapping service)
            // For now, we'll skip this as it would require a TMDB genre mapping service

            return Task.FromResult(tvShow);
        }

        public Task<TvShowSearchResultDto> MapToSearchResultDtoAsync(TmdbTvShowDto tmdbTvShow)
        {
            return Task.FromResult(new TvShowSearchResultDto
            {
                Id = tmdbTvShow.Id,
                Name = tmdbTvShow.Name,
                Overview = tmdbTvShow.Overview,
                PosterPath = tmdbTvShow.PosterPath,
                BackdropPath = tmdbTvShow.BackdropPath,
                FirstAirDate = tmdbTvShow.FirstAirDate,
                LastAirDate = tmdbTvShow.LastAirDate,
                VoteAverage = tmdbTvShow.VoteAverage,
                Popularity = tmdbTvShow.Popularity,
                OriginalLanguage = tmdbTvShow.OriginalLanguage,
                OriginalName = tmdbTvShow.OriginalName,
                GenreIds = tmdbTvShow.GenreIds,
                OriginCountry = tmdbTvShow.OriginCountry,
                NumberOfEpisodes = tmdbTvShow.NumberOfEpisodes,
                NumberOfSeasons = tmdbTvShow.NumberOfSeasons,
                Tagline = tmdbTvShow.Tagline,
                Homepage = tmdbTvShow.Homepage,
                PosterUrl = !string.IsNullOrEmpty(tmdbTvShow.PosterPath) 
                    ? $"https://image.tmdb.org/t/p/w500{tmdbTvShow.PosterPath}" 
                    : null,
                BackdropUrl = !string.IsNullOrEmpty(tmdbTvShow.BackdropPath)
                    ? $"https://image.tmdb.org/t/p/w1280{tmdbTvShow.BackdropPath}"
                    : null
            });
        }
    }
}
