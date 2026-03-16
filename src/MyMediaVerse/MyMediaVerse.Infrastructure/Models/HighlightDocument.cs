using System.Text.Json.Serialization;

namespace MyMediaVerse.Infrastructure.Models
{
    /// <summary>
    /// Typesense document model for Highlights.
    /// This mirrors the schema defined in Typesense for the highlights collection.
    /// All fields must match the collection schema exactly.
    /// </summary>
    public class HighlightDocument
    {
        /// <summary>
        /// Document ID in Typesense (matches PostgreSQL Highlights.Id)
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// The actual highlight text (searchable)
        /// </summary>
        [JsonPropertyName("text")]
        public required string Text { get; set; }

        /// <summary>
        /// Optional annotation/note attached to the highlight (searchable)
        /// </summary>
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Title of the source (book, article, podcast) - searchable
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Author of the source (searchable/facetable)
        /// </summary>
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        /// <summary>
        /// Category from Readwise (books, articles, tweets, podcasts)
        /// Facetable for filtering
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        /// <summary>
        /// List of tags (facetable)
        /// Converted from comma-separated string in the entity
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Source URL (for articles, tweets, etc.) - not indexed
        /// </summary>
        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Readwise source_type (instapaper, kindle, reader, etc.)
        /// Facetable for filtering
        /// </summary>
        [JsonPropertyName("source_type")]
        public string? SourceType { get; set; }

        /// <summary>
        /// Whether this is a favorite highlight (facetable)
        /// </summary>
        [JsonPropertyName("is_favorite")]
        public bool IsFavorite { get; set; }

        /// <summary>
        /// When the highlight was created in the original source (Unix timestamp)
        /// Used for sorting by when content was highlighted
        /// </summary>
        [JsonPropertyName("highlighted_at")]
        public long? HighlightedAt { get; set; }

        /// <summary>
        /// When this highlight was imported to MyMediaVerse (Unix timestamp)
        /// Used for sorting by recency of import
        /// </summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        /// <summary>
        /// ID of linked Article (if any) - for filtering linked vs unlinked
        /// </summary>
        [JsonPropertyName("article_id")]
        public string? ArticleId { get; set; }

        /// <summary>
        /// ID of linked Book (if any) - for filtering linked vs unlinked
        /// </summary>
        [JsonPropertyName("book_id")]
        public string? BookId { get; set; }

        /// <summary>
        /// Combined linked media ID (article_id or book_id) for easier filtering
        /// Null if not linked to any media
        /// </summary>
        [JsonPropertyName("linked_media_id")]
        public string? LinkedMediaId { get; set; }

        /// <summary>
        /// Title of the linked media item (for display without join)
        /// </summary>
        [JsonPropertyName("linked_media_title")]
        public string? LinkedMediaTitle { get; set; }

        /// <summary>
        /// Type of linked media: "article", "book", or null if unlinked
        /// Facetable for filtering
        /// </summary>
        [JsonPropertyName("linked_media_type")]
        public string? LinkedMediaType { get; set; }

        /// <summary>
        /// Location in the source (page number, timestamp, etc.)
        /// </summary>
        [JsonPropertyName("location")]
        public int? Location { get; set; }

        /// <summary>
        /// Cover/thumbnail image URL from the source
        /// </summary>
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
