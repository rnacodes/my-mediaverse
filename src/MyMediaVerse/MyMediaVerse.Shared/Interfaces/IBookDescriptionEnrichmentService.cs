namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service for enriching books from the Google Books API. A book is a candidate when it has
    /// no description and carries a lookup key: an ISBN, or a title plus a real author. The
    /// lookup fills gaps only (description, ISBN, Google volume id, publisher, year, thumbnail)
    /// so a book created from a highlight stub acquires its external identity over time.
    /// Designed for background processing with batch support and rate limiting.
    /// </summary>
    public interface IBookDescriptionEnrichmentService
    {
        /// <summary>
        /// Enriches books that are missing descriptions by looking them up in Google Books.
        /// Processes books in batches with delays between API calls to respect rate limits.
        /// </summary>
        /// <param name="batchSize">Number of books to process in this run (default: 50)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 1000)</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing counts of processed, enriched, not-found, and failed books</returns>
        Task<BookDescriptionEnrichmentResult> EnrichBooksWithoutDescriptionsAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 1000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of books that need description enrichment: no description, and either an
        /// ISBN or a title plus a real author to look up.
        /// </summary>
        Task<int> GetBooksNeedingEnrichmentCountAsync();

        /// <summary>
        /// Enriches a single book by its media ID, looking it up in Google Books by ISBN first and
        /// by title plus author when no ISBN is stored.
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
        /// Whether a description was found and applied (or was already present).
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
        /// Names of the fields this call filled in (e.g. "description", "isbn", "googleVolumeId").
        /// Empty when nothing changed.
        /// </summary>
        public List<string> FilledFields { get; set; } = new List<string>();

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
        /// Whether the book has neither an ISBN nor a title plus real author to look up.
        /// </summary>
        public bool NoLookupKey { get; set; }
    }

    /// <summary>
    /// Result of a book description enrichment run. Follows the sync/import reporting contract:
    /// <see cref="Success"/> flips only when the run itself aborted; per-book misses are counts.
    /// </summary>
    public class BookDescriptionEnrichmentResult
    {
        /// <summary>
        /// False only when the run aborted before completing (fatal). Per-book failures leave it true.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Stable identifier of the operation for notifications and sync-state records.
        /// </summary>
        public string Operation { get; set; } = "book-description-enrichment";

        /// <summary>
        /// Total number of books processed in this run.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of books that received a description.
        /// </summary>
        public int EnrichedCount { get; set; }

        /// <summary>
        /// Number of books Google Books had no usable match for. Not an error; the book stays a candidate.
        /// </summary>
        public int NotFoundCount { get; set; }

        /// <summary>
        /// Number of books whose lookup threw (network, API error).
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Number of books skipped because they had no lookup key by the time they were processed.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// List of error messages for failed enrichments.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Whether the operation was canceled before completion.
        /// </summary>
        public bool WasCancelled { get; set; }

        /// <summary>
        /// The fatal reason. Non-null only when <see cref="Success"/> is false.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Non-fatal problem summary, e.g. some lookups threw while the run completed.
        /// </summary>
        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
