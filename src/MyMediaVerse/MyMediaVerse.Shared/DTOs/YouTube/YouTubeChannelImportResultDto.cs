using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    /// <summary>
    /// Result of importing a channel's latest uploads, in the shape of the sync/import reporting
    /// contract. A completed run is returned with 200; an aborted run with 500 and the same body.
    /// </summary>
    public class YouTubeChannelImportResultDto
    {
        public const string ImportOperation = "youtube-channel-import-latest";
        public const int MaxErrors = 20;

        /// <summary>False only when the run aborted: the channel is unknown or the save failed.</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = ImportOperation;

        [JsonPropertyName("channelId")]
        public Guid ChannelId { get; set; }

        [JsonPropertyName("channelTitle")]
        public string? ChannelTitle { get; set; }

        /// <summary>How many of the newest uploads were asked for, after clamping to one YouTube page.</summary>
        [JsonPropertyName("requestedCount")]
        public int RequestedCount { get; set; }

        /// <summary>Uploads that were new to the library.</summary>
        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary>Uploads already in the library that had no channel and were linked to this one.</summary>
        [JsonPropertyName("linkedCount")]
        public int LinkedCount { get; set; }

        /// <summary>Uploads already in the library and already linked; left untouched.</summary>
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        /// <summary>Uploads YouTube listed but returned no details for; named in <see cref="Warnings"/>.</summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => CreatedCount + LinkedCount + SkippedCount + FailedCount;

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
