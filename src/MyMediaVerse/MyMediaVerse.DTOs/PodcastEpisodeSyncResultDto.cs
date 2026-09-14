using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result of syncing one podcast series' episodes against its RSS feed, in the shape of the
    /// sync/import reporting contract. A completed run (warnings included) is returned with 200; an
    /// aborted run with 500 and the same body.
    /// </summary>
    public class PodcastEpisodeSyncResultDto
    {
        public const string SyncOperation = "podcast-episode-sync";

        /// <summary>False only when the run aborted: the feed could not be read or the save failed.</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = SyncOperation;

        [JsonPropertyName("seriesId")]
        public Guid SeriesId { get; set; }

        [JsonPropertyName("seriesTitle")]
        public string SeriesTitle { get; set; } = string.Empty;

        /// <summary>Episodes created from feed items.</summary>
        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary>Stored episodes that gained their feed guid.</summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>Feed items already in the library.</summary>
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        /// <summary>Feed items with no audio or video enclosure; not episodes.</summary>
        [JsonPropertyName("ignoredCount")]
        public int IgnoredCount { get; set; }

        /// <summary>
        /// Older episodes not in the library that sync leaves alone by design; they can be imported one
        /// at a time from the feed-episodes browser.
        /// </summary>
        [JsonPropertyName("backlogCount")]
        public int BacklogCount { get; set; }

        /// <summary>Every item in the feed.</summary>
        [JsonPropertyName("feedItemCount")]
        public int FeedItemCount { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => CreatedCount + UpdatedCount + SkippedCount;

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        /// <summary>The fatal reason. Non-null only when <see cref="Success"/> is false.</summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>Non-fatal problem: the run completed but did not import everything new.</summary>
        [JsonPropertyName("warningMessage")]
        public string? WarningMessage { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        /// <summary>Null when the run aborted.</summary>
        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

        [JsonPropertyName("lastSyncDate")]
        public DateTime? LastSyncDate { get; set; }

        [JsonPropertyName("reindexTriggered")]
        public bool ReindexTriggered { get; set; }
    }
}
