using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktWatchedEpisodeDto
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("plays")]
        public int Plays { get; set; }

        [JsonPropertyName("last_watched_at")]
        public DateTime? LastWatchedAt { get; set; }
    }
}
