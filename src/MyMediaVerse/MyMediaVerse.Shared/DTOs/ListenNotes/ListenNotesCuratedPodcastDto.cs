using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesCuratedPodcastDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("source_domain")]
        public string? SourceDomain { get; set; }

        [JsonPropertyName("pub_date_ms")]
        public long? PubDateMs { get; set; }

        [JsonPropertyName("podcasts")]
        public List<PodcastSearchDto>? Podcasts { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }
    }
}
