using System.Text.Json.Serialization;

namespace MyMediaVerse.Infrastructure.Models
{
    /// <summary>
    /// Typesense document model for media items.
    /// This mirrors the schema defined in Typesense and represents how data is indexed.
    /// All fields must match the collection schema exactly.
    /// </summary>
    public class MediaItemDocument
    {
        /// <summary>
        /// Document ID in Typesense (matches PostgreSQL MediaItems.Id)
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Title of the media item (searchable, sortable)
        /// </summary>
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        /// <summary>
        /// Type of media: Article, Book, Movie, TVShow, Video, Podcast, Website, Channel, Playlist
        /// Facetable for filtering by type
        /// </summary>
        [JsonPropertyName("media_type")]
        public required string MediaType { get; set; }

        /// <summary>
        /// Description or summary of the media item (searchable)
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// List of topic names (facetable for filtering)
        /// </summary>
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new List<string>();

        /// <summary>
        /// List of genre names (facetable for filtering)
        /// </summary>
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        /// <summary>
        /// When the item was added to the library (Unix timestamp in seconds)
        /// Used for sorting by recency
        /// </summary>
        [JsonPropertyName("date_added")]
        public long DateAdded { get; set; }

        /// <summary>
        /// Current status: Uncharted, ActivelyExploring, Completed, Abandoned
        /// Facetable for filtering by status
        /// </summary>
        [JsonPropertyName("status")]
        public required string Status { get; set; }

        /// <summary>
        /// Optional user rating: SuperLike, Like, Neutral, Dislike
        /// Facetable for filtering by rating
        /// </summary>
        [JsonPropertyName("rating")]
        public string? Rating { get; set; }

        /// <summary>
        /// Thumbnail image URL (not searchable, just for display)
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Author name (for Books and Articles) - searchable and facetable
        /// </summary>
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        /// <summary>
        /// Director name (for Movies) - searchable and facetable
        /// </summary>
        [JsonPropertyName("director")]
        public string? Director { get; set; }

        /// <summary>
        /// Creator name (for TV Shows) - searchable and facetable
        /// </summary>
        [JsonPropertyName("creator")]
        public string? Creator { get; set; }

        /// <summary>
        /// Publisher/Host (for Podcasts) - searchable and facetable
        /// </summary>
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        /// <summary>
        /// Release year (for Movies and TV Shows) - facetable for filtering
        /// </summary>
        [JsonPropertyName("release_year")]
        public int? ReleaseYear { get; set; }

        /// <summary>
        /// Platform (for Videos, e.g., YouTube, Vimeo) - facetable
        /// </summary>
        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        /// <summary>
        /// Series ID (for Podcast Episodes) - used to distinguish episodes from series in routing
        /// </summary>
        [JsonPropertyName("series_id")]
        public string? SeriesId { get; set; }

        /// <summary>
        /// Text composed for semantic embedding. Typesense auto-embeds this via the collection's
        /// embedding field, so keyword and vector search stay sourced from one place. Serialized
        /// on write; ignored when search hits are deserialized back (no setter).
        /// </summary>
        [JsonPropertyName("embedding_source")]
        public string EmbeddingSource => string.Join("\n", new[]
        {
            Title,
            MediaType,
            Description,
            Topics.Count > 0 ? string.Join(", ", Topics) : null,
            Genres.Count > 0 ? string.Join(", ", Genres) : null,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
