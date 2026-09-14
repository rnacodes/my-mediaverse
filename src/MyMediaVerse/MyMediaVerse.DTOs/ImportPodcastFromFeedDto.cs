using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Request body for POST api/podcast/series/from-feed. Provide the feed URL, the Apple Podcasts id,
    /// or both.
    /// </summary>
    public class ImportPodcastFromFeedDto
    {
        [JsonPropertyName("feedUrl")]
        public string? FeedUrl { get; set; }

        [JsonPropertyName("applePodcastsId")]
        public string? ApplePodcastsId { get; set; }
    }
}
