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

        /// <summary>Page size for link checks when a run does not specify one.</summary>
        public int DefaultLinkCheckLimit { get; set; } = 100;

        /// <summary>A link is re-checked when its last check is older than this many days.</summary>
        public int DefaultLinkCheckOlderThanDays { get; set; } = 30;

        /// <summary>
        /// Pause between Wayback Machine lookups. The CDX endpoint is a shared public service;
        /// spacing requests out keeps bulk runs polite.
        /// </summary>
        public int WaybackDelayMs { get; set; } = 250;
    }
}
