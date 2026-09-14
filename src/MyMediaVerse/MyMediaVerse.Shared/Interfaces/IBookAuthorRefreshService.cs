namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Restores full author lists on books that were imported when only the first credited
    /// author was kept. Each book with a Google Books volume id (or, failing that, an Open
    /// Library work key) is looked up again, and its Author is replaced with the source's full
    /// list only when the stored value is exactly that source's first author (or the "Unknown
    /// Author" placeholder). Authors edited by hand or supplied by another source are left alone.
    /// </summary>
    public interface IBookAuthorRefreshService
    {
        /// <param name="delayBetweenCallsMs">Pause between books, to stay well inside API rate limits.</param>
        /// <param name="maxBooks">Upper bound on books checked in this run.</param>
        /// <param name="cancellationToken">Stops the run; changes made so far are kept.</param>
        Task<BookAuthorRefreshResult> RefreshAuthorsAsync(
            int delayBetweenCallsMs = 500,
            int maxBooks = 5000,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of an author refresh run. Follows the sync/import reporting contract:
    /// <see cref="Success"/> flips only when the run itself aborted; per-book misses are counts.
    /// </summary>
    public class BookAuthorRefreshResult
    {
        public bool Success { get; set; } = true;

        public string Operation { get; set; } = "book-author-refresh";

        /// <summary>Books with a Google Books volume id or Open Library key that were looked up.</summary>
        public int TotalChecked { get; set; }

        /// <summary>Books whose Author was replaced with the source's full author list.</summary>
        public int UpdatedCount { get; set; }

        /// <summary>Books whose Author already matched the source's full list.</summary>
        public int UnchangedCount { get; set; }

        /// <summary>Books whose stored Author differs from the source's first author, so it was kept.</summary>
        public int SkippedCount { get; set; }

        /// <summary>Books the source had no record or no authors for.</summary>
        public int NotFoundCount { get; set; }

        /// <summary>Books whose lookup threw (network, API error) or returned an incomplete author list.</summary>
        public int FailedCount { get; set; }

        /// <summary>Up to 20 per-book error messages.</summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>Titles of up to 50 updated books, as "Title: old → new", for a quick review.</summary>
        public List<string> Changes { get; set; } = new List<string>();

        public bool WasCancelled { get; set; }

        public string? ErrorMessage { get; set; }

        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
