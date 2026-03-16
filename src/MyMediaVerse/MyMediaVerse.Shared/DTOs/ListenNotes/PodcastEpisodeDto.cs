using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class PodcastEpisodeDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("audio")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("pub_date_ms")]
        public long PublishDateMs { get; set; }

        [JsonPropertyName("audio_length_sec")]
        public int DurationInSeconds { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }
}
