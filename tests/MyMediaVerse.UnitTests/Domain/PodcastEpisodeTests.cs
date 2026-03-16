using FluentAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    public class PodcastEpisodeTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var episode = new PodcastEpisode { Title = "", SeriesId = Guid.NewGuid() };

            // Assert
            episode.Id.Should().NotBeEmpty();
            episode.Title.Should().Be("");
            episode.AudioLink.Should().BeNull();
            episode.ReleaseDate.Should().BeNull();
            episode.DurationInSeconds.Should().Be(0);
            episode.EpisodeNumber.Should().BeNull();
            episode.SeasonNumber.Should().BeNull();
            episode.ExternalId.Should().BeNull();
            episode.Publisher.Should().BeNull();
            episode.Series.Should().BeNull();
            episode.Topics.Should().NotBeNull().And.BeEmpty();
            episode.Genres.Should().NotBeNull().And.BeEmpty();
            episode.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var episode = new PodcastEpisode { Title = "", SeriesId = seriesId };
            var testDate = DateTime.UtcNow;

            // Act
            episode.Title = "The Rise of AI";
            episode.AudioLink = "https://example.com/audio/episode1.mp3";
            episode.ReleaseDate = testDate;
            episode.DurationInSeconds = 3600;
            episode.EpisodeNumber = 42;
            episode.SeasonNumber = 3;
            episode.ExternalId = "ln_ep_abc123";
            episode.Publisher = "Tech Daily";

            // Assert
            episode.Title.Should().Be("The Rise of AI");
            episode.SeriesId.Should().Be(seriesId);
            episode.AudioLink.Should().Be("https://example.com/audio/episode1.mp3");
            episode.ReleaseDate.Should().Be(testDate);
            episode.DurationInSeconds.Should().Be(3600);
            episode.EpisodeNumber.Should().Be(42);
            episode.SeasonNumber.Should().Be(3);
            episode.ExternalId.Should().Be("ln_ep_abc123");
            episode.Publisher.Should().Be("Tech Daily");
        }

        #endregion

        #region GetEffectiveThumbnail Tests

        [Fact]
        public void GetEffectiveThumbnail_WithOwnThumbnail_ShouldReturnOwn()
        {
            // Arrange
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = Guid.NewGuid(),
                Thumbnail = "https://example.com/episode.jpg"
            };

            // Act
            var thumbnail = episode.GetEffectiveThumbnail();

            // Assert
            thumbnail.Should().Be("https://example.com/episode.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WithNoThumbnail_ShouldFallBackToSeries()
        {
            // Arrange
            var series = new PodcastSeries
            {
                Title = "Test Series",
                Thumbnail = "https://example.com/series.jpg"
            };
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = series.Id,
                Series = series,
                Thumbnail = null
            };

            // Act
            var thumbnail = episode.GetEffectiveThumbnail();

            // Assert
            thumbnail.Should().Be("https://example.com/series.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WithNoThumbnailAndNoSeries_ShouldReturnNull()
        {
            // Arrange
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = Guid.NewGuid(),
                Thumbnail = null,
                Series = null
            };

            // Act
            var thumbnail = episode.GetEffectiveThumbnail();

            // Assert
            thumbnail.Should().BeNull();
        }

        [Fact]
        public void GetEffectiveThumbnail_WithEmptyThumbnail_ShouldFallBackToSeries()
        {
            // Arrange
            var series = new PodcastSeries
            {
                Title = "Test Series",
                Thumbnail = "https://example.com/series.jpg"
            };
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = series.Id,
                Series = series,
                Thumbnail = ""
            };

            // Act
            var thumbnail = episode.GetEffectiveThumbnail();

            // Assert
            thumbnail.Should().Be("https://example.com/series.jpg");
        }

        #endregion

        #region GetEpisodeIdentifier Tests

        [Fact]
        public void GetEpisodeIdentifier_WithSeasonAndEpisode_ShouldReturnFormatted()
        {
            // Arrange
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = Guid.NewGuid(),
                SeasonNumber = 2,
                EpisodeNumber = 5
            };

            // Act
            var identifier = episode.GetEpisodeIdentifier();

            // Assert
            identifier.Should().Be("S2E5");
        }

        [Fact]
        public void GetEpisodeIdentifier_WithEpisodeOnly_ShouldReturnEpisodeFormat()
        {
            // Arrange
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = Guid.NewGuid(),
                SeasonNumber = null,
                EpisodeNumber = 42
            };

            // Act
            var identifier = episode.GetEpisodeIdentifier();

            // Assert
            identifier.Should().Be("Episode 42");
        }

        [Fact]
        public void GetEpisodeIdentifier_WithNoNumbers_ShouldReturnEmpty()
        {
            // Arrange
            var episode = new PodcastEpisode
            {
                Title = "Test",
                SeriesId = Guid.NewGuid(),
                SeasonNumber = null,
                EpisodeNumber = null
            };

            // Act
            var identifier = episode.GetEpisodeIdentifier();

            // Assert
            identifier.Should().BeEmpty();
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void Series_CanBeSetAndRetrieved()
        {
            // Arrange
            var series = new PodcastSeries { Title = "My Podcast" };
            var episode = new PodcastEpisode { Title = "Ep 1", SeriesId = series.Id };

            // Act
            episode.Series = series;

            // Assert
            episode.Series.Should().NotBeNull();
            episode.Series!.Title.Should().Be("My Podcast");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var episode = new PodcastEpisode { Title = "Test", SeriesId = Guid.NewGuid() };

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(episode);
        }

        #endregion
    }
}
