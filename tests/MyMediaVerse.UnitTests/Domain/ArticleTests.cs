using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class ArticleTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var article = new Article { Title = "" };

            // Assert
            article.Id.Should().NotBeEmpty();
            article.FullTextContent.Should().BeNull();
            article.ContentStoragePath.Should().BeNull();
            article.IsArchived.Should().BeFalse();
            article.IsStarred.Should().BeFalse();
            article.LastSyncDate.Should().BeNull();
            article.Author.Should().BeNull();
            article.Publication.Should().BeNull();
            article.PublicationDate.Should().BeNull();
            article.ReadingProgress.Should().BeNull();
            article.WordCount.Should().BeNull();
            article.ReadwiseDocumentId.Should().BeNull();
            article.ReaderLocation.Should().BeNull();
            article.LastReaderSync.Should().BeNull();
            article.SyncStatus.Should().Be(SyncStatus.LocalOnly);
            article.Highlights.Should().NotBeNull().And.BeEmpty();
            article.Topics.Should().NotBeNull().And.BeEmpty();
            article.Genres.Should().NotBeNull().And.BeEmpty();
            article.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            var testDate = DateTime.UtcNow;

            // Act
            article.Title = "Understanding React Hooks";
            article.Author = "Dan Abramov";
            article.Publication = "Overreacted";
            article.PublicationDate = testDate;
            article.ReadingProgress = 75;
            article.WordCount = 3500;
            article.ReadwiseDocumentId = "doc_abc123";
            article.ReaderLocation = "later";
            article.IsArchived = true;
            article.IsStarred = true;
            article.FullTextContent = "<p>Article content here</p>";
            article.ContentStoragePath = "articles/2024/hooks.html";

            // Assert
            article.Title.Should().Be("Understanding React Hooks");
            article.Author.Should().Be("Dan Abramov");
            article.Publication.Should().Be("Overreacted");
            article.PublicationDate.Should().Be(testDate);
            article.ReadingProgress.Should().Be(75);
            article.WordCount.Should().Be(3500);
            article.ReadwiseDocumentId.Should().Be("doc_abc123");
            article.ReaderLocation.Should().Be("later");
            article.IsArchived.Should().BeTrue();
            article.IsStarred.Should().BeTrue();
            article.FullTextContent.Should().Be("<p>Article content here</p>");
            article.ContentStoragePath.Should().Be("articles/2024/hooks.html");
        }

        [Theory]
        [InlineData(SyncStatus.LocalOnly)]
        [InlineData(SyncStatus.ReadwiseSynced)]
        [InlineData(SyncStatus.ReaderSynced)]
        [InlineData(SyncStatus.FullySynced)]
        public void SyncStatus_ShouldAcceptAllValidValues(SyncStatus syncStatus)
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();

            // Act
            article.SyncStatus = syncStatus;

            // Assert
            article.SyncStatus.Should().Be(syncStatus);
        }

        [Fact]
        public void SyncStatus_CanCombineFlags()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();

            // Act
            article.SyncStatus = SyncStatus.ReadwiseSynced | SyncStatus.ReaderSynced;

            // Assert
            article.SyncStatus.Should().Be(SyncStatus.FullySynced);
            article.SyncStatus.HasFlag(SyncStatus.ReadwiseSynced).Should().BeTrue();
            article.SyncStatus.HasFlag(SyncStatus.ReaderSynced).Should().BeTrue();
        }

        [Theory]
        [InlineData("new")]
        [InlineData("later")]
        [InlineData("archive")]
        [InlineData("feed")]
        public void ReaderLocation_ShouldAcceptKnownValues(string location)
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();

            // Act
            article.ReaderLocation = location;

            // Assert
            article.ReaderLocation.Should().Be(location);
        }

        #endregion

        #region GetContentUrl Tests

        [Fact]
        public void GetContentUrl_WithValidPath_ShouldReturnFullUrl()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.ContentStoragePath = "articles/2024/test.html";

            // Act
            var url = article.GetContentUrl("my-bucket", "sfo3.digitaloceanspaces.com");

            // Assert
            url.Should().Be("https://my-bucket.sfo3.digitaloceanspaces.com/articles/2024/test.html");
        }

        [Fact]
        public void GetContentUrl_WithNullPath_ShouldReturnNull()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.ContentStoragePath = null;

            // Act
            var url = article.GetContentUrl("my-bucket", "sfo3.digitaloceanspaces.com");

            // Assert
            url.Should().BeNull();
        }

        [Fact]
        public void GetContentUrl_WithEmptyPath_ShouldReturnNull()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.ContentStoragePath = "";

            // Act
            var url = article.GetContentUrl("my-bucket", "sfo3.digitaloceanspaces.com");

            // Assert
            url.Should().BeNull();
        }

        #endregion

        #region GetEstimatedReadingTime Tests

        [Fact]
        public void GetEstimatedReadingTime_WithValidWordCount_ShouldReturnMinutes()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.WordCount = 1000;

            // Act
            var readingTime = article.GetEstimatedReadingTime();

            // Assert
            readingTime.Should().Be(5); // 1000 / 200 = 5
        }

        [Fact]
        public void GetEstimatedReadingTime_ShouldRoundUp()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.WordCount = 250;

            // Act
            var readingTime = article.GetEstimatedReadingTime();

            // Assert
            readingTime.Should().Be(2); // 250 / 200 = 1.25, rounded up to 2
        }

        [Fact]
        public void GetEstimatedReadingTime_WithNullWordCount_ShouldReturnNull()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.WordCount = null;

            // Act
            var readingTime = article.GetEstimatedReadingTime();

            // Assert
            readingTime.Should().BeNull();
        }

        [Fact]
        public void GetEstimatedReadingTime_WithZeroWordCount_ShouldReturnNull()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            article.WordCount = 0;

            // Act
            var readingTime = article.GetEstimatedReadingTime();

            // Assert
            readingTime.Should().BeNull();
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_HighlightsCanBeAddedAndRetrieved()
        {
            // Arrange
            var article = TestDataFactory.CreateArticle();
            var highlight = new Highlight { Id = Guid.NewGuid(), ReadwiseId = 1, Text = "Key insight" };

            // Act
            article.Highlights.Add(highlight);

            // Assert
            article.Highlights.Should().ContainSingle().Which.Text.Should().Be("Key insight");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var article = TestDataFactory.CreateArticle();

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(article);
            article.MediaType.Should().Be(MediaType.Article);
        }

        #endregion
    }
}
