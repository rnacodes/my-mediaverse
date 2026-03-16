using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.Obsidian;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client for fetching content from Quartz-published Obsidian vaults.
    ///
    /// Quartz publishes a static contentIndex.json file at /static/contentIndex.json
    /// that contains metadata for all notes in the vault.
    /// </summary>
    public class QuartzApiClient : IQuartzApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QuartzApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public QuartzApiClient(HttpClient httpClient, ILogger<QuartzApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Fetches the content index from a Quartz vault.
        /// </summary>
        public async Task<Dictionary<string, QuartzNoteDto>> GetContentIndexAsync(string vaultBaseUrl, string? authToken = null)
        {
            try
            {
                var url = $"{vaultBaseUrl.TrimEnd('/')}/static/contentIndex.json";
                _logger.LogInformation("Fetching Quartz content index from {Url}", url);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Add authentication header if provided
                if (!string.IsNullOrEmpty(authToken))
                {
                    // Support multiple auth formats:
                    // 1. "username:password" - Basic Auth (detected by colon)
                    // 2. "Basic <base64>" - Pre-formatted Basic Auth
                    // 3. "Bearer <token>" - Bearer token
                    // 4. Just a token - defaults to Bearer

                    if (authToken.Contains(':') && !authToken.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
                        && !authToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        // Basic Auth with username:password format
                        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authToken));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                        _logger.LogDebug("Using Basic Authentication for vault request");
                    }
                    else if (authToken.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                    {
                        // Pre-formatted Basic Auth header
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken.Substring(6));
                    }
                    else if (authToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        // Bearer token
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Substring(7));
                    }
                    else
                    {
                        // Default to Bearer token
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    }
                }

                request.Headers.Add("User-Agent", "MyMediaVerse/1.0");

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Authentication required for Quartz vault at {Url}", vaultBaseUrl);
                    throw new UnauthorizedAccessException($"Authentication required for Quartz vault at {vaultBaseUrl}. Please provide a valid auth token.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Content index not found at {Url}. The vault may not have published a contentIndex.json.", url);
                    return new Dictionary<string, QuartzNoteDto>();
                }

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Received content index JSON ({Length} chars)", jsonContent.Length);

                // Quartz contentIndex.json structure is a dictionary where keys are slugs
                var result = JsonSerializer.Deserialize<Dictionary<string, QuartzNoteDto>>(jsonContent, _jsonOptions);

                if (result == null)
                {
                    _logger.LogWarning("Failed to deserialize content index from {Url}", url);
                    return new Dictionary<string, QuartzNoteDto>();
                }

                // Log tag statistics for debugging
                var notesWithTags = result.Count(kvp => kvp.Value.Tags != null && kvp.Value.Tags.Count > 0);
                _logger.LogInformation("Successfully fetched {Count} notes from Quartz vault at {Url}. {NotesWithTags} notes have tags.",
                    result.Count, vaultBaseUrl, notesWithTags);

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching Quartz content index from {VaultUrl}", vaultBaseUrl);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for Quartz content index from {VaultUrl}", vaultBaseUrl);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching Quartz content index from {VaultUrl}", vaultBaseUrl);
                throw;
            }
        }
    }
}
