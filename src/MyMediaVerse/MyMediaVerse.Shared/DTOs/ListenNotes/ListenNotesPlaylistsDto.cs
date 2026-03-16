using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.ListenNotes
{
    public class ListenNotesPlaylistsDto
    {
        [JsonPropertyName("playlists")]
        public List<ListenNotesPlaylistDto> Playlists { get; set; } = new List<ListenNotesPlaylistDto>();

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("has_previous")]
        public bool HasPrevious { get; set; }

        [JsonPropertyName("next_page_number")]
        public int? NextPageNumber { get; set; }

        [JsonPropertyName("previous_page_number")]
        public int? PreviousPageNumber { get; set; }

        [JsonPropertyName("page_number")]
        public int PageNumber { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
