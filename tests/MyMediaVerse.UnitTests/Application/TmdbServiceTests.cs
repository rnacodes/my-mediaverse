using AwesomeAssertions;
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
        private readonly ILogger<TmdbService> _mockLogger;
        private readonly TmdbService _tmdbService;

        public TmdbServiceTests()
        {
            _mockTmdbApiClient = Substitute.For<ITmdbApiClient>();
            _mockLogger = Substitute.For<ILogger<TmdbService>>();
            
            _tmdbService = new TmdbService(
                _mockTmdbApiClient,
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
