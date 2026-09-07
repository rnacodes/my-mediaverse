namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fills in websites that were saved as stubs (bulk imports, CSV rows, manual saves with just
    /// a URL) after the fact: page metadata, a screenshot when the page offers no image, a Wayback
    /// Machine snapshot, and the link's health. Every write is fill-only, so values the user has
    /// already set are never overwritten. Runs are paged and triggered on demand; scheduling lives
    /// outside the API.
    /// </summary>
    public interface IWebsiteEnrichmentService
    {
        /// <summary>Websites that have never been enriched.</summary>
        Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enriches up to <paramref name="limit"/> pending websites, oldest first. Each website is
        /// saved as soon as it is done, so an interrupted run keeps its progress.
        /// </summary>
        Task<WebsiteEnrichmentResult> EnrichPendingAsync(int limit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enriches one website. An already-enriched website is left alone unless
        /// <paramref name="force"/> is true, which re-runs the fill (still never overwriting).
        /// </summary>
        Task<SingleWebsiteEnrichmentResult> EnrichByIdAsync(Guid id, bool force, CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-checks up to <paramref name="limit"/> links that have never been checked or whose last
        /// check is older than <paramref name="olderThanDays"/>, recording the status on each row.
        /// </summary>
        Task<WebsiteLinkCheckResult> CheckLinksAsync(int limit, int olderThanDays, CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-renders thumbnails that point at the screenshot provider directly or at an animated
        /// placeholder, replacing them with a stored still image or clearing them.
        /// </summary>
        Task<WebsiteEnrichmentResult> RepairThumbnailsAsync(int limit, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a paged enrichment or thumbnail-repair run. Follows the sync/import reporting
    /// contract: <see cref="Success"/> flips only when the run itself aborted; per-website
    /// problems are counts.
    /// </summary>
    public class WebsiteEnrichmentResult
    {
        public const string EnrichmentOperation = "website-enrichment";
        public const string ThumbnailRepairOperation = "website-thumbnail-repair";

        /// <summary>False only when the run aborted before completing (fatal).</summary>
        public bool Success { get; set; } = true;

        /// <summary>Stable identifier of the operation for notifications and sync-state records.</summary>
        public string Operation { get; set; } = EnrichmentOperation;

        /// <summary>Websites this run looked at.</summary>
        public int TotalProcessed { get; set; }

        /// <summary>Websites that received at least one new value (or a re-rendered thumbnail).</summary>
        public int EnrichedCount { get; set; }

        /// <summary>
        /// Websites that were processed but changed nothing. In an enrichment run the page was
        /// reachable and every field was already filled; the row is still marked enriched.
        /// </summary>
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Enrichment: websites whose page could not be fetched; the HTTP status is recorded and the
        /// row is marked enriched so it is not retried forever. Repair: thumbnails cleared because
        /// nothing usable could be rendered.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>Websites whose processing threw; they stay pending and are retried next run.</summary>
        public int FailedCount { get; set; }

        /// <summary>Screenshots rendered and stored during this run.</summary>
        public int ScreenshotsRendered { get; set; }

        /// <summary>
        /// True when the monthly screenshot budget ran out during the run. Metadata enrichment
        /// continued; only screenshots were skipped.
        /// </summary>
        public bool QuotaReached { get; set; }

        /// <summary>Websites still pending after this run (enrichment only).</summary>
        public int PendingCount { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public bool WasCancelled { get; set; }

        /// <summary>The fatal reason. Non-null only when <see cref="Success"/> is false.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Non-fatal problem summary (quota reached, some websites failed).</summary>
        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }

    /// <summary>
    /// Result of enriching one website.
    /// </summary>
    public class SingleWebsiteEnrichmentResult
    {
        /// <summary>False only when the call itself failed (see <see cref="ErrorMessage"/>).</summary>
        public bool Success { get; set; } = true;

        public string Operation { get; set; } = WebsiteEnrichmentResult.EnrichmentOperation;

        /// <summary>No website has this id.</summary>
        public bool NotFound { get; set; }

        /// <summary>The website was already enriched and force was not requested; nothing ran.</summary>
        public bool AlreadyEnriched { get; set; }

        /// <summary>The page could not be fetched; <see cref="LastHttpStatus"/> carries the status.</summary>
        public bool Unreachable { get; set; }

        public string? Title { get; set; }

        /// <summary>Names of the fields this call filled in. Empty when nothing changed.</summary>
        public List<string> FilledFields { get; set; } = new List<string>();

        public bool ScreenshotRendered { get; set; }

        /// <summary>True when a screenshot was wanted but the monthly budget is exhausted.</summary>
        public bool QuotaReached { get; set; }

        public int? LastHttpStatus { get; set; }

        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a link-check run.
    /// </summary>
    public class WebsiteLinkCheckResult
    {
        public const string LinkCheckOperation = "website-link-check";

        public bool Success { get; set; } = true;
        public string Operation { get; set; } = LinkCheckOperation;

        /// <summary>Links checked in this run.</summary>
        public int TotalProcessed { get; set; }

        /// <summary>Links that answered with an error status (or did not answer) in this run.</summary>
        public int BrokenCount { get; set; }

        /// <summary>Links that were broken at the previous check and answer normally now.</summary>
        public int RecoveredCount { get; set; }

        /// <summary>Links whose recorded status changed in this run.</summary>
        public int ChangedCount { get; set; }

        /// <summary>Links that answered normally at the previous check (or had never been checked) and are broken now.</summary>
        public List<WebsiteLinkStatus> NewlyBroken { get; set; } = new List<WebsiteLinkStatus>();

        /// <summary>Links whose check threw; their recorded status is unchanged.</summary>
        public int FailedCount { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public bool WasCancelled { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }

    /// <summary>One link whose status changed for the worse in a link-check run.</summary>
    public class WebsiteLinkStatus
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Link { get; set; }

        /// <summary>The status recorded by this check (0 = unreachable).</summary>
        public int Status { get; set; }

        /// <summary>The status recorded by the previous check, if any.</summary>
        public int? PreviousStatus { get; set; }

        /// <summary>The website's archived copy, when one is known, so a dead link still has somewhere to go.</summary>
        public string? WaybackUrl { get; set; }
    }
}
