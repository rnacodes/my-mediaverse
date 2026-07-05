using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result DTO for a podcast OPML import operation. Imports land as lightweight stubs
    /// (title + feed url + Apple id) with no external enrichment, so the summary only reports
    /// how many feeds were created, skipped as duplicates, or failed.
    /// </summary>
    public class OpmlImportResultDto
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("imported")]
        public int Imported { get; set; }

        [JsonPropertyName("skipped")]
        public int Skipped { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("failures")]
        public List<OpmlImportFailureDto> Failures { get; set; } = new();
    }

    /// <summary>
    /// Details of a single OPML feed that could not be imported.
    /// </summary>
    public class OpmlImportFailureDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
