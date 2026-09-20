namespace MyMediaVerse.Domain.Constants
{
    /// <summary>
    /// Values of the search index's podcast_type field. Podcast series and their episodes are both
    /// media items of type Podcast, so search needs this to tell them apart — it backs the
    /// Series/Episodes toggle.
    /// </summary>
    public static class PodcastDocumentTypes
    {
        public const string Series = "Series";
        public const string Episode = "Episode";
    }
}
