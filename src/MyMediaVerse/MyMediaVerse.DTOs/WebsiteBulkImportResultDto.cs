namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result of a bookmark-file or URL-list import. Follows the sync/import reporting contract:
    /// <see cref="Success"/> flips only when the run itself aborted; per-entry problems are counts.
    /// Created websites are stubs; the enrichment run fills them afterwards.
    /// </summary>
    public class WebsiteBulkImportResultDto
    {
        public const string BookmarkImportOperation = "bookmark-import";
        public const string UrlListImportOperation = "url-list-import";

        public bool Success { get; set; } = true;
        public string Operation { get; set; } = BookmarkImportOperation;

        /// <summary>Web links the import looked at.</summary>
        public int TotalProcessed { get; set; }

        /// <summary>Websites created (as stubs awaiting enrichment).</summary>
        public int CreatedCount { get; set; }

        /// <summary>Existing websites that gained topics or genres from the import.</summary>
        public int UpdatedCount { get; set; }

        /// <summary>Entries already in the library with nothing new to add, plus repeats within the file.</summary>
        public int SkippedCount { get; set; }

        /// <summary>Entries with an unusable URL or that threw while being processed.</summary>
        public int FailedCount { get; set; }

        /// <summary>Entries that were not web links and were ignored (bookmarklets, browser pages, local files).</summary>
        public int NonWebLinkCount { get; set; }

        public List<string> Errors { get; set; } = new();

        /// <summary>Distinct folder paths seen in the file.</summary>
        public int FoldersFound { get; set; }

        /// <summary>Topics that did not exist before this import.</summary>
        public int TopicsCreatedCount { get; set; }

        /// <summary>Websites awaiting enrichment after this import (library-wide).</summary>
        public int PendingEnrichmentCount { get; set; }

        public bool ReindexTriggered { get; set; }
        public bool WasCancelled { get; set; }

        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }
}
