namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Summary DTO for articles with Reader document IDs, useful for testing.
    /// </summary>
    public class ReaderArticleSummaryDto
    {
        public Guid ArticleId { get; set; }
        public string? Title { get; set; }
        public string? ReadwiseDocumentId { get; set; }
        public string? Status { get; set; }
        public string? ReaderLocation { get; set; }
        public bool HasFullTextContent { get; set; }
        public int? ContentLength { get; set; }
        public DateTime? LastReaderSync { get; set; }
    }
}
