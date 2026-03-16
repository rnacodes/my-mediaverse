namespace MyMediaVerse.Shared.DTOs.Readwise
{
    /// <summary>
    /// Response from Readwise highlights endpoint with pagination
    /// </summary>
    public class ReadwiseHighlightsResponse
    {
        public int count { get; set; }
        public string? next { get; set; }
        public string? previous { get; set; }
        public List<ReadwiseHighlightDto> results { get; set; } = new();
    }
}

