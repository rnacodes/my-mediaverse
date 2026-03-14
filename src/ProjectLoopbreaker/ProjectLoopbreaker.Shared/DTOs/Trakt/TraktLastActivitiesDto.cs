using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktLastActivitiesDto
    {
        [JsonPropertyName("all")]
        public DateTime? All { get; set; }

        [JsonPropertyName("movies")]
        public TraktMovieActivitiesDto Movies { get; set; } = new();

        [JsonPropertyName("episodes")]
        public TraktEpisodeActivitiesDto Episodes { get; set; } = new();

        [JsonPropertyName("shows")]
        public TraktShowActivitiesDto Shows { get; set; } = new();
    }

    public class TraktMovieActivitiesDto
    {
        [JsonPropertyName("watched_at")]
        public DateTime? WatchedAt { get; set; }

        [JsonPropertyName("collected_at")]
        public DateTime? CollectedAt { get; set; }

        [JsonPropertyName("rated_at")]
        public DateTime? RatedAt { get; set; }

        [JsonPropertyName("watchlisted_at")]
        public DateTime? WatchlistedAt { get; set; }
    }

    public class TraktEpisodeActivitiesDto
    {
        [JsonPropertyName("watched_at")]
        public DateTime? WatchedAt { get; set; }

        [JsonPropertyName("collected_at")]
        public DateTime? CollectedAt { get; set; }

        [JsonPropertyName("rated_at")]
        public DateTime? RatedAt { get; set; }

        [JsonPropertyName("watchlisted_at")]
        public DateTime? WatchlistedAt { get; set; }
    }

    public class TraktShowActivitiesDto
    {
        [JsonPropertyName("rated_at")]
        public DateTime? RatedAt { get; set; }

        [JsonPropertyName("watchlisted_at")]
        public DateTime? WatchlistedAt { get; set; }
    }
}
