using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Outcome of an OPML subscription-list import, following the shared sync/import reporting
    /// contract. Per-feed problems are reported in the body, not as a failed request.
    /// </summary>
    public class OpmlImportResultDto
    {
        public const string OpmlImportOperation = "podcast-opml-import";

        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = OpmlImportOperation;

        /// <summary>Feed entries found in the file.</summary>
        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary>
        /// Always zero today: an OPML entry that matches an existing series is a skip, since the file
        /// carries nothing worth merging into a row that already exists.
        /// </summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        /// <summary>The same failures keyed by feed title, for a per-row display.</summary>
        [JsonPropertyName("failures")]
        public List<OpmlImportFailureDto> Failures { get; set; } = new();

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("warningMessage")]
        public string? WarningMessage { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

        [JsonPropertyName("reindexTriggered")]
        public bool ReindexTriggered { get; set; }
    }

    public class OpmlImportFailureDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
