using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result DTO for the Goodreads CSV import. Follows the sync/import reporting contract:
    /// <see cref="Success"/> flips only when the import itself aborted (unreadable CSV); per-row
    /// failures are counted and summarized in <see cref="WarningMessage"/>.
    /// </summary>
    public class GoodreadsImportResultDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = "goodreads-import";

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }

        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("importedBooks")]
        public List<GoodreadsImportedBookDto> ImportedBooks { get; set; } = new();

        /// <summary>The fatal reason. Non-null only when <see cref="Success"/> is false.</summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>Non-fatal summary, e.g. some rows failed while the import completed.</summary>
        [JsonPropertyName("warningMessage")]
        public string? WarningMessage { get; set; }

        /// <summary>Whether the controller fired the post-import search reindex.</summary>
        [JsonPropertyName("reindexTriggered")]
        public bool ReindexTriggered { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }

    /// <summary>
    /// Summary of a book that was imported from Goodreads
    /// </summary>
    public class GoodreadsImportedBookDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("wasUpdated")]
        public bool WasUpdated { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
    }
}
