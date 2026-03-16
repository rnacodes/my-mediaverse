using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.GoogleBooks
{
    public class GoogleBooksSearchResultDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("items")]
        public GoogleBooksVolumeDto[]? Items { get; set; }
    }
}
