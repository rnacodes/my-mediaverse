using FluentAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class PodcastTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var podcast = TestDataFactory.CreatePodcastSeries();

            // Assert
            podcast.PodcastType.Should().Be(PodcastType.Series);
            podcast.DurationInSeconds.Should().Be(0);
            podcast.Episodes.Should().NotBeNull().And.BeEmpty();
            podcast.ParentPodcastId.Should().BeNull();
            podcast.ParentPodcast.Should().BeNull();
        }

        [Fact]
        public void IsSeries_ShouldReturnTrue_WhenPodcastTypeIsSeries()
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastSeries();

            // Act & Assert
            podcast.IsSeries.Should().BeTrue();
        }

        [Fact]
        public void IsSeries_ShouldReturnFalse_WhenPodcastTypeIsEpisode()
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastEpisode();

            // Act & Assert
            podcast.IsSeries.Should().BeFalse();
        }

        [Fact]
        public void IsEpisode_ShouldReturnTrue_WhenPodcastTypeIsEpisode()
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastEpisode();

            // Act & Assert
            podcast.IsEpisode.Should().BeTrue();
        }

        [Fact]
        public void IsEpisode_ShouldReturnFalse_WhenPodcastTypeIsSeries()
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastSeries();

            // Act & Assert
            podcast.IsEpisode.Should().BeFalse();
        }

        [Fact]
        public void GetEffectiveThumbnail_ShouldReturnPodcastThumbnail_WhenThumbnailIsSet()
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastSeries();
            var expectedThumbnail = "https://example.com/podcast-thumbnail.jpg";
            podcast.Thumbnail = expectedThumbnail;

            // Act
            var result = podcast.GetEffectiveThumbnail();

            // Assert
            result.Should().Be(expectedThumbnail);
        }

        [Fact]
        public void GetEffectiveThumbnail_ShouldReturnParentThumbnail_WhenPodcastThumbnailIsNull()
        {
            // Arrange
            var parentPodcast = TestDataFactory.CreatePodcastSeries();
            var expectedThumbnail = "https://example.com/parent-thumbnail.jpg";
            parentPodcast.Thumbnail = expectedThumbnail;

            var episode = TestDataFactory.CreatePodcastEpisode();
            episode.ParentPodcast = parentPodcast;
            episode.Thumbnail = null;

            // Act
            var result = episode.GetEffectiveThumbnail();

            // Assert
            result.Should().Be(expectedThumbnail);
        }

        [Fact]
        public void GetEffectiveThumbnail_ShouldReturnNull_WhenBothThumbnailsAreNull()
        {
            // Arrange
            var episode = TestDataFactory.CreatePodcastEpisode();
            episode.Thumbnail = null;
            episode.ParentPodcast = null;

            // Act
            var result = episode.GetEffectiveThumbnail();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetEffectiveThumbnail_ShouldReturnNull_WhenParentThumbnailIsNull()
        {
            // Arrange
            var parentPodcast = TestDataFactory.CreatePodcastSeries();
            parentPodcast.Thumbnail = null;

            var episode = TestDataFactory.CreatePodcastEpisode();
            episode.ParentPodcast = parentPodcast;
            episode.Thumbnail = null;

            // Act
            var result = episode.GetEffectiveThumbnail();

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(60)]
        [InlineData(3600)]
        [InlineData(7200)]
        public void DurationInSeconds_ShouldAcceptValidValues(int duration)
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastEpisode();

            // Act
            podcast.DurationInSeconds = duration;

            // Assert
            podcast.DurationInSeconds.Should().Be(duration);
        }

        [Fact]
        public void Episodes_ShouldBeInitializedAsEmptyCollection()
        {
            // Arrange & Act
            var podcast = TestDataFactory.CreatePodcastSeries();

            // Assert
            podcast.Episodes.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void ParentPodcastId_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var podcast = TestDataFactory.CreatePodcastSeries();

            // Assert
            podcast.ParentPodcastId.Should().BeNull();
        }

        [Fact]
        public void ParentPodcastId_ShouldAcceptValidGuid()
        {
            // Arrange
            var podcast = TestDataFactory.CreatePodcastEpisode();
            var parentId = Guid.NewGuid();

            // Act
            podcast.ParentPodcastId = parentId;

            // Assert
            podcast.ParentPodcastId.Should().Be(parentId);
        }
    }
}
