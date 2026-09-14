namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Settings for the podcast directories (Apple Podcasts, Podcast Index). Bound from the
    /// "PodcastDirectory" configuration section; every value has a code default.
    /// </summary>
    public class PodcastDirectoryOptions
    {
        public const string SectionName = "PodcastDirectory";

        /// <summary>
        /// Apple calls that may go out back to back before the steady rate applies. Apple publishes roughly
        /// 20 calls a minute; burst plus refill stays under it in any one minute.
        /// </summary>
        public int AppleBurstLimit { get; set; } = 5;

        /// <summary>Seconds between refills of one Apple call (4 s = 15 calls a minute).</summary>
        public int AppleSecondsPerCall { get; set; } = 4;

        /// <summary>Apple calls allowed to wait for a slot; beyond this a call is refused as busy.</summary>
        public int AppleQueueLimit { get; set; } = 3;

        /// <summary>How long Apple search results and found lookups are cached, in hours.</summary>
        public int AppleCacheHours { get; set; } = 24;

        /// <summary>How long an Apple lookup that found nothing is cached, in minutes.</summary>
        public int AppleMissCacheMinutes { get; set; } = 60;
    }
}
