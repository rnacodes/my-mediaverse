namespace MyMediaVerse.Domain.Constants
{
    /// <summary>
    /// Values for <see cref="Entities.PodcastSeries.MetadataSource"/>: the source that last wrote a
    /// series' descriptive fields.
    /// </summary>
    public static class PodcastMetadataSources
    {
        /// <summary>The show's own RSS feed.</summary>
        public const string Rss = "rss";

        /// <summary>The Apple Podcasts directory (iTunes Search/Lookup).</summary>
        public const string Apple = "apple";

        /// <summary>The Podcast Index directory.</summary>
        public const string PodcastIndex = "podcastindex";

        /// <summary>A purchased ListenNotes dataset export.</summary>
        public const string ListenNotesDataset = "listennotes-dataset";

        /// <summary>Entered by hand, or a stub no source has filled yet.</summary>
        public const string Manual = "manual";
    }
}
