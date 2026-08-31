using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ReadwiseReader
{
    /// <summary>
    /// DTO for creating a document in Readwise Reader.
    /// The JsonPropertyName attributes are load-bearing: this DTO is serialized
    /// for the outbound save/ request and the API expects snake_case keys.
    /// </summary>
    public class CreateReaderDocumentDto
    {
        [JsonPropertyName("url")]
        public required string Url { get; set; }

        [JsonPropertyName("html")]
        public string? Html { get; set; }

        [JsonPropertyName("should_clean_html")]
        public bool ShouldCleanHtml { get; set; } = true;

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("published_date")]
        public string? PublishedDate { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; } = "new";

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }
}
