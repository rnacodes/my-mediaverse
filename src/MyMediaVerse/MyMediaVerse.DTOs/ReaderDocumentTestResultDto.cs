namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for testing Reader API document fetch - returns detailed information about the API response.
    /// </summary>
    public class ReaderDocumentTestResultDto
    {
        public bool Success { get; set; }
        public string? DocumentId { get; set; }
        public string? Title { get; set; }
        /// <summary>
        /// The Readwise Reader URL (e.g., https://read.readwise.io/read/...)
        /// </summary>
        public string? Url { get; set; }
        /// <summary>
        /// The original source URL of the article
        /// </summary>
        public string? SourceUrl { get; set; }
        public string? Author { get; set; }
        public string? SiteName { get; set; }
        public string? Location { get; set; }
        public string? Category { get; set; }
        public int? WordCount { get; set; }
        public double? ReadingProgress { get; set; }

        /// <summary>
        /// Whether html_content field was returned by the API
        /// </summary>
        public bool HasHtmlContent { get; set; }

        /// <summary>
        /// Whether html field was returned by the API
        /// </summary>
        public bool HasHtml { get; set; }

        /// <summary>
        /// Length of the HTML content (if available)
        /// </summary>
        public int? HtmlContentLength { get; set; }

        /// <summary>
        /// First 500 chars of HTML content for debugging
        /// </summary>
        public string? HtmlContentPreview { get; set; }

        /// <summary>
        /// Whether a matching article was found in the database
        /// </summary>
        public bool FoundInDatabase { get; set; }

        /// <summary>
        /// The internal article ID if found in database
        /// </summary>
        public Guid? ArticleId { get; set; }

        /// <summary>
        /// The article's current status if found in database
        /// </summary>
        public string? ArticleStatus { get; set; }

        /// <summary>
        /// Whether the article already has FullTextContent stored
        /// </summary>
        public bool ArticleHasContent { get; set; }

        /// <summary>
        /// Error message if the request failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Raw API response for debugging (keys only, not full content)
        /// </summary>
        public List<string>? AvailableFields { get; set; }
    }
}
