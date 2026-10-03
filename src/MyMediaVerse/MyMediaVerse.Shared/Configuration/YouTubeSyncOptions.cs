namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Settings for the YouTube channel and playlist sync actions. Bound from the "YouTubeSync"
    /// configuration section; every value has a code default so no settings file entry is required.
    /// </summary>
    public class YouTubeSyncOptions
    {
        public const string SectionName = "YouTubeSync";

        /// <summary>Most uploads one import-latest run brings in: one YouTube page.</summary>
        public const int MaxLatestUploadsCount = 50;

        /// <summary>
        /// How many of a channel's newest uploads "Import latest uploads" brings in when the
        /// request names no count. Capped at one YouTube page (50).
        /// </summary>
        public int LatestUploadsCount { get; set; } = 25;
    }
}
