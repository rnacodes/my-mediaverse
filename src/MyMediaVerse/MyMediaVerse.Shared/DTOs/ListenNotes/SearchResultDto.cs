using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class SearchResultDto
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("results")]
        public List<PodcastSearchDto> Results { get; set; } = new List<PodcastSearchDto>();

        [JsonPropertyName("next_offset")]
        public int? NextOffset { get; set; }
    }
}

