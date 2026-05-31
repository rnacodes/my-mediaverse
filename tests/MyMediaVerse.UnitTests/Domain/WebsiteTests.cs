using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class WebsiteTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var website = new Website { Title = "" };

            // Assert
            website.Id.Should().NotBeEmpty();
            website.Title.Should().Be("");
            website.RssFeedUrl.Should().BeNull();
            website.LastCheckedDate.Should().BeNull();
            website.Domain.Should().BeNull();
            website.Author.Should().BeNull();
            website.Publication.Should().BeNull();
            website.ArchiveUrl.Should().BeNull();
            website.ArchivedAt.Should().BeNull();
            website.ArchiveStatus.Should().BeNull();
            website.WaybackUrl.Should().BeNull();
            website.Topics.Should().NotBeNull().And.BeEmpty();
            website.Genres.Should().NotBeNull().And.BeEmpty();
            website.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite();
            var testDate = DateTime.UtcNow;

            // Act
            website.Title = "Hacker News";
            website.Link = "https://news.ycombinator.com";
            website.Domain = "news.ycombinator.com";
            website.RssFeedUrl = "https://news.ycombinator.com/rss";
            website.LastCheckedDate = testDate;
            website.Author = "Y Combinator";
            website.Publication = "Hacker News";
            website.ArchiveUrl = "https://web.archive.org/web/20240101/https://news.ycombinator.com";
            website.ArchivedAt = testDate;
            website.ArchiveStatus = "archived";
            website.WaybackUrl = "https://web.archive.org/web/20240101/https://news.ycombinator.com";

            // Assert
            website.Title.Should().Be("Hacker News");
            website.Link.Should().Be("https://news.ycombinator.com");
            website.Domain.Should().Be("news.ycombinator.com");
            website.RssFeedUrl.Should().Be("https://news.ycombinator.com/rss");
            website.LastCheckedDate.Should().Be(testDate);
            website.Author.Should().Be("Y Combinator");
            website.Publication.Should().Be("Hacker News");
            website.ArchiveUrl.Should().Be("https://web.archive.org/web/20240101/https://news.ycombinator.com");
            website.ArchivedAt.Should().Be(testDate);
            website.ArchiveStatus.Should().Be("archived");
            website.WaybackUrl.Should().Be("https://web.archive.org/web/20240101/https://news.ycombinator.com");
        }

        [Theory]
        [InlineData("pending")]
        [InlineData("archived")]
        [InlineData("failed")]
        public void ArchiveStatus_ShouldAcceptKnownValues(string status)
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite();

            // Act
            website.ArchiveStatus = status;

            // Assert
            website.ArchiveStatus.Should().Be(status);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_TopicsCanBeAddedAndRetrieved()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite();
            var topic = new Topic { Name = "technology" };

            // Act
            website.Topics.Add(topic);

            // Assert
            website.Topics.Should().ContainSingle().Which.Name.Should().Be("technology");
        }

        [Fact]
        public void NavigationProperties_GenresCanBeAddedAndRetrieved()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite();
            var genre = new Genre { Name = "news" };

            // Act
            website.Genres.Add(genre);

            // Assert
            website.Genres.Should().ContainSingle().Which.Name.Should().Be("news");
        }

        [Fact]
        public void NavigationProperties_MixlistsCanBeAddedAndRetrieved()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite();
            var mixlist = new Mixlist { Name = "Tech Sites" };

            // Act
            website.Mixlists.Add(mixlist);

            // Assert
            website.Mixlists.Should().ContainSingle().Which.Name.Should().Be("Tech Sites");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var website = TestDataFactory.CreateWebsite();

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(website);
            website.MediaType.Should().Be(MediaType.Website);
        }

        #endregion
    }
}
