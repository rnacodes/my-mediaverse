using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class ImportMixlistDto
    {
        [Required]
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("mediaItemIds")]
        public string MediaItemIds { get; set; } = ""; // Semicolon-separated list of GUIDs
        
        [JsonPropertyName("mediaItemTitles")]
        public string MediaItemTitles { get; set; } = ""; // Semicolon-separated list of titles
        
        [JsonPropertyName("mediaItemTypes")]
        public string MediaItemTypes { get; set; } = ""; // Semicolon-separated list of media types
    }
}
