using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class ArticleServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<ArticleService> _mockLogger;
        private readonly ArticleService _service;

        public ArticleServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<ArticleService>>();
            _service = new ArticleService(Context, _mockLogger);
        }

        #region GetAllArticlesAsync Tests

        [Fact]
        public async Task GetAllArticlesAsync_ShouldReturnAllArticles()
        {
            // Arrange
            Context.Articles.AddRange(
                new Article { Title = "Article 1", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Article { Title = "Article 2", Topics = new List<Topic>(), Genres = new List<Genre>() }
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WhenEmpty_ShouldReturnEmptyList()
        {
            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetArticleByIdAsync Tests

        [Fact]
        public async Task GetArticleByIdAsync_WhenExists_ShouldReturnArticle()
        {
            // Arrange
            var article = new Article { Title = "Test Article", Topics = new List<Topic>(), Genres = new List<Genre>() };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetArticleByIdAsync(article.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Article");
        }

        [Fact]
        public async Task GetArticleByIdAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetArticleByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetArchivedArticlesAsync Tests

        [Fact]
        public async Task GetArchivedArticlesAsync_ShouldReturnOnlyArchivedArticles()
        {
            // Arrange
            Context.Articles.AddRange(
                new Article { Title = "Archived", IsArchived = true, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Article { Title = "Not Archived", IsArchived = false, Topics = new List<Topic>(), Genres = new List<Genre>() }
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetArchivedArticlesAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Archived");
        }

        #endregion

        #region GetStarredArticlesAsync Tests

        [Fact]
        public async Task GetStarredArticlesAsync_ShouldReturnOnlyStarredArticles()
        {
            // Arrange
            Context.Articles.AddRange(
                new Article { Title = "Starred", IsStarred = true, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Article { Title = "Not Starred", IsStarred = false, Topics = new List<Topic>(), Genres = new List<Genre>() }
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetStarredArticlesAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Starred");
        }

        #endregion

        #region CreateArticleAsync Tests

        [Fact]
        public async Task CreateArticleAsync_WithValidDto_ShouldCreateArticle()
        {
            // Arrange
            var dto = new CreateArticleDto
            {
                Title = "New Article",
                Status = Status.Uncharted,
                Author = "Test Author",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateArticleAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Article");
            result.Author.Should().Be("Test Author");
            result.MediaType.Should().Be(MediaType.Article);
            result.SyncStatus.Should().Be(SyncStatus.LocalOnly);
            Context.Articles.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateArticleAsync_WithTopicsAndGenres_ShouldNormalizeToLowercase()
        {
            // Arrange
            var dto = new CreateArticleDto
            {
                Title = "Article With Tags",
                Status = Status.Uncharted,
                Topics = new[] { "Technology", "AI " },
                Genres = new[] { "  Tutorial", "GUIDE" }
            };

            // Act
            var result = await _service.CreateArticleAsync(dto);

            // Assert
            result.Topics.Should().HaveCount(2);
            result.Topics.Select(t => t.Name).Should().Contain("technology");
            result.Topics.Select(t => t.Name).Should().Contain("ai");
            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().Contain("tutorial");
            result.Genres.Select(g => g.Name).Should().Contain("guide");
        }

        [Fact]
        public async Task CreateArticleAsync_WithExistingTopics_ShouldReuseTopics()
        {
            // Arrange
            var existingTopic = new Topic { Name = "technology" };
            Context.Topics.Add(existingTopic);
            await Context.SaveChangesAsync();

            var dto = new CreateArticleDto
            {
                Title = "Article",
                Status = Status.Uncharted,
                Topics = new[] { "Technology" },
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateArticleAsync(dto);

            // Assert
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Id.Should().Be(existingTopic.Id);
            Context.Topics.Count().Should().Be(1); // Should not create a duplicate
        }

        [Fact]
        public async Task CreateArticleAsync_ShouldSetDateAddedToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;
            var dto = new CreateArticleDto
            {
                Title = "Timed Article",
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateArticleAsync(dto);

            // Assert
            result.DateAdded.Should().BeOnOrAfter(before);
            result.DateAdded.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateArticleAsync_WithEmptyTopics_ShouldNotAddTopics()
        {
            // Arrange
            var dto = new CreateArticleDto
            {
                Title = "No Tags",
                Status = Status.Uncharted,
                Topics = new[] { "", "  " },
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateArticleAsync(dto);

            // Assert
            result.Topics.Should().BeEmpty();
        }

        #endregion

        #region UpdateArticleAsync Tests

        [Fact]
        public async Task UpdateArticleAsync_WhenExists_ShouldUpdateProperties()
        {
            // Arrange
            var article = new Article
            {
                Title = "Original",
                Author = "Original Author",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            var dto = new CreateArticleDto
            {
                Title = "Updated",
                Author = "Updated Author",
                Status = Status.ActivelyExploring,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.UpdateArticleAsync(article.Id, dto);

            // Assert
            result.Title.Should().Be("Updated");
            result.Author.Should().Be("Updated Author");
            result.Status.Should().Be(Status.ActivelyExploring);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenNotExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new CreateArticleDto
            {
                Title = "Updated",
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateArticleAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateArticleAsync_ShouldUpdateTitleAndStatus()
        {
            // Arrange
            var article = new Article
            {
                Title = "Original Title",
                Status = Status.Uncharted,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            var dto = new CreateArticleDto
            {
                Title = "Updated Title",
                Status = Status.ActivelyExploring,
                Author = "New Author",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.UpdateArticleAsync(article.Id, dto);

            // Assert
            result.Title.Should().Be("Updated Title");
            result.Status.Should().Be(Status.ActivelyExploring);
            result.Author.Should().Be("New Author");
        }

        #endregion

        #region DeleteArticleAsync Tests

        [Fact]
        public async Task DeleteArticleAsync_WhenExists_ShouldReturnTrueAndRemoveArticle()
        {
            // Arrange
            var article = new Article { Title = "To Delete", Topics = new List<Topic>(), Genres = new List<Genre>() };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteArticleAsync(article.Id);

            // Assert
            result.Should().BeTrue();
            Context.Articles.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteArticleAsync_WhenNotExists_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteArticleAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region UpdateArticleSyncStatusAsync Tests

        [Fact]
        public async Task UpdateArticleSyncStatusAsync_WhenExists_ShouldUpdateStatus()
        {
            // Arrange
            var article = new Article
            {
                Title = "Sync Test",
                IsArchived = false,
                IsStarred = false,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateArticleSyncStatusAsync(article.Id, true, true);

            // Assert
            result.IsArchived.Should().BeTrue();
            result.IsStarred.Should().BeTrue();
            result.LastSyncDate.Should().NotBeNull();
            result.LastSyncDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateArticleSyncStatusAsync_WhenNotExists_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateArticleSyncStatusAsync(Guid.NewGuid(), true, true));
        }

        #endregion

        #region GetArticleContentAsync Tests

        [Fact]
        public async Task GetArticleContentAsync_WhenExists_ShouldReturnContent()
        {
            // Arrange
            var article = new Article
            {
                Title = "Content Test",
                FullTextContent = "<p>Hello World</p>",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetArticleContentAsync(article.Id);

            // Assert
            result.Should().Be("<p>Hello World</p>");
        }

        [Fact]
        public async Task GetArticleContentAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetArticleContentAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetArticleContentAsync_WhenContentIsNull_ShouldReturnNull()
        {
            // Arrange
            var article = new Article
            {
                Title = "No Content",
                FullTextContent = null,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetArticleContentAsync(article.Id);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region UpdateArticleContentAsync Tests

        [Fact]
        public async Task UpdateArticleContentAsync_WhenExists_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var article = new Article
            {
                Title = "Update Content",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Articles.Add(article);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateArticleContentAsync(article.Id, "<p>Updated</p>");

            // Assert
            result.Should().BeTrue();
            var updated = await Context.FindAsync<Article>(article.Id);
            updated!.FullTextContent.Should().Be("<p>Updated</p>");
        }

        [Fact]
        public async Task UpdateArticleContentAsync_WhenNotExists_ShouldReturnFalse()
        {
            // Act
            var result = await _service.UpdateArticleContentAsync(Guid.NewGuid(), "content");

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
