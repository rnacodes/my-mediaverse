using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Everything known about an incoming episode that can identify an existing row in its series.
    /// Populate whatever the source provides; nulls are skipped during probing.
    /// </summary>
    public record PodcastEpisodeIdentity
    {
        public Guid SeriesId { get; init; }
        /// <summary>The feed item's guid.</summary>
        public string? RssGuid { get; init; }
        /// <summary>ListenNotes id (dataset imports only).</summary>
        public string? ExternalId { get; init; }
        /// <summary>The enclosure (audio) URL; normalized internally.</summary>
        public string? AudioLink { get; init; }
        public string? Title { get; init; }
        public DateTime? ReleaseDate { get; init; }
    }

    /// <summary>
    /// The single lookup every episode-creating path uses to decide whether an incoming episode is
    /// already in its series. Probes in identity-strength order, always scoped to the series: feed
    /// item guid, then external id, then the enclosure URL, and only as a last resort title plus
    /// release date (both required, since titles like "Trailer" repeat).
    /// </summary>
    public static class PodcastEpisodeDuplicateFinder
    {
        /// <summary>
        /// Finds an existing episode matching the identity, or null. Pass a query with any Includes
        /// the caller needs on the returned entity.
        /// </summary>
        public static async Task<PodcastEpisode?> FindExistingAsync(IQueryable<PodcastEpisode> episodes, PodcastEpisodeIdentity identity)
        {
            var inSeries = episodes.Where(e => e.SeriesId == identity.SeriesId);

            if (!string.IsNullOrWhiteSpace(identity.RssGuid))
            {
                var guid = identity.RssGuid.Trim();
                var match = await inSeries.FirstOrDefaultAsync(e => e.RssGuid == guid);
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.ExternalId))
            {
                var externalId = identity.ExternalId.Trim();
                var match = await inSeries.FirstOrDefaultAsync(e => e.ExternalId == externalId);
                if (match != null) return match;
            }

            var audioKey = UrlNormalizer.GetComparisonKey(identity.AudioLink);
            if (!string.IsNullOrEmpty(audioKey))
            {
                var candidates = await inSeries
                    .Where(e => e.AudioLink != null)
                    .Select(e => new { e.Id, e.AudioLink })
                    .ToListAsync();

                var matchId = candidates
                    .FirstOrDefault(c => UrlNormalizer.GetComparisonKey(c.AudioLink) == audioKey)?.Id;
                if (matchId.HasValue)
                {
                    var match = await inSeries.FirstOrDefaultAsync(e => e.Id == matchId.Value);
                    if (match != null) return match;
                }
            }

            if (!string.IsNullOrWhiteSpace(identity.Title) && identity.ReleaseDate.HasValue)
            {
                var titleLower = identity.Title.Trim().ToLower();
                var dayStart = DateTimeNormalizer.ToUtc(identity.ReleaseDate)!.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                var match = await inSeries.FirstOrDefaultAsync(e =>
                    e.Title.ToLower() == titleLower &&
                    e.ReleaseDate != null && e.ReleaseDate >= dayStart && e.ReleaseDate < dayEnd);
                if (match != null) return match;
            }

            return null;
        }

        /// <summary>
        /// Fill-only copy of the identity's guid and external id onto an existing match. Never
        /// overwrites a value already present, and skips an id another episode in the series owns.
        /// Returns true when anything changed.
        /// </summary>
        public static async Task<bool> AbsorbIdentityAsync(
            IQueryable<PodcastEpisode> allEpisodes, PodcastEpisode existing, PodcastEpisodeIdentity identity)
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existing.RssGuid) && !string.IsNullOrWhiteSpace(identity.RssGuid))
            {
                var guid = identity.RssGuid.Trim();
                if (!await allEpisodes.AnyAsync(e => e.SeriesId == existing.SeriesId && e.Id != existing.Id && e.RssGuid == guid))
                {
                    existing.RssGuid = guid;
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(existing.ExternalId) && !string.IsNullOrWhiteSpace(identity.ExternalId))
            {
                var externalId = identity.ExternalId.Trim();
                if (!await allEpisodes.AnyAsync(e => e.SeriesId == existing.SeriesId && e.Id != existing.Id && e.ExternalId == externalId))
                {
                    existing.ExternalId = externalId;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
