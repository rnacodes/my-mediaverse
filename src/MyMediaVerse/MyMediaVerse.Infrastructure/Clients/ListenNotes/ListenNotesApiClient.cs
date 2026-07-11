using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.ListenNotes
{
    public class ListenNotesApiClient : IListenNotesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ListenNotesApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;

        public ListenNotesApiClient(HttpClient httpClient, ILogger<ListenNotesApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = Environment.GetEnvironmentVariable("LISTENNOTES_API_KEY") ?? 
                     configuration["ApiKeys:ListenNotes"] ?? 
                     "LISTENNOTES_API_KEY";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<PodcastSeriesDto> GetPodcastByIdAsync(string id, string? nextEpisodePubDate = null)
        {
            try
            {
                _logger.LogInformation("Getting podcast details for ID: {PodcastId}", id);
                
                var url = $"podcasts/{id}";
                if (!string.IsNullOrEmpty(nextEpisodePubDate))
                {
                    url += $"?next_episode_pub_date={Uri.EscapeDataString(nextEpisodePubDate)}";
                }
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PodcastSeriesDto>(jsonContent, _jsonOptions);
                
                return result ?? new PodcastSeriesDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting podcast details for ID: {PodcastId}", id);
                throw;
            }
        }

        public async Task<PodcastSeriesDto?> GetPodcastByItunesIdAsync(string itunesId)
        {
            try
            {
                _logger.LogInformation("Looking up podcast by Apple Podcasts (iTunes) id: {ItunesId}", itunesId);

                // POST /podcasts is the batch endpoint; passing a single itunes_ids value
                // yields an exact match (or an empty podcasts array when unindexed).
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["itunes_ids"] = itunesId
                });

                var response = await _httpClient.PostAsync("podcasts", content);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesBatchPodcastsDto>(jsonContent, _jsonOptions);

                return result?.Podcasts.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up podcast by iTunes id: {ItunesId}", itunesId);
                throw;
            }
        }

        public async Task<ListenNotesPlaylistsDto> GetPlaylistsAsync()
        {
            try
            {
                _logger.LogInformation("Getting playlists");
                
                var response = await _httpClient.GetAsync("playlists");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesPlaylistsDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesPlaylistsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting playlists");
                throw;
            }
        }

        public async Task<ListenNotesPlaylistDto> GetPlaylistByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting playlist details for ID: {PlaylistId}", id);
                
                var response = await _httpClient.GetAsync($"playlists/{id}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesPlaylistDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesPlaylistDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting playlist details for ID: {PlaylistId}", id);
                throw;
            }
        }

        public async Task<SearchResultDto> SearchAsync(string query, string? type = null, int? offset = null, 
            int? lenMin = null, int? lenMax = null, string? genreIds = null, 
            string? publishedBefore = null, string? publishedAfter = null, 
            string? onlyIn = null, string? language = null, string? region = null, 
            string? sortByDate = null, string? safeMode = null, string? uniquePodcasts = null)
        {
            try
            {
                _logger.LogInformation("Searching podcasts with query: {Query}", query);
                
                var queryParams = new List<string> { $"q={Uri.EscapeDataString(query)}" };

                if (type != null) queryParams.Add($"type={Uri.EscapeDataString(type)}");
                if (offset.HasValue) queryParams.Add($"offset={offset}");
                if (lenMin.HasValue) queryParams.Add($"len_min={lenMin}");
                if (lenMax.HasValue) queryParams.Add($"len_max={lenMax}");
                if (genreIds != null) queryParams.Add($"genre_ids={Uri.EscapeDataString(genreIds)}");
                if (publishedBefore != null) queryParams.Add($"published_before={Uri.EscapeDataString(publishedBefore)}");
                if (publishedAfter != null) queryParams.Add($"published_after={Uri.EscapeDataString(publishedAfter)}");
                if (onlyIn != null) queryParams.Add($"only_in={Uri.EscapeDataString(onlyIn)}");
                if (language != null) queryParams.Add($"language={Uri.EscapeDataString(language)}");
                if (region != null) queryParams.Add($"region={Uri.EscapeDataString(region)}");
                if (sortByDate != null) queryParams.Add($"sort_by_date={Uri.EscapeDataString(sortByDate)}");
                if (safeMode != null) queryParams.Add($"safe_mode={Uri.EscapeDataString(safeMode)}");
                if (uniquePodcasts != null) queryParams.Add($"unique_podcasts={Uri.EscapeDataString(uniquePodcasts)}");

                var queryString = string.Join("&", queryParams);
                var fullUrl = $"search?{queryString}";
                
                _logger.LogDebug("Making request to: {BaseAddress}{FullUrl}", _httpClient.BaseAddress, fullUrl);
                
                var response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new SearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching podcasts with query: {Query}", query);
                throw;
            }
        }

        public async Task<ListenNotesGenresDto> GetGenresAsync()
        {
            try
            {
                _logger.LogInformation("Getting genres");
                
                var response = await _httpClient.GetAsync("genres");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesGenresDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesGenresDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genres");
                throw;
            }
        }

        public async Task<PodcastEpisodeDto> GetEpisodeByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting episode details for ID: {EpisodeId}", id);
                
                var response = await _httpClient.GetAsync($"episodes/{id}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PodcastEpisodeDto>(jsonContent, _jsonOptions);
                
                return result ?? new PodcastEpisodeDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episode details for ID: {EpisodeId}", id);
                throw;
            }
        }

        public async Task<ListenNotesBestPodcastsDto> GetBestPodcastsAsync(int? genreId = null, int? page = null, 
            string? region = null, string? sortByDate = null, bool? safeMode = null)
        {
            try
            {
                _logger.LogInformation("Getting best podcasts");
                
                var queryParams = new List<string>();

                if (genreId.HasValue) queryParams.Add($"genre_id={genreId}");
                if (page.HasValue) queryParams.Add($"page={page}");
                if (region != null) queryParams.Add($"region={Uri.EscapeDataString(region)}");
                if (sortByDate != null) queryParams.Add($"sort_by_date={Uri.EscapeDataString(sortByDate)}");
                if (safeMode.HasValue) queryParams.Add($"safe_mode={safeMode.Value.ToString().ToLower()}");

                var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                var response = await _httpClient.GetAsync($"best_podcasts{queryString}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesBestPodcastsDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesBestPodcastsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting best podcasts");
                throw;
            }
        }

        public async Task<ListenNotesCuratedPodcastsDto> GetCuratedPodcastsAsync(int? page = null)
        {
            try
            {
                _logger.LogInformation("Getting curated podcasts");
                
                var queryString = page.HasValue ? $"?page={page}" : "";
                var response = await _httpClient.GetAsync($"curated_podcasts{queryString}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesCuratedPodcastsDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesCuratedPodcastsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting curated podcasts");
                throw;
            }
        }

        public async Task<ListenNotesCuratedPodcastDto> GetCuratedPodcastByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting curated podcast details for ID: {CuratedPodcastId}", id);
                
                var response = await _httpClient.GetAsync($"curated_podcasts/{id}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesCuratedPodcastDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesCuratedPodcastDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting curated podcast details for ID: {CuratedPodcastId}", id);
                throw;
            }
        }

        public async Task<ListenNotesRecommendationsDto> GetPodcastRecommendationsAsync(string id, bool? safeMode = null)
        {
            try
            {
                _logger.LogInformation("Getting podcast recommendations for ID: {PodcastId}", id);
                
                var queryString = safeMode.HasValue ? $"?safe_mode={safeMode.Value.ToString().ToLower()}" : "";
                var response = await _httpClient.GetAsync($"podcasts/{id}/recommendations{queryString}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesRecommendationsDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesRecommendationsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting podcast recommendations for ID: {PodcastId}", id);
                throw;
            }
        }

        public async Task<ListenNotesRecommendationsDto> GetEpisodeRecommendationsAsync(string id, bool? safeMode = null)
        {
            try
            {
                _logger.LogInformation("Getting episode recommendations for ID: {EpisodeId}", id);
                
                var queryString = safeMode.HasValue ? $"?safe_mode={safeMode.Value.ToString().ToLower()}" : "";
                var response = await _httpClient.GetAsync($"episodes/{id}/recommendations{queryString}");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ListenNotesRecommendationsDto>(jsonContent, _jsonOptions);
                
                return result ?? new ListenNotesRecommendationsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episode recommendations for ID: {EpisodeId}", id);
                throw;
            }
        }
    }
}
