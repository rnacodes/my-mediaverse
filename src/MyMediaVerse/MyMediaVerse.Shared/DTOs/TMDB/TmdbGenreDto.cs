using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    public class TmdbGenreDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TmdbGenreListDto
    {
        [JsonPropertyName("genres")]
        public TmdbGenreDto[] Genres { get; set; } = Array.Empty<TmdbGenreDto>();
    }
}
