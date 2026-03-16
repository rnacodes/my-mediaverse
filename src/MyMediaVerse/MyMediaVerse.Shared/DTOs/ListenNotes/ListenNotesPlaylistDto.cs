using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesPlaylistDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }

        [JsonPropertyName("total_audio_length_sec")]
        public int? TotalAudioLengthSec { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("items")]
        public List<PodcastEpisodeDto>? Items { get; set; }

        [JsonPropertyName("last_timestamp_ms")]
        public long? LastTimestampMs { get; set; }
    }
}
