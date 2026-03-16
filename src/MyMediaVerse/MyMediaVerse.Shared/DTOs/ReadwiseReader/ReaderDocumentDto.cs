namespace MyMediaVerse.Shared.DTOs.ReadwiseReader
{
    /// <summary>
    /// DTO for Readwise Reader document
    /// Based on: https://readwise.io/reader_api
    /// </summary>
    public class ReaderDocumentDto
    {
        public string id { get; set; } = string.Empty;
        /// <summary>
        /// The Readwise Reader URL (e.g., https://read.readwise.io/read/...)
        /// </summary>
        public string? url { get; set; }
        /// <summary>
        /// The original source URL of the article (e.g., https://example.com/article)
        /// </summary>
        public string? source_url { get; set; }
        public string? title { get; set; }
        public string? author { get; set; }
        public string? source { get; set; }
        public string? category { get; set; }
        public string location { get; set; } = "new";
        public Dictionary<string, object>? tags { get; set; }
        public string? site_name { get; set; }
        public int? word_count { get; set; }
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
        public string? published_date { get; set; }
        public string? summary { get; set; }
        public string? image_url { get; set; }
        public string? content { get; set; }
        public string? html { get; set; }
        public string? html_content { get; set; }
        public double? reading_progress { get; set; }
        public bool? favorite { get; set; }
        public string? parent_id { get; set; }
        public string? notes { get; set; }
    }
}

