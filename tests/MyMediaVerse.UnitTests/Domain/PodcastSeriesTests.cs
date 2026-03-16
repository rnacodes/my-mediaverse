using FluentAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    public class PodcastSeriesTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var series = new PodcastSeries { Title = "" };

            // Assert
            series.Id.Should().NotBeEmpty();
            series.Title.Should().Be("");
            series.Publisher.Should().BeNull();
            series.ExternalId.Should().BeNull();
            series.IsSubscribed.Should().BeFalse();
            series.LastSyncDate.Should().BeNull();
            series.TotalEpisodes.Should().Be(0);
            series.Episodes.Should().NotBeNull().And.BeEmpty();
            series.Topics.Should().NotBeNull().And.BeEmpty();
            series.Genres.Should().NotBeNull().And.BeEmpty();
            series.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var series = new PodcastSeries { Title = "" };
            var testDate = DateTime.UtcNow;

            // Act
            series.Title = "The Daily";
            series.Description = "Daily news podcast from The New York Times";
            series.Publisher = "The New York Times";
            series.ExternalId = "ln_abc123";
            series.IsSubscribed = true;
            series.LastSyncDate = testDate;
            series.TotalEpisodes = 2500;
            series.Thumbnail = "https://example.com/daily.jpg";
            series.Link = "https://www.nytimes.com/the-daily";

            // Assert
            series.Title.Should().Be("The Daily");
            series.Description.Should().Be("Daily news podcast from The New York Times");
            series.Publisher.Should().Be("The New York Times");
            series.ExternalId.Should().Be("ln_abc123");
            series.IsSubscribed.Should().BeTrue();
            series.LastSyncDate.Should().Be(testDate);
            series.TotalEpisodes.Should().Be(2500);
            series.Thumbnail.Should().Be("https://example.com/daily.jpg");
            series.Link.Should().Be("https://www.nytimes.com/the-daily");
        }

        #endregion

        #region EpisodeCount Tests

        [Fact]
        public void EpisodeCount_WithNoEpisodes_ShouldReturnZero()
        {
            // Arrange
            var series = new PodcastSeries { Title = "Test" };

            // Act & Assert
            series.EpisodeCount.Should().Be(0);
        }

        [Fact]
        public void EpisodeCount_WithEpisodes_ShouldReturnCount()
        {
            // Arrange
            var series = new PodcastSeries { Title = "Test" };
            series.Episodes.Add(new PodcastEpisode { Title = "Ep 1", SeriesId = series.Id });
            series.Episodes.Add(new PodcastEpisode { Title = "Ep 2", SeriesId = series.Id });
            series.Episodes.Add(new PodcastEpisode { Title = "Ep 3", SeriesId = series.Id });

            // Act & Assert
            series.EpisodeCount.Should().Be(3);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_EpisodesCanBeAddedAndRetrieved()
        {
            // Arrange
            var series = new PodcastSeries { Title = "Tech Talk" };
            var episode = new PodcastEpisode { Title = "Episode 1", SeriesId = series.Id };

            // Act
            series.Episodes.Add(episode);

            // Assert
            series.Episodes.Should().ContainSingle().Which.Title.Should().Be("Episode 1");
        }

        [Fact]
        public void NavigationProperties_TopicsCanBeAddedAndRetrieved()
        {
            // Arrange
            var series = new PodcastSeries { Title = "Tech Talk" };
            var topic = new Topic { Name = "technology" };

            // Act
            series.Topics.Add(topic);

            // Assert
            series.Topics.Should().ContainSingle().Which.Name.Should().Be("technology");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var series = new PodcastSeries { Title = "Test" };

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(series);
        }

        #endregion
    }
}
