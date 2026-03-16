using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    public class TmdbMovieDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }

        [JsonPropertyName("genre_ids")]
        public int[] GenreIds { get; set; } = Array.Empty<int>();

        [JsonPropertyName("runtime")]
        public int? Runtime { get; set; }

        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("production_companies")]
        public TmdbProductionCompanyDto[] ProductionCompanies { get; set; } = Array.Empty<TmdbProductionCompanyDto>();

        [JsonPropertyName("production_countries")]
        public TmdbProductionCountryDto[] ProductionCountries { get; set; } = Array.Empty<TmdbProductionCountryDto>();

        [JsonPropertyName("spoken_languages")]
        public TmdbSpokenLanguageDto[] SpokenLanguages { get; set; } = Array.Empty<TmdbSpokenLanguageDto>();
    }

    public class TmdbProductionCompanyDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("logo_path")]
        public string? LogoPath { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("origin_country")]
        public string? OriginCountry { get; set; }
    }

    public class TmdbProductionCountryDto
    {
        [JsonPropertyName("iso_3166_1")]
        public string Iso31661 { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TmdbSpokenLanguageDto
    {
        [JsonPropertyName("english_name")]
        public string EnglishName { get; set; } = string.Empty;

        [JsonPropertyName("iso_639_1")]
        public string Iso6391 { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
