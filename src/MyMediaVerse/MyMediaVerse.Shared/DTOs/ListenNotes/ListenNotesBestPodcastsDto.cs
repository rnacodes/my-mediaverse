using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesBestPodcastsDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("has_previous")]
        public bool HasPrevious { get; set; }

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("page_number")]
        public int PageNumber { get; set; }

        [JsonPropertyName("previous_page_number")]
        public int? PreviousPageNumber { get; set; }

        [JsonPropertyName("next_page_number")]
        public int? NextPageNumber { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("podcasts")]
        public List<PodcastSearchDto> Podcasts { get; set; } = new List<PodcastSearchDto>();
    }
}
