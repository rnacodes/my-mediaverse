//This DTO is for processing info coming in via API

using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class PodcastSeriesDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("publisher")]
        public string Publisher { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("episodes")]
        public List<PodcastEpisodeDto> Episodes { get; set; } = new();

        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = new();

        [JsonPropertyName("total_episodes")]
        public int TotalEpisodes { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("next_episode_pub_date")]
        public long? NextEpisodePubDate { get; set; }
    }
}
