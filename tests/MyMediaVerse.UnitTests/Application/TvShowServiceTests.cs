using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class TvShowServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<TvShowService> _mockLogger;
        private readonly TvShowService _service;

        public TvShowServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<TvShowService>>();
            _service = new TvShowService(Context, _mockLogger);
        }

        #region GetAllTvShowsAsync Tests

        [Fact]
        public async Task GetAllTvShowsAsync_ShouldReturnAllTvShows()
        {
            // Arrange
            var tvShows = new List<TvShow>
            {
                new TvShow { Id = Guid.NewGuid(), Title = "Breaking Bad", FirstAirYear = 2008, Creator = "Vince Gilligan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new TvShow { Id = Guid.NewGuid(), Title = "Game of Thrones", FirstAirYear = 2011, Creator = "David Benioff", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.TvShows.AddRange(tvShows);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllTvShowsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(t => t.Title).Should().Contain(new[] { "Breaking Bad", "Game of Thrones" });
        }

        [Fact]
        public async Task GetAllTvShowsAsync_ShouldReturnEmptyList_WhenNoTvShowsExist()
        {
            // Act
            var result = await _service.GetAllTvShowsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetTvShowByIdAsync Tests

        [Fact]
        public async Task GetTvShowByIdAsync_ShouldReturnTvShow_WhenTvShowExists()
        {
            // Arrange
            var tvShowId = Guid.NewGuid();
            var tvShow = new TvShow 
            { 
                Id = tvShowId, 
                Title = "Breaking Bad", 
                FirstAirYear = 2008,
                Creator = "Vince Gilligan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetTvShowByIdAsync(tvShowId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(tvShowId);
            result.Title.Should().Be("Breaking Bad");
            result.Creator.Should().Be("Vince Gilligan");
        }

        [Fact]
        public async Task GetTvShowByIdAsync_ShouldReturnNull_WhenTvShowDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.GetTvShowByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetTvShowsByCreatorAsync Tests

        [Fact(Skip = "ILike is PostgreSQL-specific and not supported in InMemory database. Test in integration tests instead.")]
        public async Task GetTvShowsByCreatorAsync_ShouldReturnTvShowsByCreator()
        {
            // Arrange
            var tvShows = new List<TvShow>
            {
                new TvShow { Id = Guid.NewGuid(), Title = "Breaking Bad", Creator = "Vince Gilligan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new TvShow { Id = Guid.NewGuid(), Title = "Better Call Saul", Creator = "Vince Gilligan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new TvShow { Id = Guid.NewGuid(), Title = "Game of Thrones", Creator = "David Benioff", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.TvShows.AddRange(tvShows);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetTvShowsByCreatorAsync("Vince Gilligan");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(t => t.Creator!.Contains("Vince Gilligan"));
        }

        [Fact(Skip = "ILike is PostgreSQL-specific and not supported in InMemory database. Test in integration tests instead.")]
        public async Task GetTvShowsByCreatorAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var tvShow = new TvShow 
            { 
                Id = Guid.NewGuid(), 
                Title = "Breaking Bad", 
                Creator = "Vince Gilligan", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetTvShowsByCreatorAsync("vince gilligan");

            // Assert
            result.Should().HaveCount(1);
            result.First().Creator.Should().Be("Vince Gilligan");
        }

        #endregion

        #region GetTvShowsByYearAsync Tests

        [Fact]
        public async Task GetTvShowsByYearAsync_ShouldReturnTvShowsByYear()
        {
            // Arrange
            var tvShows = new List<TvShow>
            {
                new TvShow { Id = Guid.NewGuid(), Title = "Breaking Bad", FirstAirYear = 2008, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new TvShow { Id = Guid.NewGuid(), Title = "Fringe", FirstAirYear = 2008, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new TvShow { Id = Guid.NewGuid(), Title = "Game of Thrones", FirstAirYear = 2011, Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.TvShows.AddRange(tvShows);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetTvShowsByYearAsync(2008);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(t => t.FirstAirYear == 2008);
        }

        #endregion

        #region CreateTvShowAsync Tests

        [Fact]
        public async Task CreateTvShowAsync_ShouldCreateNewTvShow_WhenTvShowDoesNotExist()
        {
            // Arrange
            var dto = new CreateTvShowDto
            {
                Title = "Breaking Bad",
                FirstAirYear = 2008,
                Creator = "Vince Gilligan",
                Status = Status.Uncharted,
                NumberOfSeasons = 5,
                Topics = new[] { "crime", "drama" },
                Genres = new[] { "thriller", "drama" }
            };

            // Act
            var result = await _service.CreateTvShowAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Breaking Bad");
            result.Creator.Should().Be("Vince Gilligan");
            result.FirstAirYear.Should().Be(2008);
            result.NumberOfSeasons.Should().Be(5);
            result.MediaType.Should().Be(MediaType.TVShow);
            result.Topics.Should().HaveCount(2);
            result.Genres.Should().HaveCount(2);

            // Verify saved to database
            var savedTvShow = await Context.TvShows.FindAsync(result.Id);
            savedTvShow.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTvShowAsync_ShouldThrowArgumentNullException_WhenDtoIsNull()
        {
            // Act & Assert
            await _service.Invoking(s => s.CreateTvShowAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithMessage("*TV show data is required*");
        }

        [Fact]
        public async Task CreateTvShowAsync_ShouldReturnExistingTvShow_WhenTvShowAlreadyExists()
        {
            // Arrange
            var existingTvShow = new TvShow 
            { 
                Id = Guid.NewGuid(), 
                Title = "Breaking Bad", 
                FirstAirYear = 2008,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(existingTvShow);
            await Context.SaveChangesAsync();

            var dto = new CreateTvShowDto
            {
                Title = "Breaking Bad",
                FirstAirYear = 2008,
                Creator = "Vince Gilligan",
                Status = Status.Uncharted
            };

            // Act
            var result = await _service.CreateTvShowAsync(dto);

            // Assert
            result.Id.Should().Be(existingTvShow.Id);
            
            // Verify no duplicate created
            Context.TvShows.Count().Should().Be(1);
        }

        [Fact]
        public async Task CreateTvShowAsync_ShouldReuseExistingTopicsAndGenres()
        {
            // Arrange
            var existingTopic = new Topic { Name = "crime" };
            var existingGenre = new Genre { Name = "thriller" };
            Context.Topics.Add(existingTopic);
            Context.Genres.Add(existingGenre);
            await Context.SaveChangesAsync();

            var initialTopicCount = Context.Topics.Count();
            var initialGenreCount = Context.Genres.Count();

            var dto = new CreateTvShowDto
            {
                Title = "Breaking Bad",
                FirstAirYear = 2008,
                Status = Status.Uncharted,
                Topics = new[] { "crime" },
                Genres = new[] { "thriller" }
            };

            // Act
            var result = await _service.CreateTvShowAsync(dto);

            // Assert
            result.Topics.Should().HaveCount(1);
            result.Genres.Should().HaveCount(1);
            
            // Verify no duplicates created
            Context.Topics.Count().Should().Be(initialTopicCount);
            Context.Genres.Count().Should().Be(initialGenreCount);
        }

        #endregion

        #region UpdateTvShowAsync Tests

        [Fact]
        public async Task UpdateTvShowAsync_ShouldUpdateExistingTvShow()
        {
            // Arrange
            var tvShowId = Guid.NewGuid();
            var existingTvShow = new TvShow 
            { 
                Id = tvShowId, 
                Title = "Original Title", 
                FirstAirYear = 2008,
                Creator = "Original Creator",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(existingTvShow);
            await Context.SaveChangesAsync();

            var dto = new CreateTvShowDto
            {
                Title = "Updated Title",
                FirstAirYear = 2009,
                Creator = "Updated Creator",
                Status = Status.ActivelyExploring,
                NumberOfSeasons = 10
            };

            // Act
            var result = await _service.UpdateTvShowAsync(tvShowId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.FirstAirYear.Should().Be(2009);
            result.Creator.Should().Be("Updated Creator");
            result.Status.Should().Be(Status.ActivelyExploring);
            result.NumberOfSeasons.Should().Be(10);

            // Clear tracker and reload from database to verify persistence
            Context.ChangeTracker.Clear();
            var updatedTvShow = await Context.TvShows.FindAsync(tvShowId);
            updatedTvShow!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateTvShowAsync_ShouldThrowInvalidOperationException_WhenTvShowDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new CreateTvShowDto { Title = "Test", Status = Status.Uncharted };

            // Act & Assert
            await _service.Invoking(s => s.UpdateTvShowAsync(nonExistentId, dto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"TV show with ID {nonExistentId} not found.");
        }

        #endregion

        #region DeleteTvShowAsync Tests

        [Fact]
        public async Task DeleteTvShowAsync_ShouldReturnTrue_WhenTvShowExists()
        {
            // Arrange
            var tvShowId = Guid.NewGuid();
            var tvShow = new TvShow 
            { 
                Id = tvShowId, 
                Title = "Breaking Bad", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteTvShowAsync(tvShowId);

            // Assert
            result.Should().BeTrue();

            // Verify deleted from database
            var deletedTvShow = await Context.TvShows.FindAsync(tvShowId);
            deletedTvShow.Should().BeNull();
        }

        [Fact]
        public async Task DeleteTvShowAsync_ShouldReturnFalse_WhenTvShowDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.DeleteTvShowAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region TvShowExistsAsync Tests

        [Fact]
        public async Task TvShowExistsAsync_ShouldReturnTrue_WhenTvShowExists()
        {
            // Arrange
            var tvShow = new TvShow 
            { 
                Title = "Breaking Bad", 
                FirstAirYear = 2008,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.TvShowExistsAsync("Breaking Bad", 2008);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TvShowExistsAsync_ShouldReturnFalse_WhenTvShowDoesNotExist()
        {
            // Act
            var result = await _service.TvShowExistsAsync("Non-existent Show", 2020);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TvShowExistsAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var tvShow = new TvShow 
            { 
                Title = "Breaking Bad", 
                FirstAirYear = 2008,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.TvShowExistsAsync("breaking bad", 2008);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region GetTvShowByTitleAndYearAsync Tests

        [Fact]
        public async Task GetTvShowByTitleAndYearAsync_ShouldReturnTvShow_WhenTvShowExists()
        {
            // Arrange
            var tvShow = new TvShow 
            { 
                Title = "Breaking Bad", 
                FirstAirYear = 2008,
                Creator = "Vince Gilligan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetTvShowByTitleAndYearAsync("Breaking Bad", 2008);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Breaking Bad");
            result.FirstAirYear.Should().Be(2008);
            result.Creator.Should().Be("Vince Gilligan");
        }

        [Fact]
        public async Task GetTvShowByTitleAndYearAsync_ShouldReturnNull_WhenTvShowDoesNotExist()
        {
            // Act
            var result = await _service.GetTvShowByTitleAndYearAsync("Non-existent Show", 2020);

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}



