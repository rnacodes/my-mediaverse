using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Readwise;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class ReadwiseServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<IReadwiseApiClient> _mockReadwiseClient;
        private readonly Mock<ILogger<ReadwiseService>> _mockLogger;
        private readonly ReadwiseService _service;

        public ReadwiseServiceTests()
        {
            _mockReadwiseClient = new Mock<IReadwiseApiClient>();
            _mockLogger = new Mock<ILogger<ReadwiseService>>();
            _service = new ReadwiseService(Context, _mockReadwiseClient.Object, _mockLogger.Object, null);
        }

        #region ValidateConnectionAsync

        [Fact]
        public async Task ValidateConnectionAsync_ValidToken_ReturnsTrue()
        {
            _mockReadwiseClient.Setup(c => c.ValidateTokenAsync()).ReturnsAsync(true);

            var result = await _service.ValidateConnectionAsync();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConnectionAsync_InvalidToken_ReturnsFalse()
        {
            _mockReadwiseClient.Setup(c => c.ValidateTokenAsync()).ReturnsAsync(false);

            var result = await _service.ValidateConnectionAsync();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConnectionAsync_ApiThrows_PropagatesException()
        {
            _mockReadwiseClient.Setup(c => c.ValidateTokenAsync()).ThrowsAsync(new Exception("API error"));

            Func<Task> act = async () => await _service.ValidateConnectionAsync();

            await act.Should().ThrowAsync<Exception>().WithMessage("API error");
        }

        #endregion

        #region SyncBooksAsync

        [Fact]
        public async Task SyncBooksAsync_EmptyResponse_ReturnsZero()
        {
            _mockReadwiseClient.Setup(c => c.GetBooksAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ReadwiseBooksResponse
                {
                    count = 0,
                    results = new List<ReadwiseBookDto>(),
                    next = null
                });

            var result = await _service.SyncBooksAsync();

            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task SyncBooksAsync_WithBooks_ProcessesAndReturnsResult()
        {
            _mockReadwiseClient.Setup(c => c.GetBooksAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ReadwiseBooksResponse
                {
                    count = 1,
                    results = new List<ReadwiseBookDto>
                    {
                        new ReadwiseBookDto
                        {
                            id = 123,
                            title = "Test Book from Readwise",
                            author = "Test Author",
                            category = "books",
                            source = "kindle",
                            num_highlights = 5
                        }
                    },
                    next = null
                });

            var result = await _service.SyncBooksAsync();

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task SyncBooksAsync_WithCategoryFilter_PassesToClient()
        {
            _mockReadwiseClient.Setup(c => c.GetBooksAsync(
                It.IsAny<string?>(), "books", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ReadwiseBooksResponse
                {
                    count = 0,
                    results = new List<ReadwiseBookDto>(),
                    next = null
                });

            await _service.SyncBooksAsync(category: "books");

            _mockReadwiseClient.Verify(c => c.GetBooksAsync(
                It.IsAny<string?>(), "books", It.IsAny<int>(), It.IsAny<int>()),
                Times.Once);
        }

        #endregion

        #region LinkHighlightsToMediaAsync

        [Fact]
        public async Task LinkHighlightsToMediaAsync_NoUnlinkedHighlights_ReturnsZero()
        {
            var result = await _service.LinkHighlightsToMediaAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task LinkHighlightsToMediaAsync_MatchesByArticleUrl_LinksHighlight()
        {
            var article = TestDataFactory.CreateArticle("Test Article");
            article.Link = "https://example.com/article";
            Context.Articles.Add(article);

            var highlight = TestDataFactory.CreateHighlight("Important passage");
            highlight.SourceUrl = "https://example.com/article";
            highlight.ArticleId = null;
            highlight.BookId = null;
            Context.Highlights.Add(highlight);

            await Context.SaveChangesAsync();

            var result = await _service.LinkHighlightsToMediaAsync();

            result.Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region ExportHighlightToReadwiseAsync

        [Fact]
        public async Task ExportHighlightToReadwiseAsync_HighlightNotFound_ReturnsFalse()
        {
            var result = await _service.ExportHighlightToReadwiseAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExportHighlightToReadwiseAsync_ValidHighlight_ExportsToReadwise()
        {
            var article = TestDataFactory.CreateArticle("Test Article");
            article.Link = "https://example.com/article";
            article.Author = "Test Author";
            Context.Articles.Add(article);

            var highlight = TestDataFactory.CreateHighlight("An important quote from the article");
            highlight.ArticleId = article.Id;
            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            _mockReadwiseClient.Setup(c => c.CreateHighlightsAsync(It.IsAny<List<CreateReadwiseHighlightDto>>()))
                .ReturnsAsync(true);

            var result = await _service.ExportHighlightToReadwiseAsync(highlight.Id);

            result.Should().BeTrue();
            _mockReadwiseClient.Verify(c => c.CreateHighlightsAsync(It.IsAny<List<CreateReadwiseHighlightDto>>()), Times.Once);
        }

        [Fact]
        public async Task ExportHighlightToReadwiseAsync_ApiFailure_ReturnsFalse()
        {
            var highlight = TestDataFactory.CreateHighlight("Test highlight");
            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            _mockReadwiseClient.Setup(c => c.CreateHighlightsAsync(It.IsAny<List<CreateReadwiseHighlightDto>>()))
                .ReturnsAsync(false);

            var result = await _service.ExportHighlightToReadwiseAsync(highlight.Id);

            result.Should().BeFalse();
        }

        #endregion
    }
}
