using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IMovieMappingService
    {
        Task<Movie> MapFromDtoAsync(CreateMovieDto dto);
        Task<MovieResponseDto> MapToResponseDtoAsync(Movie movie);
        Task<Movie> MapFromTmdbAsync(TmdbMovieDto tmdbMovie);
        Task<MovieSearchResultDto> MapToSearchResultDtoAsync(TmdbMovieDto tmdbMovie);
    }
}
