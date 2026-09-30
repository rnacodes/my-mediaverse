using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    /// <summary>
    /// Result of a YouTube refresh run over stored channels, playlists and videos, in the shape
    /// of the sync/import reporting contract. A completed run (per-item failures and a used-up
    /// quota included) is returned with 200; an aborted run with 500 and the same body.
    /// </summary>
    public class YouTubeRefreshResultDto
    {
        public const string RefreshOperation = "youtube-refresh-stale";
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

        /// <summary>Items YouTube had nothing new for; their refresh timestamp still moves forward.</summary>
        [JsonPropertyName("unchangedCount")]
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Items YouTube no longer returns (deleted or made private). They are kept, named in
        /// <see cref="Warnings"/>, and stamped as checked so they come due again next cycle.
        /// </summary>
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        /// <summary>Items whose YouTube request failed. They keep their old timestamp and return next run.</summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => UpdatedCount + UnchangedCount + SkippedCount + FailedCount;

        /// <summary>Stale items still waiting after this run.</summary>
        [JsonPropertyName("remainingCount")]
        public int RemainingCount { get; set; }

        [JsonPropertyName("videosProcessed")]
        public int VideosProcessed { get; set; }

        [JsonPropertyName("channelsProcessed")]
        public int ChannelsProcessed { get; set; }

        [JsonPropertyName("playlistsProcessed")]
        public int PlaylistsProcessed { get; set; }

        /// <summary>
        /// True when YouTube's daily quota ran out mid-run. The run stopped there and everything
        /// refreshed up to that point was saved.
        /// </summary>
        [JsonPropertyName("quotaExceeded")]
        public bool QuotaExceeded { get; set; }

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
