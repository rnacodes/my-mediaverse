using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Services.Web;
using System.Net;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class WebsiteScraperServiceTests
    {
        private readonly Mock<ILogger<WebsiteScraperService>> _mockLogger;

        public WebsiteScraperServiceTests()
        {
            _mockLogger = new Mock<ILogger<WebsiteScraperService>>();
        }

        private WebsiteScraperService CreateServiceWithResponse(string htmlContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(htmlContent)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            return new WebsiteScraperService(httpClient, _mockLogger.Object);
        }

        #region ScrapeWebsiteAsync - Validation

        [Fact]
        public async Task ScrapeWebsiteAsync_NullUrl_ThrowsArgumentException()
        {
            var service = CreateServiceWithResponse("");

            Func<Task> act = async () => await service.ScrapeWebsiteAsync(null!);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_EmptyUrl_ThrowsArgumentException()
        {
            var service = CreateServiceWithResponse("");

            Func<Task> act = async () => await service.ScrapeWebsiteAsync("");

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_InvalidUrl_ThrowsArgumentException()
        {
            var service = CreateServiceWithResponse("");

            Func<Task> act = async () => await service.ScrapeWebsiteAsync("not-a-valid-url");

            await act.Should().ThrowAsync<ArgumentException>();
        }

        #endregion

        #region ScrapeWebsiteAsync - Extraction

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsTitle_FromOgTitle()
        {
            var html = @"
                <html><head>
                    <meta property=""og:title"" content=""My Website Title"" />
                    <title>Fallback Title</title>
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com");

            result.Title.Should().Be("My Website Title");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsTitle_FallsBackToTitleTag()
        {
            var html = @"
                <html><head>
                    <title>Page Title</title>
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com");

            result.Title.Should().Be("Page Title");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsDescription_FromOgDescription()
        {
            var html = @"
                <html><head>
                    <meta property=""og:description"" content=""A great website description"" />
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com");

            result.Description.Should().Be("A great website description");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsImageUrl_FromOgImage()
        {
            var html = @"
                <html><head>
                    <meta property=""og:image"" content=""https://example.com/image.jpg"" />
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com");

            result.ImageUrl.Should().Be("https://example.com/image.jpg");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsRssFeedUrl()
        {
            var html = @"
                <html><head>
                    <link rel=""alternate"" type=""application/rss+xml"" href=""/feed.xml"" />
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com");

            result.RssFeedUrl.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsDomain_RemovesWww()
        {
            var html = @"<html><head><title>Test</title></head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://www.example.com/path");

            result.Domain.Should().Be("example.com");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsAuthor_FromMetaTag()
        {
            var html = @"
                <html><head>
                    <meta name=""author"" content=""John Doe"" />
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com");

            result.Author.Should().Be("John Doe");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_ExtractsPublication_FromOgSiteName()
        {
            var html = @"
                <html><head>
                    <meta property=""og:site_name"" content=""The Daily News"" />
                </head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://dailynews.com");

            result.Publication.Should().Be("The Daily News");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_SetsUrl()
        {
            var html = @"<html><head><title>Test</title></head><body></body></html>";

            var service = CreateServiceWithResponse(html);

            var result = await service.ScrapeWebsiteAsync("https://example.com/page");

            result.Url.Should().Be("https://example.com/page");
        }

        [Fact]
        public async Task ScrapeWebsiteAsync_HttpError_ThrowsWithDescriptiveMessage()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var httpClient = new HttpClient(handlerMock.Object);
            var service = new WebsiteScraperService(httpClient, _mockLogger.Object);

            Func<Task> act = async () => await service.ScrapeWebsiteAsync("https://example.com");

            await act.Should().ThrowAsync<Exception>();
        }

        #endregion
    }
}
