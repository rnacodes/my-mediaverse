using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Everything known about an incoming video that can identify an existing row.
    /// Populate whatever the source provides; blanks are skipped during probing.
    /// </summary>
    public record VideoIdentity
    {
        public string? Platform { get; init; }
        /// <summary>The platform's own id for the video (the YouTube video id, a Vimeo id, ...).</summary>
        public string? ExternalId { get; init; }
        /// <summary>Raw link as the source provided it; normalized internally.</summary>
        public string? Link { get; init; }
        public string? Title { get; init; }
        public Guid? ChannelId { get; init; }

        public static VideoIdentity From(Video video) => new()
        {
            Platform = video.Platform,
            ExternalId = video.ExternalId,
            Link = video.Link,
            Title = video.Title,
            ChannelId = video.ChannelId
        };
    }

    /// <summary>
    /// The single lookup every video-creating path uses to decide whether an incoming video is
    /// already in the library. A video's identity is its id on its platform; a YouTube link
    /// carries that id too, so it identifies rows saved before the id was stored. Any other
    /// link identifies a video by itself, and only as a last resort does a title count, and
    /// then only within one channel.
    /// </summary>
    public static class VideoDuplicateFinder
    {
        public const string YouTubePlatform = "YouTube";

        /// <summary>Trims an id; a blank one becomes null, which is how a missing id is stored.</summary>
        public static string? NormalizeExternalId(string? externalId) =>
            string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();

        /// <summary>
        /// The YouTube video id held by a link, or null when the link is not a YouTube video link.
        /// </summary>
        public static string? ExtractYouTubeId(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return null;

            var isYouTubeLink = link.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                || link.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);

            return isYouTubeLink ? YouTubeHelper.ExtractVideoIdFromUrl(link.Trim()) : null;
        }

        /// <summary>
        /// Finds an existing video matching the identity, or null. Pass a query with any Includes
        /// the caller needs on the returned entity. Probes run in priority order so a strong match
        /// always wins over a weaker one, even when different rows would match different keys.
        /// A video with no id, no link and no channel never matches anything: two unrelated
        /// videos that merely share a title are never merged.
        /// </summary>
        public static async Task<Video?> FindExistingAsync(IQueryable<Video> videos, VideoIdentity identity)
        {
            var externalId = NormalizeExternalId(identity.ExternalId);
            var platformLower = identity.Platform?.Trim().ToLower();
            var hasPlatform = !string.IsNullOrEmpty(platformLower);
            var isYouTube = platformLower == YouTubePlatform.ToLower();

            if (externalId != null && hasPlatform)
            {
                var match = await videos.FirstOrDefaultAsync(v =>
                    v.ExternalId == externalId && v.Platform.ToLower() == platformLower);
                if (match != null) return match;
            }

            var youTubeId = (isYouTube ? externalId : null) ?? ExtractYouTubeId(identity.Link);
            if (youTubeId != null)
            {
                var youTubeLower = YouTubePlatform.ToLower();
                var match = await videos.FirstOrDefaultAsync(v =>
                    v.ExternalId == youTubeId && v.Platform.ToLower() == youTubeLower);
                if (match != null) return match;

                // Legacy rows: saved before the id was stored, so the id lives only in the link.
                // The substring test narrows the candidates; the extraction confirms the id.
                var candidates = await videos
                    .Where(v => (v.ExternalId == null || v.ExternalId == "") && v.Link != null && v.Link.Contains(youTubeId))
                    .ToListAsync();
                match = candidates.FirstOrDefault(v => ExtractYouTubeId(v.Link) == youTubeId);
                if (match != null) return match;
            }

            // The id the incoming video is known by, used below to refuse a row that is known
            // by a different one.
            var knownId = youTubeId ?? externalId;

            // A YouTube link was fully handled by its id above; comparing it as text here
            // would ignore case, and YouTube ids are case-sensitive.
            var key = youTubeId == null ? UrlNormalizer.GetComparisonKey(identity.Link) : string.Empty;
            if (!string.IsNullOrEmpty(key))
            {
                // Stored links keep their scheme and any "www.", so probe each variant plus the
                // bare key. The ToLower() guards rows stored with mixed case.
                var variants = new[]
                {
                    "https://" + key, "http://" + key,
                    "https://www." + key, "http://www." + key,
                    key,
                    "https://" + key + "/", "http://" + key + "/",
                    "https://www." + key + "/", "http://www." + key + "/"
                };
                var match = await videos.FirstOrDefaultAsync(v =>
                    v.Link != null && variants.Contains(v.Link.ToLower()) &&
                    (knownId == null || v.ExternalId == null || v.ExternalId == "" || v.ExternalId == knownId));
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.Title) && hasPlatform && identity.ChannelId.HasValue)
            {
                var titleLower = identity.Title.Trim().ToLower();
                var channelId = identity.ChannelId.Value;
                var match = await videos.FirstOrDefaultAsync(v =>
                    v.Title.ToLower() == titleLower &&
                    v.Platform.ToLower() == platformLower &&
                    v.ChannelId == channelId &&
                    (knownId == null || v.ExternalId == null || v.ExternalId == "" || v.ExternalId == knownId));
                if (match != null) return match;
            }

            return null;
        }

        /// <summary>
        /// Fill-only copy of the incoming identity onto an existing match, so a row found by its
        /// link or title gains the id it was missing. Never overwrites a value already present.
        /// The id is only taken when both sides name the same platform, because an id means
        /// nothing outside its platform. Returns true when anything changed.
        /// </summary>
        public static bool AbsorbIdentity(Video existing, VideoIdentity incoming)
        {
            var changed = false;

            var incomingId = NormalizeExternalId(incoming.ExternalId);
            var samePlatform = string.Equals(
                existing.Platform?.Trim(), incoming.Platform?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(existing.ExternalId) && incomingId != null && samePlatform)
            {
                existing.ExternalId = incomingId;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Link) && !string.IsNullOrWhiteSpace(incoming.Link))
            {
                existing.Link = incoming.Link.Trim();
                changed = true;
            }

            if (existing.ChannelId == null && incoming.ChannelId.HasValue)
            {
                existing.ChannelId = incoming.ChannelId;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Fill-only merge of an incoming video's descriptive metadata onto the existing row, so a
        /// second save of the same video enriches rather than duplicates. Values already present
        /// are never overwritten, and the title, notes, status and rating are never touched.
        /// Returns true when anything changed.
        /// </summary>
        public static bool AbsorbMetadata(Video existing, Video incoming)
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(existing.Description) && !string.IsNullOrWhiteSpace(incoming.Description))
            {
                existing.Description = incoming.Description;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Thumbnail) && !string.IsNullOrWhiteSpace(incoming.Thumbnail))
            {
                existing.Thumbnail = incoming.Thumbnail;
                changed = true;
            }

            if (existing.LengthInSeconds <= 0 && incoming.LengthInSeconds > 0)
            {
                existing.LengthInSeconds = incoming.LengthInSeconds;
                changed = true;
            }

            if (existing.PublishedAt == null && incoming.PublishedAt != null)
            {
                existing.PublishedAt = incoming.PublishedAt;
                changed = true;
            }

            return changed;
        }
    }
}
