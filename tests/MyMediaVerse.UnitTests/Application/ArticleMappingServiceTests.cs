using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Application
{
    public class ArticleMappingServiceTests
    {
        private readonly ILogger<ArticleMappingService> _mockLogger;
        private readonly IConfiguration _mockConfiguration;
        private readonly ArticleMappingService _service;

        public ArticleMappingServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<ArticleMappingService>>();
            _mockConfiguration = Substitute.For<IConfiguration>();
            _service = new ArticleMappingService(_mockLogger, _mockConfiguration);
        }

        #region MapToResponseDtoAsync (single)

        [Fact]
        public async Task MapToResponseDtoAsync_ValidArticle_MapsAllProperties()
        {
            var article = TestDataFactory.CreateArticle("Test Article", "John Doe");
            article.Link = "https://example.com/article";
            article.Description = "Article description";
            article.IsArchived = true;
            article.IsStarred = true;
            article.Author = "John Doe";
            article.Publication = "Test Publication";
            article.PublicationDate = new DateTime(2024, 6, 15);
            article.ReadingProgress = 75;
            article.WordCount = 2000;
            article.ReadwiseDocumentId = "rw_123";
            article.FullTextContent = "Full text content here";
            article.ReaderLocation = "archive";
            article.ContentStoragePath = "/legacy/path";
            article.Topics.Add(new Topic { Name = "technology" });
            article.Genres.Add(new Genre { Name = "news" });

            var result = await _service.MapToResponseDtoAsync(article);

            result.Should().NotBeNull();
            result.Id.Should().Be(article.Id);
            result.Title.Should().Be("Test Article");
            result.Link.Should().Be("https://example.com/article");
            result.Description.Should().Be("Article description");
            result.IsArchived.Should().BeTrue();
            result.IsStarred.Should().BeTrue();
            result.Author.Should().Be("John Doe");
            result.Publication.Should().Be("Test Publication");
            result.ReadingProgress.Should().Be(75);
            result.WordCount.Should().Be(2000);
            result.ReadwiseDocumentId.Should().Be("rw_123");
            result.HasFullTextContent.Should().BeTrue();
            result.ReaderLocation.Should().Be("archive");
            result.ContentStoragePath.Should().Be("/legacy/path");
            result.ContentUrl.Should().BeNull(); // No longer using S3
            result.Topics.Should().Contain("technology");
            result.Genres.Should().Contain("news");
        }

        [Fact]
        public async Task MapToResponseDtoAsync_NullFullTextContent_HasFullTextContentIsFalse()
        {
            var article = TestDataFactory.CreateArticle();
            article.FullTextContent = null;

            var result = await _service.MapToResponseDtoAsync(article);

            result.HasFullTextContent.Should().BeFalse();
        }

        [Fact]
        public async Task MapToResponseDtoAsync_EmptyFullTextContent_HasFullTextContentIsFalse()
        {
            var article = TestDataFactory.CreateArticle();
            article.FullTextContent = "";

            var result = await _service.MapToResponseDtoAsync(article);

            result.HasFullTextContent.Should().BeFalse();
        }

        [Fact]
        public async Task MapToResponseDtoAsync_EmptyTopicsAndGenres_MapsToEmptyArrays()
        {
            var article = TestDataFactory.CreateArticle();

            var result = await _service.MapToResponseDtoAsync(article);

            result.Topics.Should().NotBeNull();
            result.Topics.Should().BeEmpty();
            result.Genres.Should().NotBeNull();
            result.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task MapToResponseDtoAsync_WithEstimatedReadingTime_ComputedFromWordCount()
        {
            var article = TestDataFactory.CreateArticle();
            article.WordCount = 500; // 500 words â‰ˆ 2 min at ~250 wpm

            var result = await _service.MapToResponseDtoAsync(article);

            result.EstimatedReadingTime.Should().NotBeNull();
        }

        #endregion

        #region MapToResponseDtoAsync (collection)

        [Fact]
        public async Task MapToResponseDtoAsync_MultipleArticles_MapsAll()
        {
            var articles = TestDataFactory.CreateArticles(3);

            var result = await _service.MapToResponseDtoAsync(articles);

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task MapToResponseDtoAsync_EmptyCollection_ReturnsEmpty()
        {
            var articles = new List<Article>();

            var result = await _service.MapToResponseDtoAsync(articles);

            result.Should().BeEmpty();
        }

        #endregion
    }
}
