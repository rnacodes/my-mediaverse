using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.Paperless;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client for Paperless-ngx REST API.
    ///
    /// PLACEHOLDER CONFIGURATION:
    /// This client requires the following environment variables to be set:
    /// - PAPERLESS_API_URL: Base URL of Paperless-ngx API (e.g., http://localhost:8000/api)
    /// - PAPERLESS_API_TOKEN: API token for authentication
    ///
    /// Configure these in Program.cs when registering the HttpClient.
    /// </summary>
    public class PaperlessApiClient : IPaperlessApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaperlessApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PaperlessApiClient(HttpClient httpClient, ILogger<PaperlessApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        // ============================================
        // Document Operations
        // ============================================

        public async Task<PaperlessDocumentListResponseDto> GetDocumentsAsync(int page = 1, int pageSize = 25)
        {
            try
            {
                _logger.LogInformation("Fetching Paperless documents (page: {Page}, pageSize: {PageSize})", page, pageSize);

                var response = await _httpClient.GetAsync($"documents/?page={page}&page_size={pageSize}");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessDocumentListResponseDto>(jsonContent, _jsonOptions);

                return result ?? new PaperlessDocumentListResponseDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless documents");
                throw;
            }
        }

        public async Task<PaperlessDocumentDto?> GetDocumentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching Paperless document with ID: {Id}", id);

                var response = await _httpClient.GetAsync($"documents/{id}/");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaperlessDocumentDto>(jsonContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless document with ID: {Id}", id);
                throw;
            }
        }

        public async Task<PaperlessDocumentListResponseDto> SearchDocumentsAsync(string query, int page = 1, int pageSize = 25)
        {
            try
            {
                _logger.LogInformation("Searching Paperless documents with query: {Query}", query);

                var encodedQuery = Uri.EscapeDataString(query);
                var response = await _httpClient.GetAsync($"documents/?query={encodedQuery}&page={page}&page_size={pageSize}");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessDocumentListResponseDto>(jsonContent, _jsonOptions);

                return result ?? new PaperlessDocumentListResponseDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Paperless documents for query: {Query}", query);
                throw;
            }
        }

        public async Task<byte[]> GetDocumentContentAsync(int id)
        {
            try
            {
                _logger.LogInformation("Downloading Paperless document content for ID: {Id}", id);

                var response = await _httpClient.GetAsync($"documents/{id}/download/");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading Paperless document content for ID: {Id}", id);
                throw;
            }
        }

        public async Task<byte[]> GetDocumentThumbnailAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching thumbnail for Paperless document ID: {Id}", id);

                var response = await _httpClient.GetAsync($"documents/{id}/thumb/");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching thumbnail for Paperless document ID: {Id}", id);
                throw;
            }
        }

        public string GetDocumentPreviewUrl(int id)
        {
            var baseUrl = GetBaseUrl();
            return $"{baseUrl}documents/{id}/preview/";
        }

        public string GetDocumentDownloadUrl(int id)
        {
            var baseUrl = GetBaseUrl();
            return $"{baseUrl}documents/{id}/download/";
        }

        public async Task<string> UploadDocumentAsync(
            Stream fileStream,
            string fileName,
            string? title = null,
            int? correspondentId = null,
            int? documentTypeId = null,
            List<int>? tagIds = null)
        {
            try
            {
                _logger.LogInformation("Uploading document to Paperless: {FileName}", fileName);

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);

                // Set content type based on file extension
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".tiff" or ".tif" => "image/tiff",
                    ".txt" => "text/plain",
                    _ => "application/octet-stream"
                };
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                content.Add(fileContent, "document", fileName);

                if (!string.IsNullOrEmpty(title))
                    content.Add(new StringContent(title), "title");

                if (correspondentId.HasValue)
                    content.Add(new StringContent(correspondentId.Value.ToString()), "correspondent");

                if (documentTypeId.HasValue)
                    content.Add(new StringContent(documentTypeId.Value.ToString()), "document_type");

                if (tagIds?.Any() == true)
                {
                    foreach (var tagId in tagIds)
                    {
                        content.Add(new StringContent(tagId.ToString()), "tags");
                    }
                }

                var response = await _httpClient.PostAsync("documents/post_document/", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Paperless returns a task ID for async processing
                _logger.LogInformation("Document uploaded successfully: {FileName}, Response: {Response}", fileName, responseContent);

                return responseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document to Paperless: {FileName}", fileName);
                throw;
            }
        }

        public async Task<PaperlessDocumentDto> UpdateDocumentAsync(
            int id,
            string? title = null,
            int? correspondentId = null,
            int? documentTypeId = null,
            List<int>? tagIds = null)
        {
            try
            {
                _logger.LogInformation("Updating Paperless document ID: {Id}", id);

                var updateData = new Dictionary<string, object>();

                if (title != null)
                    updateData["title"] = title;

                if (correspondentId.HasValue)
                    updateData["correspondent"] = correspondentId.Value;

                if (documentTypeId.HasValue)
                    updateData["document_type"] = documentTypeId.Value;

                if (tagIds != null)
                    updateData["tags"] = tagIds;

                var jsonContent = JsonSerializer.Serialize(updateData, _jsonOptions);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"documents/{id}/", httpContent);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessDocumentDto>(responseJson, _jsonOptions);

                return result ?? throw new InvalidOperationException("Failed to deserialize updated document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Paperless document ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting Paperless document ID: {Id}", id);

                var response = await _httpClient.DeleteAsync($"documents/{id}/");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Paperless document ID: {Id}", id);
                throw;
            }
        }

        // ============================================
        // Tag Operations
        // ============================================

        public async Task<List<PaperlessTagDto>> GetTagsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Paperless tags");

                var response = await _httpClient.GetAsync("tags/?page_size=1000");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessTagListResponseDto>(jsonContent, _jsonOptions);

                return result?.Results ?? new List<PaperlessTagDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless tags");
                throw;
            }
        }

        public async Task<PaperlessTagDto?> GetTagByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching Paperless tag ID: {Id}", id);

                var response = await _httpClient.GetAsync($"tags/{id}/");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaperlessTagDto>(jsonContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless tag ID: {Id}", id);
                throw;
            }
        }

        public async Task<PaperlessTagDto> CreateTagAsync(string name, string? color = null)
        {
            try
            {
                _logger.LogInformation("Creating Paperless tag: {Name}", name);

                var tagData = new Dictionary<string, object>
                {
                    ["name"] = name
                };

                if (!string.IsNullOrEmpty(color))
                    tagData["color"] = color;

                var jsonContent = JsonSerializer.Serialize(tagData, _jsonOptions);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("tags/", httpContent);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessTagDto>(responseJson, _jsonOptions);

                return result ?? throw new InvalidOperationException("Failed to deserialize created tag");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Paperless tag: {Name}", name);
                throw;
            }
        }

        // ============================================
        // Document Type Operations
        // ============================================

        public async Task<List<PaperlessDocumentTypeDto>> GetDocumentTypesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Paperless document types");

                var response = await _httpClient.GetAsync("document_types/?page_size=1000");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessDocumentTypeListResponseDto>(jsonContent, _jsonOptions);

                return result?.Results ?? new List<PaperlessDocumentTypeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless document types");
                throw;
            }
        }

        public async Task<PaperlessDocumentTypeDto?> GetDocumentTypeByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching Paperless document type ID: {Id}", id);

                var response = await _httpClient.GetAsync($"document_types/{id}/");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaperlessDocumentTypeDto>(jsonContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless document type ID: {Id}", id);
                throw;
            }
        }

        // ============================================
        // Correspondent Operations
        // ============================================

        public async Task<List<PaperlessCorrespondentDto>> GetCorrespondentsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Paperless correspondents");

                var response = await _httpClient.GetAsync("correspondents/?page_size=1000");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PaperlessCorrespondentListResponseDto>(jsonContent, _jsonOptions);

                return result?.Results ?? new List<PaperlessCorrespondentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless correspondents");
                throw;
            }
        }

        public async Task<PaperlessCorrespondentDto?> GetCorrespondentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching Paperless correspondent ID: {Id}", id);

                var response = await _httpClient.GetAsync($"correspondents/{id}/");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaperlessCorrespondentDto>(jsonContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Paperless correspondent ID: {Id}", id);
                throw;
            }
        }

        // ============================================
        // Health & Utility
        // ============================================

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Check if we can reach the API by fetching a single document
                var response = await _httpClient.GetAsync("documents/?page_size=1");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Paperless API is not available");
                return false;
            }
        }

        public string GetBaseUrl()
        {
            return _httpClient.BaseAddress?.ToString() ?? string.Empty;
        }
    }
}
