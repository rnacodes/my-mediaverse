using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Obsidian
{
    /// <summary>
    /// DTO representing a single note from Quartz's /static/contentIndex.json.
    /// The contentIndex.json is a dictionary where keys are slugs and values are note objects.
    /// </summary>
    public class QuartzNoteDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("links")]
        public List<string>? Links { get; set; }
    }
}
