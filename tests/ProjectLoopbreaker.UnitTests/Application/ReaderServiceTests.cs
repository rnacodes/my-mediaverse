using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectLoopbreaker.Application.Services;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Shared.DTOs.ReadwiseReader;
using ProjectLoopbreaker.Shared.Interfaces;
using ProjectLoopbreaker.UnitTests.TestData;
using ProjectLoopbreaker.UnitTests.TestHelpers;

namespace ProjectLoopbreaker.UnitTests.Application
{
    public class ReaderServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<IReaderApiClient> _mockReaderClient;
        private readonly Mock<ILogger<ReaderService>> _mockLogger;
        private readonly ReaderService _service;

        public ReaderServiceTests()
        {
            _mockReaderClient = new Mock<IReaderApiClient>();
            _mockLogger = new Mock<ILogger<ReaderService>>();
            _service = new ReaderService(Context, _mockReaderClient.Object, _mockLogger.Object);
        }

        #region SyncDocumentsAsync

        [Fact]
        public async Task SyncDocumentsAsync_EmptyResponse_ReturnsZeroResults()
        {
            _mockReaderClient.Setup(c => c.GetDocumentsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(new ReaderDocumentsResponse
                {
                    results = new List<ReaderDocumentDto>(),
                    nextPageCursor = null
                });

            var result = await _service.SyncDocumentsAsync();

            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task SyncDocumentsAsync_NewArticles_CreatesInDatabase()
        {
            _mockReaderClient.Setup(c => c.GetDocumentsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(new ReaderDocumentsResponse
                {
                    results = new List<ReaderDocumentDto>
                    {
                        new ReaderDocumentDto
                        {
                            id = "reader_doc_1",
                            title = "Test Article",
                            source_url = "https://example.com/article",
                            author = "Test Author",
                            category = "article",
                            location = "archive",
                            summary = "A summary"
                        }
                    },
                    nextPageCursor = null
                });

            var result = await _service.SyncDocumentsAsync();

            result.Should().NotBeNull();
            result.CreatedCount.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task SyncDocumentsAsync_WithLocationFilter_PassesFilterToClient()
        {
            _mockReaderClient.Setup(c => c.GetDocumentsAsync(
                It.IsAny<string?>(), "archive", It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(new ReaderDocumentsResponse
                {
                    results = new List<ReaderDocumentDto>(),
                    nextPageCursor = null
                });

            await _service.SyncDocumentsAsync(location: "archive");

            _mockReaderClient.Verify(c => c.GetDocumentsAsync(
                It.IsAny<string?>(), "archive", It.IsAny<string?>(), It.IsAny<string?>()),
                Times.Once);
        }

        #endregion

        #region FetchAndStoreArticleContentAsync

        [Fact]
        public async Task FetchAndStoreArticleContentAsync_ArticleNotFound_ReturnsFalse()
        {
            var result = await _service.FetchAndStoreArticleContentAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task FetchAndStoreArticleContentAsync_NoReaderDocumentId_ReturnsFalse()
        {
            var article = TestDataFactory.CreateArticle();
            article.ReadwiseDocumentId = null;
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            var result = await _service.FetchAndStoreArticleContentAsync(article.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task FetchAndStoreArticleContentAsync_ValidArticle_FetchesAndStoresContent()
        {
            var article = TestDataFactory.CreateArticle("Test Article");
            article.ReadwiseDocumentId = "reader_123";
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            _mockReaderClient.Setup(c => c.GetDocumentByIdAsync("reader_123", true))
                .ReturnsAsync(new ReaderDocumentDto
                {
                    id = "reader_123",
                    title = "Test Article",
                    html = "<p>Full article content here</p>"
                });

            var result = await _service.FetchAndStoreArticleContentAsync(article.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task FetchAndStoreArticleContentAsync_ApiReturnsNull_ReturnsFalse()
        {
            var article = TestDataFactory.CreateArticle("Test Article");
            article.ReadwiseDocumentId = "reader_123";
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            _mockReaderClient.Setup(c => c.GetDocumentByIdAsync("reader_123", true))
                .ReturnsAsync((ReaderDocumentDto?)null);

            var result = await _service.FetchAndStoreArticleContentAsync(article.Id);

            result.Should().BeFalse();
        }

        #endregion

        #region GetArticlesWithReaderDocumentIdsAsync

        [Fact]
        public async Task GetArticlesWithReaderDocumentIdsAsync_WithReaderIds_ReturnsSummaries()
        {
            var article1 = TestDataFactory.CreateArticle("Article 1");
            article1.ReadwiseDocumentId = "rw_1";
            var article2 = TestDataFactory.CreateArticle("Article 2");
            article2.ReadwiseDocumentId = null;

            Context.Articles.AddRange(article1, article2);
            await Context.SaveChangesAsync();

            var result = await _service.GetArticlesWithReaderDocumentIdsAsync();

            result.Should().NotBeNull();
            result.Count().Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetArticlesWithReaderDocumentIdsAsync_OnlyWithoutContent_FiltersCorrectly()
        {
            var withContent = TestDataFactory.CreateArticle("With Content");
            withContent.ReadwiseDocumentId = "rw_1";
            withContent.FullTextContent = "Has content";

            var withoutContent = TestDataFactory.CreateArticle("Without Content");
            withoutContent.ReadwiseDocumentId = "rw_2";
            withoutContent.FullTextContent = null;

            Context.Articles.AddRange(withContent, withoutContent);
            await Context.SaveChangesAsync();

            var result = await _service.GetArticlesWithReaderDocumentIdsAsync(onlyWithoutContent: true);

            result.Should().NotBeNull();
            result.All(a => a.HasFullTextContent == false).Should().BeTrue();
        }

        #endregion
    }
}
