using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.OpenLibrary
{
    public class OpenLibraryApiClient : IOpenLibraryApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenLibraryApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public OpenLibraryApiClient(HttpClient httpClient, ILogger<OpenLibraryApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<OpenLibrarySearchResultDto> SearchBooksAsync(string query, int? offset = null, int? limit = null)
        {
            try
            {
                var queryParams = new List<string> { $"q={Uri.EscapeDataString(query)}" };

                // Request specific fields including isbn to ensure it's returned in results
                queryParams.Add("fields=key,title,author_name,author_key,first_publish_year,isbn,subject,cover_i,publisher,language,publish_date,publish_year,number_of_pages_median,rating_average,rating_count,has_fulltext,edition_count,edition_key");

                if (offset.HasValue) queryParams.Add($"offset={offset}");
                if (limit.HasValue) queryParams.Add($"limit={limit}");

                var queryString = string.Join("&", queryParams);
                var fullUrl = $"search.json?{queryString}";
                
                _logger.LogInformation("Searching OpenLibrary books with query: {Query}, offset: {Offset}, limit: {Limit}", query, offset, limit);
                
                var response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenLibrarySearchResultDto>(jsonContent, _jsonOptions);
                
                return result ?? new OpenLibrarySearchResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OpenLibrary books for query: {Query}", query);
                throw;
            }
        }

        public async Task<OpenLibrarySearchResultDto> SearchBooksByTitleAsync(string title, int? offset = null, int? limit = null)
        {
            var query = $"title:{title}";
            return await SearchBooksAsync(query, offset, limit);
        }

        public async Task<OpenLibrarySearchResultDto> SearchBooksByAuthorAsync(string author, int? offset = null, int? limit = null)
        {
            var query = $"author:{author}";
            return await SearchBooksAsync(query, offset, limit);
        }

        public async Task<OpenLibrarySearchResultDto> SearchBooksByISBNAsync(string isbn)
        {
            var query = $"isbn:{isbn}";
            return await SearchBooksAsync(query);
        }

        public async Task<OpenLibraryWorkDto> GetBookByOpenLibraryIdAsync(string openLibraryId)
        {
            try
            {
                _logger.LogInformation("Getting OpenLibrary work details for ID: {OpenLibraryId}", openLibraryId);
                
                var response = await _httpClient.GetAsync($"works/{openLibraryId}.json");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenLibraryWorkDto>(jsonContent, _jsonOptions);
                
                if (result == null)
                {
                    throw new InvalidOperationException($"Work with ID {openLibraryId} not found");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary work details for ID: {OpenLibraryId}", openLibraryId);
                throw;
            }
        }

        public async Task<OpenLibraryBookDto> GetBookByISBNAsync(string isbn)
        {
            try
            {
                _logger.LogInformation("Getting OpenLibrary book details for ISBN: {ISBN}", isbn);

                var response = await _httpClient.GetAsync($"isbn/{isbn}.json");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenLibraryBookDto>(jsonContent, _jsonOptions);

                if (result == null)
                {
                    throw new InvalidOperationException($"Book with ISBN {isbn} not found");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary book details for ISBN: {ISBN}", isbn);
                throw;
            }
        }

        public async Task<OpenLibraryEditionDto?> GetEditionByISBNAsync(string isbn)
        {
            try
            {
                _logger.LogInformation("Getting OpenLibrary edition for ISBN: {ISBN}", isbn);

                var response = await _httpClient.GetAsync($"isbn/{isbn}.json");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("No edition found for ISBN: {ISBN}", isbn);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OpenLibraryEditionDto>(jsonContent, _jsonOptions);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No edition found for ISBN: {ISBN}", isbn);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary edition for ISBN: {ISBN}", isbn);
                throw;
            }
        }

        public async Task<string?> GetBookDescriptionByISBNAsync(string isbn)
        {
            try
            {
                _logger.LogInformation("Getting book description for ISBN: {ISBN}", isbn);

                // Step 1: Get edition to find Work ID
                var edition = await GetEditionByISBNAsync(isbn);
                if (edition == null)
                {
                    _logger.LogWarning("No edition found for ISBN: {ISBN}", isbn);
                    return null;
                }

                var workId = edition.GetWorkId();
                if (string.IsNullOrEmpty(workId))
                {
                    _logger.LogWarning("No work reference found for ISBN: {ISBN}", isbn);
                    return null;
                }

                // Step 2: Get work to retrieve description
                var work = await GetBookByOpenLibraryIdAsync(workId);

                return ExtractDescription(work.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book description for ISBN: {ISBN}", isbn);
                return null;
            }
        }

        /// <summary>
        /// Extracts description text from the Open Library description field,
        /// which can be either a string or an object with a "value" property.
        /// </summary>
        private static string? ExtractDescription(object? description)
        {
            if (description == null)
                return null;

            // If it's already a string, return it
            if (description is string str)
                return str;

            // If it's a JsonElement (from deserialization), handle both cases
            if (description is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString();
                }
                else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var valueElement))
                {
                    return valueElement.GetString();
                }
            }

            return null;
        }

        public async Task<OpenLibraryAuthorDto> GetAuthorAsync(string authorId)
        {
            try
            {
                _logger.LogInformation("Getting OpenLibrary author details for ID: {AuthorId}", authorId);
                
                var response = await _httpClient.GetAsync($"authors/{authorId}.json");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenLibraryAuthorDto>(jsonContent, _jsonOptions);
                
                if (result == null)
                {
                    throw new InvalidOperationException($"Author with ID {authorId} not found");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary author details for ID: {AuthorId}", authorId);
                throw;
            }
        }

        public async Task<string> GetSubjectsAsync()
        {
            try
            {
                _logger.LogInformation("Getting OpenLibrary subjects");
                
                var response = await _httpClient.GetAsync("subjects.json");
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary subjects");
                throw;
            }
        }

        public string GetCoverImageUrl(int? coverId, string size = "L")
        {
            if (!coverId.HasValue)
                return string.Empty;
                
            return $"https://covers.openlibrary.org/b/id/{coverId}-{size}.jpg";
        }
    }
}
