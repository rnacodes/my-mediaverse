using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    /// <summary>
    /// Result of a TMDB refresh run over stored movies and TV shows, in the shape of the
    /// sync/import reporting contract. A completed run (per-item failures included) is returned
    /// with 200; an aborted run with 500 and the same body.
    /// </summary>
    public class MovieTvRefreshResultDto
    {
        public const string RefreshOperation = "tmdb-refresh-stale";
        public const int MaxErrors = 20;

        /// <summary>False only when the run aborted: candidates could not be read or the save failed.</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = RefreshOperation;

        /// <summary>Items whose stored data changed.</summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>Items TMDB had nothing new for; their refresh timestamp still moves forward.</summary>
        [JsonPropertyName("unchangedCount")]
        public int UnchangedCount { get; set; }

        /// <summary>Items whose stored TMDB id is not a usable number; no request was made.</summary>
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        /// <summary>Items whose TMDB request failed. They keep their old timestamp and return next run.</summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => UpdatedCount + UnchangedCount + SkippedCount + FailedCount;

        /// <summary>Stale items still waiting after this run.</summary>
        [JsonPropertyName("remainingCount")]
        public int RemainingCount { get; set; }

        /// <summary>Per-item failures, capped at <see cref="MaxErrors"/>.</summary>
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();

        /// <summary>The fatal reason. Non-null only when <see cref="Success"/> is false.</summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        /// <summary>Null when the run aborted.</summary>
        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("reindexTriggered")]
        public bool ReindexTriggered { get; set; }

        public void AddError(string message)
        {
            if (Errors.Count < MaxErrors) Errors.Add(message);
        }
    }
}
