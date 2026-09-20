using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.Itunes
{
    /// <summary>
    /// Calls Apple's free iTunes Lookup API. No API key or auth is required.
    /// </summary>
    public class ItunesLookupClient : IItunesLookupClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ItunesLookupClient> _logger;

        public ItunesLookupClient(HttpClient httpClient, ILogger<ItunesLookupClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ItunesPodcastDto?> GetPodcastByCollectionIdAsync(
            string collectionId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Looking up podcast by Apple Podcasts collection id: {CollectionId}", collectionId);

                var url = $"lookup?id={Uri.EscapeDataString(collectionId)}&entity=podcast";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ItunesLookupResponseDto>(jsonContent);

                // A lookup by id returns at most one podcast collection.
                return result?.Results.FirstOrDefault(r => r.Kind is null or "podcast");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Apple Podcasts lookup for {CollectionId} was refused as busy", collectionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up podcast by Apple Podcasts collection id: {CollectionId}", collectionId);
                throw;
            }
        }

        /// <summary>The Search API query for podcasts (shows only, not episodes).</summary>
        public static string BuildSearchQuery(string term, int limit) =>
            $"search?media=podcast&entity=podcast&term={Uri.EscapeDataString(term)}&limit={limit}";

        public async Task<IReadOnlyList<ItunesPodcastDto>> SearchPodcastsAsync(
            string term, int limit, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Searching Apple Podcasts for: {Term}", term);

                var response = await _httpClient.GetAsync(BuildSearchQuery(term, limit), cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ItunesLookupResponseDto>(jsonContent);

                return result?.Results.Where(r => r.Kind is null or "podcast").ToList()
                    ?? new List<ItunesPodcastDto>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Apple Podcasts search for {Term} was refused as busy", term);
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error searching Apple Podcasts for: {Term}", term);
                throw;
            }
        }
    }
}
