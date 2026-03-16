namespace MyMediaVerse.Shared.DTOs.WebsiteScraper
{
    /// <summary>
    /// DTO containing metadata scraped from a website URL.
    /// Used to transfer scraped data from the scraper service to the application layer.
    /// </summary>
    public class ScrapedWebsiteDataDto
    {
        /// <summary>
        /// The original URL that was scraped.
        /// </summary>
        public required string Url { get; set; }
        
        /// <summary>
        /// The title extracted from the website (og:title or title tag).
        /// </summary>
        public string? Title { get; set; }
        
        /// <summary>
        /// The description extracted from the website (og:description or meta description).
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// The image URL extracted from the website (og:image or twitter:image).
        /// </summary>
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// The RSS feed URL discovered on the website, if any.
        /// </summary>
        public string? RssFeedUrl { get; set; }
        
        /// <summary>
        /// The domain name extracted from the URL.
        /// </summary>
        public string? Domain { get; set; }
        
        /// <summary>
        /// The author of the content, if available.
        /// </summary>
        public string? Author { get; set; }
        
        /// <summary>
        /// The publication/site name, if available.
        /// </summary>
        public string? Publication { get; set; }
    }
}


