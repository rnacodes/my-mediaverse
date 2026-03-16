using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class UpdateMixlistDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("topics")]
        public string[]? Topics { get; set; }

        [JsonPropertyName("genres")]
        public string[]? Genres { get; set; }
    }
}
