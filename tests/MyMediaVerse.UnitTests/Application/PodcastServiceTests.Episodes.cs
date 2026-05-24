using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public partial class PodcastServiceTests
    {
        #region PodcastEpisode Tests

        [Fact]
        public async Task GetEpisodesBySeriesIdAsync_ShouldReturnEpisodesBySeries()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries 
            { 
                Id = seriesId, 
                Title = "Joe Rogan Experience", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var episodes = new List<PodcastEpisode>
            {
                new PodcastEpisode { Id = Guid.NewGuid(), Title = "Episode 1", SeriesId = seriesId, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new PodcastEpisode { Id = Guid.NewGuid(), Title = "Episode 2", SeriesId = seriesId, Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.PodcastEpisodes.AddRange(episodes);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetEpisodesBySeriesIdAsync(seriesId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(e => e.SeriesId == seriesId);
        }

        [Fact]
        public async Task GetPodcastEpisodeByIdAsync_ShouldReturnEpisode_WhenEpisodeExists()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries 
            { 
                Id = seriesId, 
                Title = "Joe Rogan Experience", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var episodeId = Guid.NewGuid();
            var episode = new PodcastEpisode 
            { 
                Id = episodeId, 
                Title = "Episode 1", 
                SeriesId = seriesId,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastEpisodes.Add(episode);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetPodcastEpisodeByIdAsync(episodeId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(episodeId);
            result.Title.Should().Be("Episode 1");
            result.SeriesId.Should().Be(seriesId);
        }

        [Fact]
        public async Task GetAllPodcastEpisodesAsync_ShouldReturnAllEpisodes()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries 
            { 
                Id = seriesId, 
                Title = "Joe Rogan Experience", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var episodes = new List<PodcastEpisode>
            {
                new PodcastEpisode { Id = Guid.NewGuid(), Title = "Episode 1", SeriesId = seriesId, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new PodcastEpisode { Id = Guid.NewGuid(), Title = "Episode 2", SeriesId = seriesId, Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.PodcastEpisodes.AddRange(episodes);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllPodcastEpisodesAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreatePodcastEpisodeAsync_ShouldCreateNewEpisode()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries 
            { 
                Id = seriesId, 
                Title = "Joe Rogan Experience", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 1",
                SeriesId = seriesId,
                Status = Status.Uncharted,
                AudioLink = "https://example.com/episode1.mp3",
                Topics = new[] { "comedy", "interview" },
                Genres = new[] { "talk" }
            };

            // Act
            var result = await _service.CreatePodcastEpisodeAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Episode 1");
            result.SeriesId.Should().Be(seriesId);
            result.AudioLink.Should().Be("https://example.com/episode1.mp3");
            result.MediaType.Should().Be(MediaType.Podcast);
            result.Topics.Should().HaveCount(2);
            result.Genres.Should().HaveCount(1);

            // Verify saved to database
            var savedEpisode = await Context.PodcastEpisodes.FindAsync(result.Id);
            savedEpisode.Should().NotBeNull();
        }

        [Fact]
        public async Task CreatePodcastEpisodeAsync_ShouldThrowArgumentException_WhenParentSeriesDoesNotExist()
        {
            // Arrange
            var nonExistentSeriesId = Guid.NewGuid();
            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 1",
                SeriesId = nonExistentSeriesId,
                Status = Status.Uncharted
            };

            // Act & Assert
            await _service.Invoking(s => s.CreatePodcastEpisodeAsync(dto))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Parent podcast series with ID {nonExistentSeriesId} not found.");
        }

        [Fact]
        public async Task DeletePodcastEpisodeAsync_ShouldReturnTrue_WhenEpisodeExists()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries 
            { 
                Id = seriesId, 
                Title = "Joe Rogan Experience", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var episodeId = Guid.NewGuid();
            var episode = new PodcastEpisode 
            { 
                Id = episodeId, 
                Title = "Episode 1", 
                SeriesId = seriesId,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastEpisodes.Add(episode);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeletePodcastEpisodeAsync(episodeId);

            // Assert
            result.Should().BeTrue();

            // Verify deleted from database
            var deletedEpisode = await Context.PodcastEpisodes.FindAsync(episodeId);
            deletedEpisode.Should().BeNull();
        }

        [Fact]
        public async Task DeletePodcastEpisodeAsync_ShouldReturnFalse_WhenEpisodeDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.DeletePodcastEpisodeAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Topic/Genre Inheritance Tests

        [Fact]
        public async Task CreatePodcastEpisodeAsync_WithNoTopicsOrGenres_ShouldInheritFromParentSeries()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var seriesTopic = new Topic { Name = "technology" };
            var seriesGenre = new Genre { Name = "educational" };
            Context.Topics.Add(seriesTopic);
            Context.Genres.Add(seriesGenre);
            await Context.SaveChangesAsync();

            var series = new PodcastSeries
            {
                Id = seriesId,
                Title = "Tech Podcast",
                Topics = new List<Topic> { seriesTopic },
                Genres = new List<Genre> { seriesGenre }
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 1",
                SeriesId = seriesId,
                Status = Status.Uncharted
                // No topics or genres provided
            };

            // Act
            var result = await _service.CreatePodcastEpisodeAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("technology");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("educational");
        }

        [Fact]
        public async Task CreatePodcastEpisodeAsync_WithExplicitTopicsAndGenres_ShouldNotInheritFromParentSeries()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var seriesTopic = new Topic { Name = "technology" };
            var seriesGenre = new Genre { Name = "educational" };
            Context.Topics.Add(seriesTopic);
            Context.Genres.Add(seriesGenre);
            await Context.SaveChangesAsync();

            var series = new PodcastSeries
            {
                Id = seriesId,
                Title = "Tech Podcast",
                Topics = new List<Topic> { seriesTopic },
                Genres = new List<Genre> { seriesGenre }
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 1",
                SeriesId = seriesId,
                Status = Status.Uncharted,
                Topics = new[] { "science", "biology" },
                Genres = new[] { "documentary" }
            };

            // Act
            var result = await _service.CreatePodcastEpisodeAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(2);
            result.Topics.Select(t => t.Name).Should().Contain(new[] { "science", "biology" });
            result.Topics.Select(t => t.Name).Should().NotContain("technology");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("documentary");
        }

        [Fact]
        public async Task CreatePodcastEpisodeAsync_WhenParentSeriesHasNoTopicsOrGenres_ShouldCreateWithEmptyCollections()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries
            {
                Id = seriesId,
                Title = "Empty Podcast",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 1",
                SeriesId = seriesId,
                Status = Status.Uncharted
            };

            // Act
            var result = await _service.CreatePodcastEpisodeAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().BeEmpty();
            result.Genres.Should().BeEmpty();
        }

        #endregion
    }
}
