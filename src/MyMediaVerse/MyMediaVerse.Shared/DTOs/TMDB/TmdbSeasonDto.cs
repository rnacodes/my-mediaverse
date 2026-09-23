using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.TMDB
{
    /// <summary>
    /// One season of a TV show as returned by <c>tv/{id}/season/{n}</c>, episodes included.
    /// </summary>
    public class TmdbSeasonDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }

        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("episodes")]
        public List<TmdbTvEpisodeDto> Episodes { get; set; } = new();
    }

    /// <summary>
    /// A season entry in a TV show's details payload. Season 0 holds specials, and numbering can
    /// have gaps, so this list — not <c>number_of_seasons</c> — is what an episode import walks.
    /// </summary>
    public class TmdbSeasonSummaryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }

        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("episode_count")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
    }
}
