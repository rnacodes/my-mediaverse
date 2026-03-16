using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class TmdbApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<TmdbApiClient>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly HttpClient _httpClient;
        private readonly TmdbApiClient _tmdbApiClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public TmdbApiClientTests()
        {
            // Ensure Environment Variable matches test expectation
            Environment.SetEnvironmentVariable("TMDB_API_KEY", "test-api-key");

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<TmdbApiClient>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.themoviedb.org/3/")
            };

            // Setup configuration to return test API key (fallback)
            _mockConfiguration.Setup(x => x["ApiKeys:TMDB"]).Returns("test-api-key");

            _tmdbApiClient = new TmdbApiClient(_httpClient, _mockLogger.Object, _mockConfiguration.Object);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        #region Search Operations Tests

        [Fact]
        public async Task SearchMoviesAsync_ShouldReturnMovieSearchResults_WhenValidResponseReceived()
        {
            // Arrange
            var query = "inception";
            var expectedMovie = TestDataFactory.CreateTmdbMovieDto(27205, "Inception");
            var expectedResult = TestDataFactory.CreateTmdbMovieSearchResultDto(expectedMovie);
            var jsonResponse = JsonSerializer.Serialize(expectedResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.SearchMoviesAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results[0].Title.Should().Be("Inception");
            result.Results[0].Id.Should().Be(27205);

            VerifyHttpRequest("GET", $"search/movie?api_key=test-api-key&query={Uri.EscapeDataString(query)}&page=1&language=en-US");
        }

        [Fact]
        public async Task SearchTvShowsAsync_ShouldReturnTvSearchResults_WhenValidResponseReceived()
        {
            // Arrange
            var query = "game of thrones";
            var expectedTvShow = TestDataFactory.CreateTmdbTvShowDto(1399, "Game of Thrones");
            var expectedResult = TestDataFactory.CreateTmdbTvSearchResultDto(expectedTvShow);
            var jsonResponse = JsonSerializer.Serialize(expectedResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.SearchTvShowsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results[0].Name.Should().Be("Game of Thrones");
            result.Results[0].Id.Should().Be(1399);

            VerifyHttpRequest("GET", $"search/tv?api_key=test-api-key&query={Uri.EscapeDataString(query)}&page=1&language=en-US");
        }

        [Fact]
        public async Task SearchMoviesAsync_ShouldReturnEmptyResult_WhenEmptyResponseReceived()
        {
            // Arrange
            var query = "nonexistentmovie";
            var emptyResult = new TmdbMovieSearchResultDto { Page = 1, Results = Array.Empty<TmdbMovieDto>(), TotalPages = 0, TotalResults = 0 };
            var jsonResponse = JsonSerializer.Serialize(emptyResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.SearchMoviesAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().BeEmpty();
            result.TotalResults.Should().Be(0);
        }

        [Fact]
        public async Task SearchMoviesAsync_ShouldThrowException_WhenHttpRequestFails()
        {
            // Arrange
            var query = "inception";
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _tmdbApiClient.SearchMoviesAsync(query));
        }

        #endregion

        #region Detail Operations Tests

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldReturnMovieDetails_WhenValidResponseReceived()
        {
            // Arrange
            var movieId = 27205;
            var expectedMovie = TestDataFactory.CreateTmdbMovieDto(movieId, "Inception");
            var jsonResponse = JsonSerializer.Serialize(expectedMovie, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.GetMovieDetailsAsync(movieId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(movieId);
            result.Title.Should().Be("Inception");
            result.Runtime.Should().Be(148);

            VerifyHttpRequest("GET", $"movie/{movieId}?api_key=test-api-key&language=en-US");
        }

        [Fact]
        public async Task GetTvShowDetailsAsync_ShouldReturnTvShowDetails_WhenValidResponseReceived()
        {
            // Arrange
            var tvShowId = 1399;
            var expectedTvShow = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Game of Thrones");
            var jsonResponse = JsonSerializer.Serialize(expectedTvShow, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.GetTvShowDetailsAsync(tvShowId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(tvShowId);
            result.Name.Should().Be("Game of Thrones");
            result.NumberOfSeasons.Should().Be(8);

            VerifyHttpRequest("GET", $"tv/{tvShowId}?api_key=test-api-key&language=en-US");
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldThrowException_WhenMovieNotFound()
        {
            // Arrange
            var movieId = 999999;
            SetupHttpResponse(HttpStatusCode.NotFound, "Not Found");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _tmdbApiClient.GetMovieDetailsAsync(movieId));
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldThrowInvalidOperationException_WhenNullResponseReceived()
        {
            // Arrange
            var movieId = 27205;
            SetupHttpResponse(HttpStatusCode.OK, "null");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _tmdbApiClient.GetMovieDetailsAsync(movieId));
            exception.Message.Should().Contain($"Movie with ID {movieId} not found");
        }

        #endregion

        #region Popular Content Tests

        [Fact]
        public async Task GetPopularMoviesAsync_ShouldReturnPopularMovies_WhenValidResponseReceived()
        {
            // Arrange
            var expectedMovie = TestDataFactory.CreateTmdbMovieDto(27205, "Inception");
            var expectedResult = TestDataFactory.CreateTmdbMovieSearchResultDto(expectedMovie);
            var jsonResponse = JsonSerializer.Serialize(expectedResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.GetPopularMoviesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results[0].Title.Should().Be("Inception");

            VerifyHttpRequest("GET", "movie/popular?api_key=test-api-key&page=1&language=en-US");
        }

        [Fact]
        public async Task GetPopularTvShowsAsync_ShouldReturnPopularTvShows_WhenValidResponseReceived()
        {
            // Arrange
            var expectedTvShow = TestDataFactory.CreateTmdbTvShowDto(1399, "Game of Thrones");
            var expectedResult = TestDataFactory.CreateTmdbTvSearchResultDto(expectedTvShow);
            var jsonResponse = JsonSerializer.Serialize(expectedResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.GetPopularTvShowsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results[0].Name.Should().Be("Game of Thrones");

            VerifyHttpRequest("GET", "tv/popular?api_key=test-api-key&page=1&language=en-US");
        }

        #endregion

        #region Genre Operations Tests

        [Fact]
        public async Task GetMovieGenresAsync_ShouldReturnGenres_WhenValidResponseReceived()
        {
            // Arrange
            var expectedGenres = new TmdbGenreListDto
            {
                Genres = new[]
                {
                    new TmdbGenreDto { Id = 28, Name = "Action" },
                    new TmdbGenreDto { Id = 878, Name = "Science Fiction" }
                }
            };
            var jsonResponse = JsonSerializer.Serialize(expectedGenres, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.GetMovieGenresAsync();

            // Assert
            result.Should().NotBeNull();
            result.Genres.Should().HaveCount(2);
            result.Genres[0].Name.Should().Be("Action");
            result.Genres[1].Name.Should().Be("Science Fiction");

            VerifyHttpRequest("GET", "genre/movie/list?api_key=test-api-key&language=en-US");
        }

        [Fact]
        public async Task GetTvGenresAsync_ShouldReturnGenres_WhenValidResponseReceived()
        {
            // Arrange
            var expectedGenres = new TmdbGenreListDto
            {
                Genres = new[]
                {
                    new TmdbGenreDto { Id = 18, Name = "Drama" },
                    new TmdbGenreDto { Id = 10759, Name = "Action & Adventure" }
                }
            };
            var jsonResponse = JsonSerializer.Serialize(expectedGenres, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _tmdbApiClient.GetTvGenresAsync();

            // Assert
            result.Should().NotBeNull();
            result.Genres.Should().HaveCount(2);
            result.Genres[0].Name.Should().Be("Drama");
            result.Genres[1].Name.Should().Be("Action & Adventure");

            VerifyHttpRequest("GET", "genre/tv/list?api_key=test-api-key&language=en-US");
        }

        #endregion

        #region Utility Tests

        [Fact]
        public void GetImageUrl_ShouldReturnCorrectUrl_WhenImagePathProvided()
        {
            // Arrange
            var imagePath = "/test-image.jpg";
            var size = "w500";

            // Act
            var result = _tmdbApiClient.GetImageUrl(imagePath, size);

            // Assert
            result.Should().Be("https://image.tmdb.org/t/p/w500/test-image.jpg");
        }

        [Fact]
        public void GetImageUrl_ShouldReturnEmptyString_WhenImagePathIsNull()
        {
            // Act
            var result = _tmdbApiClient.GetImageUrl(null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetImageUrl_ShouldReturnEmptyString_WhenImagePathIsEmpty()
        {
            // Act
            var result = _tmdbApiClient.GetImageUrl("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetImageUrl_ShouldUseDefaultSize_WhenSizeNotProvided()
        {
            // Arrange
            var imagePath = "/test-image.jpg";

            // Act
            var result = _tmdbApiClient.GetImageUrl(imagePath);

            // Assert
            result.Should().Be("https://image.tmdb.org/t/p/w500/test-image.jpg");
        }

        #endregion

        #region Helper Methods

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void VerifyHttpRequest(string method, string expectedUriStart)
        {
            // Extract path and query from expectedUriStart
            var parts = expectedUriStart.Split('?');
            var path = parts[0];
            var query = parts.Length > 1 ? parts[1] : string.Empty;

            // Normalize query components for comparison (handles order differences)
            var expectedQueryParams = query.Split('&').Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();

            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method.ToString() == method &&
                        req.RequestUri!.ToString().Contains(path) &&
                        CheckQueryParams(req.RequestUri!.Query, expectedQueryParams)),
                    ItExpr.IsAny<CancellationToken>());
        }

        private bool CheckQueryParams(string actualQuery, List<string> expectedQueryParams)
        {
            if (string.IsNullOrEmpty(actualQuery) && expectedQueryParams.Count == 0) return true;
            if (string.IsNullOrEmpty(actualQuery)) return false;

            var actualParams = actualQuery.TrimStart('?').Split('&').Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();
            
            // Check if all expected params are present in actual params
            return expectedQueryParams.All(expected => actualParams.Any(actual => 
                string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(Uri.UnescapeDataString(actual), Uri.UnescapeDataString(expected), StringComparison.OrdinalIgnoreCase)));
        }

        #endregion
    }
}
