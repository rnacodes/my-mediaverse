namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Settings for the website enrichment runs (metadata fill, link checks, thumbnail repair).
    /// Bound from the "WebsiteEnrichment" configuration section; every value has a code default
    /// so no settings file entry is required.
    /// </summary>
    public class WebsiteEnrichmentOptions
    {
        public const string SectionName = "WebsiteEnrichment";

        /// <summary>Largest page a single run may process; larger requests are rejected.</summary>
        public int MaxLimit { get; set; } = 200;

        /// <summary>Page size when a run does not specify one.</summary>
        public int DefaultLimit { get; set; } = 50;

        /// <summary>
        /// Wall-clock budget for one enrichment run, in seconds. A real bookmark export holds dead
        /// hosts that each cost a full connect timeout, so a page is bounded by time as well as by
        /// count: once the budget is spent no further website is started and the rest stay pending
        /// for the next call. Zero disables the budget.
        /// </summary>
        public int RunTimeBudgetSeconds { get; set; } = 20;

        /// <summary>Page size for link checks when a run does not specify one.</summary>
        public int DefaultLinkCheckLimit { get; set; } = 100;

        /// <summary>A link is re-checked when its last check is older than this many days.</summary>
        public int DefaultLinkCheckOlderThanDays { get; set; } = 30;

        /// <summary>
        /// Pause between Wayback Machine lookups. The archive is a shared public service; spacing
        /// requests out keeps bulk runs polite.
        /// </summary>
        public int WaybackDelayMs { get; set; } = 250;

        /// <summary>
        /// Timeout for one Wayback lookup, in seconds. Shared by the HTTP client registration and the
        /// enrichment run, which treats a lookup that used most of this as a slow response.
        /// </summary>
        public int WaybackTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// After this many consecutive slow Wayback responses in one run, the run stops asking the
        /// archive for its remaining websites and says so in its warning; a fast answer resets the
        /// count. The archive throttles sustained callers, and each slow answer costs the full
        /// timeout for nothing. Zero disables the pause.
        /// </summary>
        public int WaybackPauseAfterSlowLookups { get; set; } = 3;
    }
}
