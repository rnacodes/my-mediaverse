namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Partial update for a highlight. A null field is left unchanged; an empty
    /// string clears an optional text field, and an empty tag list clears tags.
    /// Article/book links are managed separately via the link endpoint.
    /// </summary>
    public class UpdateHighlightDto
    {
        public string? Text { get; set; }
        public string? Note { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Category { get; set; }
        public string? SourceUrl { get; set; }
        public List<string>? Tags { get; set; }
        public int? Location { get; set; }
        public string? LocationType { get; set; }
        public DateTime? HighlightedAt { get; set; }
        public bool? IsFavorite { get; set; }
        public string? Color { get; set; }
    }
}
