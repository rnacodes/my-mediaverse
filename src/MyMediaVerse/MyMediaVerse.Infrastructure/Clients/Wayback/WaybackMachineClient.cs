using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.Wayback
{
    /// <summary>
    /// Asks the Wayback Machine's availability endpoint for the newest capture of a page. It answers
    /// from an index in about a second, unlike the CDX search, which scans captures and throttles
    /// sustained callers. No API key is required. Any failure (no captures, HTTP error, malformed
    /// body, timeout) yields null so enrichment simply leaves the archive link empty.
    /// </summary>
    public class WaybackMachineClient : IWaybackMachineClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WaybackMachineClient> _logger;

        public WaybackMachineClient(HttpClient httpClient, ILogger<WaybackMachineClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// The availability query for a page. Without a timestamp the endpoint returns the most
        /// recent capture as <c>archived_snapshots.closest</c>.
        /// </summary>
        public static string BuildQuery(string url) =>
            $"wayback/available?url={Uri.EscapeDataString(url)}";

        public async Task<string?> FindLatestSnapshotAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                using var response = await _httpClient.GetAsync(BuildQuery(url), cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Wayback availability returned {StatusCode} for {Url}", response.StatusCode, url);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseSnapshotUrl(body);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Wayback lookup failed for {Url}", url);
                return null;
            }
        }

        /// <summary>
        /// The body is <c>{"url": ..., "archived_snapshots": {"closest": {"available": true,
        /// "url": "http://web.archive.org/web/20240101120000/https://example.com/", "timestamp":
        /// "20240101120000", "status": "200"}}}</c>; a page with no captures comes back with an
        /// empty <c>archived_snapshots</c> object.
        /// </summary>
        private static string? ParseSnapshotUrl(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("archived_snapshots", out var snapshots)
                || snapshots.ValueKind != JsonValueKind.Object
                || !snapshots.TryGetProperty("closest", out var closest)
                || closest.ValueKind != JsonValueKind.Object)
                return null;

            if (closest.TryGetProperty("available", out var available)
                && available.ValueKind is JsonValueKind.False or JsonValueKind.Null)
                return null;

            if (closest.TryGetProperty("status", out var status)
                && status.ValueKind == JsonValueKind.String
                && !(status.GetString() ?? string.Empty).StartsWith('2'))
                return null;

            if (!closest.TryGetProperty("url", out var urlElement) || urlElement.ValueKind != JsonValueKind.String)
                return null;

            var snapshotUrl = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(snapshotUrl))
                return null;

            // The endpoint hands back http:// links; the archive serves https and browsers prefer it.
            return snapshotUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? "https://" + snapshotUrl.Substring("http://".Length)
                : snapshotUrl;
        }
    }
}
