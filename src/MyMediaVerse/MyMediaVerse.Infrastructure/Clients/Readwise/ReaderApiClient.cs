using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Shared.DTOs.ReadwiseReader;
using System;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace MyMediaVerse.Infrastructure.Clients.Readwise
{
    public class ReaderApiClient : IReaderApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReaderApiClient> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _apiToken;

        public ReaderApiClient(
            HttpClient httpClient,
            ILogger<ReaderApiClient> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Reuse same token as Readwise API - check environment variables first
            _apiToken = Environment.GetEnvironmentVariable("READWISE_API_KEY") ??
                       Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                       _configuration["ApiKeys:Readwise"];

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://readwise.io/api/v3/");
            }
            
            if (!string.IsNullOrEmpty(_apiToken) && _httpClient.DefaultRequestHeaders.Authorization == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Token", _apiToken);
            }
        }

        public async Task<ReaderDocumentsResponse> GetDocumentsAsync(
            string? updatedAfter = null,
            string? location = null,
            string? category = null,
            string? pageCursor = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(updatedAfter))
                {
                    queryParams.Add($"updatedAfter={Uri.EscapeDataString(updatedAfter)}");
                }

                if (!string.IsNullOrEmpty(location))
                {
                    queryParams.Add($"location={location}");
                }

                if (!string.IsNullOrEmpty(category))
                {
                    queryParams.Add($"category={category}");
                }

                if (!string.IsNullOrEmpty(pageCursor))
                {
                    queryParams.Add($"pageCursor={Uri.EscapeDataString(pageCursor)}");
                }

                var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"list/{query}");
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReaderDocumentsResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Retrieved {Count} documents from Reader", 
                    result?.results.Count ?? 0);

                return result ?? new ReaderDocumentsResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents from Reader");
                return new ReaderDocumentsResponse();
            }
        }

        public async Task<ReaderDocumentDto?> GetDocumentByIdAsync(string documentId, bool includeHtml = true)
        {
            try
            {
                var query = includeHtml ? "&withHtmlContent=true" : "";
                var response = await _httpClient.GetAsync($"list/?id={documentId}{query}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Document {DocumentId} not found in Reader", documentId);
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReaderDocumentsResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document {DocumentId} from Reader", documentId);
                return null;
            }
        }

        public async Task<ReaderDocumentDto?> CreateDocumentAsync(CreateReaderDocumentDto dto)
        {
            try
            {
                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("save/", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReaderDocumentDto>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Successfully created document in Reader: {Url}", dto.url);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document in Reader");
                return null;
            }
        }

        public async Task<bool> UpdateDocumentLocationAsync(string documentId, string location)
        {
            try
            {
                var payload = new { id = documentId, location };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Patch, "save/")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Updated document {DocumentId} location to {Location}", 
                        documentId, location);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId} location", documentId);
                return false;
            }
        }
    }
}

