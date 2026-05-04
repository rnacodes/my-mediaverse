using FluentAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    public class HighlightTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var highlight = new Highlight();

            // Assert
            highlight.Text.Should().Be(string.Empty);
            highlight.Note.Should().BeNull();
            highlight.Title.Should().BeNull();
            highlight.Author.Should().BeNull();
            highlight.Category.Should().BeNull();
            highlight.SourceUrl.Should().BeNull();
            highlight.ImageUrl.Should().BeNull();
            highlight.HighlightUrl.Should().BeNull();
            highlight.Location.Should().BeNull();
            highlight.LocationType.Should().BeNull();
            highlight.HighlightedAt.Should().BeNull();
            highlight.UpdatedAt.Should().BeNull();
            highlight.Tags.Should().BeNull();
            highlight.ArticleId.Should().BeNull();
            highlight.Article.Should().BeNull();
            highlight.BookId.Should().BeNull();
            highlight.Book.Should().BeNull();
            highlight.ReadwiseBookId.Should().BeNull();
            highlight.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            highlight.SourceType.Should().BeNull();
            highlight.IsFavorite.Should().BeFalse();
            highlight.Color.Should().BeNull();
            highlight.Metadata.Should().BeNull();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var highlight = new Highlight();
            var testDate = DateTime.UtcNow;
            var articleId = Guid.NewGuid();
            var bookId = Guid.NewGuid();

            // Act
            highlight.Id = Guid.NewGuid();
            highlight.ReadwiseId = 42;
            highlight.Text = "The only way to do great work is to love what you do.";
            highlight.Note = "Great quote from Steve Jobs";
            highlight.Title = "Steve Jobs Biography";
            highlight.Author = "Walter Isaacson";
            highlight.Category = "books";
            highlight.SourceUrl = "https://example.com/steve-jobs";
            highlight.ImageUrl = "https://example.com/cover.jpg";
            highlight.HighlightUrl = "https://readwise.io/highlight/42";
            highlight.Location = 256;
            highlight.LocationType = "page";
            highlight.HighlightedAt = testDate;
            highlight.UpdatedAt = testDate;
            highlight.Tags = "biography,technology,apple";
            highlight.ArticleId = articleId;
            highlight.BookId = bookId;
            highlight.ReadwiseBookId = 100;
            highlight.SourceType = "kindle";
            highlight.IsFavorite = true;
            highlight.Color = "yellow";
            highlight.Metadata = "{\"key\": \"value\"}";

            // Assert
            highlight.ReadwiseId.Should().Be(42);
            highlight.Text.Should().Be("The only way to do great work is to love what you do.");
            highlight.Note.Should().Be("Great quote from Steve Jobs");
            highlight.Title.Should().Be("Steve Jobs Biography");
            highlight.Author.Should().Be("Walter Isaacson");
            highlight.Category.Should().Be("books");
            highlight.SourceUrl.Should().Be("https://example.com/steve-jobs");
            highlight.ImageUrl.Should().Be("https://example.com/cover.jpg");
            highlight.HighlightUrl.Should().Be("https://readwise.io/highlight/42");
            highlight.Location.Should().Be(256);
            highlight.LocationType.Should().Be("page");
            highlight.HighlightedAt.Should().Be(testDate);
            highlight.UpdatedAt.Should().Be(testDate);
            highlight.Tags.Should().Be("biography,technology,apple");
            highlight.ArticleId.Should().Be(articleId);
            highlight.BookId.Should().Be(bookId);
            highlight.ReadwiseBookId.Should().Be(100);
            highlight.SourceType.Should().Be("kindle");
            highlight.IsFavorite.Should().BeTrue();
            highlight.Color.Should().Be("yellow");
            highlight.Metadata.Should().Be("{\"key\": \"value\"}");
        }

        [Theory]
        [InlineData("books")]
        [InlineData("articles")]
        [InlineData("tweets")]
        [InlineData("podcasts")]
        public void Category_ShouldAcceptKnownValues(string category)
        {
            // Arrange
            var highlight = new Highlight();

            // Act
            highlight.Category = category;

            // Assert
            highlight.Category.Should().Be(category);
        }

        [Theory]
        [InlineData("page")]
        [InlineData("location")]
        [InlineData("order")]
        [InlineData("offset")]
        [InlineData("time_offset")]
        public void LocationType_ShouldAcceptKnownValues(string locationType)
        {
            // Arrange
            var highlight = new Highlight();

            // Act
            highlight.LocationType = locationType;

            // Assert
            highlight.LocationType.Should().Be(locationType);
        }

        [Theory]
        [InlineData("instapaper")]
        [InlineData("kindle")]
        [InlineData("reader")]
        public void SourceType_ShouldAcceptKnownValues(string sourceType)
        {
            // Arrange
            var highlight = new Highlight();

            // Act
            highlight.SourceType = sourceType;

            // Assert
            highlight.SourceType.Should().Be(sourceType);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void Article_CanBeLinked()
        {
            // Arrange
            var article = new Article { Title = "Test Article" };
            var highlight = new Highlight
            {
                Id = Guid.NewGuid(),
                ReadwiseId = 1,
                Text = "Highlighted text"
            };

            // Act
            highlight.Article = article;
            highlight.ArticleId = article.Id;

            // Assert
            highlight.Article.Should().NotBeNull();
            highlight.Article!.Title.Should().Be("Test Article");
            highlight.ArticleId.Should().Be(article.Id);
        }

        [Fact]
        public void Book_CanBeLinked()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author" };
            var highlight = new Highlight
            {
                Id = Guid.NewGuid(),
                ReadwiseId = 1,
                Text = "Highlighted text"
            };

            // Act
            highlight.Book = book;
            highlight.BookId = book.Id;

            // Assert
            highlight.Book.Should().NotBeNull();
            highlight.Book!.Title.Should().Be("Test Book");
            highlight.BookId.Should().Be(book.Id);
        }

        [Fact]
        public void CanBeUnlinked_NullForeignKeys()
        {
            // Arrange & Act
            var highlight = new Highlight
            {
                Id = Guid.NewGuid(),
                ReadwiseId = 1,
                Text = "Orphan highlight",
                ArticleId = null,
                BookId = null
            };

            // Assert
            highlight.ArticleId.Should().BeNull();
            highlight.BookId.Should().BeNull();
            highlight.Article.Should().BeNull();
            highlight.Book.Should().BeNull();
        }

        #endregion
    }
}
