using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class MovieSearchResultDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("posterPath")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdropPath")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("voteAverage")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("originalTitle")]
        public string? OriginalTitle { get; set; }

        [JsonPropertyName("genreIds")]
        public int[] GenreIds { get; set; } = Array.Empty<int>();

        [JsonPropertyName("runtime")]
        public int? Runtime { get; set; }

        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("imdbId")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("posterUrl")]
        public string? PosterUrl { get; set; }

        [JsonPropertyName("backdropUrl")]
        public string? BackdropUrl { get; set; }
    }
}
