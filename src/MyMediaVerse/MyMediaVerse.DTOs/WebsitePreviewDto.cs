using MyMediaVerse.Shared.DTOs.WebsiteScraper;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// What the import page sees before it saves a URL: the scraped metadata plus, when the
    /// URL is already in the library, the id and title of that existing website so the user
    /// can be warned before submitting.
    /// </summary>
    public class WebsitePreviewDto
    {
        public required string Url { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? RssFeedUrl { get; set; }
        public string? Domain { get; set; }
        public string? Author { get; set; }
        public string? Publication { get; set; }

        /// <summary>Id of the website already saved for this URL, if any.</summary>
        public Guid? ExistingWebsiteId { get; set; }

        /// <summary>Title of the website already saved for this URL, if any.</summary>
        public string? ExistingTitle { get; set; }

        public static WebsitePreviewDto FromScraped(ScrapedWebsiteDataDto scraped, Guid? existingId, string? existingTitle)
        {
            return new WebsitePreviewDto
            {
                Url = scraped.Url,
                Title = scraped.Title,
                Description = scraped.Description,
                ImageUrl = scraped.ImageUrl,
                RssFeedUrl = scraped.RssFeedUrl,
                Domain = scraped.Domain,
                Author = scraped.Author,
                Publication = scraped.Publication,
                ExistingWebsiteId = existingId,
                ExistingTitle = existingTitle
            };
        }
    }
}
