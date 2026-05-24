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
    [Trait("Category", "Unit")]
    public partial class PodcastServiceTests : InMemoryDbTestBase
    {
        private readonly IListenNotesApiClient _mockListenNotesApiClient;
        private readonly IPodcastMappingService _mockPodcastMappingService;
        private readonly ILogger<PodcastService> _mockLogger;
        private readonly PodcastService _service;

        public PodcastServiceTests()
        {
            _mockListenNotesApiClient = Substitute.For<IListenNotesApiClient>();
            _mockPodcastMappingService = Substitute.For<IPodcastMappingService>();
            _mockLogger = Substitute.For<ILogger<PodcastService>>();
            _service = new PodcastService(Context, _mockListenNotesApiClient, 
                _mockPodcastMappingService, _mockLogger);
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
    }
}
