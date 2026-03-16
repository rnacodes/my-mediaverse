namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// DTO for an RSS feed item.
    /// </summary>
    public class RssFeedItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Link { get; set; }
        public string? Description { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? Author { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Service for fetching and parsing RSS feeds.
    /// </summary>
    public interface IRssFeedService
    {
        /// <summary>
        /// Fetches the latest items from an RSS feed.
        /// </summary>
        /// <param name="rssFeedUrl">The URL of the RSS feed.</param>
        /// <param name="maxItems">Maximum number of items to return (default 3).</param>
        /// <returns>A list of RSS feed items, or empty list if feed cannot be parsed.</returns>
        Task<List<RssFeedItemDto>> GetLatestFeedItemsAsync(string rssFeedUrl, int maxItems = 3);
    }
}
