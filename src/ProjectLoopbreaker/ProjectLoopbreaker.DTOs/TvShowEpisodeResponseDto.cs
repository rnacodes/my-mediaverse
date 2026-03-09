using ProjectLoopbreaker.Domain.Entities;
using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.DTOs
{
    public class TvShowEpisodeResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }

        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("showId")]
        public Guid ShowId { get; set; }

        [JsonPropertyName("showTitle")]
        public string? ShowTitle { get; set; }

        [JsonPropertyName("seasonNumber")]
        public int? SeasonNumber { get; set; }

        [JsonPropertyName("episodeNumber")]
        public int? EpisodeNumber { get; set; }

        [JsonPropertyName("airDate")]
        public DateTime? AirDate { get; set; }

        [JsonPropertyName("durationInMinutes")]
        public int? DurationInMinutes { get; set; }

        [JsonPropertyName("tmdbEpisodeId")]
        public int? TmdbEpisodeId { get; set; }

        [JsonPropertyName("traktEpisodeId")]
        public int? TraktEpisodeId { get; set; }

        [JsonPropertyName("stillPath")]
        public string? StillPath { get; set; }

        [JsonPropertyName("traktPlays")]
        public int? TraktPlays { get; set; }

        [JsonPropertyName("traktLastWatchedAt")]
        public DateTime? TraktLastWatchedAt { get; set; }

        [JsonPropertyName("episodeIdentifier")]
        public string? EpisodeIdentifier { get; set; }
    }
}
