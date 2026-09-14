using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Copies what a feed (and optionally a directory entry) says about a show onto a
    /// <see cref="PodcastSeries"/>. The feed wins; the directory only fills what the feed left empty.
    /// Values too long for their column are skipped rather than failing the save.
    /// </summary>
    public static class PodcastFeedMapper
    {
        private const string PlaceholderPrefix = "Podcast at ";

        /// <summary>The title given to a series before any source has named it.</summary>
        public static string PlaceholderTitle(string? feedUrl)
        {
            var host = UrlNormalizer.ExtractDomain(feedUrl);
            return PlaceholderPrefix + (string.IsNullOrEmpty(host) ? "an unknown address" : host);
        }

        public static bool IsPlaceholderTitle(string? title) =>
            string.IsNullOrWhiteSpace(title) || title.StartsWith(PlaceholderPrefix, StringComparison.Ordinal);

        /// <summary>
        /// Applies the feed's channel metadata. With <paramref name="fillOnly"/> only empty fields
        /// (and a placeholder title) are written; otherwise the feed's non-empty values replace the
        /// series' own. The feed GUID is identity, so it is only ever filled. Returns the genre names
        /// from the feed's categories and the directory, trimmed, lowercased and without duplicates.
        /// </summary>
        public static IReadOnlyList<string> ApplyToSeries(
            PodcastSeries series, PodcastFeed feed, DirectoryPodcast? directoryHit, bool fillOnly)
        {
            var channel = feed.Series;

            if (channel.Title != null && (!fillOnly || IsPlaceholderTitle(series.Title)))
                series.Title = Truncate(channel.Title, 500);

            series.Description = Pick(series.Description, channel.Description, fillOnly, maxLength: null);
            series.Publisher = Pick(series.Publisher, channel.Publisher, fillOnly, 500);
            series.Thumbnail = Pick(series.Thumbnail, channel.ImageUrl, fillOnly, 2000);
            series.Link = Pick(series.Link, channel.Link, fillOnly, 2000);
            series.Language = Pick(series.Language, channel.Language, fillOnly, 20);

            if (string.IsNullOrWhiteSpace(series.FeedGuid) && channel.PodcastGuid is { Length: <= 100 } guid)
                series.FeedGuid = guid;

            if (feed.TotalItemCount > 0)
                series.TotalEpisodes = feed.TotalItemCount;

            var directoryGenres = ApplyDirectory(series, directoryHit);
            return NormalizeGenres(channel.Categories.Concat(directoryGenres));
        }

        /// <summary>
        /// Fills empty fields from a directory entry (title only when it is a placeholder) and returns
        /// its genre names, normalized.
        /// </summary>
        public static IReadOnlyList<string> ApplyDirectory(PodcastSeries series, DirectoryPodcast? directoryHit)
        {
            if (directoryHit == null)
                return Array.Empty<string>();

            if (IsPlaceholderTitle(series.Title) && !string.IsNullOrWhiteSpace(directoryHit.Title))
                series.Title = Truncate(directoryHit.Title.Trim(), 500);

            series.Publisher = Pick(series.Publisher, directoryHit.Publisher, fillOnly: true, 500);
            series.Thumbnail = Pick(series.Thumbnail, directoryHit.ArtworkUrl, fillOnly: true, 2000);

            if (series.TotalEpisodes == 0 && directoryHit.EpisodeCount > 0)
                series.TotalEpisodes = directoryHit.EpisodeCount.Value;

            return NormalizeGenres(directoryHit.Genres);
        }

        public static IReadOnlyList<string> NormalizeGenres(IEnumerable<string> names) =>
            names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

        private static string? Pick(string? current, string? incoming, bool fillOnly, int? maxLength)
        {
            if (string.IsNullOrWhiteSpace(incoming) || (maxLength.HasValue && incoming.Length > maxLength))
                return current;

            return fillOnly && !string.IsNullOrWhiteSpace(current) ? current : incoming;
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
