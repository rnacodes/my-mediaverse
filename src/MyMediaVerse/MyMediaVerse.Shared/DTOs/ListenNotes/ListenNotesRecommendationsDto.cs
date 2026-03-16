using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesRecommendationsDto
    {
        [JsonPropertyName("recommendations")]
        public List<PodcastSearchDto> Recommendations { get; set; } = new List<PodcastSearchDto>();
    }
}
