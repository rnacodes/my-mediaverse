using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class BulkHighlightResultDto
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("linked")]
        public int Linked { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();
    }
}
