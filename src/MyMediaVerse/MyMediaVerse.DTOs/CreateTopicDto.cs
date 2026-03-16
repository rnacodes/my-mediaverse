using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class CreateTopicDto
    {
        [Required]
        [JsonPropertyName("name")]
        public required string Name { get; set; }
    }
}
