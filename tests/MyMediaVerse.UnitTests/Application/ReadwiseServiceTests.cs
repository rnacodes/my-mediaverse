using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Readwise;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class ReadwiseServiceTests : InMemoryDbTestBase
    {
        private readonly IReadwiseApiClient _mockReadwiseClient;
        private readonly ILogger<ReadwiseService> _mockLogger;
        private readonly ReadwiseService _service;

        public ReadwiseServiceTests()
        {
            _mockReadwiseClient = Substitute.For<IReadwiseApiClient>();
            _mockLogger = Substitute.For<ILogger<ReadwiseService>>();
            _service = new ReadwiseService(Context, _mockReadwiseClient, _mockLogger);
        }

        #region ValidateConnectionAsync

        [Fact]
        public async Task ValidateConnectionAsync_ValidToken_ReturnsTrue()
        {
            _mockReadwiseClient.ValidateTokenAsync().Returns(true);

            var result = await _service.ValidateConnectionAsync();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConnectionAsync_InvalidToken_ReturnsFalse()
        {
            _mockReadwiseClient.ValidateTokenAsync().Returns(false);

            var result = await _service.ValidateConnectionAsync();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConnectionAsync_ApiThrows_PropagatesException()
        {
            _mockReadwiseClient.ValidateTokenAsync().Throws(new Exception("API error"));

            Func<Task> act = async () => await _service.ValidateConnectionAsync();

            await act.Should().ThrowAsync<Exception>().WithMessage("API error");
        }

        #endregion

        #region SyncBooksAsync

        [Fact]
        public async Task SyncBooksAsync_EmptyResponse_ReturnsZero()
        {
            _mockReadwiseClient.GetBooksAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new ReadwiseBooksResponse
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
            _mockReadwiseClient.GetBooksAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new ReadwiseBooksResponse
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
            _mockReadwiseClient.GetBooksAsync(
                Arg.Any<string?>(), "books", Arg.Any<int>(), Arg.Any<int>())
                .Returns(new ReadwiseBooksResponse
                {
                    count = 0,
                    results = new List<ReadwiseBookDto>(),
                    next = null
                });

            await _service.SyncBooksAsync(category: "books");

            _mockReadwiseClient.Received(1).GetBooksAsync(
                Arg.Any<string?>(), "books", Arg.Any<int>(), Arg.Any<int>());
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

            result.Should().Be(1);
            var linked = await Context.Highlights.FindAsync(highlight.Id);
            linked!.ArticleId.Should().Be(article.Id);
        }

        [Fact]
        public async Task LinkHighlightsToMediaAsync_MatchesBookByTitleAndAuthor_CaseInsensitive()
        {
            var book = new Book { Id = Guid.NewGuid(), Title = "Meditations", Author = "Marcus Aurelius" };
            Context.Books.Add(book);

            var highlight = TestDataFactory.CreateHighlight("Memento mori");
            highlight.Title = "MEDITATIONS";
            highlight.Author = "marcus aurelius";
            highlight.Category = "books";
            highlight.SourceUrl = null;
            highlight.ArticleId = null;
            highlight.BookId = null;
            Context.Highlights.Add(highlight);

            await Context.SaveChangesAsync();

            var result = await _service.LinkHighlightsToMediaAsync();

            result.Should().Be(1);
            var linked = await Context.Highlights.FindAsync(highlight.Id);
            linked!.BookId.Should().Be(book.Id);
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

            _mockReadwiseClient.CreateHighlightsAsync(Arg.Any<List<CreateReadwiseHighlightDto>>())
                .Returns(true);

            var result = await _service.ExportHighlightToReadwiseAsync(highlight.Id);

            result.Should().BeTrue();
            _mockReadwiseClient.Received(1).CreateHighlightsAsync(Arg.Any<List<CreateReadwiseHighlightDto>>());
        }

        [Fact]
        public async Task ExportHighlightToReadwiseAsync_ApiFailure_ReturnsFalse()
        {
            var highlight = TestDataFactory.CreateHighlight("Test highlight");
            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            _mockReadwiseClient.CreateHighlightsAsync(Arg.Any<List<CreateReadwiseHighlightDto>>())
                .Returns(false);

            var result = await _service.ExportHighlightToReadwiseAsync(highlight.Id);

            result.Should().BeFalse();
        }

        #endregion
    }
}
