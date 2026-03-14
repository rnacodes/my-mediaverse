using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktSyncResultDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("moviesCreated")]
        public int MoviesCreated { get; set; }

        [JsonPropertyName("moviesUpdated")]
        public int MoviesUpdated { get; set; }

        [JsonPropertyName("showsCreated")]
        public int ShowsCreated { get; set; }

        [JsonPropertyName("showsUpdated")]
        public int ShowsUpdated { get; set; }

        [JsonPropertyName("episodesCreated")]
        public int EpisodesCreated { get; set; }

        [JsonPropertyName("episodesUpdated")]
        public int EpisodesUpdated { get; set; }

        [JsonPropertyName("watchlistItemsProcessed")]
        public int WatchlistItemsProcessed { get; set; }

        [JsonPropertyName("ratingsProcessed")]
        public int RatingsProcessed { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime CompletedAt { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    public class TraktConnectionStatusDto
    {
        [JsonPropertyName("connected")]
        public bool Connected { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
