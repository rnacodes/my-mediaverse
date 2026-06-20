using System.Text.Json.Serialization;

namespace MyMediaVerse.Infrastructure.Models
{
    /// <summary>
    /// Typesense document model for mixlists.
    /// This mirrors the schema defined in Typesense and represents how mixlist data is indexed.
    /// All fields must match the collection schema exactly.
    /// </summary>
    public class MixlistDocument
    {
        /// <summary>
        /// Document ID in Typesense (matches PostgreSQL Mixlists.Id)
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Name of the mixlist (searchable, sortable)
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        /// <summary>
        /// Description of the mixlist (searchable)
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Thumbnail image URL (not searchable, just for display)
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        /// <summary>
        /// When the mixlist was created (Unix timestamp in seconds)
        /// Used for sorting by recency
        /// </summary>
        [JsonPropertyName("date_created")]
        public long DateCreated { get; set; }

        /// <summary>
        /// Number of media items in the mixlist (sortable, facetable)
        /// </summary>
        [JsonPropertyName("media_item_count")]
        public int MediaItemCount { get; set; }

        /// <summary>
        /// Titles of media items contained in the mixlist (searchable)
        /// Allows searching mixlists by their contents
        /// </summary>
        [JsonPropertyName("media_item_titles")]
        public List<string> MediaItemTitles { get; set; } = new List<string>();

        /// <summary>
        /// Aggregated topics from all media items in the mixlist (facetable)
        /// Allows filtering mixlists by topic
        /// </summary>
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new List<string>();

        /// <summary>
        /// Aggregated genres from all media items in the mixlist (facetable)
        /// Allows filtering mixlists by genre
        /// </summary>
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        /// <summary>
        /// Text composed for semantic embedding. Typesense auto-embeds this via the collection's
        /// embedding field, so keyword and vector search stay sourced from one place. Serialized
        /// on write; ignored when search hits are deserialized back (no setter).
        /// </summary>
        [JsonPropertyName("embedding_source")]
        public string EmbeddingSource => string.Join("\n", new[]
        {
            Name,
            Description,
            Topics.Count > 0 ? string.Join(", ", Topics) : null,
            Genres.Count > 0 ? string.Join(", ", Genres) : null,
            MediaItemTitles.Count > 0 ? string.Join(", ", MediaItemTitles) : null,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}




















