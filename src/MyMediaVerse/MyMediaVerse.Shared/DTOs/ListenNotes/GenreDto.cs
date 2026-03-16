using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class GenreDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }
    }
}
