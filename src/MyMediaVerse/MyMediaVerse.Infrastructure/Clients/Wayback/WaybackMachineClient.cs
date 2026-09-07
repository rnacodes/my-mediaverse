using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.Wayback
{
    /// <summary>
    /// Queries the Wayback Machine's CDX index for the newest successful capture of a page.
    /// No API key is required. Any failure (no captures, HTTP error, malformed body, timeout)
    /// yields null so enrichment simply leaves the archive link empty.
    /// </summary>
    public class WaybackMachineClient : IWaybackMachineClient
    {
        private const string SnapshotBaseUrl = "https://web.archive.org/web/";

        private readonly HttpClient _httpClient;
        private readonly ILogger<WaybackMachineClient> _logger;

        public WaybackMachineClient(HttpClient httpClient, ILogger<WaybackMachineClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// The CDX query for a page: JSON output, newest capture only (<c>limit=-1</c>), successful
        /// captures only, one row per distinct content digest.
        /// </summary>
        public static string BuildQuery(string url) =>
            $"cdx/search/cdx?url={Uri.EscapeDataString(url)}&output=json&limit=-1&fl=timestamp,original&filter=statuscode:200&collapse=digest";

        public async Task<string?> FindLatestSnapshotAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                using var response = await _httpClient.GetAsync(BuildQuery(url), cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Wayback CDX returned {StatusCode} for {Url}", response.StatusCode, url);
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
        /// The CDX JSON body is an array of rows whose first row is the header
        /// (<c>[["timestamp","original"],["20240101120000","https://example.com/"]]</c>).
        /// </summary>
        private static string? ParseSnapshotUrl(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            // Skip the header row; the newest capture is the last data row.
            JsonElement? latest = null;
            var index = 0;
            foreach (var row in document.RootElement.EnumerateArray())
            {
                if (index++ == 0) continue;
                latest = row;
            }

            if (latest is not { ValueKind: JsonValueKind.Array } data || data.GetArrayLength() < 2)
                return null;

            var timestamp = data[0].GetString();
            var original = data[1].GetString();
            if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(original))
                return null;

            return $"{SnapshotBaseUrl}{timestamp}/{original}";
        }
    }
}
