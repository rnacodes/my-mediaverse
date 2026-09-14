using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Response for POST api/podcast/series/from-feed.
    /// </summary>
    public class PodcastFeedImportResultDto
    {
        [JsonPropertyName("series")]
        public required PodcastSeriesResponseDto Series { get; set; }

        /// <summary>False when an existing series already matched the feed or Apple id.</summary>
        [JsonPropertyName("created")]
        public bool Created { get; set; }

        /// <summary>Whether the feed was fetched and parsed during this request.</summary>
        [JsonPropertyName("feedRead")]
        public bool FeedRead { get; set; }

        /// <summary>Set when the series was saved without feed details.</summary>
        [JsonPropertyName("warningMessage")]
        public string? WarningMessage { get; set; }
    }
}
