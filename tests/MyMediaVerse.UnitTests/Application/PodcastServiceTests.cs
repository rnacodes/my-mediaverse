using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class PodcastServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<IListenNotesApiClient> _mockListenNotesApiClient;
        private readonly Mock<IPodcastMappingService> _mockPodcastMappingService;
        private readonly Mock<ILogger<PodcastService>> _mockLogger;
        private readonly PodcastService _service;

        public PodcastServiceTests()
        {
            _mockListenNotesApiClient = new Mock<IListenNotesApiClient>();
            _mockPodcastMappingService = new Mock<IPodcastMappingService>();
            _mockLogger = new Mock<ILogger<PodcastService>>();
            _service = new PodcastService(Context, _mockListenNotesApiClient.Object, 
                _mockPodcastMappingService.Object, _mockLogger.Object);
        }

        #region PodcastSeries Tests

        [Fact]
        public async Task GetAllPodcastSeriesAsync_ShouldReturnAllSeries()
        {
            // Arrange
            var series = new List<PodcastSeries>
            {
                new PodcastSeries { Id = Guid.NewGuid(), Title = "Joe Rogan Experience", Publisher = "Joe Rogan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new PodcastSeries { Id = Guid.NewGuid(), Title = "Tim Ferriss Show", Publisher = "Tim Ferriss", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.PodcastSeries.AddRange(series);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllPodcastSeriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(s => s.Title).Should().Contain(new[] { "Joe Rogan Experience", "Tim Ferriss Show" });
        }

        [Fact]
        public async Task GetPodcastSeriesByIdAsync_ShouldReturnSeries_WhenSeriesExists()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var series = new PodcastSeries 
            { 
                Id = seriesId, 
                Title = "Joe Rogan Experience", 
                Publisher = "Joe Rogan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>(),
                Episodes = new List<PodcastEpisode>()
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetPodcastSeriesByIdAsync(seriesId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(seriesId);
            result.Title.Should().Be("Joe Rogan Experience");
            result.Publisher.Should().Be("Joe Rogan");
        }

        [Fact]
        public async Task GetPodcastSeriesByIdAsync_ShouldReturnNull_WhenSeriesDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.GetPodcastSeriesByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact(Skip = "ILike is PostgreSQL-specific and not supported in InMemory database. Test in integration tests instead.")]
        public async Task SearchPodcastSeriesAsync_ShouldReturnMatchingSeries()
        {
            // Arrange
            var series = new List<PodcastSeries>
            {
                new PodcastSeries { Id = Guid.NewGuid(), Title = "Joe Rogan Experience", Publisher = "Joe Rogan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new PodcastSeries { Id = Guid.NewGuid(), Title = "Tim Ferriss Show", Publisher = "Tim Ferriss", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.PodcastSeries.AddRange(series);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.SearchPodcastSeriesAsync("Rogan");

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Joe Rogan Experience");
        }

        [Fact]
        public async Task CreatePodcastSeriesAsync_ShouldCreateNewSeries()
        {
            // Arrange
            var dto = new CreatePodcastSeriesDto
            {
                Title = "Joe Rogan Experience",
                Publisher = "Joe Rogan",
                Status = Status.Uncharted,
                IsSubscribed = true,
                Topics = new[] { "comedy", "interview" },
                Genres = new[] { "talk", "entertainment" }
            };

            // Act
            var result = await _service.CreatePodcastSeriesAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Joe Rogan Experience");
            result.Publisher.Should().Be("Joe Rogan");
            result.IsSubscribed.Should().BeTrue();
            result.MediaType.Should().Be(MediaType.Podcast);
            result.Topics.Should().HaveCount(2);
            result.Genres.Should().HaveCount(2);

            // Verify saved to database
            var savedSeries = await Context.PodcastSeries.FindAsync(result.Id);
            savedSeries.Should().NotBeNull();
        }

        [Fact]
        public async Task DeletePodcastSeriesAsync_ShouldReturnTrue_WhenSeriesExists()
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

            // Act
            var result = await _service.DeletePodcastSeriesAsync(seriesId);

            // Assert
            result.Should().BeTrue();

            // Verify deleted from database
            var deletedSeries = await Context.PodcastSeries.FindAsync(seriesId);
            deletedSeries.Should().BeNull();
        }

        [Fact]
        public async Task DeletePodcastSeriesAsync_ShouldReturnFalse_WhenSeriesDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.DeletePodcastSeriesAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task PodcastSeriesExistsAsync_ShouldReturnTrue_WhenSeriesExists()
        {
            // Arrange
            var series = new PodcastSeries 
            { 
                Title = "Joe Rogan Experience", 
                Publisher = "Joe Rogan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.PodcastSeriesExistsAsync("Joe Rogan Experience", "Joe Rogan");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task PodcastSeriesExistsAsync_ShouldReturnFalse_WhenSeriesDoesNotExist()
        {
            // Act
            var result = await _service.PodcastSeriesExistsAsync("Non-existent Podcast", "Unknown");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetPodcastSeriesByTitleAsync_ShouldReturnSeries_WhenSeriesExists()
        {
            // Arrange
            var series = new PodcastSeries 
            { 
                Title = "Joe Rogan Experience", 
                Publisher = "Joe Rogan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetPodcastSeriesByTitleAsync("Joe Rogan Experience", "Joe Rogan");

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Joe Rogan Experience");
            result.Publisher.Should().Be("Joe Rogan");
        }

        #endregion

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

