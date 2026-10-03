using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    /// <summary>
    /// Result of mirroring one playlist from YouTube, in the shape of the sync/import reporting
    /// contract. A completed run is returned with 200; an aborted run with 500 and the same body.
    /// </summary>
    public class YouTubePlaylistSyncResultDto
    {
        public const string SyncOperation = "youtube-playlist-sync";

        /// <summary>False only when the run aborted: the playlist is unknown or the save failed.</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = SyncOperation;

        [JsonPropertyName("playlistId")]
        public Guid PlaylistId { get; set; }

        [JsonPropertyName("playlistTitle")]
        public string? PlaylistTitle { get; set; }

        /// <summary>Videos that were new to the library.</summary>
        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary>Videos already in the library that joined the playlist.</summary>
        [JsonPropertyName("linkedCount")]
        public int LinkedCount { get; set; }

        /// <summary>Videos that left the playlist; their rows stay.</summary>
        [JsonPropertyName("unlinkedCount")]
        public int UnlinkedCount { get; set; }

        /// <summary>Videos whose place in the playlist moved.</summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>Videos the playlist holds on YouTube, deleted and private ones left out.</summary>
        [JsonPropertyName("videoCount")]
        public int VideoCount { get; set; }

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
