using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ReadwiseReader
{
    /// <summary>
    /// DTO for Readwise Reader document
    /// Based on: https://readwise.io/reader_api
    /// </summary>
    public class ReaderDocumentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The Readwise Reader URL (e.g., https://read.readwise.io/read/...)
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// The original source URL of the article (e.g., https://example.com/article)
        /// </summary>
        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; } = "new";

        /// <summary>
        /// Reader returns tags as a JSON object keyed by tag name (not an array);
        /// the keys are the tag names, the values are metadata objects.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, object>? Tags { get; set; }

        [JsonPropertyName("site_name")]
        public string? SiteName { get; set; }

        [JsonPropertyName("word_count")]
        public int? WordCount { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("published_date")]
        public string? PublishedDate { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("html")]
        public string? Html { get; set; }

        [JsonPropertyName("html_content")]
        public string? HtmlContent { get; set; }

        [JsonPropertyName("reading_progress")]
        public double? ReadingProgress { get; set; }

        [JsonPropertyName("favorite")]
        public bool? Favorite { get; set; }

        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
