using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class BulkHighlightResultDto
    {
        // Fatal-only: false means the batch save failed and nothing was persisted.
        // Per-item failures land in Errors and leave Success true.
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        // Stable identifier of the operation for sync-state records and notifications.
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = "highlight-bulk-import";

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

        // Per-item failures; the rest of the batch still imports.
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();

        // Set only when Success is false.
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => Created + Updated + Skipped;

        [JsonPropertyName("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
