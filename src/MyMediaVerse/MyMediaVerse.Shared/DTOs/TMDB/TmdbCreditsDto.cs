using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    public class TmdbCreditsDto
    {
        [JsonPropertyName("cast")]
        public List<TmdbCastMemberDto> Cast { get; set; } = new();

        [JsonPropertyName("crew")]
        public List<TmdbCrewMemberDto> Crew { get; set; } = new();
    }

    public class TmdbCastMemberDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }

    public class TmdbCrewMemberDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("job")]
        public string? Job { get; set; }
    }

    public class TmdbCreatedByDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    // Movie certifications: one entry per country, each with one or more dated releases.
    public class TmdbReleaseDatesDto
    {
        [JsonPropertyName("results")]
        public List<TmdbCountryReleaseDatesDto> Results { get; set; } = new();
    }

    public class TmdbCountryReleaseDatesDto
    {
        [JsonPropertyName("iso_3166_1")]
        public string Iso31661 { get; set; } = string.Empty;

        [JsonPropertyName("release_dates")]
        public List<TmdbReleaseDateDto> ReleaseDates { get; set; } = new();
    }

    public class TmdbReleaseDateDto
    {
        [JsonPropertyName("certification")]
        public string? Certification { get; set; }
    }

    // TV content ratings: one entry per country.
    public class TmdbContentRatingsDto
    {
        [JsonPropertyName("results")]
        public List<TmdbCountryContentRatingDto> Results { get; set; } = new();
    }

    public class TmdbCountryContentRatingDto
    {
        [JsonPropertyName("iso_3166_1")]
        public string Iso31661 { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public string? Rating { get; set; }
    }
}
