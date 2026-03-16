using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;
using System.Net;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Service for scraping metadata from websites using HtmlAgilityPack.
    /// </summary>
    public class WebsiteScraperService : IWebsiteScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebsiteScraperService> _logger;

        public WebsiteScraperService(HttpClient httpClient, ILogger<WebsiteScraperService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Configure HttpClient with a user agent to avoid being blocked
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<ScrapedWebsiteDataDto> ScrapeWebsiteAsync(string url)
        {
            // Validate URL
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Invalid URL format. Must be a valid HTTP or HTTPS URL.", nameof(url));
            }

            try
            {
                _logger.LogInformation("Scraping website: {Url}", url);

                // Fetch the HTML content
                var html = await _httpClient.GetStringAsync(url);

                // Load HTML into HtmlAgilityPack
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Extract metadata
                var scrapedData = new ScrapedWebsiteDataDto
                {
                    Url = url,
                    Domain = ExtractDomain(uri),
                    Title = ExtractTitle(htmlDoc),
                    Description = ExtractDescription(htmlDoc),
                    ImageUrl = ExtractImageUrl(htmlDoc, uri),
                    RssFeedUrl = ExtractRssFeedUrl(htmlDoc, uri),
                    Author = ExtractAuthor(htmlDoc),
                    Publication = ExtractPublication(htmlDoc)
                };

                _logger.LogInformation("Successfully scraped website: {Url}", url);
                return scrapedData;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch URL: {Url}", url);
                throw new HttpRequestException($"Failed to fetch URL: {url}. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping website: {Url}", url);
                throw;
            }
        }

        private string ExtractDomain(Uri uri)
        {
            return uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host;
        }

        private string? ExtractTitle(HtmlDocument htmlDoc)
        {
            // Priority: og:title > twitter:title > title tag
            var ogTitle = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@property='og:title']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(ogTitle))
                return WebUtility.HtmlDecode(ogTitle);

            var twitterTitle = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@name='twitter:title']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(twitterTitle))
                return WebUtility.HtmlDecode(twitterTitle);

            var titleTag = htmlDoc.DocumentNode
                .SelectSingleNode("//title")
                ?.InnerText;
            if (!string.IsNullOrWhiteSpace(titleTag))
                return WebUtility.HtmlDecode(titleTag);

            return null;
        }

        private string? ExtractDescription(HtmlDocument htmlDoc)
        {
            // Priority: og:description > twitter:description > meta description
            var ogDescription = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@property='og:description']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(ogDescription))
                return WebUtility.HtmlDecode(ogDescription);

            var twitterDescription = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@name='twitter:description']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(twitterDescription))
                return WebUtility.HtmlDecode(twitterDescription);

            var metaDescription = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@name='description']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(metaDescription))
                return WebUtility.HtmlDecode(metaDescription);

            return null;
        }

        private string? ExtractImageUrl(HtmlDocument htmlDoc, Uri baseUri)
        {
            // Priority: og:image > twitter:image > first img tag
            var ogImage = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@property='og:image']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(ogImage))
                return ResolveUrl(ogImage, baseUri);

            var twitterImage = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@name='twitter:image']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(twitterImage))
                return ResolveUrl(twitterImage, baseUri);

            // Optionally, get the first img tag (commented out by default)
            // var firstImg = htmlDoc.DocumentNode
            //     .SelectSingleNode("//img[@src]")
            //     ?.GetAttributeValue("src", null);
            // if (!string.IsNullOrWhiteSpace(firstImg))
            //     return ResolveUrl(firstImg, baseUri);

            return null;
        }

        private string? ExtractRssFeedUrl(HtmlDocument htmlDoc, Uri baseUri)
        {
            // Look for RSS or Atom feed links
            var rssLink = htmlDoc.DocumentNode
                .SelectSingleNode("//link[@type='application/rss+xml' or @type='application/atom+xml']")
                ?.GetAttributeValue("href", null);

            if (!string.IsNullOrWhiteSpace(rssLink))
                return ResolveUrl(rssLink, baseUri);

            return null;
        }

        private string? ExtractAuthor(HtmlDocument htmlDoc)
        {
            // Try various common author meta tags
            var author = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@name='author']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(author))
                return WebUtility.HtmlDecode(author);

            var ogAuthor = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@property='article:author']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(ogAuthor))
                return WebUtility.HtmlDecode(ogAuthor);

            return null;
        }

        private string? ExtractPublication(HtmlDocument htmlDoc)
        {
            // Try og:site_name for publication name
            var siteName = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@property='og:site_name']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(siteName))
                return WebUtility.HtmlDecode(siteName);

            var publisher = htmlDoc.DocumentNode
                .SelectSingleNode("//meta[@property='og:publisher' or @name='publisher']")
                ?.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(publisher))
                return WebUtility.HtmlDecode(publisher);

            return null;
        }

        private string ResolveUrl(string url, Uri baseUri)
        {
            // Handle relative URLs
            if (Uri.TryCreate(baseUri, url, out var resolvedUri))
            {
                return resolvedUri.AbsoluteUri;
            }
            return url;
        }
    }
}

