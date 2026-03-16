namespace MyMediaVerse.Shared.DTOs.Readwise
{
    /// <summary>
    /// DTO representing a book/source from Readwise API
    /// </summary>
    public class ReadwiseBookDto
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? author { get; set; }
        public string? category { get; set; }
        public int num_highlights { get; set; }
        public string? last_highlight_at { get; set; }
        public string? updated { get; set; }
        public string? cover_image_url { get; set; }
        public string? highlights_url { get; set; }
        public string? source_url { get; set; }
        public string? asin { get; set; }
        public string[]? tags { get; set; }
        public string? source { get; set; }
        public string? document_note { get; set; }
    }
}

