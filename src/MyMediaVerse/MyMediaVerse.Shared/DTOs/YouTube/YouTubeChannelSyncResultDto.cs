using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    /// <summary>
    /// Result of refreshing one channel's metadata from YouTube, in the shape of the sync/import
    /// reporting contract. A completed run is returned with 200; an aborted run with 500 and the
    /// same body.
    /// </summary>
    public class YouTubeChannelSyncResultDto
    {
        public const string SyncOperation = "youtube-channel-sync";

        /// <summary>False only when the run aborted: the channel is unknown or the save failed.</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = SyncOperation;

        [JsonPropertyName("channelId")]
        public Guid ChannelId { get; set; }

        [JsonPropertyName("channelTitle")]
        public string? ChannelTitle { get; set; }

        /// <summary>1 when YouTube changed any stored value, else 0.</summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>The channel's upload count as YouTube reports it.</summary>
        [JsonPropertyName("youTubeVideoCount")]
        public long? YouTubeVideoCount { get; set; }

        /// <summary>Videos in the library linked to this channel.</summary>
        [JsonPropertyName("storedVideoCount")]
        public int StoredVideoCount { get; set; }

        /// <summary>
        /// Uploads not yet in the library: YouTube's count minus the stored count, never below
        /// zero. An approximation — a stored video from another source counts as covered, and a
        /// deleted upload still counts on YouTube's side.
        /// </summary>
        [JsonPropertyName("newUploadsCount")]
        public long NewUploadsCount { get; set; }

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
    }
}
