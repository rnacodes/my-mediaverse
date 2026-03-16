using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class GenreResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("mediaItemIds")]
        public Guid[] MediaItemIds { get; set; } = Array.Empty<Guid>();

        [JsonPropertyName("mediaItemCount")]
        public int MediaItemCount { get; set; }
    }
}
