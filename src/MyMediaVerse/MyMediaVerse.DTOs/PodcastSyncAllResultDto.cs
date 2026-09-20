using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result of syncing every subscribed podcast series, in the shape of the sync/import reporting
    /// contract. One series failing is a per-item failure; only a run that could not start or finish
    /// its bookkeeping sets <see cref="Success"/> to false.
    /// </summary>
    public class PodcastSyncAllResultDto
    {
        public const string SyncAllOperation = "podcast-sync-all";

        /// <summary>At most this many per-series error lines are kept.</summary>
        public const int MaxErrors = 20;

        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = SyncAllOperation;

        /// <summary>Series this run attempted.</summary>
        [JsonPropertyName("seriesChecked")]
        public int SeriesChecked { get; set; }

        [JsonPropertyName("seriesSucceeded")]
        public int SeriesSucceeded { get; set; }

        [JsonPropertyName("seriesFailed")]
        public int SeriesFailed { get; set; }

        /// <summary>Subscribed series the run did not reach (cancelled or out of time).</summary>
        [JsonPropertyName("pendingSeriesCount")]
        public int PendingSeriesCount { get; set; }

        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount => SeriesFailed;

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => SeriesChecked;

        /// <summary>One line per attempted series.</summary>
        [JsonPropertyName("series")]
        public List<PodcastSeriesSyncSummaryDto> Series { get; set; } = new();

        /// <summary>"{series title}: {reason}" for failed series, capped at <see cref="MaxErrors"/>.</summary>
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("wasCancelled")]
        public bool WasCancelled { get; set; }

        [JsonPropertyName("timeBudgetReached")]
        public bool TimeBudgetReached { get; set; }

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

    /// <summary>What happened to one series during sync-all.</summary>
    public class PodcastSeriesSyncSummaryDto
    {
        [JsonPropertyName("seriesId")]
        public Guid SeriesId { get; set; }

        [JsonPropertyName("seriesTitle")]
        public string SeriesTitle { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        [JsonPropertyName("warningMessage")]
        public string? WarningMessage { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}
