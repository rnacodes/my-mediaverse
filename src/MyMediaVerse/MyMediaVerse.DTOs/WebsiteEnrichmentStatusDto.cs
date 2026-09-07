namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Snapshot of the website enrichment backlog and the screenshot budget left this month.
    /// </summary>
    public class WebsiteEnrichmentStatusDto
    {
        /// <summary>Websites that have never been enriched.</summary>
        public int PendingCount { get; set; }

        /// <summary>Screenshot renders left in the current calendar month.</summary>
        public int ScreenshotQuotaRemaining { get; set; }
    }
}
