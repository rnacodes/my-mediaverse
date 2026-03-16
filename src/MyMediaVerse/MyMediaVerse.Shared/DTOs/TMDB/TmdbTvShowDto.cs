using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    public class TmdbTvShowDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }

        [JsonPropertyName("last_air_date")]
        public string? LastAirDate { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("original_name")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("genre_ids")]
        public int[] GenreIds { get; set; } = Array.Empty<int>();

        [JsonPropertyName("origin_country")]
        public string[] OriginCountry { get; set; } = Array.Empty<string>();

        [JsonPropertyName("number_of_episodes")]
        public int NumberOfEpisodes { get; set; }

        [JsonPropertyName("number_of_seasons")]
        public int NumberOfSeasons { get; set; }

        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("networks")]
        public TmdbNetworkDto[] Networks { get; set; } = Array.Empty<TmdbNetworkDto>();

        [JsonPropertyName("production_companies")]
        public TmdbProductionCompanyDto[] ProductionCompanies { get; set; } = Array.Empty<TmdbProductionCompanyDto>();

        [JsonPropertyName("production_countries")]
        public TmdbProductionCountryDto[] ProductionCountries { get; set; } = Array.Empty<TmdbProductionCountryDto>();

        [JsonPropertyName("spoken_languages")]
        public TmdbSpokenLanguageDto[] SpokenLanguages { get; set; } = Array.Empty<TmdbSpokenLanguageDto>();
    }

    public class TmdbTvEpisodeDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }

        [JsonPropertyName("episode_number")]
        public int EpisodeNumber { get; set; }

        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("still_path")]
        public string? StillPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }
    }

    public class TmdbNetworkDto
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
}
