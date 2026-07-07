using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.YouTube
{
    public class YouTubeApiClient : IYouTubeApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YouTubeApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;

        public YouTubeApiClient(HttpClient httpClient, ILogger<YouTubeApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY") ??
                     configuration["ApiKeys:YouTube"] ??
                     "YOUTUBE_API_KEY";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Search for videos, channels, and playlists on YouTube
        /// </summary>
        public async Task<YouTubeSearchResultDto> SearchAsync(string query, string type = "video", int maxResults = 25, string? pageToken = null, string? channelId = null)
        {
            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"search?part=snippet&q={encodedQuery}&type={type}&maxResults={maxResults}&key={_apiKey}";

                if (!string.IsNullOrEmpty(pageToken))
                    url += $"&pageToken={pageToken}";

                if (!string.IsNullOrEmpty(channelId))
                    url += $"&channelId={channelId}";

                _logger.LogInformation($"Searching YouTube with query: {query}, type: {type}, maxResults: {maxResults}");

                var jsonContent = await GetJsonAsync(url, $"search for '{query}'");
                var result = JsonSerializer.Deserialize<YouTubeSearchResultDto>(jsonContent, _jsonOptions);

                return result ?? new YouTubeSearchResultDto();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error searching YouTube for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Get detailed information about a specific video
        /// </summary>
        public async Task<YouTubeVideoDto?> GetVideoDetailsAsync(string videoId)
        {
            try
            {
                var url = $"videos?part=snippet,contentDetails,statistics,status&id={videoId}&key={_apiKey}";

                _logger.LogInformation($"Getting YouTube video details for ID: {videoId}");

                var jsonContent = await GetJsonAsync(url, $"video details for {videoId}");
                var result = JsonSerializer.Deserialize<YouTubeVideoListResponseDto>(jsonContent, _jsonOptions);

                return result?.Items?.FirstOrDefault();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube video details for ID: {VideoId}", videoId);
                throw;
            }
        }

        /// <summary>
        /// Get multiple videos by their IDs
        /// </summary>
        public async Task<List<YouTubeVideoDto>> GetVideosAsync(List<string> videoIds)
        {
            try
            {
                if (!videoIds.Any())
                    return new List<YouTubeVideoDto>();

                var ids = string.Join(",", videoIds);
                var url = $"videos?part=snippet,contentDetails,statistics,status&id={ids}&key={_apiKey}";

                _logger.LogInformation($"Getting YouTube videos for IDs: {ids}");

                var jsonContent = await GetJsonAsync(url, "video details batch");
                var result = JsonSerializer.Deserialize<YouTubeVideoListResponseDto>(jsonContent, _jsonOptions);

                return result?.Items ?? new List<YouTubeVideoDto>();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube videos for IDs: {VideoIds}", string.Join(",", videoIds));
                throw;
            }
        }

        /// <summary>
        /// Get detailed information about a specific playlist
        /// </summary>
        public async Task<YouTubePlaylistDto?> GetPlaylistDetailsAsync(string playlistId)
        {
            try
            {
                var url = $"playlists?part=snippet,status,contentDetails&id={playlistId}&key={_apiKey}";

                _logger.LogInformation($"Getting YouTube playlist details for ID: {playlistId}");

                var jsonContent = await GetJsonAsync(url, $"playlist details for {playlistId}");
                var result = JsonSerializer.Deserialize<YouTubePlaylistListResponseDto>(jsonContent, _jsonOptions);

                return result?.Items?.FirstOrDefault();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube playlist details for ID: {PlaylistId}", playlistId);
                throw;
            }
        }

        /// <summary>
        /// Get a single page of playlist items along with its pagination token.
        /// Used by both the public single-page and full-pagination methods so each
        /// page is fetched exactly once (1 quota unit per page).
        /// </summary>
        private async Task<YouTubePlaylistItemListResponseDto> GetPlaylistItemsPageAsync(string playlistId, int maxResults, string? pageToken)
        {
            var url = $"playlistItems?part=snippet,contentDetails&playlistId={playlistId}&maxResults={maxResults}&key={_apiKey}";

            if (!string.IsNullOrEmpty(pageToken))
                url += $"&pageToken={pageToken}";

            _logger.LogInformation($"Getting YouTube playlist items for playlist ID: {playlistId}");

            var jsonContent = await GetJsonAsync(url, $"playlist items for {playlistId}");
            return JsonSerializer.Deserialize<YouTubePlaylistItemListResponseDto>(jsonContent, _jsonOptions)
                   ?? new YouTubePlaylistItemListResponseDto();
        }

        /// <summary>
        /// Get videos from a specific playlist
        /// </summary>
        public async Task<List<YouTubePlaylistItemDto>> GetPlaylistItemsAsync(string playlistId, int maxResults = 50, string? pageToken = null)
        {
            try
            {
                var page = await GetPlaylistItemsPageAsync(playlistId, maxResults, pageToken);
                return page.Items ?? new List<YouTubePlaylistItemDto>();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube playlist items for playlist ID: {PlaylistId}", playlistId);
                throw;
            }
        }

        /// <summary>
        /// Get all videos from a playlist (handles pagination)
        /// </summary>
        public async Task<List<YouTubePlaylistItemDto>> GetAllPlaylistItemsAsync(string playlistId)
        {
            var allItems = new List<YouTubePlaylistItemDto>();
            string? nextPageToken = null;

            try
            {
                do
                {
                    var page = await GetPlaylistItemsPageAsync(playlistId, 50, nextPageToken);
                    if (page.Items != null)
                        allItems.AddRange(page.Items);

                    nextPageToken = page.NextPageToken;

                } while (!string.IsNullOrEmpty(nextPageToken));

                return allItems;
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting all YouTube playlist items for playlist ID: {PlaylistId}", playlistId);
                throw;
            }
        }

        /// <summary>
        /// Get detailed information about a specific channel
        /// </summary>
        public async Task<YouTubeChannelDto?> GetChannelDetailsAsync(string channelId)
        {
            try
            {
                var url = $"channels?part=snippet,contentDetails,statistics,brandingSettings&id={channelId}&key={_apiKey}";

                _logger.LogInformation($"Getting YouTube channel details for ID: {channelId}");

                var jsonContent = await GetJsonAsync(url, $"channel details for {channelId}");
                var result = JsonSerializer.Deserialize<YouTubeChannelListResponseDto>(jsonContent, _jsonOptions);

                return result?.Items?.FirstOrDefault();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube channel details for ID: {ChannelId}", channelId);
                throw;
            }
        }

        /// <summary>
        /// Get channel details by username/handle
        /// </summary>
        public async Task<YouTubeChannelDto?> GetChannelByUsernameAsync(string username)
        {
            try
            {
                var url = $"channels?part=snippet,contentDetails,statistics,brandingSettings&forUsername={username}&key={_apiKey}";

                _logger.LogInformation($"Getting YouTube channel details for username: {username}");

                var jsonContent = await GetJsonAsync(url, $"channel details for username {username}");
                var result = JsonSerializer.Deserialize<YouTubeChannelListResponseDto>(jsonContent, _jsonOptions);

                return result?.Items?.FirstOrDefault();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube channel details for username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Get channel details by handle (e.g., @TheTaleFoundry)
        /// </summary>
        public async Task<YouTubeChannelDto?> GetChannelByHandleAsync(string handle)
        {
            try
            {
                // Ensure handle has @ prefix as required by the YouTube API
                var handleWithPrefix = handle.StartsWith("@") ? handle : $"@{handle}";
                var url = $"channels?part=snippet,contentDetails,statistics,brandingSettings&forHandle={handleWithPrefix}&key={_apiKey}";

                _logger.LogInformation("Getting YouTube channel details for handle: {Handle}", handleWithPrefix);

                var jsonContent = await GetJsonAsync(url, $"channel details for handle {handleWithPrefix}");
                var result = JsonSerializer.Deserialize<YouTubeChannelListResponseDto>(jsonContent, _jsonOptions);

                return result?.Items?.FirstOrDefault();
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube channel details for handle: {Handle}", handle);
                throw;
            }
        }

        /// <summary>
        /// Get videos from a channel's uploads playlist
        /// </summary>
        public async Task<List<YouTubePlaylistItemDto>> GetChannelUploadsAsync(string channelId, int maxResults = 25, string? pageToken = null)
        {
            try
            {
                // First get the channel to find the uploads playlist ID
                var channel = await GetChannelDetailsAsync(channelId);
                var uploadsPlaylistId = channel?.ContentDetails?.RelatedPlaylists?.Uploads;

                if (string.IsNullOrEmpty(uploadsPlaylistId))
                {
                    _logger.LogWarning($"No uploads playlist found for channel ID: {channelId}");
                    return new List<YouTubePlaylistItemDto>();
                }

                return await GetPlaylistItemsAsync(uploadsPlaylistId, maxResults, pageToken);
            }
            catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
            {
                _logger.LogError(ex, "Error getting YouTube channel uploads for channel ID: {ChannelId}", channelId);
                throw;
            }
        }

        /// <summary>
        /// Issue a GET and return the response body as a string. Transient failures
        /// (429 / 5xx) are retried with backoff by the resilience handler on the HttpClient
        /// (see <see cref="YouTubeResilience"/>); this method adds the one thing a status-code
        /// policy can't do: distinguishing a fatal daily-quota 403 from other errors by
        /// inspecting the response body, and surfacing it as a typed exception so a bulk
        /// import can stop cleanly and resume after the quota resets.
        /// </summary>
        private async Task<string> GetJsonAsync(string url, string operationDescription)
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var body = await response.Content.ReadAsStringAsync();
                var reason = ExtractYouTubeErrorReason(body);
                if (reason is "quotaExceeded" or "dailyLimitExceeded")
                {
                    _logger.LogError(
                        "YouTube API daily quota exhausted (reason: {Reason}) during {Operation}. " +
                        "Import should stop and resume after the quota resets.",
                        reason, operationDescription);
                    throw new YouTubeQuotaExceededException(
                        $"YouTube API daily quota exhausted (reason: {reason}) during {operationDescription}. " +
                        "Resume once the quota resets.",
                        reason);
                }
                // Some other 403 (e.g. forbidden resource) — fall through to the standard throw.
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Extract the first error reason from a YouTube API error body
        /// (<c>error.errors[].reason</c>), or null if it can't be parsed.
        /// </summary>
        private static string? ExtractYouTubeErrorReason(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in errors.EnumerateArray())
                    {
                        if (e.TryGetProperty("reason", out var reason) &&
                            reason.ValueKind == JsonValueKind.String)
                        {
                            return reason.GetString();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON or unexpected shape — treat as "reason unknown".
            }

            return null;
        }

        /// <summary>
        /// Extract video ID from various YouTube URL formats
        /// </summary>
        public static string? ExtractVideoIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Handle different YouTube URL formats
            var patterns = new[]
            {
                @"(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/v\/([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/watch\?.*v=([a-zA-Z0-9_-]{11})"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            // If it's already just a video ID
            if (System.Text.RegularExpressions.Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{11}$"))
                return url;

            return null;
        }

        /// <summary>
        /// Extract playlist ID from YouTube URL
        /// </summary>
        public static string? ExtractPlaylistIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(url, @"[?&]list=([a-zA-Z0-9_-]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Extract channel ID from YouTube URL
        /// </summary>
        public static string? ExtractChannelIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var patterns = new[]
            {
                @"youtube\.com\/channel\/([a-zA-Z0-9_-]+)",
                @"youtube\.com\/c\/([a-zA-Z0-9_-]+)",
                @"youtube\.com\/user\/([a-zA-Z0-9_-]+)",
                @"youtube\.com\/@([a-zA-Z0-9_.-]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// Parse ISO 8601 duration format (PT4M13S) to seconds
        /// </summary>
        public static int ParseDurationToSeconds(string? duration)
        {
            if (string.IsNullOrEmpty(duration))
                return 0;

            try
            {
                var timeSpan = System.Xml.XmlConvert.ToTimeSpan(duration);
                return (int)timeSpan.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
