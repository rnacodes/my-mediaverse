using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Trakt
{
    public class TraktShowDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("ids")]
        public TraktIdsDto Ids { get; set; } = new();

        // Populated only when the request uses extended=full.
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();
    }
}
