using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Interfaces
{
    // Business-level facade for importing/searching movies from external sources.
    // Today this delegates to TMDB. Surface is intentionally narrow (search + import);
    // TMDB-specific affordances (popular, genres, image URLs) remain on ITmdbService.
    public interface IExternalMovieService
    {
        Task<TmdbMovieSearchResultDto> SearchAsync(string query, int page = 1, string language = "en-US");

        Task<Movie> ImportAsync(int sourceId, string language = "en-US");
    }
}
