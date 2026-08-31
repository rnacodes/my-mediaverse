using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ReadwiseReader
{
    /// <summary>
    /// Response from Readwise Reader list endpoint
    /// </summary>
    public class ReaderDocumentsResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("nextPageCursor")]
        public string? NextPageCursor { get; set; }

        [JsonPropertyName("results")]
        public List<ReaderDocumentDto> Results { get; set; } = new();
    }
}
