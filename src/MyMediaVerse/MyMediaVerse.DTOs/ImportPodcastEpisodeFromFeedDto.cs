using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Request body for POST api/podcast/episodes/from-feed: which item of the series' feed to import.
    /// Identify it by its guid, or by its audio URL when the feed gives no guid.
    /// </summary>
    public class ImportPodcastEpisodeFromFeedDto
    {
        [JsonPropertyName("seriesId")]
        public Guid SeriesId { get; set; }

        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        [JsonPropertyName("audioUrl")]
        public string? AudioUrl { get; set; }
    }
}
