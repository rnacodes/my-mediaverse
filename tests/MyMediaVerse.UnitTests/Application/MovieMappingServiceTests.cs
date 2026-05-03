using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class MovieMappingServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ILogger<MovieMappingService>> _mockLogger;
        private readonly MovieMappingService _service;

        public MovieMappingServiceTests()
        {
            _mockLogger = new Mock<ILogger<MovieMappingService>>();
            _service = new MovieMappingService(Context, _mockLogger.Object);
        }

        #region MapFromDtoAsync

        [Fact]
        public async Task MapFromDtoAsync_ValidDto_MapsAllProperties()
        {
            var dto = new CreateMovieDto
            {
                Title = "Inception",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                Director = "Christopher Nolan",
                Cast = "Leonardo DiCaprio, Tom Hardy",
                ReleaseYear = 2010,
                RuntimeMinutes = 148,
                MpaaRating = "PG-13",
                ImdbId = "tt1375666",
                TmdbId = "27205",
                TmdbRating = 8.4,
                Tagline = "Your mind is the scene of the crime.",
                Homepage = "https://www.warnerbros.com/movies/inception",
                OriginalLanguage = "en",
                OriginalTitle = "Inception",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            var result = await _service.MapFromDtoAsync(dto);

            result.Should().NotBeNull();
            result.Title.Should().Be("Inception");
            result.MediaType.Should().Be(MediaType.Movie);
            result.Director.Should().Be("Christopher Nolan");
            result.Cast.Should().Be("Leonardo DiCaprio, Tom Hardy");
            result.ReleaseYear.Should().Be(2010);
            result.RuntimeMinutes.Should().Be(148);
            result.ImdbId.Should().Be("tt1375666");
            result.TmdbId.Should().Be("27205");
            result.TmdbRating.Should().Be(8.4);
            result.Tagline.Should().Be("Your mind is the scene of the crime.");
            result.DateAdded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task MapFromDtoAsync_WithTopics_NormalizesToLowercase()
        {
            var dto = TestDataFactory.CreateMovieDto();
            dto.Topics = new[] { "Science Fiction", "  ACTION  " };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(2);
            result.Topics.Select(t => t.Name).Should().BeEquivalentTo("science fiction", "action");
        }

        [Fact]
        public async Task MapFromDtoAsync_WithExistingTopic_ReusesExistingTopic()
        {
            var existingTopic = new Topic { Name = "action" };
            Context.Topics.Add(existingTopic);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateMovieDto();
            dto.Topics = new[] { "Action" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(1);
            result.Topics.First().Id.Should().Be(existingTopic.Id);
        }

        [Fact]
        public async Task MapFromDtoAsync_WithGenres_NormalizesToLowercase()
        {
            var dto = TestDataFactory.CreateMovieDto();
            dto.Genres = new[] { "Thriller", "  DRAMA  " };

            var result = await _service.MapFromDtoAsync(dto);

            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().BeEquivalentTo("thriller", "drama");
        }

        [Fact]
        public async Task MapFromDtoAsync_SkipsWhitespaceTopicsAndGenres()
        {
            var dto = TestDataFactory.CreateMovieDto();
            dto.Topics = new[] { "", "  ", "valid topic" };
            dto.Genres = new[] { "", "valid genre" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(1);
            result.Genres.Should().HaveCount(1);
        }

        [Fact]
        public async Task MapFromDtoAsync_NullTopicsAndGenres_CreatesEmptyCollections()
        {
            var dto = TestDataFactory.CreateMovieDto();
            dto.Topics = null;
            dto.Genres = null;

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().BeEmpty();
            result.Genres.Should().BeEmpty();
        }

        #endregion

        #region MapToResponseDtoAsync

        [Fact]
        public async Task MapToResponseDtoAsync_ValidMovie_MapsAllProperties()
        {
            var movie = TestDataFactory.CreateMovie("Inception", 2010, "27205");
            movie.Director = "Christopher Nolan";
            movie.RuntimeMinutes = 148;
            movie.ImdbId = "tt1375666";
            movie.TmdbRating = 8.4;
            movie.Tagline = "Your mind is the scene of the crime.";
            movie.Topics.Add(new Topic { Name = "science fiction" });
            movie.Genres.Add(new Genre { Name = "thriller" });

            var result = await _service.MapToResponseDtoAsync(movie);

            result.Should().NotBeNull();
            result.Id.Should().Be(movie.Id);
            result.Title.Should().Be("Inception");
            result.Director.Should().Be("Christopher Nolan");
            result.RuntimeMinutes.Should().Be(148);
            result.TmdbRating.Should().Be(8.4);
            result.Topics.Should().Contain("science fiction");
            result.Genres.Should().Contain("thriller");
            result.FormattedRuntime.Should().Be("2h 28m");
        }

        [Fact]
        public async Task MapToResponseDtoAsync_NullRuntime_FormattedRuntimeIsNull()
        {
            var movie = TestDataFactory.CreateMovie();
            movie.RuntimeMinutes = null;

            var result = await _service.MapToResponseDtoAsync(movie);

            result.FormattedRuntime.Should().BeNull();
        }

        [Fact]
        public async Task MapToResponseDtoAsync_RuntimeUnderOneHour_FormatsMinutesOnly()
        {
            var movie = TestDataFactory.CreateMovie();
            movie.RuntimeMinutes = 45;

            var result = await _service.MapToResponseDtoAsync(movie);

            result.FormattedRuntime.Should().Be("45m");
        }

        [Fact]
        public async Task MapToResponseDtoAsync_RuntimeExactHours_FormatsHoursOnly()
        {
            var movie = TestDataFactory.CreateMovie();
            movie.RuntimeMinutes = 120;

            var result = await _service.MapToResponseDtoAsync(movie);

            result.FormattedRuntime.Should().Be("2h");
        }

        #endregion

        #region MapFromTmdbAsync

        [Fact]
        public async Task MapFromTmdbAsync_ValidTmdbMovie_MapsCorrectly()
        {
            var tmdbMovie = TestDataFactory.CreateTmdbMovieDto();

            var result = await _service.MapFromTmdbAsync(tmdbMovie);

            result.Title.Should().Be("Inception");
            result.MediaType.Should().Be(MediaType.Movie);
            result.Status.Should().Be(Status.Uncharted);
            result.TmdbId.Should().Be("27205");
            result.TmdbRating.Should().Be(8.4);
            result.ImdbId.Should().Be("tt1375666");
            result.RuntimeMinutes.Should().Be(148);
            result.Tagline.Should().Be("Your mind is the scene of the crime.");
            result.ReleaseYear.Should().Be(2010);
            result.Thumbnail.Should().Contain("image.tmdb.org/t/p/w500");
        }

        [Fact]
        public async Task MapFromTmdbAsync_NullTitle_DefaultsToUnknownTitle()
        {
            var tmdbMovie = new TmdbMovieDto { Title = null, Id = 1 };

            var result = await _service.MapFromTmdbAsync(tmdbMovie);

            result.Title.Should().Be("Unknown Title");
        }

        [Fact]
        public async Task MapFromTmdbAsync_NullPosterPath_ThumbnailIsNull()
        {
            var tmdbMovie = new TmdbMovieDto { Title = "Test", PosterPath = null };

            var result = await _service.MapFromTmdbAsync(tmdbMovie);

            result.Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task MapFromTmdbAsync_InvalidReleaseDate_ReleaseYearIsNull()
        {
            var tmdbMovie = new TmdbMovieDto { Title = "Test", ReleaseDate = "invalid" };

            var result = await _service.MapFromTmdbAsync(tmdbMovie);

            result.ReleaseYear.Should().BeNull();
        }

        [Fact]
        public async Task MapFromTmdbAsync_NullReleaseDate_ReleaseYearIsNull()
        {
            var tmdbMovie = new TmdbMovieDto { Title = "Test", ReleaseDate = null };

            var result = await _service.MapFromTmdbAsync(tmdbMovie);

            result.ReleaseYear.Should().BeNull();
        }

        #endregion

        #region MapToSearchResultDtoAsync

        [Fact]
        public async Task MapToSearchResultDtoAsync_ValidTmdbMovie_MapsCorrectly()
        {
            var tmdbMovie = TestDataFactory.CreateTmdbMovieDto();

            var result = await _service.MapToSearchResultDtoAsync(tmdbMovie);

            result.Id.Should().Be(27205);
            result.Title.Should().Be("Inception");
            result.Overview.Should().NotBeNullOrEmpty();
            result.PosterUrl.Should().Contain("image.tmdb.org/t/p/w500");
            result.BackdropUrl.Should().Contain("image.tmdb.org/t/p/w1280");
            result.VoteAverage.Should().Be(8.4);
            result.Runtime.Should().Be(148);
        }

        [Fact]
        public async Task MapToSearchResultDtoAsync_NullPaths_UrlsAreNull()
        {
            var tmdbMovie = new TmdbMovieDto { Title = "Test", PosterPath = null, BackdropPath = null };

            var result = await _service.MapToSearchResultDtoAsync(tmdbMovie);

            result.PosterUrl.Should().BeNull();
            result.BackdropUrl.Should().BeNull();
        }

        #endregion
    }
}
