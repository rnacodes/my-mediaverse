namespace MyMediaVerse.Shared.DTOs.Readwise
{
    /// <summary>
    /// DTO representing a single highlight from Readwise API
    /// Based on: https://readwise.io/api_deets
    /// </summary>
    public class ReadwiseHighlightDto
    {
        public int id { get; set; }
        public string text { get; set; } = string.Empty;
        public string? note { get; set; }
        public int? location { get; set; }
        public string? location_type { get; set; }
        public string? highlighted_at { get; set; }
        public string? url { get; set; }
        public string? color { get; set; }
        public string? updated { get; set; }
        public int book_id { get; set; }
        public string[]? tags { get; set; }
        public bool is_favorite { get; set; }
        public bool is_discard { get; set; }
    }
}

