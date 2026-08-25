namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Sets a highlight's media link. At most one of ArticleId/BookId may be
    /// provided; both null unlinks the highlight.
    /// </summary>
    public class HighlightLinkDto
    {
        public Guid? ArticleId { get; set; }
        public Guid? BookId { get; set; }
    }
}
