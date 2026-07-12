using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesBatchPodcastsDto
    {
        [JsonPropertyName("podcasts")]
        public List<PodcastSeriesDto> Podcasts { get; set; } = new();
    }
}
