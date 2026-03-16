using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesGenresDto
    {
        [JsonPropertyName("genres")]
        public List<GenreDto> Genres { get; set; } = new List<GenreDto>();
    }
}
