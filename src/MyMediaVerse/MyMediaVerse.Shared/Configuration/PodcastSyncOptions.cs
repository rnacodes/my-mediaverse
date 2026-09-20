namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Settings for podcast episode sync and the feed-episodes browser. Bound from the "PodcastSync"
    /// configuration section; every value has a code default so no settings file entry is required.
    /// </summary>
    public class PodcastSyncOptions
    {
        public const string SectionName = "PodcastSync";

        /// <summary>
        /// How many of the newest episodes a series' first sync imports. The rest of the back
        /// catalog stays in the feed for the browser.
        /// </summary>
        public int FirstSyncEpisodeCount { get; set; } = 25;

        /// <summary>
        /// Most episodes a later sync creates. It only comes into play when a show has published
        /// more than this since its last sync; the result then carries a warning.
        /// </summary>
        public int MaxEpisodesPerSync { get; set; } = 50;

        /// <summary>Pause between two feed reads on the same host during sync-all, in milliseconds.</summary>
        public int HostDelayMs { get; set; } = 250;

        /// <summary>
        /// Wall-clock budget for one sync-all run, in seconds. Series not reached stay due and are
        /// first in line next run. Zero disables the budget.
        /// </summary>
        public int RunTimeBudgetSeconds { get; set; } = 600;

        /// <summary>How long a parsed feed is kept for the feed-episodes browser, in minutes.</summary>
        public int FeedCacheMinutes { get; set; } = 15;
    }
}
