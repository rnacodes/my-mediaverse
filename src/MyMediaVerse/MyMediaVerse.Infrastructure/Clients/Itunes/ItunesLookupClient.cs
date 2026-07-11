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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up podcast by Apple Podcasts collection id: {CollectionId}", collectionId);
                throw;
            }
        }
    }
}
