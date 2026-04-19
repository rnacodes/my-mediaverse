using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Web
{
    /// <summary>
    /// Service for fetching and parsing RSS/Atom feeds.
    /// </summary>
    public class RssFeedService : IRssFeedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RssFeedService> _logger;

        public RssFeedService(HttpClient httpClient, ILogger<RssFeedService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        /// <inheritdoc />
        public async Task<List<RssFeedItemDto>> GetLatestFeedItemsAsync(string rssFeedUrl, int maxItems = 3)
        {
            var items = new List<RssFeedItemDto>();

            if (string.IsNullOrEmpty(rssFeedUrl))
            {
                return items;
            }

            try
            {
                _logger.LogInformation("Fetching RSS feed from: {Url}", rssFeedUrl);

                using var response = await _httpClient.GetAsync(rssFeedUrl);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = XmlReader.Create(stream);

                var feed = SyndicationFeed.Load(reader);

                if (feed?.Items == null)
                {
                    _logger.LogWarning("RSS feed at {Url} has no items", rssFeedUrl);
                    return items;
                }

                foreach (var item in feed.Items.Take(maxItems))
                {
                    var feedItem = new RssFeedItemDto
                    {
                        Title = item.Title?.Text ?? "Untitled",
                        Link = item.Links?.FirstOrDefault()?.Uri?.ToString(),
                        Description = GetDescription(item),
                        PublishedDate = item.PublishDate != DateTimeOffset.MinValue
                            ? item.PublishDate.UtcDateTime
                            : item.LastUpdatedTime != DateTimeOffset.MinValue
                                ? item.LastUpdatedTime.UtcDateTime
                                : null,
                        Author = GetAuthors(item),
                        ImageUrl = GetImageUrl(item)
                    };

                    items.Add(feedItem);
                }

                _logger.LogInformation("Successfully fetched {Count} items from RSS feed: {Url}", items.Count, rssFeedUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch RSS feed from: {Url}", rssFeedUrl);
            }
            catch (XmlException ex)
            {
                _logger.LogWarning(ex, "Failed to parse RSS feed from: {Url}", rssFeedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching RSS feed from: {Url}", rssFeedUrl);
            }

            return items;
        }

        private static string? GetDescription(SyndicationItem item)
        {
            if (item.Summary != null)
            {
                var text = item.Summary.Text;
                // Strip HTML tags for display
                return StripHtml(text);
            }

            // Try to get content
            var content = item.Content as TextSyndicationContent;
            if (content != null)
            {
                return StripHtml(content.Text);
            }

            return null;
        }

        private static string? GetAuthors(SyndicationItem item)
        {
            if (item.Authors?.Count > 0)
            {
                return string.Join(", ", item.Authors.Select(a => a.Name ?? a.Email).Where(n => !string.IsNullOrEmpty(n)));
            }
            return null;
        }

        private static string? GetImageUrl(SyndicationItem item)
        {
            // Try to find image in links
            var imageLink = item.Links?.FirstOrDefault(l =>
                l.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true ||
                l.RelationshipType?.Equals("enclosure", StringComparison.OrdinalIgnoreCase) == true);

            if (imageLink != null)
            {
                return imageLink.Uri?.ToString();
            }

            // Try to extract from content/description
            var content = item.Summary?.Text ?? (item.Content as TextSyndicationContent)?.Text;
            if (!string.IsNullOrEmpty(content))
            {
                // Simple regex to find first image
                var match = System.Text.RegularExpressions.Regex.Match(
                    content,
                    @"<img[^>]+src=[""']([^""']+)[""']",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        private static string? StripHtml(string? html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            // Remove HTML tags
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);
            // Normalize whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            // Truncate if too long
            if (text.Length > 300)
            {
                text = text.Substring(0, 297) + "...";
            }
            return text;
        }
    }
}
