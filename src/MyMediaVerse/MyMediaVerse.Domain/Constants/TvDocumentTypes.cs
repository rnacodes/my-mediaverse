namespace MyMediaVerse.Domain.Constants
{
    /// <summary>
    /// Values of the search index's tv_type field. A TV show and its episodes are both media items
    /// of type TVShow, so search needs this to tell them apart — it lets the default results show
    /// shows only and an episode list filter to one show.
    /// </summary>
    public static class TvDocumentTypes
    {
        public const string Show = "Show";
        public const string Episode = "Episode";
    }
}
