using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Interfaces
{
    // Business-level facade for importing/searching TV shows from external sources.
    // Today this delegates to TMDB (same source as movies, separate facade per media type).
    public interface IExternalTvShowService
    {
        Task<TmdbTvSearchResultDto> SearchAsync(string query, int page = 1, string language = "en-US");

        Task<TvShow> ImportAsync(int sourceId, string language = "en-US");
    }
}
