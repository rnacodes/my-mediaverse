using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class GenreTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var genre = new Genre { Name = "" };

            // Assert
            genre.Id.Should().NotBeEmpty();
            genre.Name.Should().Be("");
            genre.MediaItems.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Name_CanBeSetAndRetrieved()
        {
            // Arrange
            var genre = TestDataFactory.CreateGenre();

            // Act
            genre.Name = "science fiction";

            // Assert
            genre.Name.Should().Be("science fiction");
        }

        [Fact]
        public void Name_ShouldFollowLowercaseConvention()
        {
            // Arrange & Act - per project standards, genres should be lowercase
            var genre = TestDataFactory.CreateGenre("thriller");

            // Assert
            genre.Name.Should().Be("thriller");
            genre.Name.Should().Be(genre.Name.ToLower());
        }

        [Fact]
        public void Id_ShouldBeUniqueAcrossInstances()
        {
            // Arrange & Act
            var genre1 = TestDataFactory.CreateGenre("fiction");
            var genre2 = TestDataFactory.CreateGenre("non-fiction");

            // Assert
            genre1.Id.Should().NotBe(genre2.Id);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void MediaItems_CanContainMultipleMediaTypes()
        {
            // Arrange
            var genre = TestDataFactory.CreateGenre("drama");
            var movie = TestDataFactory.CreateMovie("The Shawshank Redemption");
            var tvShow = TestDataFactory.CreateTvShow("Breaking Bad");
            var book = TestDataFactory.CreateBook("To Kill a Mockingbird");

            // Act
            genre.MediaItems.Add(movie);
            genre.MediaItems.Add(tvShow);
            genre.MediaItems.Add(book);

            // Assert
            genre.MediaItems.Should().HaveCount(3);
            genre.MediaItems.Select(m => m.Title).Should().Contain(new[]
            {
                "The Shawshank Redemption", "Breaking Bad", "To Kill a Mockingbird"
            });
        }

        [Fact]
        public void MediaItems_InitializedAsEmptyCollection()
        {
            // Arrange & Act
            var genre = new Genre { Name = "test" };

            // Assert
            genre.MediaItems.Should().NotBeNull().And.BeEmpty();
        }

        #endregion
    }
}
