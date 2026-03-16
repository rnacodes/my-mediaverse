using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Paperless
{
    /// <summary>
    /// Represents a tag from Paperless-ngx API
    /// </summary>
    public class PaperlessTagDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("text_color")]
        public string? TextColor { get; set; }

        [JsonPropertyName("match")]
        public string? Match { get; set; }

        [JsonPropertyName("matching_algorithm")]
        public int MatchingAlgorithm { get; set; }

        [JsonPropertyName("is_insensitive")]
        public bool IsInsensitive { get; set; }

        [JsonPropertyName("is_inbox_tag")]
        public bool IsInboxTag { get; set; }

        [JsonPropertyName("document_count")]
        public int DocumentCount { get; set; }

        [JsonPropertyName("owner")]
        public int? Owner { get; set; }
    }

    /// <summary>
    /// Paginated list response for tags from Paperless-ngx
    /// </summary>
    public class PaperlessTagListResponseDto
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("all")]
        public List<int>? All { get; set; }

        [JsonPropertyName("results")]
        public List<PaperlessTagDto> Results { get; set; } = new();
    }
}
