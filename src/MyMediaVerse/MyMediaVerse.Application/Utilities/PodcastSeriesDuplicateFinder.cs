using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Everything known about an incoming podcast series that can identify an existing row.
    /// Populate whatever the source provides; nulls are skipped during probing.
    /// </summary>
    public record PodcastSeriesIdentity
    {
        /// <summary>The feed's podcast:guid.</summary>
        public string? FeedGuid { get; init; }
        /// <summary>Raw feed URL as the source provided it; normalized internally.</summary>
        public string? FeedUrl { get; init; }
        public string? ApplePodcastsId { get; init; }
        public long? PodcastIndexId { get; init; }
        /// <summary>ListenNotes id (dataset imports only).</summary>
        public string? ExternalId { get; init; }
        public string? Title { get; init; }
        public string? Publisher { get; init; }

        public static PodcastSeriesIdentity From(PodcastSeries series) => new()
        {
            FeedGuid = series.FeedGuid,
            FeedUrl = series.RssFeedUrl,
            ApplePodcastsId = series.ApplePodcastsId,
            PodcastIndexId = series.PodcastIndexId,
            ExternalId = series.ExternalId,
            Title = series.Title,
            Publisher = series.Publisher
        };
    }

    /// <summary>
    /// The single lookup every podcast-series-creating path uses to decide whether an incoming
    /// show is already in the library. A series' identity is its feed: podcast:guid first (it
    /// survives a host move), then the normalized feed URL (with a fallback for rows saved before
    /// <see cref="PodcastSeries.FeedUrlKey"/> existed), then directory ids, and only as a last
    /// resort case-insensitive title + publisher equality.
    /// </summary>
    public static class PodcastSeriesDuplicateFinder
    {
        /// <summary>
        /// Finds an existing series matching the identity, or null. Pass a query with any Includes
        /// the caller needs on the returned entity. Probes run in priority order so a strong match
        /// always wins over a weaker one, even when different rows would match different keys.
        /// </summary>
        public static async Task<PodcastSeries?> FindExistingAsync(IQueryable<PodcastSeries> series, PodcastSeriesIdentity identity)
        {
            if (!string.IsNullOrWhiteSpace(identity.FeedGuid))
            {
                var guid = identity.FeedGuid.Trim();
                var match = await series.FirstOrDefaultAsync(s => s.FeedGuid == guid);
                if (match != null) return match;
            }

            var key = UrlNormalizer.GetComparisonKey(identity.FeedUrl);
            if (!string.IsNullOrEmpty(key))
            {
                var match = await series.FirstOrDefaultAsync(s => s.FeedUrlKey == key);
                if (match != null) return match;

                // Legacy rows: stored feed URLs keep their scheme and any "www.", so probe each
                // variant plus the bare key. The ToLower() guards rows stored with mixed case.
                var variants = new[]
                {
                    "https://" + key, "http://" + key,
                    "https://www." + key, "http://www." + key,
                    key,
                    "https://" + key + "/", "http://" + key + "/"
                };
                match = await series.FirstOrDefaultAsync(s =>
                    s.FeedUrlKey == null && s.RssFeedUrl != null && variants.Contains(s.RssFeedUrl.ToLower()));
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.ApplePodcastsId))
            {
                var appleId = identity.ApplePodcastsId.Trim();
                var match = await series.FirstOrDefaultAsync(s => s.ApplePodcastsId == appleId);
                if (match != null) return match;
            }

            if (identity.PodcastIndexId.HasValue)
            {
                var match = await series.FirstOrDefaultAsync(s => s.PodcastIndexId == identity.PodcastIndexId);
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.ExternalId))
            {
                var externalId = identity.ExternalId.Trim();
                var match = await series.FirstOrDefaultAsync(s => s.ExternalId == externalId);
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.Title) && !string.IsNullOrWhiteSpace(identity.Publisher))
            {
                var titleLower = identity.Title.Trim().ToLower();
                var publisherLower = identity.Publisher.Trim().ToLower();
                var match = await series.FirstOrDefaultAsync(s =>
                    s.Title.ToLower() == titleLower && s.Publisher != null && s.Publisher.ToLower() == publisherLower);
                if (match != null) return match;
            }

            return null;
        }

        /// <summary>
        /// Sets <see cref="PodcastSeries.FeedUrlKey"/> from the stored feed URL when it is missing.
        /// Returns true when anything changed.
        /// </summary>
        public static bool FillIdentity(PodcastSeries series)
        {
            if (!string.IsNullOrEmpty(series.FeedUrlKey))
                return false;

            var key = UrlNormalizer.GetComparisonKey(series.RssFeedUrl);
            if (string.IsNullOrEmpty(key))
                return false;

            series.FeedUrlKey = key;
            return true;
        }

        /// <summary>
        /// Fill-only copy of the identity's feed and directory ids onto an existing match, so a
        /// series acquires ids as more sources see it. Never overwrites a value already present,
        /// and skips any id another row already owns (each is unique). Returns true when anything
        /// changed.
        /// </summary>
        public static async Task<bool> AbsorbIdentityAsync(
            IQueryable<PodcastSeries> allSeries, PodcastSeries existing, PodcastSeriesIdentity identity)
        {
            var changed = FillIdentity(existing);

            if (string.IsNullOrWhiteSpace(existing.RssFeedUrl) && !string.IsNullOrWhiteSpace(identity.FeedUrl))
            {
                var key = UrlNormalizer.GetComparisonKey(identity.FeedUrl);
                if (!string.IsNullOrEmpty(key) &&
                    !await allSeries.AnyAsync(s => s.Id != existing.Id && s.FeedUrlKey == key))
                {
                    existing.RssFeedUrl = identity.FeedUrl.Trim();
                    existing.FeedUrlKey = key;
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(existing.FeedGuid) && !string.IsNullOrWhiteSpace(identity.FeedGuid))
            {
                var guid = identity.FeedGuid.Trim();
                if (!await allSeries.AnyAsync(s => s.Id != existing.Id && s.FeedGuid == guid))
                {
                    existing.FeedGuid = guid;
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(existing.ApplePodcastsId) && !string.IsNullOrWhiteSpace(identity.ApplePodcastsId))
            {
                var appleId = identity.ApplePodcastsId.Trim();
                if (!await allSeries.AnyAsync(s => s.Id != existing.Id && s.ApplePodcastsId == appleId))
                {
                    existing.ApplePodcastsId = appleId;
                    changed = true;
                }
            }

            if (existing.PodcastIndexId == null && identity.PodcastIndexId.HasValue)
            {
                if (!await allSeries.AnyAsync(s => s.Id != existing.Id && s.PodcastIndexId == identity.PodcastIndexId))
                {
                    existing.PodcastIndexId = identity.PodcastIndexId;
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(existing.ExternalId) && !string.IsNullOrWhiteSpace(identity.ExternalId))
            {
                var externalId = identity.ExternalId.Trim();
                if (!await allSeries.AnyAsync(s => s.Id != existing.Id && s.ExternalId == externalId))
                {
                    existing.ExternalId = externalId;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Fill-only merge of an incoming series' descriptive metadata onto the existing row, so a
        /// second save of the same show enriches rather than duplicates. Values already present are
        /// never overwritten and the title is never touched; topics and genres are added when the
        /// existing row lacks them. Returns true when anything changed.
        /// </summary>
        public static bool AbsorbMetadata(PodcastSeries existing, PodcastSeries incoming)
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existing.Description) && !string.IsNullOrWhiteSpace(incoming.Description))
            {
                existing.Description = incoming.Description;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Publisher) && !string.IsNullOrWhiteSpace(incoming.Publisher))
            {
                existing.Publisher = incoming.Publisher;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Thumbnail) && !string.IsNullOrWhiteSpace(incoming.Thumbnail))
            {
                existing.Thumbnail = incoming.Thumbnail;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Link) && !string.IsNullOrWhiteSpace(incoming.Link))
            {
                existing.Link = incoming.Link;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Language) && !string.IsNullOrWhiteSpace(incoming.Language))
            {
                existing.Language = incoming.Language;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Notes) && !string.IsNullOrWhiteSpace(incoming.Notes))
            {
                existing.Notes = incoming.Notes;
                changed = true;
            }

            foreach (var topic in incoming.Topics)
            {
                if (existing.Topics.All(t => t.Name != topic.Name))
                {
                    existing.Topics.Add(topic);
                    changed = true;
                }
            }

            foreach (var genre in incoming.Genres)
            {
                if (existing.Genres.All(g => g.Name != genre.Name))
                {
                    existing.Genres.Add(genre);
                    changed = true;
                }
            }

            return changed;
        }
    }
}
