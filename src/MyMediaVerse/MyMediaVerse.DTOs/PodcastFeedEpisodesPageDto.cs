using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// One page of a podcast series' feed, read live (or from a short cache), with each item marked
    /// when it is already in the library.
    /// </summary>
    public class PodcastFeedEpisodesPageDto
    {
        [JsonPropertyName("seriesId")]
        public Guid SeriesId { get; set; }

        /// <summary>Every item in the feed.</summary>
        [JsonPropertyName("feedItemCount")]
        public int FeedItemCount { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        /// <summary>When the feed was fetched (older than now when the page came from the cache).</summary>
        [JsonPropertyName("fetchedAt")]
        public DateTime FetchedAt { get; set; }

        [JsonPropertyName("items")]
        public List<PodcastFeedEpisodeItemDto> Items { get; set; } = new();
    }

    /// <summary>A feed item as the browser shows it.</summary>
    public class PodcastFeedEpisodeItemDto
    {
        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("durationSeconds")]
        public int? DurationSeconds { get; set; }

        [JsonPropertyName("audioUrl")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("episodeNumber")]
        public int? EpisodeNumber { get; set; }

        [JsonPropertyName("seasonNumber")]
        public int? SeasonNumber { get; set; }

        /// <summary>Plain-text description, shortened for listing.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>False when the item has no audio or video to import.</summary>
        [JsonPropertyName("importable")]
        public bool Importable { get; set; }

        /// <summary>The library episode this item already is, or null.</summary>
        [JsonPropertyName("existingEpisodeId")]
        public Guid? ExistingEpisodeId { get; set; }
    }
}
