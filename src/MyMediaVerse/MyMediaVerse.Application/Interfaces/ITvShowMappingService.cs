using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Interfaces
{
    public interface ITvShowMappingService
    {
        Task<TvShow> MapFromDtoAsync(CreateTvShowDto dto);
        Task<TvShowResponseDto> MapToResponseDtoAsync(TvShow tvShow);
        Task<TvShow> MapFromTmdbAsync(TmdbTvShowDto tmdbTvShow);
        Task<TvShowSearchResultDto> MapToSearchResultDtoAsync(TmdbTvShowDto tmdbTvShow);
    }
}
