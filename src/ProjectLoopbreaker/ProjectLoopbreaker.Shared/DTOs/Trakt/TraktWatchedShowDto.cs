using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktWatchedShowDto
    {
        [JsonPropertyName("plays")]
        public int Plays { get; set; }

        [JsonPropertyName("last_watched_at")]
        public DateTime? LastWatchedAt { get; set; }

        [JsonPropertyName("last_updated_at")]
        public DateTime? LastUpdatedAt { get; set; }

        [JsonPropertyName("reset_at")]
        public DateTime? ResetAt { get; set; }

        [JsonPropertyName("show")]
        public TraktShowDto Show { get; set; } = new();

        [JsonPropertyName("seasons")]
        public List<TraktWatchedSeasonDto> Seasons { get; set; } = new();
    }
}
