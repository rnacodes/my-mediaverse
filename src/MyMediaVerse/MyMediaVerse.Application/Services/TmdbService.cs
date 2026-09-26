using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class TmdbService : ITmdbService
    {
        private readonly ITmdbApiClient _tmdbApiClient;
        private readonly ILogger<TmdbService> _logger;

        public TmdbService(
            ITmdbApiClient tmdbApiClient,
            ILogger<TmdbService> logger)
        {
            _tmdbApiClient = tmdbApiClient;
            _logger = logger;
        }

        // Search operations (return DTOs for API consumption)
        public async Task<TmdbMovieSearchResultDto> SearchMoviesAsync(string query, int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Searching movies with query: {Query}, page: {Page}", query, page);
            return await _tmdbApiClient.SearchMoviesAsync(query, page, language);
        }

        public async Task<TmdbTvSearchResultDto> SearchTvShowsAsync(string query, int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Searching TV shows with query: {Query}, page: {Page}", query, page);
            return await _tmdbApiClient.SearchTvShowsAsync(query, page, language);
        }

        public async Task<TmdbMultiSearchResultDto> SearchMultiAsync(string query, int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Searching multi with query: {Query}, page: {Page}", query, page);
            return await _tmdbApiClient.SearchMultiAsync(query, page, language);
        }

        // Detail operations (return DTOs for API consumption)
        public async Task<TmdbMovieDto> GetMovieDetailsAsync(int movieId, string language = "en-US")
        {
            _logger.LogInformation("Getting movie details for ID: {MovieId}", movieId);
            return await _tmdbApiClient.GetMovieDetailsAsync(movieId, language);
        }

        public async Task<TmdbTvShowDto> GetTvShowDetailsAsync(int tvShowId, string language = "en-US")
        {
            _logger.LogInformation("Getting TV show details for ID: {TvShowId}", tvShowId);
            return await _tmdbApiClient.GetTvShowDetailsAsync(tvShowId, language);
        }

        // Popular content operations
        public async Task<TmdbMovieSearchResultDto> GetPopularMoviesAsync(int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Getting popular movies, page: {Page}", page);
            return await _tmdbApiClient.GetPopularMoviesAsync(page, language);
        }

        public async Task<TmdbTvSearchResultDto> GetPopularTvShowsAsync(int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Getting popular TV shows, page: {Page}", page);
            return await _tmdbApiClient.GetPopularTvShowsAsync(page, language);
        }

        // Genre operations
        public async Task<TmdbGenreListDto> GetMovieGenresAsync(string language = "en-US")
        {
            _logger.LogInformation("Getting movie genres");
            return await _tmdbApiClient.GetMovieGenresAsync(language);
        }

        public async Task<TmdbGenreListDto> GetTvGenresAsync(string language = "en-US")
        {
            _logger.LogInformation("Getting TV genres");
            return await _tmdbApiClient.GetTvGenresAsync(language);
        }

        // Utility operations
        public string GetImageUrl(string imagePath, string size = "w500")
        {
            return _tmdbApiClient.GetImageUrl(imagePath, size);
        }
    }
}
