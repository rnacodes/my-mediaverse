using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectLoopbreaker.Application.Services;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Domain.Enums;
using ProjectLoopbreaker.UnitTests.TestData;
using ProjectLoopbreaker.UnitTests.TestHelpers;

namespace ProjectLoopbreaker.UnitTests.Application
{
    public class ArticleDeduplicationServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ILogger<ArticleDeduplicationService>> _mockLogger;
        private readonly ArticleDeduplicationService _service;

        public ArticleDeduplicationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ArticleDeduplicationService>>();
            _service = new ArticleDeduplicationService(Context, _mockLogger.Object);
        }

        #region FindDuplicatesAsync

        [Fact]
        public async Task FindDuplicatesAsync_NoDuplicates_ReturnsEmptyList()
        {
            var article1 = TestDataFactory.CreateArticle("Article 1");
            article1.Link = "https://example.com/article1";
            var article2 = TestDataFactory.CreateArticle("Article 2");
            article2.Link = "https://example.com/article2";

            Context.Articles.AddRange(article1, article2);
            await Context.SaveChangesAsync();

            var result = await _service.FindDuplicatesAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task FindDuplicatesAsync_WithDuplicates_GroupsByNormalizedUrl()
        {
            var article1 = TestDataFactory.CreateArticle("Article 1");
            article1.Link = "https://example.com/article";
            var article2 = TestDataFactory.CreateArticle("Article 2");
            article2.Link = "https://example.com/article"; // Same URL

            Context.Articles.AddRange(article1, article2);
            await Context.SaveChangesAsync();

            var result = await _service.FindDuplicatesAsync();

            result.Should().HaveCount(1);
            result[0].Articles.Should().HaveCount(2);
        }

        [Fact]
        public async Task FindDuplicatesAsync_NullLinks_Excluded()
        {
            var article1 = TestDataFactory.CreateArticle("Article 1");
            article1.Link = null;
            var article2 = TestDataFactory.CreateArticle("Article 2");
            article2.Link = null;

            Context.Articles.AddRange(article1, article2);
            await Context.SaveChangesAsync();

            var result = await _service.FindDuplicatesAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task FindDuplicatesAsync_IdentifiesReaderDataAndContent()
        {
            var article1 = TestDataFactory.CreateArticle("Article 1");
            article1.Link = "https://example.com/test";
            article1.ReadwiseDocumentId = "rw_123";
            article1.FullTextContent = "Full content here";

            var article2 = TestDataFactory.CreateArticle("Article 2");
            article2.Link = "https://example.com/test";

            Context.Articles.AddRange(article1, article2);
            await Context.SaveChangesAsync();

            var result = await _service.FindDuplicatesAsync();

            result.Should().HaveCount(1);
            var group = result[0];
            var articleWithReader = group.Articles.First(a => a.Id == article1.Id);
            articleWithReader.HasReaderData.Should().BeTrue();
            articleWithReader.HasContent.Should().BeTrue();
        }

        #endregion

        #region FindAndMergeDuplicatesAsync

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_NoDuplicates_ReturnsSuccessWithZeroMerged()
        {
            var article = TestDataFactory.CreateArticle("Only Article");
            article.Link = "https://example.com/unique";

            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            var result = await _service.FindAndMergeDuplicatesAsync();

            result.Success.Should().BeTrue();
            result.MergedCount.Should().Be(0);
            result.GroupCount.Should().Be(0);
        }

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_WithDuplicates_MergesAndRemovesDuplicates()
        {
            var primary = TestDataFactory.CreateArticle("Primary");
            primary.Link = "https://example.com/article";
            primary.Author = "Full Author Name";
            primary.Description = "A complete description of the article";
            primary.ReadwiseDocumentId = "rw_123";
            primary.FullTextContent = "Full content";

            var duplicate = TestDataFactory.CreateArticle("Duplicate");
            duplicate.Link = "https://example.com/article";

            Context.Articles.AddRange(primary, duplicate);
            await Context.SaveChangesAsync();

            var result = await _service.FindAndMergeDuplicatesAsync();

            result.Success.Should().BeTrue();
            result.MergedCount.Should().Be(1);
            result.GroupCount.Should().Be(1);
            result.MergedGroups.Should().HaveCount(1);
            result.MergedGroups[0].PrimaryId.Should().Be(primary.Id);

            // Duplicate should be removed
            Context.Articles.Should().HaveCount(1);
            Context.Articles.First().Id.Should().Be(primary.Id);
        }

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_PrefersArticleWithReaderContent()
        {
            var withoutReader = TestDataFactory.CreateArticle("Without Reader");
            withoutReader.Link = "https://example.com/article";
            withoutReader.DateAdded = DateTime.UtcNow.AddDays(-5); // Older

            var withReader = TestDataFactory.CreateArticle("With Reader");
            withReader.Link = "https://example.com/article";
            withReader.ReadwiseDocumentId = "rw_456";
            withReader.FullTextContent = "Content from Reader";

            Context.Articles.AddRange(withoutReader, withReader);
            await Context.SaveChangesAsync();

            var result = await _service.FindAndMergeDuplicatesAsync();

            result.Success.Should().BeTrue();
            result.MergedGroups[0].PrimaryId.Should().Be(withReader.Id);
        }

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_MergesMetadataFromDuplicate()
        {
            var primary = TestDataFactory.CreateArticle("Primary");
            primary.Link = "https://example.com/article";
            primary.Author = null;
            primary.ReadwiseDocumentId = "rw_123";
            primary.FullTextContent = "Content";

            var duplicate = TestDataFactory.CreateArticle("Duplicate");
            duplicate.Link = "https://example.com/article";
            duplicate.Author = "Author from Duplicate";
            duplicate.Publication = "Great Publication";
            duplicate.WordCount = 5000;

            Context.Articles.AddRange(primary, duplicate);
            await Context.SaveChangesAsync();

            await _service.FindAndMergeDuplicatesAsync();

            var merged = Context.Articles.First();
            merged.Author.Should().Be("Author from Duplicate");
            merged.Publication.Should().Be("Great Publication");
            merged.WordCount.Should().Be(5000);
        }

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_MergesTopicsAndGenres()
        {
            var primary = TestDataFactory.CreateArticle("Primary");
            primary.Link = "https://example.com/article";
            primary.ReadwiseDocumentId = "rw_123";
            primary.FullTextContent = "Content";
            primary.Topics.Add(new Topic { Name = "technology" });

            var duplicate = TestDataFactory.CreateArticle("Duplicate");
            duplicate.Link = "https://example.com/article";
            duplicate.Topics.Add(new Topic { Name = "science" });
            duplicate.Genres.Add(new Genre { Name = "news" });

            Context.Articles.AddRange(primary, duplicate);
            await Context.SaveChangesAsync();

            await _service.FindAndMergeDuplicatesAsync();

            var merged = Context.Articles.First();
            merged.Topics.Should().HaveCount(2);
            merged.Topics.Select(t => t.Name).Should().Contain("technology");
            merged.Topics.Select(t => t.Name).Should().Contain("science");
            merged.Genres.Should().HaveCount(1);
        }

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_ReassignsHighlights()
        {
            var primary = TestDataFactory.CreateArticle("Primary");
            primary.Link = "https://example.com/article";
            primary.ReadwiseDocumentId = "rw_123";
            primary.FullTextContent = "Content";

            var duplicate = TestDataFactory.CreateArticle("Duplicate");
            duplicate.Link = "https://example.com/article";

            var highlight = TestDataFactory.CreateHighlight("Important passage");
            highlight.ArticleId = duplicate.Id;
            highlight.Article = duplicate;
            duplicate.Highlights.Add(highlight);

            Context.Articles.AddRange(primary, duplicate);
            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            await _service.FindAndMergeDuplicatesAsync();

            var reassignedHighlight = Context.Highlights.First();
            reassignedHighlight.ArticleId.Should().Be(primary.Id);
        }

        [Fact]
        public async Task FindAndMergeDuplicatesAsync_MergesReaderDataToPrimary()
        {
            var primary = TestDataFactory.CreateArticle("Primary");
            primary.Link = "https://example.com/article";
            primary.Author = "Author";
            primary.Description = "A long description with lots of details about the article";
            // No Reader data

            var duplicate = TestDataFactory.CreateArticle("Duplicate");
            duplicate.Link = "https://example.com/article";
            duplicate.ReadwiseDocumentId = "rw_789";
            duplicate.ReaderLocation = "archive";
            duplicate.IsArchived = true;
            duplicate.IsStarred = true;

            Context.Articles.AddRange(primary, duplicate);
            await Context.SaveChangesAsync();

            await _service.FindAndMergeDuplicatesAsync();

            var merged = Context.Articles.First();
            merged.ReadwiseDocumentId.Should().Be("rw_789");
            merged.ReaderLocation.Should().Be("archive");
            merged.IsArchived.Should().BeTrue();
            merged.IsStarred.Should().BeTrue();
        }

        #endregion
    }
}
