using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class ImportPodcastByNameDto
    {
        [Required]
        [JsonPropertyName("podcastName")]
        public string PodcastName { get; set; } = string.Empty;
    }
}
