using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.GoogleBooks
{
    public class GoogleBooksIndustryIdentifierDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }  // "ISBN_10", "ISBN_13", "ISSN", "OTHER"

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
    }
}
