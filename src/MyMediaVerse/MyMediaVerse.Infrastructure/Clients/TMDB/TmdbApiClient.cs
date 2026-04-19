using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.TMDB
{
    public class TmdbApiClient : ITmdbApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TmdbApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;

        public TmdbApiClient(HttpClient httpClient, ILogger<TmdbApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY") ?? 
                     configuration["ApiKeys:TMDB"] ?? 
                     "TMDB_API_KEY";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        public async Task<TmdbMovieSearchResultDto> SearchMoviesAsync(string query, int page = 1, string language = "en-US")
        {
            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"search/movie?api_key={_apiKey}&query={encodedQuery}&page={page}&language={language}";
                
                _logger.LogInformation($"Searching movies with query: {query}, page: {page}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbMovieSearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbMovieSearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching movies for query: {Query}", query);
                throw;
            }
        }

        public async Task<TmdbTvSearchResultDto> SearchTvShowsAsync(string query, int page = 1, string language = "en-US")
        {
            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"search/tv?api_key={_apiKey}&query={encodedQuery}&page={page}&language={language}";
                
                _logger.LogInformation($"Searching TV shows with query: {query}, page: {page}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbTvSearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbTvSearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching TV shows for query: {Query}", query);
                throw;
            }
        }

        public async Task<TmdbMultiSearchResultDto> SearchMultiAsync(string query, int page = 1, string language = "en-US")
        {
            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"search/multi?api_key={_apiKey}&query={encodedQuery}&page={page}&language={language}";
                
                _logger.LogInformation($"Searching multi with query: {query}, page: {page}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbMultiSearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbMultiSearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching multi for query: {Query}", query);
                throw;
            }
        }

        public async Task<TmdbMovieDto> GetMovieDetailsAsync(int movieId, string language = "en-US")
        {
            try
            {
                var url = $"movie/{movieId}?api_key={_apiKey}&language={language}";
                
                _logger.LogInformation($"Getting movie details for ID: {movieId}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbMovieDto>(jsonContent, _jsonOptions);
                
                if (result == null)
                {
                    throw new InvalidOperationException($"Movie with ID {movieId} not found");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie details for ID: {MovieId}", movieId);
                throw;
            }
        }

        public async Task<TmdbTvShowDto> GetTvShowDetailsAsync(int tvShowId, string language = "en-US")
        {
            try
            {
                var url = $"tv/{tvShowId}?api_key={_apiKey}&language={language}";
                
                _logger.LogInformation($"Getting TV show details for ID: {tvShowId}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbTvShowDto>(jsonContent, _jsonOptions);
                
                if (result == null)
                {
                    throw new InvalidOperationException($"TV show with ID {tvShowId} not found");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TV show details for ID: {TvShowId}", tvShowId);
                throw;
            }
        }

        public async Task<TmdbGenreListDto> GetMovieGenresAsync(string language = "en-US")
        {
            try
            {
                var url = $"genre/movie/list?api_key={_apiKey}&language={language}";
                
                _logger.LogInformation("Getting movie genres");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbGenreListDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbGenreListDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie genres");
                throw;
            }
        }

        public async Task<TmdbGenreListDto> GetTvGenresAsync(string language = "en-US")
        {
            try
            {
                var url = $"genre/tv/list?api_key={_apiKey}&language={language}";
                
                _logger.LogInformation("Getting TV genres");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbGenreListDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbGenreListDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TV genres");
                throw;
            }
        }

        public async Task<TmdbMovieSearchResultDto> GetPopularMoviesAsync(int page = 1, string language = "en-US")
        {
            try
            {
                var url = $"movie/popular?api_key={_apiKey}&page={page}&language={language}";
                
                _logger.LogInformation($"Getting popular movies, page: {page}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbMovieSearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbMovieSearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular movies");
                throw;
            }
        }

        public async Task<TmdbTvSearchResultDto> GetPopularTvShowsAsync(int page = 1, string language = "en-US")
        {
            try
            {
                var url = $"tv/popular?api_key={_apiKey}&page={page}&language={language}";
                
                _logger.LogInformation($"Getting popular TV shows, page: {page}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbTvSearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new TmdbTvSearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular TV shows");
                throw;
            }
        }

        public string GetImageUrl(string? imagePath, string size = "w500")
        {
            if (string.IsNullOrEmpty(imagePath))
                return string.Empty;
                
            return $"https://image.tmdb.org/t/p/{size}{imagePath}";
        }
    }
}
