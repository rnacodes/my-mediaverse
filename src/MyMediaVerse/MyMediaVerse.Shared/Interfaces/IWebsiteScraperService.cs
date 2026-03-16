using MyMediaVerse.Shared.DTOs.WebsiteScraper;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service interface for scraping website metadata.
    /// Implemented in Infrastructure layer using HtmlAgilityPack.
    /// </summary>
    public interface IWebsiteScraperService
    {
        /// <summary>
        /// Scrapes metadata from a website URL.
        /// </summary>
        /// <param name="url">The URL to scrape</param>
        /// <returns>Scraped website data including title, description, image, and RSS feed</returns>
        /// <exception cref="HttpRequestException">Thrown when the URL cannot be accessed</exception>
        /// <exception cref="ArgumentException">Thrown when the URL is invalid</exception>
        Task<ScrapedWebsiteDataDto> ScrapeWebsiteAsync(string url);
    }
}

