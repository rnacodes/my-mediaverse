using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.Google
{
    /// <summary>
    /// Client for Google Books API.
    /// Requires GOOGLE_BOOKS_API_KEY environment variable or GoogleBooks:ApiKey configuration.
    /// </summary>
    public class GoogleBooksApiClient : IGoogleBooksApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleBooksApiClient> _logger;
        private readonly string? _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public GoogleBooksApiClient(
            HttpClient httpClient,
            ILogger<GoogleBooksApiClient> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = Environment.GetEnvironmentVariable("GOOGLE_BOOKS_API_KEY")
                      ?? configuration["GoogleBooks:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google Books API key not configured. Set GOOGLE_BOOKS_API_KEY environment variable or GoogleBooks:ApiKey in configuration.");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksAsync(string query, int? startIndex = null, int? maxResults = null)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"q={Uri.EscapeDataString(query)}"
                };

                if (startIndex.HasValue) queryParams.Add($"startIndex={startIndex}");
                if (maxResults.HasValue) queryParams.Add($"maxResults={Math.Min(maxResults.Value, 40)}"); // Max 40 per request
                if (!string.IsNullOrEmpty(_apiKey)) queryParams.Add($"key={_apiKey}");

                var queryString = string.Join("&", queryParams);
                var fullUrl = $"volumes?{queryString}";

                _logger.LogInformation("Searching Google Books with query: {Query}, startIndex: {StartIndex}, maxResults: {MaxResults}",
                    query, startIndex, maxResults);

                var response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleBooksSearchResultDto>(jsonContent, _jsonOptions);

                return result ?? new GoogleBooksSearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Google Books for query: {Query}", query);
                throw;
            }
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksByTitleAsync(string title, int? startIndex = null, int? maxResults = null)
        {
            var query = $"intitle:{title}";
            return await SearchBooksAsync(query, startIndex, maxResults);
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksByAuthorAsync(string author, int? startIndex = null, int? maxResults = null)
        {
            var query = $"inauthor:{author}";
            return await SearchBooksAsync(query, startIndex, maxResults);
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksByISBNAsync(string isbn)
        {
            // Clean ISBN - remove dashes and spaces
            var cleanIsbn = isbn.Replace("-", "").Replace(" ", "").Trim();
            var query = $"isbn:{cleanIsbn}";
            return await SearchBooksAsync(query, maxResults: 1);
        }

        public async Task<GoogleBooksVolumeDto?> GetVolumeByIdAsync(string volumeId)
        {
            try
            {
                _logger.LogInformation("Getting Google Books volume details for ID: {VolumeId}", volumeId);

                var url = $"volumes/{volumeId}";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"?key={_apiKey}";
                }

                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Volume not found for ID: {VolumeId}", volumeId);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GoogleBooksVolumeDto>(jsonContent, _jsonOptions);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Volume not found for ID: {VolumeId}", volumeId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Google Books volume for ID: {VolumeId}", volumeId);
                throw;
            }
        }

        public async Task<string?> GetBookDescriptionByISBNAsync(string isbn)
        {
            try
            {
                _logger.LogInformation("Getting book description for ISBN: {ISBN}", isbn);

                var searchResult = await SearchBooksByISBNAsync(isbn);

                var volume = searchResult.Items?.FirstOrDefault();
                if (volume?.VolumeInfo?.Description == null)
                {
                    _logger.LogWarning("No description found for ISBN: {ISBN}", isbn);
                    return null;
                }

                // Strip HTML tags from description
                var description = StripHtmlTags(volume.VolumeInfo.Description);

                _logger.LogDebug("Found description for ISBN {ISBN}: {DescriptionPreview}...",
                    isbn, description.Length > 100 ? description.Substring(0, 100) : description);

                return description;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book description for ISBN: {ISBN}", isbn);
                return null;
            }
        }

        /// <summary>
        /// Strips HTML tags from a string and decodes HTML entities.
        /// </summary>
        private static string StripHtmlTags(string? html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // Remove HTML tags
            var withoutTags = Regex.Replace(html, "<.*?>", " ");

            // Decode common HTML entities
            withoutTags = withoutTags
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#39;", "'")
                .Replace("&apos;", "'");

            // Collapse multiple spaces into one
            withoutTags = Regex.Replace(withoutTags, @"\s+", " ");

            return withoutTags.Trim();
        }
    }
}
