using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Shared.DTOs.Readwise;
using System;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace MyMediaVerse.Infrastructure.Clients.Readwise
{
    public class ReadwiseApiClient : IReadwiseApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReadwiseApiClient> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _apiToken;

        public ReadwiseApiClient(
            HttpClient httpClient,
            ILogger<ReadwiseApiClient> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Load Readwise API token from environment variables or configuration
            // Check both common environment variable names for flexibility
            _apiToken = Environment.GetEnvironmentVariable("READWISE_API_KEY") ??
                       Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                       _configuration["ApiKeys:Readwise"];

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://readwise.io/api/v2/");
            }
            
            if (!string.IsNullOrEmpty(_apiToken) && _httpClient.DefaultRequestHeaders.Authorization == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Token", _apiToken);
            }
        }

        public async Task<bool> ValidateTokenAsync()
        {
            if (string.IsNullOrEmpty(_apiToken) || _apiToken == "READWISE_API_TOKEN")
            {
                _logger.LogWarning("Readwise API token not configured. Please set READWISE_API_KEY/READWISE_API_TOKEN environment variable or ApiKeys:Readwise in appsettings.json.");
                throw new InvalidOperationException("Readwise API token not configured. Please configure your API key as environment variable (READWISE_API_KEY or READWISE_API_TOKEN) or in appsettings.json (ApiKeys:Readwise).");
            }

            try
            {
                _logger.LogInformation("Validating Readwise API token...");
                var response = await _httpClient.GetAsync("auth/");
                
                _logger.LogInformation("Readwise auth endpoint returned status: {StatusCode}", response.StatusCode);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("Readwise API token is valid");
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Readwise API token is invalid or expired");
                    throw new UnauthorizedAccessException("Readwise API token is invalid or expired. Please check your API key.");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Unexpected response from Readwise API: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"Readwise API returned unexpected status: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while validating Readwise API token");
                throw new HttpRequestException("Failed to connect to Readwise API. Please check your internet connection.", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Error validating Readwise API token");
                throw new Exception("Unexpected error validating Readwise connection: " + ex.Message, ex);
            }
        }

        public async Task<ReadwiseHighlightsResponse> GetHighlightsAsync(
            string? updatedAfter = null,
            int page = 1,
            int pageSize = 1000)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"page_size={Math.Min(pageSize, 1000)}"
                };

                if (!string.IsNullOrEmpty(updatedAfter))
                {
                    queryParams.Add($"updated__gt={Uri.EscapeDataString(updatedAfter)}");
                }

                var query = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"highlights/?{query}");
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReadwiseHighlightsResponse>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Retrieved {Count} highlights from Readwise (page {Page})", 
                    result?.results.Count ?? 0, page);

                return result ?? new ReadwiseHighlightsResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching highlights from Readwise");
                return new ReadwiseHighlightsResponse();
            }
        }

        public async Task<ReadwiseBooksResponse> GetBooksAsync(
            string? updatedAfter = null,
            string? category = null,
            int page = 1,
            int pageSize = 1000)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"page_size={Math.Min(pageSize, 1000)}"
                };

                if (!string.IsNullOrEmpty(updatedAfter))
                {
                    queryParams.Add($"updated__gt={Uri.EscapeDataString(updatedAfter)}");
                }

                if (!string.IsNullOrEmpty(category))
                {
                    queryParams.Add($"category={category}");
                }

                var query = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"books/?{query}");
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReadwiseBooksResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Retrieved {Count} books from Readwise (page {Page})", 
                    result?.results.Count ?? 0, page);

                return result ?? new ReadwiseBooksResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching books from Readwise");
                return new ReadwiseBooksResponse();
            }
        }

        public async Task<ReadwiseBookDto?> GetBookByIdAsync(int bookId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"books/{bookId}/");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Book {BookId} not found in Readwise", bookId);
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ReadwiseBookDto>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching book {BookId} from Readwise", bookId);
                return null;
            }
        }

        public async Task<bool> CreateHighlightsAsync(List<CreateReadwiseHighlightDto> highlights)
        {
            try
            {
                var payload = new { highlights };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("highlights/", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully created/updated {Count} highlights in Readwise", 
                    highlights.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating highlights in Readwise");
                return false;
            }
        }

        public async Task<ReadwiseExportResponse> GetExportAsync(string? updatedAfter = null, string? pageCursor = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(updatedAfter))
                {
                    queryParams.Add($"updatedAfter={Uri.EscapeDataString(updatedAfter)}");
                }

                if (!string.IsNullOrEmpty(pageCursor))
                {
                    queryParams.Add($"pageCursor={Uri.EscapeDataString(pageCursor)}");
                }

                var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"export/{query}");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReadwiseExportResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var totalHighlights = result?.results.Sum(b => b.highlights.Count) ?? 0;
                _logger.LogInformation("Retrieved {BookCount} books with {HighlightCount} highlights from Readwise export",
                    result?.results.Count ?? 0, totalHighlights);

                return result ?? new ReadwiseExportResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching export from Readwise");
                return new ReadwiseExportResponse();
            }
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_apiToken);
        }
    }
}

