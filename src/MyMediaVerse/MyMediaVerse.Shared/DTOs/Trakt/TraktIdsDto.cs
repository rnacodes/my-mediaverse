using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Trakt
{
    public class TraktIdsDto
    {
        [JsonPropertyName("trakt")]
        public int? Trakt { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("imdb")]
        public string? Imdb { get; set; }

        [JsonPropertyName("tmdb")]
        public int? Tmdb { get; set; }

        [JsonPropertyName("tvdb")]
        public int? Tvdb { get; set; }
    }
}
