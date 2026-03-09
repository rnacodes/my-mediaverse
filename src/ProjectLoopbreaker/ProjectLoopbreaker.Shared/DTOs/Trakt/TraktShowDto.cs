using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.Shared.DTOs.Trakt
{
    public class TraktShowDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("ids")]
        public TraktIdsDto Ids { get; set; } = new();
    }
}
