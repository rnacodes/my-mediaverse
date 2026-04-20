using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Services
{
    public class ExternalMovieService : IExternalMovieService
    {
        private readonly ITmdbService _tmdb;

        public ExternalMovieService(ITmdbService tmdb)
        {
            _tmdb = tmdb;
        }

        public Task<TmdbMovieSearchResultDto> SearchAsync(string query, int page = 1, string language = "en-US")
            => _tmdb.SearchMoviesAsync(query, page, language);

        public Task<Movie> ImportAsync(int sourceId, string language = "en-US")
            => _tmdb.ImportMovieAsync(sourceId, language);
    }
}
