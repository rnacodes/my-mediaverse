using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Builds a new <see cref="PodcastEpisode"/> from a create DTO: the one mapping every episode-creating
    /// path shares. Topics and genres are left to the caller, which resolves them against the context.
    /// </summary>
    public static class PodcastEpisodeFactory
    {
        public static PodcastEpisode Create(CreatePodcastEpisodeDto dto) => new()
        {
            Title = dto.Title,
            MediaType = MediaType.Podcast,
            Link = dto.Link,
            Notes = dto.Notes,
            Status = dto.Status,
            DateAdded = DateTime.UtcNow,
            DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
            Rating = dto.Rating,
            OwnershipStatus = dto.OwnershipStatus,
            Description = dto.Description,
            RelatedNotes = dto.RelatedNotes,
            Thumbnail = dto.Thumbnail,
            SeriesId = dto.SeriesId,
            AudioLink = dto.AudioLink,
            ReleaseDate = DateTimeNormalizer.ToUtc(dto.ReleaseDate),
            DurationInSeconds = dto.DurationInSeconds,
            EpisodeNumber = dto.EpisodeNumber,
            SeasonNumber = dto.SeasonNumber,
            ExternalId = BlankToNull(dto.ExternalId),
            RssGuid = BlankToNull(dto.RssGuid),
            Publisher = dto.Publisher
        };

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
