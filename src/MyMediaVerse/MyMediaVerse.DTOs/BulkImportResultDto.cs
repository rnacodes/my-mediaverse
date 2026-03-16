using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class BulkImportResultDto
    {
        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }
        
        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }
        
        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }
        
        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }
        
        [JsonPropertyName("imported")]
        public List<object> Imported { get; set; } = new List<object>();
        
        [JsonPropertyName("skipped")]
        public List<string> Skipped { get; set; } = new List<string>();
        
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();
    }
}


