using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Trakt
{
    public class TraktWatchedMovieDto
    {
        [JsonPropertyName("plays")]
        public int Plays { get; set; }

        [JsonPropertyName("last_watched_at")]
        public DateTime? LastWatchedAt { get; set; }

        [JsonPropertyName("last_updated_at")]
        public DateTime? LastUpdatedAt { get; set; }

        [JsonPropertyName("movie")]
        public TraktMovieDto Movie { get; set; } = new();
    }
}
