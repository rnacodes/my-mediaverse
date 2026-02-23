using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.DTOs
{
    public class CreateMixlistDto
    {
        [Required]
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
    }
}




