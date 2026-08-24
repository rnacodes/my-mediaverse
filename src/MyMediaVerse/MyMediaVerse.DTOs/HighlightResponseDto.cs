namespace MyMediaVerse.DTOs
{
    public class HighlightResponseDto
    {
        public Guid id { get; set; }
        public string text { get; set; } = string.Empty;
        public string? note { get; set; }
        public string? title { get; set; }
        public string? author { get; set; }
        public string? category { get; set; }
        public string? sourceUrl { get; set; }
        // Deep link to this highlight in Readwise (populated by the Readwise sync).
        public string? highlightUrl { get; set; }
        public string? imageUrl { get; set; }
        public Guid? articleId { get; set; }
        public string? articleTitle { get; set; }
        public Guid? bookId { get; set; }
        public string? bookTitle { get; set; }
        public List<string>? tags { get; set; }
        public int? location { get; set; }
        public string? locationType { get; set; }
        public DateTime? highlightedAt { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
        public string? color { get; set; }
        public bool isFavorite { get; set; }
    }
}

