using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result of importing a purchased Listen Notes dataset export (a podcasts or an episodes CSV).
    /// Follows the shared import reporting shape: one row of the file is one processed item.
    /// </summary>
    public class PodcastDatasetImportResultDto
    {
        public const string DatasetImportOperation = "podcast-dataset-import";
        public const string PodcastsFile = "podcasts";
        public const string EpisodesFile = "episodes";

        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = DatasetImportOperation;

        /// <summary>Which export the file was: "podcasts" or "episodes" (told apart by its header).</summary>
        [JsonPropertyName("fileKind")]
        public string? FileKind { get; set; }

        /// <summary>Data rows found in the file.</summary>
        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        /// <summary>Rows that added a new item: a show (podcasts file) or an episode (episodes file).</summary>
        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary>Always zero: a row that matches an existing item is a skip.</summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>Rows already in the library, or with no title.</summary>
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        /// <summary>New shows added. For an episodes file, these are the shows its episodes belong to.</summary>
        [JsonPropertyName("seriesCreatedCount")]
        public int SeriesCreatedCount { get; set; }

        [JsonPropertyName("episodesCreatedCount")]
        public int EpisodesCreatedCount { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        /// <summary>The same failures keyed by row title, for a per-row display.</summary>
        [JsonPropertyName("failures")]
        public List<PodcastDatasetImportFailureDto> Failures { get; set; } = new();

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

    public class PodcastDatasetImportFailureDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
