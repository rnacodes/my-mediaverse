using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktWatchlistItemDto
    {
        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("listed_at")]
        public DateTime? ListedAt { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("movie")]
        public TraktMovieDto? Movie { get; set; }

        [JsonPropertyName("show")]
        public TraktShowDto? Show { get; set; }
    }
}
