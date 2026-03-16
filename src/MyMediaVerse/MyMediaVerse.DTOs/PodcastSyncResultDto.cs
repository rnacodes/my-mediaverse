using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class PodcastSyncResultDto
    {
        [JsonPropertyName("seriesTitle")]
        public string SeriesTitle { get; set; } = string.Empty;
        
        [JsonPropertyName("newEpisodesCount")]
        public int NewEpisodesCount { get; set; }
        
        [JsonPropertyName("totalEpisodesCount")]
        public int TotalEpisodesCount { get; set; }
        
        [JsonPropertyName("lastSyncDate")]
        public DateTime LastSyncDate { get; set; }
    }
}

