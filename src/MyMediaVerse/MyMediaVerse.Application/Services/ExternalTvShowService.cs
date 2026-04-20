using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Services
{
    public class ExternalTvShowService : IExternalTvShowService
    {
        private readonly ITmdbService _tmdb;

        public ExternalTvShowService(ITmdbService tmdb)
        {
            _tmdb = tmdb;
        }

        public Task<TmdbTvSearchResultDto> SearchAsync(string query, int page = 1, string language = "en-US")
            => _tmdb.SearchTvShowsAsync(query, page, language);

        public Task<TvShow> ImportAsync(int sourceId, string language = "en-US")
            => _tmdb.ImportTvShowAsync(sourceId, language);
    }
}
