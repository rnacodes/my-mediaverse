using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.Trakt
{
    public class TraktApiClient : ITraktApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TraktApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public TraktApiClient(HttpClient httpClient, ILogger<TraktApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _clientId = Environment.GetEnvironmentVariable("TRAKT_CLIENT_ID") ??
                        configuration["ApiKeys:TraktClientId"] ??
                        "TRAKT_CLIENT_ID";

            _clientSecret = Environment.GetEnvironmentVariable("TRAKT_CLIENT_SECRET") ??
                            configuration["ApiKeys:TraktClientSecret"] ??
                            "TRAKT_CLIENT_SECRET";

            // Set the trakt-api-key header (= client_id) on all requests
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-key", _clientId);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        // --- Device Auth Flow ---

        public async Task<TraktDeviceCodeDto> GetDeviceCodeAsync()
        {
            try
            {
                _logger.LogInformation("Requesting Trakt device code");

                var payload = new { client_id = _clientId };
                var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("oauth/device/code", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TraktDeviceCodeDto>(json, _jsonOptions);

                return result ?? throw new InvalidOperationException("Failed to deserialize Trakt device code response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting Trakt device code");
                throw;
            }
        }

        public async Task<TraktOAuthTokenDto?> PollDeviceTokenAsync(string deviceCode)
        {
            try
            {
                var payload = new
                {
                    code = deviceCode,
                    client_id = _clientId,
                    client_secret = _clientSecret
                };
                var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("oauth/device/token", content);

                // 400 = pending authorization, return null to indicate polling should continue
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return null;
                }

                // 409 = code already approved
                // 410 = code expired
                // 418 = code denied
                // 429 = polling too fast
                if (response.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    throw new InvalidOperationException("Device code has expired. Please restart the authorization process.");
                }

                if ((int)response.StatusCode == 418)
                {
                    throw new InvalidOperationException("User denied the authorization request.");
                }

                if ((int)response.StatusCode == 429)
                {
                    _logger.LogWarning("Trakt device token polling too fast, slowing down");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TraktOAuthTokenDto>(json, _jsonOptions);

                _logger.LogInformation("Successfully obtained Trakt access token via device auth");
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error polling Trakt device token");
                throw;
            }
        }

        // --- Token Management ---

        public async Task<TraktOAuthTokenDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Refreshing Trakt access token");

                var payload = new
                {
                    refresh_token = refreshToken,
                    client_id = _clientId,
                    client_secret = _clientSecret,
                    redirect_uri = "urn:ietf:wg:oauth:2.0:oob",
                    grant_type = "refresh_token"
                };
                var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("oauth/token", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TraktOAuthTokenDto>(json, _jsonOptions);

                _logger.LogInformation("Successfully refreshed Trakt access token");
                return result ?? throw new InvalidOperationException("Failed to deserialize Trakt token refresh response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Trakt access token");
                throw;
            }
        }

        public async Task RevokeTokenAsync(string accessToken)
        {
            try
            {
                _logger.LogInformation("Revoking Trakt access token");

                var payload = new
                {
                    token = accessToken,
                    client_id = _clientId,
                    client_secret = _clientSecret
                };
                var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("oauth/revoke", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully revoked Trakt access token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking Trakt access token");
                throw;
            }
        }

        // --- Sync Endpoints ---

        // extended=full is required so the nested movie/show objects include their genre slugs.
        public async Task<List<TraktWatchedMovieDto>> GetWatchedMoviesAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<List<TraktWatchedMovieDto>>("sync/watched/movies?extended=full", accessToken)
                   ?? new List<TraktWatchedMovieDto>();
        }

        public async Task<List<TraktWatchedShowDto>> GetWatchedShowsAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<List<TraktWatchedShowDto>>("sync/watched/shows?extended=full", accessToken)
                   ?? new List<TraktWatchedShowDto>();
        }

        public async Task<List<TraktWatchlistItemDto>> GetWatchlistMoviesAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<List<TraktWatchlistItemDto>>("sync/watchlist/movies?extended=full", accessToken)
                   ?? new List<TraktWatchlistItemDto>();
        }

        public async Task<List<TraktWatchlistItemDto>> GetWatchlistShowsAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<List<TraktWatchlistItemDto>>("sync/watchlist/shows?extended=full", accessToken)
                   ?? new List<TraktWatchlistItemDto>();
        }

        public async Task<List<TraktRatingItemDto>> GetRatingsMoviesAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<List<TraktRatingItemDto>>("sync/ratings/movies?extended=full", accessToken)
                   ?? new List<TraktRatingItemDto>();
        }

        public async Task<List<TraktRatingItemDto>> GetRatingsShowsAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<List<TraktRatingItemDto>>("sync/ratings/shows?extended=full", accessToken)
                   ?? new List<TraktRatingItemDto>();
        }

        public async Task<TraktLastActivitiesDto> GetLastActivitiesAsync(string accessToken)
        {
            return await GetAuthenticatedAsync<TraktLastActivitiesDto>("sync/last_activities", accessToken)
                   ?? new TraktLastActivitiesDto();
        }

        // --- Private Helpers ---

        private async Task<T?> GetAuthenticatedAsync<T>(string url, string accessToken) where T : class
        {
            try
            {
                _logger.LogDebug("Calling Trakt API: GET {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Trakt API: GET {Url}", url);
                throw;
            }
        }
    }
}
