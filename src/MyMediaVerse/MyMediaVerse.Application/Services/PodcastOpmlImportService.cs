using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Imports podcast subscriptions from an OPML export as lightweight <see cref="PodcastSeries"/>
    /// stubs. Mirrors the Goodreads importer's "land dumb + fast, enrich later" strategy: the OPML
    /// file alone supplies the title, RSS feed url and Apple Podcasts id, so the import makes no
    /// external API calls and a separate paced pass enriches the stubs afterward.
    /// </summary>
    public class PodcastOpmlImportService : IPodcastOpmlImportService
    {
        private readonly IPodcastService _podcastService;
        private readonly ILogger<PodcastOpmlImportService> _logger;

        public PodcastOpmlImportService(
            IPodcastService podcastService,
            ILogger<PodcastOpmlImportService> logger)
        {
            _podcastService = podcastService;
            _logger = logger;
        }

        public async Task<OpmlImportResultDto> ImportFromOpmlAsync(Stream opmlStream)
        {
            var result = new OpmlImportResultDto();

            XDocument document;
            try
            {
                document = XDocument.Load(opmlStream);
            }
            catch (Exception ex)
            {
                // Malformed XML: nothing parseable, report a single clear failure rather than throwing.
                _logger.LogError(ex, "Failed to parse OPML file");
                result.Failed++;
                result.Failures.Add(new OpmlImportFailureDto
                {
                    Title = "(file)",
                    Reason = $"Could not parse OPML: {ex.Message}"
                });
                return result;
            }

            // Podcast feeds are the <outline type="rss"> elements. Use Descendants so the outer
            // <outline text="feeds"> wrapper (which has no type) is skipped rather than treated as a feed.
            var feeds = document.Descendants("outline")
                .Where(o => string.Equals((string?)o.Attribute("type"), "rss", StringComparison.OrdinalIgnoreCase))
                .ToList();

            result.Total = feeds.Count;
            _logger.LogInformation("Processing {Count} podcast feeds from OPML", feeds.Count);

            // Preload existing series once into an in-memory index so each feed is deduplicated
            // against a lookup instead of a per-feed query. Newly created stubs are added to the
            // index too, so an in-file duplicate is skipped rather than inserted twice.
            var dedup = new DedupIndex();
            foreach (var series in await _podcastService.GetAllPodcastSeriesAsync())
            {
                dedup.Add(series.Title, series.RssFeedUrl);
            }

            foreach (var feed in feeds)
            {
                var title = ((string?)feed.Attribute("text"))?.Trim();
                var rssFeedUrl = ((string?)feed.Attribute("xmlUrl"))?.Trim();
                var applePodcastsId = ((string?)feed.Attribute("applePodcastsID"))?.Trim();

                if (string.IsNullOrWhiteSpace(title))
                {
                    result.Skipped++;
                    continue;
                }

                if (dedup.Contains(title, rssFeedUrl))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    var dto = new CreatePodcastSeriesDto
                    {
                        Title = title,
                        Status = Status.Uncharted,
                        IsSubscribed = true,
                        RssFeedUrl = string.IsNullOrWhiteSpace(rssFeedUrl) ? null : rssFeedUrl,
                        ApplePodcastsId = string.IsNullOrWhiteSpace(applePodcastsId) ? null : applePodcastsId
                    };

                    await _podcastService.CreatePodcastSeriesAsync(dto);

                    // Register the stub so a later duplicate row in the same file is skipped.
                    dedup.Add(title, rssFeedUrl);
                    result.Imported++;
                }
                catch (Exception ex)
                {
                    // Isolate per-feed failures so one bad feed never aborts the whole import.
                    result.Failed++;
                    result.Failures.Add(new OpmlImportFailureDto { Title = title, Reason = ex.Message });
                    _logger.LogError(ex, "Error importing podcast feed: {Title}", title);
                }
            }

            _logger.LogInformation("OPML import complete: {Imported} imported, {Skipped} skipped, {Failed} failed",
                result.Imported, result.Skipped, result.Failed);

            return result;
        }

        /// <summary>
        /// In-memory dedup lookup for a single import run: existing (and newly created) series keyed
        /// by RSS feed url (primary) and by normalized title (fallback), so each feed is matched
        /// without a query.
        /// </summary>
        private sealed class DedupIndex
        {
            private readonly HashSet<string> _byFeedUrl = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _byTitle = new(StringComparer.OrdinalIgnoreCase);

            public bool Contains(string title, string? feedUrl)
            {
                if (!string.IsNullOrWhiteSpace(feedUrl) && _byFeedUrl.Contains(feedUrl.Trim()))
                {
                    return true;
                }

                return _byTitle.Contains(NormalizeTitle(title));
            }

            public void Add(string title, string? feedUrl)
            {
                if (!string.IsNullOrWhiteSpace(feedUrl))
                {
                    _byFeedUrl.Add(feedUrl.Trim());
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    _byTitle.Add(NormalizeTitle(title));
                }
            }

            private static string NormalizeTitle(string title) => title.Trim().ToLowerInvariant();
        }
    }
}
