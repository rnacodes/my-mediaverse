using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class BulkHighlightResultDto
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("linked")]
        public int Linked { get; set; }

        // Existing highlights matched by (title, text) and updated in place instead of duplicated.
        [JsonPropertyName("updated")]
        public int Updated { get; set; }

        // Duplicate (title, text) rows within the same upload; only the first is imported.
        [JsonPropertyName("skipped")]
        public int Skipped { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();
    }
}
