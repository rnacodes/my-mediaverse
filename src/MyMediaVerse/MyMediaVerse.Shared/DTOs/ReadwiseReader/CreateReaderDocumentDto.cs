namespace MyMediaVerse.Shared.DTOs.ReadwiseReader
{
    /// <summary>
    /// DTO for creating a document in Readwise Reader
    /// </summary>
    public class CreateReaderDocumentDto
    {
        public required string url { get; set; }
        public string? html { get; set; }
        public bool should_clean_html { get; set; } = true;
        public string? title { get; set; }
        public string? author { get; set; }
        public string? summary { get; set; }
        public string? published_date { get; set; }
        public string? image_url { get; set; }
        public string location { get; set; } = "new";
        public string? category { get; set; }
        public string[]? tags { get; set; }
    }
}

