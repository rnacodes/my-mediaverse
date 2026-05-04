using FluentAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class MixlistTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var mixlist = new Mixlist { Name = "" };

            // Assert
            mixlist.Id.Should().NotBeEmpty();
            mixlist.Name.Should().Be("");
            mixlist.Description.Should().BeNull();
            mixlist.Thumbnail.Should().BeNull();
            mixlist.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            mixlist.MediaItems.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var mixlist = TestDataFactory.CreateMixlist();
            var testDate = DateTime.UtcNow;

            // Act
            mixlist.Name = "Weekend Watchlist";
            mixlist.Description = "Movies and shows for the weekend";
            mixlist.Thumbnail = "https://example.com/watchlist.jpg";
            mixlist.DateCreated = testDate;

            // Assert
            mixlist.Name.Should().Be("Weekend Watchlist");
            mixlist.Description.Should().Be("Movies and shows for the weekend");
            mixlist.Thumbnail.Should().Be("https://example.com/watchlist.jpg");
            mixlist.DateCreated.Should().Be(testDate);
        }

        [Fact]
        public void Id_ShouldBeUniqueAcrossInstances()
        {
            // Arrange & Act
            var mixlist1 = TestDataFactory.CreateMixlist("List 1");
            var mixlist2 = TestDataFactory.CreateMixlist("List 2");

            // Assert
            mixlist1.Id.Should().NotBe(mixlist2.Id);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void MediaItems_CanContainMultipleMediaTypes()
        {
            // Arrange
            var mixlist = TestDataFactory.CreateMixlist("Mixed Content");
            var movie = TestDataFactory.CreateMovie("Inception");
            var book = TestDataFactory.CreateBook("Dune");
            var website = TestDataFactory.CreateWebsite("Tech Blog");

            // Act
            mixlist.MediaItems.Add(movie);
            mixlist.MediaItems.Add(book);
            mixlist.MediaItems.Add(website);

            // Assert
            mixlist.MediaItems.Should().HaveCount(3);
            mixlist.MediaItems.Select(m => m.MediaType).Should().Contain(new[]
            {
                MediaType.Movie, MediaType.Book, MediaType.Website
            });
        }

        [Fact]
        public void MediaItems_InitializedAsEmptyCollection()
        {
            // Arrange & Act
            var mixlist = new Mixlist { Name = "Empty List" };

            // Assert
            mixlist.MediaItems.Should().NotBeNull().And.BeEmpty();
        }

        #endregion
    }
}
