using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class TmdbServiceTests
    {
        private readonly ITmdbApiClient _mockTmdbApiClient;
        private readonly IMovieService _mockMovieService;
        private readonly ITvShowService _mockTvShowService;
        private readonly ILogger<TmdbService> _mockLogger;
        private readonly TmdbService _tmdbService;

        public TmdbServiceTests()
        {
            _mockTmdbApiClient = Substitute.For<ITmdbApiClient>();
            _mockMovieService = Substitute.For<IMovieService>();
            _mockTvShowService = Substitute.For<ITvShowService>();
            _mockLogger = Substitute.For<ILogger<TmdbService>>();
            
            _tmdbService = new TmdbService(
                _mockTmdbApiClient,
                _mockMovieService,
                _mockTvShowService,
                _mockLogger);
        }

        #region Search Operations Tests

        [Fact]
        public async Task SearchMoviesAsync_ShouldReturnSearchResults_WhenValidQueryProvided()
        {
            // Arrange
            var query = "inception";
            var expectedResult = TestDataFactory.CreateTmdbMovieSearchResultDto(
                TestDataFactory.CreateTmdbMovieDto(27205, "Inception"));
            
            _mockTmdbApiClient
                .SearchMoviesAsync(query, 1, "en-US")
                .Returns(expectedResult);

            // Act
            var result = await _tmdbService.SearchMoviesAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results[0].Title.Should().Be("Inception");
            _mockTmdbApiClient.Received(1).SearchMoviesAsync(query, 1, "en-US");
        }

        [Fact]
        public async Task SearchTvShowsAsync_ShouldReturnSearchResults_WhenValidQueryProvided()
        {
            // Arrange
            var query = "game of thrones";
            var expectedResult = TestDataFactory.CreateTmdbTvSearchResultDto(
                TestDataFactory.CreateTmdbTvShowDto(1399, "Game of Thrones"));
            
            _mockTmdbApiClient
                .SearchTvShowsAsync(query, 1, "en-US")
                .Returns(expectedResult);

            // Act
            var result = await _tmdbService.SearchTvShowsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results[0].Name.Should().Be("Game of Thrones");
            _mockTmdbApiClient.Received(1).SearchTvShowsAsync(query, 1, "en-US");
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldReturnMovieDetails_WhenValidIdProvided()
        {
            // Arrange
            var movieId = 27205;
            var expectedMovie = TestDataFactory.CreateTmdbMovieDto(movieId, "Inception");
            
            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Returns(expectedMovie);

            // Act
            var result = await _tmdbService.GetMovieDetailsAsync(movieId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(movieId);
            result.Title.Should().Be("Inception");
            _mockTmdbApiClient.Received(1).GetMovieDetailsAsync(movieId, "en-US");
        }

        [Fact]
        public async Task GetTvShowDetailsAsync_ShouldReturnTvShowDetails_WhenValidIdProvided()
        {
            // Arrange
            var tvShowId = 1399;
            var expectedTvShow = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Game of Thrones");
            
            _mockTmdbApiClient
                .GetTvShowDetailsAsync(tvShowId, "en-US")
                .Returns(expectedTvShow);

            // Act
            var result = await _tmdbService.GetTvShowDetailsAsync(tvShowId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(tvShowId);
            result.Name.Should().Be("Game of Thrones");
            _mockTmdbApiClient.Received(1).GetTvShowDetailsAsync(tvShowId, "en-US");
        }

        #endregion

        #region Import Operations Tests

        [Fact]
        public async Task ImportMovieAsync_ShouldCreateNewMovie_WhenMovieDoesNotExist()
        {
            // Arrange
            var movieId = 27205;
            var tmdbMovieDto = TestDataFactory.CreateTmdbMovieDto(movieId, "Inception");
            var expectedMovie = TestDataFactory.CreateMovie("Inception", 2010, movieId.ToString());

            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Returns(tmdbMovieDto);
            
            _mockMovieService
                .GetMovieByTitleAndYearAsync("Inception", 2010)
                .Returns((Movie?)null);
            
            _mockMovieService
                .CreateMovieAsync(Arg.Any<CreateMovieDto>())
                .Returns(expectedMovie);

            // Act
            var result = await _tmdbService.ImportMovieAsync(movieId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Inception");
            result.ReleaseYear.Should().Be(2010);
            result.TmdbId.Should().Be(movieId.ToString());
            
            _mockTmdbApiClient.Received(1).GetMovieDetailsAsync(movieId, "en-US");
            _mockMovieService.Received(1).GetMovieByTitleAndYearAsync("Inception", 2010);
            _mockMovieService.Received(1).CreateMovieAsync(Arg.Is<CreateMovieDto>(dto => 
                dto.Title == "Inception" && 
                dto.ReleaseYear == 2010 &&
                dto.TmdbId == movieId.ToString() &&
                dto.MediaType == MediaType.Movie &&
                dto.Status == Status.Uncharted));
        }

        [Fact]
        public async Task ImportMovieAsync_ShouldReturnExistingMovie_WhenMovieAlreadyExists()
        {
            // Arrange
            var movieId = 27205;
            var tmdbMovieDto = TestDataFactory.CreateTmdbMovieDto(movieId, "Inception");
            var existingMovie = TestDataFactory.CreateMovie("Inception", 2010, movieId.ToString());

            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Returns(tmdbMovieDto);
            
            _mockMovieService
                .GetMovieByTitleAndYearAsync("Inception", 2010)
                .Returns(existingMovie);

            // Act
            var result = await _tmdbService.ImportMovieAsync(movieId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(existingMovie);
            result.Title.Should().Be("Inception");
            
            _mockTmdbApiClient.Received(1).GetMovieDetailsAsync(movieId, "en-US");
            _mockMovieService.Received(1).GetMovieByTitleAndYearAsync("Inception", 2010);
            _mockMovieService.DidNotReceive().CreateMovieAsync(Arg.Any<CreateMovieDto>());
        }

        [Fact]
        public async Task ImportTvShowAsync_ShouldCreateNewTvShow_WhenTvShowDoesNotExist()
        {
            // Arrange
            var tvShowId = 1399;
            var tmdbTvShowDto = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Game of Thrones");
            var expectedTvShow = TestDataFactory.CreateTvShow("Game of Thrones", 2011, tvShowId.ToString());

            _mockTmdbApiClient
                .GetTvShowDetailsAsync(tvShowId, "en-US")
                .Returns(tmdbTvShowDto);
            
            _mockTvShowService
                .GetTvShowByTitleAndYearAsync("Game of Thrones", 2011)
                .Returns((TvShow?)null);
            
            _mockTvShowService
                .CreateTvShowAsync(Arg.Any<CreateTvShowDto>())
                .Returns(expectedTvShow);

            // Act
            var result = await _tmdbService.ImportTvShowAsync(tvShowId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Game of Thrones");
            result.FirstAirYear.Should().Be(2011);
            result.TmdbId.Should().Be(tvShowId.ToString());
            
            _mockTmdbApiClient.Received(1).GetTvShowDetailsAsync(tvShowId, "en-US");
            _mockTvShowService.Received(1).GetTvShowByTitleAndYearAsync("Game of Thrones", 2011);
            _mockTvShowService.Received(1).CreateTvShowAsync(Arg.Is<CreateTvShowDto>(dto => 
                dto.Title == "Game of Thrones" && 
                dto.FirstAirYear == 2011 &&
                dto.TmdbId == tvShowId.ToString() &&
                dto.MediaType == MediaType.TVShow &&
                dto.Status == Status.Uncharted));
        }

        [Fact]
        public async Task ImportTvShowAsync_ShouldReturnExistingTvShow_WhenTvShowAlreadyExists()
        {
            // Arrange
            var tvShowId = 1399;
            var tmdbTvShowDto = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Game of Thrones");
            var existingTvShow = TestDataFactory.CreateTvShow("Game of Thrones", 2011, tvShowId.ToString());

            _mockTmdbApiClient
                .GetTvShowDetailsAsync(tvShowId, "en-US")
                .Returns(tmdbTvShowDto);
            
            _mockTvShowService
                .GetTvShowByTitleAndYearAsync("Game of Thrones", 2011)
                .Returns(existingTvShow);

            // Act
            var result = await _tmdbService.ImportTvShowAsync(tvShowId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(existingTvShow);
            result.Title.Should().Be("Game of Thrones");
            
            _mockTmdbApiClient.Received(1).GetTvShowDetailsAsync(tvShowId, "en-US");
            _mockTvShowService.Received(1).GetTvShowByTitleAndYearAsync("Game of Thrones", 2011);
            _mockTvShowService.DidNotReceive().CreateTvShowAsync(Arg.Any<CreateTvShowDto>());
        }

        [Fact]
        public async Task ImportMovieAsync_ShouldHandleMovieWithoutReleaseDate()
        {
            // Arrange
            var movieId = 12345;
            var tmdbMovieDto = TestDataFactory.CreateTmdbMovieDto(movieId, "Test Movie");
            tmdbMovieDto.ReleaseDate = null; // No release date
            var expectedMovie = TestDataFactory.CreateMovie("Test Movie", null, movieId.ToString());

            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Returns(tmdbMovieDto);
            
            _mockMovieService
                .GetMovieByTitleAndYearAsync("Test Movie", null)
                .Returns((Movie?)null);
            
            _mockMovieService
                .CreateMovieAsync(Arg.Any<CreateMovieDto>())
                .Returns(expectedMovie);

            // Act
            var result = await _tmdbService.ImportMovieAsync(movieId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Movie");
            
            _mockMovieService.Received(1).CreateMovieAsync(Arg.Is<CreateMovieDto>(dto => 
                dto.Title == "Test Movie" && 
                dto.ReleaseYear == null));
        }

        [Fact]
        public async Task ImportMovieAsync_ShouldStoreTmdbRatingCorrectly()
        {
            // Arrange
            var movieId = 12345;
            var tmdbMovieDto = TestDataFactory.CreateTmdbMovieDto(movieId, "Test Movie");
            tmdbMovieDto.VoteAverage = 8.4; // Should store as TmdbRating, not convert to personal Rating
            var expectedMovie = TestDataFactory.CreateMovie("Test Movie", 2010, movieId.ToString());

            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Returns(tmdbMovieDto);
            
            _mockMovieService
                .GetMovieByTitleAndYearAsync("Test Movie", 2010)
                .Returns((Movie?)null);
            
            _mockMovieService
                .CreateMovieAsync(Arg.Any<CreateMovieDto>())
                .Returns(expectedMovie);

            // Act
            var result = await _tmdbService.ImportMovieAsync(movieId);

            // Assert
            _mockMovieService.Received(1).CreateMovieAsync(Arg.Is<CreateMovieDto>(dto => 
                dto.Rating == null && // Personal rating should be null for imports
                dto.TmdbRating == 8.4)); // TMDB rating should be stored as-is
        }

        [Fact]
        public async Task ImportTvShowAsync_ShouldStoreTmdbRatingCorrectly()
        {
            // Arrange
            var tvShowId = 12345;
            var tmdbTvShowDto = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Test TV Show");
            tmdbTvShowDto.VoteAverage = 7.8; // Should store as TmdbRating, not convert to personal Rating
            var expectedTvShow = TestDataFactory.CreateTvShow("Test TV Show", 2020, tvShowId.ToString());

            _mockTmdbApiClient
                .GetTvShowDetailsAsync(tvShowId, "en-US")
                .Returns(tmdbTvShowDto);
            
            _mockTvShowService
                .GetTvShowByTitleAndYearAsync("Test TV Show", 2020)
                .Returns((TvShow?)null);
            
            _mockTvShowService
                .CreateTvShowAsync(Arg.Any<CreateTvShowDto>())
                .Returns(expectedTvShow);

            // Act
            var result = await _tmdbService.ImportTvShowAsync(tvShowId);

            // Assert
            _mockTvShowService.Received(1).CreateTvShowAsync(Arg.Is<CreateTvShowDto>(dto => 
                dto.Rating == null && // Personal rating should be null for imports
                dto.TmdbRating == 7.8)); // TMDB rating should be stored as-is
        }

        [Fact]
        public async Task ImportMovieAsync_ShouldStoreAllTmdbProperties()
        {
            // Arrange
            var movieId = 27205;
            var tmdbMovieDto = TestDataFactory.CreateTmdbMovieDto(movieId, "Inception");
            var expectedMovie = TestDataFactory.CreateMovie("Inception", 2010, movieId.ToString());

            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Returns(tmdbMovieDto);
            
            _mockMovieService
                .GetMovieByTitleAndYearAsync("Inception", 2010)
                .Returns((Movie?)null);
            
            _mockMovieService
                .CreateMovieAsync(Arg.Any<CreateMovieDto>())
                .Returns(expectedMovie);

            // Act
            var result = await _tmdbService.ImportMovieAsync(movieId);

            // Assert
            _mockMovieService.Received(1).CreateMovieAsync(Arg.Is<CreateMovieDto>(dto => 
                dto.TmdbId == movieId.ToString() &&
                dto.TmdbRating == tmdbMovieDto.VoteAverage &&
                dto.TmdbBackdropPath == tmdbMovieDto.BackdropPath &&
                dto.Tagline == tmdbMovieDto.Tagline &&
                dto.Homepage == tmdbMovieDto.Homepage &&
                dto.OriginalLanguage == tmdbMovieDto.OriginalLanguage &&
                dto.OriginalTitle == tmdbMovieDto.OriginalTitle));
        }

        [Fact]
        public async Task ImportTvShowAsync_ShouldStoreAllTmdbProperties()
        {
            // Arrange
            var tvShowId = 1399;
            var tmdbTvShowDto = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Game of Thrones");
            var expectedTvShow = TestDataFactory.CreateTvShow("Game of Thrones", 2011, tvShowId.ToString());

            _mockTmdbApiClient
                .GetTvShowDetailsAsync(tvShowId, "en-US")
                .Returns(tmdbTvShowDto);
            
            _mockTvShowService
                .GetTvShowByTitleAndYearAsync("Game of Thrones", 2011)
                .Returns((TvShow?)null);
            
            _mockTvShowService
                .CreateTvShowAsync(Arg.Any<CreateTvShowDto>())
                .Returns(expectedTvShow);

            // Act
            var result = await _tmdbService.ImportTvShowAsync(tvShowId);

            // Assert
            _mockTvShowService.Received(1).CreateTvShowAsync(Arg.Is<CreateTvShowDto>(dto => 
                dto.TmdbId == tvShowId.ToString() &&
                dto.TmdbRating == tmdbTvShowDto.VoteAverage &&
                dto.TmdbPosterPath == tmdbTvShowDto.PosterPath &&
                dto.Tagline == tmdbTvShowDto.Tagline &&
                dto.Homepage == tmdbTvShowDto.Homepage &&
                dto.OriginalLanguage == tmdbTvShowDto.OriginalLanguage &&
                dto.OriginalName == tmdbTvShowDto.OriginalName));
        }

        [Fact]
        public async Task ImportMovieAsync_ShouldLogErrorAndRethrow_WhenExceptionOccurs()
        {
            // Arrange
            var movieId = 27205;
            var expectedException = new InvalidOperationException("TMDB API error");

            _mockTmdbApiClient
                .GetMovieDetailsAsync(movieId, "en-US")
                .Throws(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _tmdbService.ImportMovieAsync(movieId));
            
            exception.Should().Be(expectedException);
        }

        #endregion

        #region Utility Operations Tests

        [Fact]
        public void GetImageUrl_ShouldReturnCorrectUrl_WhenImagePathProvided()
        {
            // Arrange
            var imagePath = "/test-image.jpg";
            var size = "w500";
            var expectedUrl = "https://image.tmdb.org/t/p/w500/test-image.jpg";

            _mockTmdbApiClient
                .GetImageUrl(imagePath, size)
                .Returns(expectedUrl);

            // Act
            var result = _tmdbService.GetImageUrl(imagePath, size);

            // Assert
            result.Should().Be(expectedUrl);
            _mockTmdbApiClient.Received(1).GetImageUrl(imagePath, size);
        }

        #endregion
    }
}
