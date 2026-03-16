using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for importing articles from CSV files.
    /// Used during data import and migration operations.
    /// </summary>
    public class ArticleCsvImportDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; } = false;

        [JsonPropertyName("isStarred")]
        public bool IsStarred { get; set; } = false;

        [JsonPropertyName("publicationDate")]
        public DateTime? PublicationDate { get; set; }

        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
    }
}
