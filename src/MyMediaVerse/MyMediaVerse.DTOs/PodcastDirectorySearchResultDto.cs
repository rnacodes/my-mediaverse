using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// One show from GET api/podcast/directory/search.
    /// </summary>
    public class PodcastDirectorySearchResultDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("feedUrl")]
        public string? FeedUrl { get; set; }

        [JsonPropertyName("applePodcastsId")]
        public string? ApplePodcastsId { get; set; }

        [JsonPropertyName("podcastIndexId")]
        public long? PodcastIndexId { get; set; }

        [JsonPropertyName("artworkUrl")]
        public string? ArtworkUrl { get; set; }

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("episodeCount")]
        public int? EpisodeCount { get; set; }

        [JsonPropertyName("storeUrl")]
        public string? StoreUrl { get; set; }

        [JsonPropertyName("latestReleaseDate")]
        public DateTime? LatestReleaseDate { get; set; }

        /// <summary>The directory the result came from, e.g. "apple".</summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>The library series this show already matches, or null when it is not in the library.</summary>
        [JsonPropertyName("existingSeriesId")]
        public Guid? ExistingSeriesId { get; set; }
    }
}
