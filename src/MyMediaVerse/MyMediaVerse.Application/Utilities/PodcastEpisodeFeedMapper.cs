using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.Podcasts;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Turns a feed item into an episode create request. Values too long for their column are dropped
    /// rather than failing the save; titles are truncated.
    /// </summary>
    public static class PodcastEpisodeFeedMapper
    {
        public const string UntitledEpisode = "Untitled episode";

        /// <summary>
        /// Returns the create request, or null when the item has no audio or video enclosure (it is not
        /// a playable episode). Topics and genres are left empty so creation inherits the series'.
        /// </summary>
        public static CreatePodcastEpisodeDto? ToCreateDto(FeedEpisode item, PodcastSeries series)
        {
            if (!IsImportable(item))
                return null;

            var title = string.IsNullOrWhiteSpace(item.Title) ? UntitledEpisode : item.Title.Trim();

            return new CreatePodcastEpisodeDto
            {
                Title = title.Length <= 500 ? title : title[..500],
                SeriesId = series.Id,
                Status = Status.Uncharted,
                Description = item.Description,
                AudioLink = FitOrNull(item.EnclosureUrl, 2000),
                Link = FitOrNull(item.Link, 2000),
                Thumbnail = FitOrNull(item.ImageUrl, 2000),
                ReleaseDate = item.PublishedAt,
                DurationInSeconds = item.DurationSeconds ?? 0,
                EpisodeNumber = item.EpisodeNumber,
                SeasonNumber = item.SeasonNumber,
                RssGuid = FitOrNull(item.Guid, 500),
                Publisher = series.Publisher
            };
        }

        /// <summary>An item is an episode worth storing when it carries a media enclosure URL.</summary>
        public static bool IsImportable(FeedEpisode item) =>
            UrlNormalizer.IsValid(item.EnclosureUrl) && item.EnclosureUrl!.Length <= 2000;

        private static string? FitOrNull(string? value, int maxLength) =>
            string.IsNullOrWhiteSpace(value) || value.Length > maxLength ? null : value.Trim();
    }
}
