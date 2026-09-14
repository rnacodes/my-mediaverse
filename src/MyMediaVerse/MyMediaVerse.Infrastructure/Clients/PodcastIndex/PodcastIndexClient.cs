using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.PodcastIndex;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.PodcastIndex
{
    /// <summary>
    /// Calls the Podcast Index API. Requests are signed by <see cref="PodcastIndexAuthHandler"/>; when no key
    /// is configured, every method returns nothing without making a request.
    /// </summary>
    public class PodcastIndexClient : IPodcastIndexClient
    {
        public const string BaseUrl = "https://api.podcastindex.org/api/1.0/";

        private readonly HttpClient _httpClient;
        private readonly PodcastIndexCredentials _credentials;
        private readonly ILogger<PodcastIndexClient> _logger;

        public PodcastIndexClient(HttpClient httpClient, PodcastIndexCredentials credentials, ILogger<PodcastIndexClient> logger)
        {
            _httpClient = httpClient;
            _credentials = credentials;
            _logger = logger;
        }

        public bool IsConfigured => _credentials.IsConfigured;

        public async Task<IReadOnlyList<PodcastIndexFeedDto>> SearchByTermAsync(string term, int max, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                return Array.Empty<PodcastIndexFeedDto>();

            using var document = await GetAsync($"search/byterm?q={Uri.EscapeDataString(term)}&max={max}", cancellationToken);
            if (document == null
                || !document.RootElement.TryGetProperty("feeds", out var feeds)
                || feeds.ValueKind != JsonValueKind.Array)
                return Array.Empty<PodcastIndexFeedDto>();

            return feeds.EnumerateArray().Where(f => f.ValueKind == JsonValueKind.Object).Select(ParseFeed).ToList();
        }

        public Task<PodcastIndexFeedDto?> GetByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken = default) =>
            GetSingleAsync($"podcasts/byfeedurl?url={Uri.EscapeDataString(feedUrl)}", cancellationToken);

        public Task<PodcastIndexFeedDto?> GetByItunesIdAsync(long itunesId, CancellationToken cancellationToken = default) =>
            GetSingleAsync($"podcasts/byitunesid?id={itunesId.ToString(CultureInfo.InvariantCulture)}", cancellationToken);

        private async Task<PodcastIndexFeedDto?> GetSingleAsync(string query, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
                return null;

            using var document = await GetAsync(query, cancellationToken);

            // A lookup that finds nothing answers with "feed": [] (or status "false") instead of a feed object.
            if (document == null
                || !document.RootElement.TryGetProperty("feed", out var feed)
                || feed.ValueKind != JsonValueKind.Object)
                return null;

            return ParseFeed(feed);
        }

        /// <summary>Returns the parsed body, or null for a 404. Other failures throw <see cref="HttpRequestException"/>.</summary>
        private async Task<JsonDocument?> GetAsync(string query, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(query, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Podcast Index returned {StatusCode} for {Path}", (int)response.StatusCode, query.Split('?')[0]);
                throw new HttpRequestException(
                    $"Podcast Index returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("Podcast Index returned an unreadable response.", ex);
            }
        }

        public static PodcastIndexFeedDto ParseFeed(JsonElement feed) => new()
        {
            Id = GetLong(feed, "id") ?? 0,
            PodcastGuid = GetString(feed, "podcastGuid"),
            Title = GetString(feed, "title"),
            Url = GetString(feed, "url"),
            Link = GetString(feed, "link"),
            Description = GetString(feed, "description"),
            Author = GetString(feed, "author"),
            OwnerName = GetString(feed, "ownerName"),
            Image = GetString(feed, "image"),
            Artwork = GetString(feed, "artwork"),
            ItunesId = GetLong(feed, "itunesId") is > 0 and var itunesId ? itunesId : null,
            Language = GetString(feed, "language"),
            Categories = feed.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Object
                ? categories.EnumerateObject()
                    .Select(c => c.Value.ValueKind == JsonValueKind.String ? c.Value.GetString() : null)
                    .OfType<string>()
                    .Where(name => name.Length > 0)
                    .ToList()
                : Array.Empty<string>(),
            EpisodeCount = (int?)GetLong(feed, "episodeCount"),
            Dead = GetLong(feed, "dead") is > 0,
            NewestItemPublishedAt = (GetLong(feed, "newestItemPublishTime") ?? GetLong(feed, "newestItemPubdate")) is > 0 and var unix
                ? DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
                : null
        };

        private static string? GetString(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()!.Trim()
                : null;

        // Numbers sometimes arrive as strings.
        private static long? GetLong(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetInt64(out var number) => number,
                JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null
            };
        }
    }
}
