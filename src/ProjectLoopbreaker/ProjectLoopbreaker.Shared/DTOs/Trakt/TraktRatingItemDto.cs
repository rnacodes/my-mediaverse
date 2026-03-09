using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktRatingItemDto
    {
        [JsonPropertyName("rated_at")]
        public DateTime? RatedAt { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("movie")]
        public TraktMovieDto? Movie { get; set; }

        [JsonPropertyName("show")]
        public TraktShowDto? Show { get; set; }
    }
}
