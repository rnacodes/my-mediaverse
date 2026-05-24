using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class BaseMediaItemTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var mediaItem = TestDataFactory.CreateBook();

            // Assert
            mediaItem.Id.Should().NotBeEmpty();
            mediaItem.DateAdded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            mediaItem.Status.Should().Be(Status.Uncharted);
            mediaItem.Topics.Should().NotBeNull().And.BeEmpty();
            mediaItem.Genres.Should().NotBeNull().And.BeEmpty();
            mediaItem.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        [Theory]
        [InlineData("Valid Title")]
        [InlineData("Another Valid Title")]
        public void Title_ShouldAcceptValidValues(string title)
        {
            // Arrange
            var mediaItem = TestDataFactory.CreateBook();

            // Act
            mediaItem.Title = title;

            // Assert
            mediaItem.Title.Should().Be(title);
        }

        [Theory]
        [InlineData(Status.Uncharted)]
        [InlineData(Status.ActivelyExploring)]
        [InlineData(Status.Completed)]
        [InlineData(Status.Abandoned)]
        public void Status_ShouldAcceptAllValidValues(Status status)
        {
            // Arrange
            var mediaItem = TestDataFactory.CreateBook();

            // Act
            mediaItem.Status = status;

            // Assert
            mediaItem.Status.Should().Be(status);
        }

        [Theory]
        [InlineData(Rating.SuperLike)]
        [InlineData(Rating.Like)]
        [InlineData(Rating.Neutral)]
        [InlineData(Rating.Dislike)]
        public void Rating_ShouldAcceptAllValidValues(Rating rating)
        {
            // Arrange
            var mediaItem = TestDataFactory.CreateBook();

            // Act
            mediaItem.Rating = rating;

            // Assert
            mediaItem.Rating.Should().Be(rating);
        }

        [Theory]
        [InlineData(OwnershipStatus.Own)]
        [InlineData(OwnershipStatus.Rented)]
        [InlineData(OwnershipStatus.Streamed)]
        public void OwnershipStatus_ShouldAcceptAllValidValues(OwnershipStatus ownershipStatus)
        {
            // Arrange
            var mediaItem = TestDataFactory.CreateBook();

            // Act
            mediaItem.OwnershipStatus = ownershipStatus;

            // Assert
            mediaItem.OwnershipStatus.Should().Be(ownershipStatus);
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("https://www.example.com/path")]
        [InlineData("http://localhost:3000")]
        public void Link_ShouldAcceptValidUrls(string url)
        {
            // Arrange
            var mediaItem = TestDataFactory.CreateBook();

            // Act
            mediaItem.Link = url;

            // Assert
            mediaItem.Link.Should().Be(url);
        }

        [Fact]
        public void DateCompleted_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var mediaItem = TestDataFactory.CreateBook();

            // Assert
            mediaItem.DateCompleted.Should().BeNull();
        }

        [Fact]
        public void DateCompleted_ShouldAcceptValidDateTime()
        {
            // Arrange
            var mediaItem = TestDataFactory.CreateBook();
            var completionDate = DateTime.UtcNow.AddDays(-1);

            // Act
            mediaItem.DateCompleted = completionDate;

            // Assert
            mediaItem.DateCompleted.Should().Be(completionDate);
        }

        [Fact]
        public void Topics_ShouldBeInitializedAsEmptyCollection()
        {
            // Arrange & Act
            var mediaItem = TestDataFactory.CreateBook();

            // Assert
            mediaItem.Topics.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Genres_ShouldBeInitializedAsEmptyCollection()
        {
            // Arrange & Act
            var mediaItem = TestDataFactory.CreateBook();

            // Assert
            mediaItem.Genres.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Mixlists_ShouldBeInitializedAsEmptyCollection()
        {
            // Arrange & Act
            var mediaItem = TestDataFactory.CreateBook();

            // Assert
            mediaItem.Mixlists.Should().NotBeNull().And.BeEmpty();
        }
    }
}
