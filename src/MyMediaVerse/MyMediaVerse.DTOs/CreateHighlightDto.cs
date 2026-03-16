namespace MyMediaVerse.DTOs
{
    public class CreateHighlightDto
    {
        public required string Text { get; set; }
        public string? Note { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Category { get; set; }
        public string? SourceUrl { get; set; }
        public Guid? ArticleId { get; set; }
        public Guid? BookId { get; set; }
        public List<string>? Tags { get; set; }
        public int? Location { get; set; }
        public string? LocationType { get; set; }
        public DateTime? HighlightedAt { get; set; }
    }
}

