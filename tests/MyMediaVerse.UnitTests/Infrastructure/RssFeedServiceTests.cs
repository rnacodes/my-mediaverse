using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Infrastructure.Services.Web;
using MyMediaVerse.UnitTests.TestHelpers;
using System.Net;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class RssFeedServiceTests
    {
        private readonly ILogger<RssFeedService> _mockLogger;

        public RssFeedServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<RssFeedService>>();
        }

        private RssFeedService CreateServiceWithResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handlerMock = new TestHttpMessageHandler();
            handlerMock.RespondWith(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

            var httpClient = new HttpClient(handlerMock);
            return new RssFeedService(httpClient, _mockLogger);
        }

        #region GetLatestFeedItemsAsync

        [Fact]
        public async Task GetLatestFeedItemsAsync_NullUrl_ReturnsEmptyList()
        {
            var service = CreateServiceWithResponse("");

            var result = await service.GetLatestFeedItemsAsync(null!);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_EmptyUrl_ReturnsEmptyList()
        {
            var service = CreateServiceWithResponse("");

            var result = await service.GetLatestFeedItemsAsync("");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_ValidRssFeed_ReturnsItems()
        {
            var rssFeed = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <rss version=""2.0"">
                    <channel>
                        <title>Test Feed</title>
                        <item>
                            <title>First Article</title>
                            <link>https://example.com/article1</link>
                            <description>Description of the first article</description>
                            <pubDate>Mon, 01 Jan 2024 12:00:00 GMT</pubDate>
                        </item>
                        <item>
                            <title>Second Article</title>
                            <link>https://example.com/article2</link>
                            <description>Description of the second article</description>
                            <pubDate>Tue, 02 Jan 2024 12:00:00 GMT</pubDate>
                        </item>
                    </channel>
                </rss>";

            var service = CreateServiceWithResponse(rssFeed);

            var result = await service.GetLatestFeedItemsAsync("https://example.com/feed.xml");

            result.Should().HaveCount(2);
            result[0].Title.Should().Be("First Article");
            result[0].Link.Should().Be("https://example.com/article1");
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_RespectsMaxItems()
        {
            var rssFeed = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <rss version=""2.0"">
                    <channel>
                        <title>Test Feed</title>
                        <item><title>Item 1</title><link>https://example.com/1</link></item>
                        <item><title>Item 2</title><link>https://example.com/2</link></item>
                        <item><title>Item 3</title><link>https://example.com/3</link></item>
                        <item><title>Item 4</title><link>https://example.com/4</link></item>
                    </channel>
                </rss>";

            var service = CreateServiceWithResponse(rssFeed);

            var result = await service.GetLatestFeedItemsAsync("https://example.com/feed.xml", maxItems: 2);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_InvalidXml_ReturnsEmptyList()
        {
            var invalidXml = "This is not valid XML at all <><";

            var service = CreateServiceWithResponse(invalidXml);

            var result = await service.GetLatestFeedItemsAsync("https://example.com/feed.xml");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_HttpError_ReturnsEmptyList()
        {
            var handlerMock = new TestHttpMessageHandler
            {
                OnSend = (req, ct) => throw new HttpRequestException("Connection refused")
            };

            var httpClient = new HttpClient(handlerMock);
            var service = new RssFeedService(httpClient, _mockLogger);

            var result = await service.GetLatestFeedItemsAsync("https://example.com/feed.xml");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_ValidAtomFeed_ReturnsItems()
        {
            var atomFeed = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <feed xmlns=""http://www.w3.org/2005/Atom"">
                    <title>Test Atom Feed</title>
                    <entry>
                        <title>Atom Entry 1</title>
                        <link href=""https://example.com/entry1"" />
                        <summary>Summary of entry 1</summary>
                        <updated>2024-01-01T12:00:00Z</updated>
                    </entry>
                </feed>";

            var service = CreateServiceWithResponse(atomFeed);

            var result = await service.GetLatestFeedItemsAsync("https://example.com/atom.xml");

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Atom Entry 1");
        }

        [Fact]
        public async Task GetLatestFeedItemsAsync_FeedWithHtmlDescription_StripsHtml()
        {
            var rssFeed = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <rss version=""2.0"">
                    <channel>
                        <title>Test Feed</title>
                        <item>
                            <title>Article</title>
                            <link>https://example.com/article</link>
                            <description><![CDATA[<p>This is a <strong>bold</strong> description with <a href=""#"">links</a>.</p>]]></description>
                        </item>
                    </channel>
                </rss>";

            var service = CreateServiceWithResponse(rssFeed);

            var result = await service.GetLatestFeedItemsAsync("https://example.com/feed.xml");

            result.Should().HaveCount(1);
            result[0].Description.Should().NotContain("<p>");
            result[0].Description.Should().NotContain("<strong>");
        }

        #endregion
    }
}
