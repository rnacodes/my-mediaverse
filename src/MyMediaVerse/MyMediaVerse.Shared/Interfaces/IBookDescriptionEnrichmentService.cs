namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service for enriching book descriptions from external APIs (Open Library).
    /// Designed for background processing with batch support and rate limiting.
    /// </summary>
    public interface IBookDescriptionEnrichmentService
    {
        /// <summary>
        /// Enriches books that are missing descriptions by fetching from Open Library API.
        /// Processes books in batches with delays between API calls to respect rate limits.
        /// </summary>
        /// <param name="batchSize">Number of books to process in this run (default: 50)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 1000)</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing counts of processed, enriched, and failed books</returns>
        Task<BookDescriptionEnrichmentResult> EnrichBooksWithoutDescriptionsAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 1000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of books that need description enrichment (have ISBN but no description).
        /// </summary>
        Task<int> GetBooksNeedingEnrichmentCountAsync();

        /// <summary>
        /// Enriches a single book by its media ID.
        /// Fetches description from Open Library API using the book's ISBN.
        /// </summary>
        /// <param name="bookId">The media ID of the book to enrich</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing the enrichment outcome for the single book</returns>
        Task<SingleBookEnrichmentResult> EnrichBookByIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of enriching a single book.
    /// </summary>
    public class SingleBookEnrichmentResult
    {
        /// <summary>
        /// Whether the enrichment was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The book title that was processed.
        /// </summary>
        public string? BookTitle { get; set; }

        /// <summary>
        /// The description that was found and applied, if any.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Error message if enrichment failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Whether the book was not found.
        /// </summary>
        public bool NotFound { get; set; }

        /// <summary>
        /// Whether the book already has a description.
        /// </summary>
        public bool AlreadyHasDescription { get; set; }

        /// <summary>
        /// Whether the book has no ISBN to look up.
        /// </summary>
        public bool NoIsbn { get; set; }
    }

    /// <summary>
    /// Result of a book description enrichment run.
    /// </summary>
    public class BookDescriptionEnrichmentResult
    {
        /// <summary>
        /// Total number of books processed in this run.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of books successfully enriched with descriptions.
        /// </summary>
        public int EnrichedCount { get; set; }

        /// <summary>
        /// Number of books where enrichment failed (API error, no description found, etc.).
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Number of books skipped (no ISBN available).
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// List of error messages for failed enrichments.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Whether the operation was cancelled before completion.
        /// </summary>
        public bool WasCancelled { get; set; }
    }
}
