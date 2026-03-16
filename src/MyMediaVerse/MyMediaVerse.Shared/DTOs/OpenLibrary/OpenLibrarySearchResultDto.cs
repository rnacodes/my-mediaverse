using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.OpenLibrary
{
    public class OpenLibrarySearchResultDto
    {
        [JsonPropertyName("numFound")]
        public int NumFound { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("numFoundExact")]
        public bool NumFoundExact { get; set; }

        [JsonPropertyName("docs")]
        public OpenLibraryBookDto[] Docs { get; set; } = Array.Empty<OpenLibraryBookDto>();
    }
}
