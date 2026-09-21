using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class MovieServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<MovieService> _mockLogger;
        private readonly MovieService _service;

        public MovieServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<MovieService>>();
            _service = new MovieService(Context, _mockLogger);
        }

        #region GetAllMoviesAsync Tests

        [Fact]
        public async Task GetAllMoviesAsync_ShouldReturnAllMovies()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie { Id = Guid.NewGuid(), Title = "Inception", ReleaseYear = 2010, Director = "Christopher Nolan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Movie { Id = Guid.NewGuid(), Title = "The Matrix", ReleaseYear = 1999, Director = "Wachowski Brothers", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.Movies.AddRange(movies);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllMoviesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(m => m.Title).Should().Contain(new[] { "Inception", "The Matrix" });
        }

        [Fact]
        public async Task GetAllMoviesAsync_ShouldReturnEmptyList_WhenNoMoviesExist()
        {
            // Act
            var result = await _service.GetAllMoviesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetMovieByIdAsync Tests

        [Fact]
        public async Task GetMovieByIdAsync_ShouldReturnMovie_WhenMovieExists()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movie = new Movie 
            { 
                Id = movieId, 
                Title = "Inception", 
                ReleaseYear = 2010,
                Director = "Christopher Nolan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetMovieByIdAsync(movieId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(movieId);
            result.Title.Should().Be("Inception");
            result.Director.Should().Be("Christopher Nolan");
        }

        [Fact]
        public async Task GetMovieByIdAsync_ShouldReturnNull_WhenMovieDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.GetMovieByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetMoviesByDirectorAsync Tests

        [Fact]
        public async Task GetMoviesByDirectorAsync_ShouldReturnMoviesByDirector()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie { Id = Guid.NewGuid(), Title = "Inception", Director = "Christopher Nolan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Movie { Id = Guid.NewGuid(), Title = "Interstellar", Director = "Christopher Nolan", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Movie { Id = Guid.NewGuid(), Title = "The Matrix", Director = "Wachowski Brothers", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.Movies.AddRange(movies);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetMoviesByDirectorAsync("Christopher Nolan");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(m => m.Director!.Contains("Christopher Nolan"));
        }

        [Fact]
        public async Task GetMoviesByDirectorAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var movie = new Movie 
            { 
                Id = Guid.NewGuid(), 
                Title = "Inception", 
                Director = "Christopher Nolan", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetMoviesByDirectorAsync("christopher nolan");

            // Assert
            result.Should().HaveCount(1);
            result.First().Director.Should().Be("Christopher Nolan");
        }

        #endregion

        #region GetMoviesByYearAsync Tests

        [Fact]
        public async Task GetMoviesByYearAsync_ShouldReturnMoviesByYear()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie { Id = Guid.NewGuid(), Title = "Inception", ReleaseYear = 2010, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Movie { Id = Guid.NewGuid(), Title = "Shutter Island", ReleaseYear = 2010, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Movie { Id = Guid.NewGuid(), Title = "The Matrix", ReleaseYear = 1999, Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.Movies.AddRange(movies);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetMoviesByYearAsync(2010);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(m => m.ReleaseYear == 2010);
        }

        #endregion

        #region CreateMovieAsync Tests

        [Fact]
        public async Task CreateMovieAsync_ShouldCreateNewMovie_WhenMovieDoesNotExist()
        {
            // Arrange
            var dto = new CreateMovieDto
            {
                Title = "Inception",
                ReleaseYear = 2010,
                Director = "Christopher Nolan",
                Status = Status.Uncharted,
                Topics = new[] { "science fiction", "action" },
                Genres = new[] { "thriller", "sci-fi" }
            };

            // Act
            var result = (await _service.CreateMovieAsync(dto)).Movie;

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Inception");
            result.Director.Should().Be("Christopher Nolan");
            result.ReleaseYear.Should().Be(2010);
            result.MediaType.Should().Be(MediaType.Movie);
            result.Topics.Should().HaveCount(2);
            result.Genres.Should().HaveCount(2);

            // Verify saved to database
            var savedMovie = await Context.Movies.FindAsync(result.Id);
            savedMovie.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateMovieAsync_ShouldThrowArgumentNullException_WhenDtoIsNull()
        {
            // Act & Assert
            await _service.Invoking(s => s.CreateMovieAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithMessage("*Movie data is required*");
        }

        [Fact]
        public async Task CreateMovieAsync_ShouldReturnExistingMovie_WhenMovieAlreadyExists()
        {
            // Arrange
            var existingMovie = new Movie 
            { 
                Id = Guid.NewGuid(), 
                Title = "Inception", 
                ReleaseYear = 2010,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(existingMovie);
            await Context.SaveChangesAsync();

            var dto = new CreateMovieDto
            {
                Title = "Inception",
                ReleaseYear = 2010,
                Director = "Christopher Nolan",
                Status = Status.Uncharted
            };

            // Act
            var result = (await _service.CreateMovieAsync(dto)).Movie;

            // Assert
            result.Id.Should().Be(existingMovie.Id);
            
            // Verify no duplicate created
            Context.Movies.Count().Should().Be(1);
        }

        [Fact]
        public async Task CreateMovieAsync_ShouldReuseExistingTopicsAndGenres()
        {
            // Arrange
            var existingTopic = new Topic { Name = "science fiction" };
            var existingGenre = new Genre { Name = "thriller" };
            Context.Topics.Add(existingTopic);
            Context.Genres.Add(existingGenre);
            await Context.SaveChangesAsync();

            var initialTopicCount = Context.Topics.Count();
            var initialGenreCount = Context.Genres.Count();

            var dto = new CreateMovieDto
            {
                Title = "Inception",
                ReleaseYear = 2010,
                Status = Status.Uncharted,
                Topics = new[] { "science fiction" },
                Genres = new[] { "thriller" }
            };

            // Act
            var result = (await _service.CreateMovieAsync(dto)).Movie;

            // Assert
            result.Topics.Should().HaveCount(1);
            result.Genres.Should().HaveCount(1);
            
            // Verify no duplicates created
            Context.Topics.Count().Should().Be(initialTopicCount);
            Context.Genres.Count().Should().Be(initialGenreCount);
        }

        #endregion

        #region UpdateMovieAsync Tests

        [Fact]
        public async Task UpdateMovieAsync_ShouldUpdateExistingMovie()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var existingMovie = new Movie 
            { 
                Id = movieId, 
                Title = "Original Title", 
                ReleaseYear = 2010,
                Director = "Original Director",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(existingMovie);
            await Context.SaveChangesAsync();

            var dto = new CreateMovieDto
            {
                Title = "Updated Title",
                ReleaseYear = 2011,
                Director = "Updated Director",
                Status = Status.ActivelyExploring
            };

            // Act
            var result = await _service.UpdateMovieAsync(movieId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.ReleaseYear.Should().Be(2011);
            result.Director.Should().Be("Updated Director");
            result.Status.Should().Be(Status.ActivelyExploring);

            // Clear tracker and reload from database to verify persistence
            Context.ChangeTracker.Clear();
            var updatedMovie = await Context.Movies.FindAsync(movieId);
            updatedMovie!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateMovieAsync_ShouldThrowInvalidOperationException_WhenMovieDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new CreateMovieDto { Title = "Test", Status = Status.Uncharted };

            // Act & Assert
            await _service.Invoking(s => s.UpdateMovieAsync(nonExistentId, dto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Movie with ID {nonExistentId} not found.");
        }

        #endregion

        #region DeleteMovieAsync Tests

        [Fact]
        public async Task DeleteMovieAsync_ShouldReturnTrue_WhenMovieExists()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movie = new Movie 
            { 
                Id = movieId, 
                Title = "Inception", 
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteMovieAsync(movieId);

            // Assert
            result.Should().BeTrue();

            // Verify deleted from database
            var deletedMovie = await Context.Movies.FindAsync(movieId);
            deletedMovie.Should().BeNull();
        }

        [Fact]
        public async Task DeleteMovieAsync_ShouldReturnFalse_WhenMovieDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.DeleteMovieAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region MovieExistsAsync Tests

        [Fact]
        public async Task MovieExistsAsync_ShouldReturnTrue_WhenMovieExists()
        {
            // Arrange
            var movie = new Movie 
            { 
                Title = "Inception", 
                ReleaseYear = 2010,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.MovieExistsAsync("Inception", 2010);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task MovieExistsAsync_ShouldReturnFalse_WhenMovieDoesNotExist()
        {
            // Act
            var result = await _service.MovieExistsAsync("Non-existent Movie", 2020);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task MovieExistsAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var movie = new Movie 
            { 
                Title = "Inception", 
                ReleaseYear = 2010,
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.MovieExistsAsync("inception", 2010);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region GetMovieByTitleAndYearAsync Tests

        [Fact]
        public async Task GetMovieByTitleAndYearAsync_ShouldReturnMovie_WhenMovieExists()
        {
            // Arrange
            var movie = new Movie 
            { 
                Title = "Inception", 
                ReleaseYear = 2010,
                Director = "Christopher Nolan",
                Topics = new List<Topic>(), 
                Genres = new List<Genre>() 
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetMovieByTitleAndYearAsync("Inception", 2010);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Inception");
            result.ReleaseYear.Should().Be(2010);
            result.Director.Should().Be("Christopher Nolan");
        }

        [Fact]
        public async Task GetMovieByTitleAndYearAsync_ShouldReturnNull_WhenMovieDoesNotExist()
        {
            // Act
            var result = await _service.GetMovieByTitleAndYearAsync("Non-existent Movie", 2020);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Genre resolution

        [Fact]
        public async Task CreateMovieAsync_ShouldNormalizeGenres_AndReuseAnExistingGenre()
        {
            Context.Genres.Add(new Genre { Name = "science fiction" });
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateMovieDto("Arrival");
            dto.Genres = new[] { " Science Fiction ", "DRAMA", "drama" };

            var result = (await _service.CreateMovieAsync(dto)).Movie;

            result.Genres.Select(g => g.Name).Should().BeEquivalentTo(new[] { "science fiction", "drama" });
            Context.Genres.Count(g => g.Name == "science fiction").Should().Be(1);
            Context.Genres.Count(g => g.Name == "drama").Should().Be(1);
        }

        [Fact]
        public async Task CreateMovieAsync_TwoMoviesNamingTheSameNewGenre_ShareOneGenreRow()
        {
            var first = TestDataFactory.CreateMovieDto("Heat");
            first.Genres = new[] { "crime" };
            var second = TestDataFactory.CreateMovieDto("Collateral");
            second.Genres = new[] { "Crime" };

            var firstMovie = (await _service.CreateMovieAsync(first)).Movie;
            var secondMovie = (await _service.CreateMovieAsync(second)).Movie;

            Context.Genres.Count(g => g.Name == "crime").Should().Be(1);
            firstMovie.Genres.Single().Id.Should().Be(secondMovie.Genres.Single().Id);
        }

        #endregion

        #region TMDB refresh stamp

        [Fact]
        public async Task CreateMovieAsync_FromTmdb_ShouldStampTmdbRefreshedAt()
        {
            var result = (await _service.CreateMovieAsync(TestDataFactory.CreateMovieDto("Inception"), fromTmdb: true)).Movie;

            result.TmdbRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task CreateMovieAsync_ManualCreate_ShouldLeaveTmdbRefreshedAtNull()
        {
            var result = (await _service.CreateMovieAsync(TestDataFactory.CreateMovieDto("Inception"))).Movie;

            result.TmdbRefreshedAt.Should().BeNull();
        }

        #endregion

        #region Create: existing-item lookup

        [Fact]
        public async Task CreateMovieAsync_NewMovie_ShouldReportCreated()
        {
            var result = await _service.CreateMovieAsync(TestDataFactory.CreateMovieDto("Arrival", 2016));

            result.Created.Should().BeTrue();
            Context.Movies.Count().Should().Be(1);
        }

        [Fact]
        public async Task CreateMovieAsync_StoredTmdbId_ShouldReturnTheStoredMovieUntouched_EvenWhenTitleAndYearDiffer()
        {
            var stored = TestDataFactory.CreateMovie("My Renamed Copy", 1999, "27205");
            stored.Description = "My own description";
            Context.Movies.Add(stored);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateMovieDto("Inception", 2010);
            dto.TmdbId = "27205";
            dto.Description = "TMDB overview";

            var result = await _service.CreateMovieAsync(dto, fromTmdb: true);

            result.Created.Should().BeFalse();
            result.Movie.Id.Should().Be(stored.Id);
            result.Movie.Title.Should().Be("My Renamed Copy");
            result.Movie.Description.Should().Be("My own description");
            result.Movie.TmdbRefreshedAt.Should().BeNull();
            Context.Movies.Count().Should().Be(1);
        }

        [Fact]
        public async Task CreateMovieAsync_SameTitleAndYear_ShouldFallBackToTheStoredMovie()
        {
            var stored = TestDataFactory.CreateMovie("Heat", 1995, "");
            Context.Movies.Add(stored);
            await Context.SaveChangesAsync();

            var result = await _service.CreateMovieAsync(TestDataFactory.CreateMovieDto("heat", 1995));

            result.Created.Should().BeFalse();
            result.Movie.Id.Should().Be(stored.Id);
        }

        [Fact]
        public async Task CreateMovieAsync_SameTitleAndYearButDifferentTmdbId_ShouldCreateASecondMovie()
        {
            // Two different films can share a title and a year; their TMDB ids tell them apart.
            Context.Movies.Add(TestDataFactory.CreateMovie("Crash", 2004, "1640"));
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateMovieDto("Crash", 2004);
            dto.TmdbId = "99999";

            var result = await _service.CreateMovieAsync(dto, fromTmdb: true);

            result.Created.Should().BeTrue();
            Context.Movies.Count().Should().Be(2);
        }

        [Fact]
        public async Task GetMovieByTmdbIdAsync_ShouldReturnNull_ForBlankOrUnknownIds()
        {
            Context.Movies.Add(TestDataFactory.CreateMovie("Manual", 2000, ""));
            await Context.SaveChangesAsync();

            (await _service.GetMovieByTmdbIdAsync("")).Should().BeNull();
            (await _service.GetMovieByTmdbIdAsync("424242")).Should().BeNull();
        }

        #endregion
    }
}



