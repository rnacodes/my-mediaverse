using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for returning results of a Paperless-ngx sync operation.
    /// </summary>
    public class DocumentSyncResultDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        [JsonPropertyName("addedCount")]
        public int AddedCount { get; set; }

        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("errors")]
        public List<DocumentSyncError> Errors { get; set; } = new();

        [JsonPropertyName("syncStartTime")]
        public DateTime SyncStartTime { get; set; }

        [JsonPropertyName("syncEndTime")]
        public DateTime? SyncEndTime { get; set; }

        [JsonPropertyName("durationSeconds")]
        public double? DurationSeconds => SyncEndTime.HasValue
            ? (SyncEndTime.Value - SyncStartTime).TotalSeconds
            : null;
    }

    /// <summary>
    /// Details about an individual sync error.
    /// </summary>
    public class DocumentSyncError
    {
        [JsonPropertyName("paperlessId")]
        public int? PaperlessId { get; set; }

        [JsonPropertyName("documentTitle")]
        public string? DocumentTitle { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        [JsonPropertyName("errorType")]
        public string? ErrorType { get; set; }
    }
}
