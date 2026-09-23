using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    /// <summary>
    /// Result of importing one TV show's episodes from TMDB, in the shape of the sync/import
    /// reporting contract. A completed run (season failures included) is returned with 200; an
    /// aborted run with 500 and the same body.
    /// </summary>
    public class TvEpisodeImportResultDto
    {
        public const string ImportOperation = "tv-episodes-from-tmdb";
        public const int MaxErrors = 20;

        /// <summary>False only when the run aborted: the show is unknown, has no TMDB id, its details could not be read, or the save failed.</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = ImportOperation;

        [JsonPropertyName("showId")]
        public Guid ShowId { get; set; }

        [JsonPropertyName("showTitle")]
        public string? ShowTitle { get; set; }

        /// <summary>Episodes that did not exist before this run.</summary>
        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary>Stored episodes that gained at least one value (a placeholder title, a missing description, a still, ...).</summary>
        [JsonPropertyName("updatedCount")]
        public int UpdatedCount { get; set; }

        /// <summary>Stored episodes TMDB had nothing to add to.</summary>
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        /// <summary>Seasons whose TMDB request failed; their episodes were not touched.</summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        /// <summary>Seasons whose episodes were walked, season 0 (specials) included.</summary>
        [JsonPropertyName("seasonsProcessed")]
        public int SeasonsProcessed { get; set; }

        /// <summary>Episodes seen across the processed seasons.</summary>
        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed => CreatedCount + UpdatedCount + SkippedCount;

        /// <summary>Per-season failures, capped at <see cref="MaxErrors"/>.</summary>
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
