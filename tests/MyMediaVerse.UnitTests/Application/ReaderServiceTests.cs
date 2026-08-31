using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.Shared.DTOs.ReadwiseReader;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class ReaderServiceTests : InMemoryDbTestBase
    {
        private readonly IReaderApiClient _mockReaderClient;
        private readonly ILogger<ReaderService> _mockLogger;
        private readonly ReaderService _service;

        public ReaderServiceTests()
        {
            _mockReaderClient = Substitute.For<IReaderApiClient>();
            _mockLogger = Substitute.For<ILogger<ReaderService>>();
            _service = new ReaderService(Context, _mockReaderClient, _mockLogger)
            {
                PageDelayMs = 0,
                ContentFetchDelayMs = 0
            };
        }

        private static ReaderDocumentDto Doc(
            string id,
            string sourceUrl,
            string title = "Test Article",
            string location = "archive",
            Dictionary<string, object>? tags = null,
            string? publishedDate = null) => new()
        {
            Id = id,
            Title = title,
            SourceUrl = sourceUrl,
            Url = $"https://read.readwise.io/read/{id}",
            Author = "Test Author",
            Category = "article",
            Location = location,
            Summary = "A summary",
            Tags = tags,
            PublishedDate = publishedDate
        };

        private void ClientReturnsSinglePage(params ReaderDocumentDto[] docs)
        {
            _mockReaderClient.GetDocumentsAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(new ReaderDocumentsResponse
                {
                    Results = docs.ToList(),
                    NextPageCursor = null
                });
        }

        #region SyncDocumentsAsync

        [Fact]
        public async Task SyncDocumentsAsync_EmptyResponse_ReturnsZeroResults()
        {
            ClientReturnsSinglePage();

            var result = await _service.SyncDocumentsAsync();

            result.Success.Should().BeTrue();
            result.TotalProcessed.Should().Be(0);
            result.WarningMessage.Should().BeNull();
        }

        [Fact]
        public async Task SyncDocumentsAsync_NewArticle_CreatesWithNormalizedLink()
        {
            ClientReturnsSinglePage(Doc("reader_doc_1", "https://www.Example.com/article/?utm_source=x"));

            var result = await _service.SyncDocumentsAsync();

            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(0);

            var article = await Context.Articles.SingleAsync();
            article.ReadwiseDocumentId.Should().Be("reader_doc_1");
            article.Link.Should().Be("https://example.com/article");
            article.Status.Should().Be(Status.Completed);
            article.SyncStatus.Should().Be(SyncStatus.ReaderSynced);
        }

        [Fact]
        public async Task SyncDocumentsAsync_WithLocationFilter_PassesFilterToClient()
        {
            _mockReaderClient.GetDocumentsAsync(
                Arg.Any<string?>(), "archive", Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(new ReaderDocumentsResponse());

            await _service.SyncDocumentsAsync(location: "archive");

            await _mockReaderClient.Received(1).GetDocumentsAsync(
                Arg.Any<string?>(), "archive", Arg.Any<string?>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task SyncDocumentsAsync_ExistingArticleByDocumentId_UpdatesInsteadOfCreating()
        {
            var existing = TestDataFactory.CreateArticle("Old Title");
            existing.ReadwiseDocumentId = "reader_doc_1";
            existing.Link = "https://somewhere-else.com/x";
            Context.Articles.Add(existing);
            await Context.SaveChangesAsync();

            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/new-url"));

            var result = await _service.SyncDocumentsAsync();

            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1);
            (await Context.Articles.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task SyncDocumentsAsync_ExistingArticleByHttpVariantOfUrl_UpdatesInsteadOfCreating()
        {
            var existing = TestDataFactory.CreateArticle("Existing");
            existing.ReadwiseDocumentId = null;
            existing.Link = "http://example.com/article";
            Context.Articles.Add(existing);
            await Context.SaveChangesAsync();

            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/article"));

            var result = await _service.SyncDocumentsAsync();

            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1);
            var article = await Context.Articles.SingleAsync();
            article.ReadwiseDocumentId.Should().Be("reader_doc_1");
        }

        [Fact]
        public async Task SyncDocumentsAsync_ReaderTags_BecomeNormalizedTopics()
        {
            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/a", tags: new Dictionary<string, object>
            {
                ["Tech"] = new object(),
                ["  Deep Work "] = new object(),
                ["tech"] = new object()
            }));

            await _service.SyncDocumentsAsync();

            var article = await Context.Articles.Include(a => a.Topics).SingleAsync();
            article.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "tech", "deep work" });
            (await Context.Topics.CountAsync(t => t.Name == "tech")).Should().Be(1);
        }

        [Fact]
        public async Task SyncDocumentsAsync_ReaderTags_ReuseExistingTopicRow()
        {
            Context.Topics.Add(new Topic { Name = "tech" });
            await Context.SaveChangesAsync();

            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/a", tags: new Dictionary<string, object>
            {
                ["TECH"] = new object()
            }));

            await _service.SyncDocumentsAsync();

            (await Context.Topics.CountAsync()).Should().Be(1);
            var article = await Context.Articles.Include(a => a.Topics).SingleAsync();
            article.Topics.Should().ContainSingle(t => t.Name == "tech");
        }

        [Fact]
        public async Task SyncDocumentsAsync_ExistingArticle_AddsReaderTagsWithoutRemovingManualTopics()
        {
            var existing = TestDataFactory.CreateArticle("Existing");
            existing.ReadwiseDocumentId = "reader_doc_1";
            existing.Link = "https://example.com/a";
            existing.Topics.Add(new Topic { Name = "manual" });
            Context.Articles.Add(existing);
            await Context.SaveChangesAsync();

            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/a", tags: new Dictionary<string, object>
            {
                ["tech"] = new object()
            }));

            await _service.SyncDocumentsAsync();

            var article = await Context.Articles.Include(a => a.Topics).SingleAsync();
            article.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "manual", "tech" });
        }

        [Fact]
        public async Task SyncDocumentsAsync_NoTags_AddsNoTopics()
        {
            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/a", tags: new Dictionary<string, object>()));

            await _service.SyncDocumentsAsync();

            var article = await Context.Articles.Include(a => a.Topics).SingleAsync();
            article.Topics.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncDocumentsAsync_MalformedPublishedDate_StillCreatesArticle()
        {
            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/a", publishedDate: "not-a-date"));

            var result = await _service.SyncDocumentsAsync();

            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            var article = await Context.Articles.SingleAsync();
            article.PublicationDate.Should().BeNull();
        }

        [Fact]
        public async Task SyncDocumentsAsync_ValidPublishedDate_IsStoredAsUtc()
        {
            ClientReturnsSinglePage(Doc("reader_doc_1", "https://example.com/a", publishedDate: "2026-01-15T10:00:00Z"));

            await _service.SyncDocumentsAsync();

            var article = await Context.Articles.SingleAsync();
            article.PublicationDate.Should().Be(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task SyncDocumentsAsync_ClientThrows_ReportsFatalFailure()
        {
            _mockReaderClient.GetDocumentsAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns<ReaderDocumentsResponse>(_ => throw new HttpRequestException("Reader API unreachable"));

            var result = await _service.SyncDocumentsAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("unreachable");
            result.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task SyncDocumentsAsync_PageLimitReached_CompletesWithWarning()
        {
            // Every page claims there is another one, so the safety cap is what ends the run.
            _mockReaderClient.GetDocumentsAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(_ => new ReaderDocumentsResponse
                {
                    Results = new List<ReaderDocumentDto> { Doc("reader_doc_1", "https://example.com/a") },
                    NextPageCursor = "more"
                });

            var result = await _service.SyncDocumentsAsync();

            result.Success.Should().BeTrue();
            result.WarningMessage.Should().Contain("safety limit");
            await _mockReaderClient.Received(ReaderService.MaxSyncPages).GetDocumentsAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task SyncDocumentsAsync_LastPageReached_HasNoWarning()
        {
            var pages = new Queue<ReaderDocumentsResponse>(new[]
            {
                new ReaderDocumentsResponse
                {
                    Results = new List<ReaderDocumentDto> { Doc("reader_doc_1", "https://example.com/a") },
                    NextPageCursor = "page2"
                },
                new ReaderDocumentsResponse
                {
                    Results = new List<ReaderDocumentDto> { Doc("reader_doc_2", "https://example.com/b") },
                    NextPageCursor = null
                }
            });
            _mockReaderClient.GetDocumentsAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(_ => pages.Dequeue());

            var result = await _service.SyncDocumentsAsync();

            result.Success.Should().BeTrue();
            result.WarningMessage.Should().BeNull();
            result.CreatedCount.Should().Be(2);
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

            _mockReaderClient.GetDocumentByIdAsync("reader_123", true)
                .Returns(new ReaderDocumentDto
                {
                    Id = "reader_123",
                    Title = "Test Article",
                    Html = "<p>Full article content here</p>",
                    WordCount = 5
                });

            var result = await _service.FetchAndStoreArticleContentAsync(article.Id);

            result.Should().BeTrue();
            var stored = await Context.Articles.SingleAsync(a => a.Id == article.Id);
            stored.FullTextContent.Should().Be("<p>Full article content here</p>");
            stored.WordCount.Should().Be(5);
            stored.LastReaderSync.Should().NotBeNull();
        }

        [Fact]
        public async Task FetchAndStoreArticleContentAsync_ApiReturnsNull_ReturnsFalse()
        {
            var article = TestDataFactory.CreateArticle("Test Article");
            article.ReadwiseDocumentId = "reader_123";
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            _mockReaderClient.GetDocumentByIdAsync("reader_123", true)
                .Returns((ReaderDocumentDto?)null);

            var result = await _service.FetchAndStoreArticleContentAsync(article.Id);

            result.Should().BeFalse();
        }

        #endregion

        #region BulkFetchArticleContentsAsync

        [Fact]
        public async Task BulkFetchArticleContentsAsync_ReportsFetchedAndSkippedCounts()
        {
            var fetched = TestDataFactory.CreateArticle("Fetched");
            fetched.ReadwiseDocumentId = "d1";
            fetched.FullTextContent = null;
            fetched.Status = Status.Completed;

            var unavailable = TestDataFactory.CreateArticle("Unavailable");
            unavailable.ReadwiseDocumentId = "d2";
            unavailable.FullTextContent = null;
            unavailable.Status = Status.Completed;

            var notArchived = TestDataFactory.CreateArticle("Not archived");
            notArchived.ReadwiseDocumentId = "d3";
            notArchived.FullTextContent = null;
            notArchived.Status = Status.Uncharted;

            Context.Articles.AddRange(fetched, unavailable, notArchived);
            await Context.SaveChangesAsync();

            _mockReaderClient.GetDocumentByIdAsync("d1", true)
                .Returns(new ReaderDocumentDto { Id = "d1", HtmlContent = "<p>hi</p>" });
            _mockReaderClient.GetDocumentByIdAsync("d2", true)
                .Returns((ReaderDocumentDto?)null);

            var result = await _service.BulkFetchArticleContentsAsync(batchSize: 50);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("reader-bulk-fetch-content");
            result.UpdatedCount.Should().Be(1);
            result.SkippedCount.Should().Be(1);
            result.WarningMessage.Should().Contain("1 of 2");
            await _mockReaderClient.DidNotReceive().GetDocumentByIdAsync("d3", Arg.Any<bool>());
        }

        [Fact]
        public async Task BulkFetchArticleContentsAsync_NothingToFetch_SucceedsWithZeroCounts()
        {
            var result = await _service.BulkFetchArticleContentsAsync();

            result.Success.Should().BeTrue();
            result.TotalProcessed.Should().Be(0);
            result.WarningMessage.Should().BeNull();
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

            result.Should().ContainSingle(a => a.ReadwiseDocumentId == "rw_1");
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

            result.Should().ContainSingle(a => a.ReadwiseDocumentId == "rw_2");
        }

        #endregion
    }
}
