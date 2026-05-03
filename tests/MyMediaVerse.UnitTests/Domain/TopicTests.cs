using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class TopicTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var topic = new Topic { Name = "" };

            // Assert
            topic.Id.Should().NotBeEmpty();
            topic.Name.Should().Be("");
            topic.MediaItems.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Name_CanBeSetAndRetrieved()
        {
            // Arrange
            var topic = TestDataFactory.CreateTopic();

            // Act
            topic.Name = "machine learning";

            // Assert
            topic.Name.Should().Be("machine learning");
        }

        [Fact]
        public void Name_ShouldFollowLowercaseConvention()
        {
            // Arrange & Act - per project standards, topics should be lowercase
            var topic = TestDataFactory.CreateTopic("artificial intelligence");

            // Assert
            topic.Name.Should().Be("artificial intelligence");
            topic.Name.Should().Be(topic.Name.ToLower());
        }

        [Fact]
        public void Id_ShouldBeUniqueAcrossInstances()
        {
            // Arrange & Act
            var topic1 = TestDataFactory.CreateTopic("philosophy");
            var topic2 = TestDataFactory.CreateTopic("psychology");

            // Assert
            topic1.Id.Should().NotBe(topic2.Id);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void MediaItems_CanContainMultipleMediaTypes()
        {
            // Arrange
            var topic = TestDataFactory.CreateTopic("history");
            var movie = TestDataFactory.CreateMovie("Schindler's List");
            var book = TestDataFactory.CreateBook("Sapiens");

            // Act
            topic.MediaItems.Add(movie);
            topic.MediaItems.Add(book);

            // Assert
            topic.MediaItems.Should().HaveCount(2);
        }

        [Fact]
        public void MediaItems_InitializedAsEmptyCollection()
        {
            // Arrange & Act
            var topic = new Topic { Name = "test" };

            // Assert
            topic.MediaItems.Should().NotBeNull().And.BeEmpty();
        }

        #endregion
    }
}
