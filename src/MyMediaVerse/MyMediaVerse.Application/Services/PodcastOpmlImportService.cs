using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
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
            var result = new OpmlImportResultDto { StartedAt = DateTime.UtcNow };

            XDocument document;
            try
            {
                document = XDocument.Load(opmlStream);
            }
            catch (Exception ex)
            {
                // Malformed XML: nothing parseable, report a single clear failure rather than throwing.
                _logger.LogError(ex, "Failed to parse OPML file");
                result.Success = false;
                result.ErrorMessage = "The file could not be read as OPML.";
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Podcast feeds are the <outline type="rss"> elements. Use Descendants so the outer
            // <outline text="feeds"> wrapper (which has no type) is skipped rather than treated as a feed.
            var feeds = document.Descendants("outline")
                .Where(o => string.Equals((string?)o.Attribute("type"), "rss", StringComparison.OrdinalIgnoreCase))
                .ToList();

            result.TotalProcessed = feeds.Count;
            _logger.LogInformation("Processing {Count} podcast feeds from OPML", feeds.Count);

            // Preload existing series once into an in-memory index so each feed is deduplicated
            // against a lookup instead of a per-feed query. Newly created stubs are added to the
            // index too, so an in-file duplicate is skipped rather than inserted twice.
            var dedup = new DedupIndex();
            foreach (var series in await _podcastService.GetAllPodcastSeriesAsync())
            {
                dedup.Add(series.Title, series.RssFeedUrl, series.ApplePodcastsId);
            }

            foreach (var feed in feeds)
            {
                var title = ((string?)feed.Attribute("text"))?.Trim();
                var rssFeedUrl = ((string?)feed.Attribute("xmlUrl"))?.Trim();
                var applePodcastsId = ((string?)feed.Attribute("applePodcastsID"))?.Trim();

                if (string.IsNullOrWhiteSpace(title))
                {
                    result.SkippedCount++;
                    continue;
                }

                if (dedup.Contains(title, rssFeedUrl, applePodcastsId))
                {
                    result.SkippedCount++;
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

                    var creation = await _podcastService.CreatePodcastSeriesAsync(dto);

                    // Register the stub so a later duplicate row in the same file is skipped.
                    dedup.Add(title, rssFeedUrl, applePodcastsId);

                    // The service's own identity probe is the final word: a match it found that the
                    // in-memory index missed is still a skip, not an import.
                    if (creation.Created)
                    {
                        result.CreatedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    // Isolate per-feed failures so one bad feed never aborts the whole import. The
                    // reason stays generic; the exception goes to the log, not to the caller.
                    const string reason = "Could not be imported.";
                    result.FailedCount++;
                    result.Failures.Add(new OpmlImportFailureDto { Title = title, Reason = reason });
                    result.Errors.Add($"{title}: {reason}");
                    _logger.LogError(ex, "Error importing podcast feed: {Title}", title);
                }
            }

            if (result.FailedCount > 0)
            {
                result.WarningMessage = $"{result.FailedCount} of {result.TotalProcessed} feeds could not be imported.";
            }

            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("OPML import complete: {Imported} imported, {Skipped} skipped, {Failed} failed",
                result.CreatedCount, result.SkippedCount, result.FailedCount);

            return result;
        }

        /// <summary>
        /// In-memory dedup lookup for a single import run: existing (and newly created) series keyed
        /// by normalized feed URL and Apple Podcasts id (primary) and by normalized title (fallback),
        /// so each feed is matched without a query.
        /// </summary>
        private sealed class DedupIndex
        {
            private readonly HashSet<string> _byFeedKey = new(StringComparer.Ordinal);
            private readonly HashSet<string> _byAppleId = new(StringComparer.Ordinal);
            private readonly HashSet<string> _byTitle = new(StringComparer.OrdinalIgnoreCase);

            public bool Contains(string title, string? feedUrl, string? applePodcastsId)
            {
                var feedKey = UrlNormalizer.GetComparisonKey(feedUrl);
                if (!string.IsNullOrEmpty(feedKey) && _byFeedKey.Contains(feedKey))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(applePodcastsId) && _byAppleId.Contains(applePodcastsId.Trim()))
                {
                    return true;
                }

                return _byTitle.Contains(NormalizeTitle(title));
            }

            public void Add(string title, string? feedUrl, string? applePodcastsId)
            {
                var feedKey = UrlNormalizer.GetComparisonKey(feedUrl);
                if (!string.IsNullOrEmpty(feedKey))
                {
                    _byFeedKey.Add(feedKey);
                }

                if (!string.IsNullOrWhiteSpace(applePodcastsId))
                {
                    _byAppleId.Add(applePodcastsId.Trim());
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
