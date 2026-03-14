using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktWatchedSeasonDto
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("episodes")]
        public List<TraktWatchedEpisodeDto> Episodes { get; set; } = new();
    }
}
