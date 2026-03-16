using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class TvShowSearchResultDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("posterPath")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdropPath")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("firstAirDate")]
        public string? FirstAirDate { get; set; }

        [JsonPropertyName("lastAirDate")]
        public string? LastAirDate { get; set; }

        [JsonPropertyName("voteAverage")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("originalName")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("genreIds")]
        public int[] GenreIds { get; set; } = Array.Empty<int>();

        [JsonPropertyName("originCountry")]
        public string[] OriginCountry { get; set; } = Array.Empty<string>();

        [JsonPropertyName("numberOfEpisodes")]
        public int NumberOfEpisodes { get; set; }

        [JsonPropertyName("numberOfSeasons")]
        public int NumberOfSeasons { get; set; }

        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("posterUrl")]
        public string? PosterUrl { get; set; }

        [JsonPropertyName("backdropUrl")]
        public string? BackdropUrl { get; set; }
    }
}
