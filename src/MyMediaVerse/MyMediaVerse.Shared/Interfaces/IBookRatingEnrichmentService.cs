namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Derives the MMV <c>Rating</c> enum from the raw <c>GoodreadsRating</c> stored at import time.
    /// This is the "enrich" half of the deferred conversion: Goodreads CSV import stores only the raw
    /// 1-5 rating, and this service converts it to the app rating so the upload path does no conversion.
    /// Goodreads-primary: a book whose raw rating maps to a different value has its Rating overwritten;
    /// books with no real 1-5 Goodreads rating are left untouched.
    /// </summary>
    public interface IBookRatingEnrichmentService
    {
        /// <summary>
        /// Converts the stored Goodreads rating to the MMV Rating enum for every book that has a real
        /// 1-5 <c>GoodreadsRating</c> whose derived value differs from the current Rating.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for stopping the operation.</param>
        /// <returns>Counts of candidates considered and ratings converted.</returns>
        Task<BookRatingConversionResult> ConvertGoodreadsRatingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of books that still need their Goodreads rating converted (have a real 1-5
        /// <c>GoodreadsRating</c> but no MMV <c>Rating</c> yet).
        /// </summary>
        Task<int> GetBooksNeedingRatingConversionCountAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a Goodreads rating conversion run.
    /// </summary>
    public class BookRatingConversionResult
    {
        /// <summary>Books with a real 1-5 Goodreads rating that were considered.</summary>
        public int TotalCandidates { get; set; }

        /// <summary>Books whose MMV Rating was set/updated from the Goodreads rating.</summary>
        public int Converted { get; set; }

        /// <summary>Books whose Rating already matched the derived value (no change).</summary>
        public int Unchanged { get; set; }
    }
}
