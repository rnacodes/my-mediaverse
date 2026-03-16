namespace MyMediaVerse.Shared.DTOs.Readwise
{
    /// <summary>
    /// DTO for creating/updating highlights in Readwise API
    /// </summary>
    public class CreateReadwiseHighlightDto
    {
        public required string text { get; set; }
        public string? title { get; set; }
        public string? author { get; set; }
        public string? image_url { get; set; }
        public string? source_url { get; set; }
        public string? source_type { get; set; }
        public string? category { get; set; }
        public string? note { get; set; }
        public int? location { get; set; }
        public string? location_type { get; set; }
        public string? highlighted_at { get; set; }
        public string? highlight_url { get; set; }
    }
}

