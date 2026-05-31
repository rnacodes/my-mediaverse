using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class TmdbControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public TmdbControllerIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region Search Endpoint Tests

        [Fact]
        public async Task SearchMovies_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/tmdb/search/movies?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchMovies_ShouldReturnBadRequest_WhenQueryIsMissing()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/tmdb/search/movies");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchTvShows_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/tmdb/search/tv?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchMulti_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/tmdb/search/multi?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Image URL Tests

        [Fact]
        public async Task GetImageUrl_ShouldReturnImageUrl_WhenValidImagePathProvided()
        {
            // Arrange
            var imagePath = "/test-image.jpg";
            var size = "w500";
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/tmdb/image?imagePath={imagePath}&size={size}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var imageUrl = await response.Content.ReadAsStringAsync();
            imageUrl.Should().Contain("https://image.tmdb.org/t/p/w500/test-image.jpg");
        }

        [Fact]
        public async Task GetImageUrl_ShouldReturnBadRequest_WhenImagePathIsEmpty()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/tmdb/image?imagePath=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetImageUrl_ShouldReturnBadRequest_WhenImagePathIsMissing()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/tmdb/image");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Import Tests with Mocked TMDB Service

        [Fact]
        public async Task ImportMovie_ShouldReturnCreatedMovie_WhenValidMovieIdProvided()
        {
            // Arrange
            var movieId = 27205;
            var expectedMovie = TestDataFactory.CreateMovie("Inception", 2010, movieId.ToString());
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.ImportMovieAsync(movieId, "en-US").Returns(expectedMovie));

            // Act
            var response = await client.PostAsync($"/api/tmdb/import/movie/{movieId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var movie = await response.Content.ReadFromJsonAsync<Movie>(_jsonOptions);
            movie.Should().NotBeNull();
            movie!.Title.Should().Be("Inception");
            movie.ReleaseYear.Should().Be(2010);
        }

        [Fact]
        public async Task ImportTvShow_ShouldReturnCreatedTvShow_WhenValidTvShowIdProvided()
        {
            // Arrange
            var tvShowId = 1399;
            var expectedTvShow = TestDataFactory.CreateTvShow("Game of Thrones", 2011, tvShowId.ToString());
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.ImportTvShowAsync(tvShowId, "en-US").Returns(expectedTvShow));

            // Act
            var response = await client.PostAsync($"/api/tmdb/import/tv/{tvShowId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var tvShow = await response.Content.ReadFromJsonAsync<TvShow>(_jsonOptions);
            tvShow.Should().NotBeNull();
            tvShow!.Title.Should().Be("Game of Thrones");
            tvShow.FirstAirYear.Should().Be(2011);
        }

        [Fact]
        public async Task ImportMovie_ShouldReturnNotFound_WhenMovieDoesNotExist()
        {
            // Arrange
            var movieId = 999999;
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.ImportMovieAsync(movieId, "en-US")
                    .Throws(new InvalidOperationException($"Movie with ID {movieId} not found")));

            // Act
            var response = await client.PostAsync($"/api/tmdb/import/movie/{movieId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportTvShow_ShouldReturnNotFound_WhenTvShowDoesNotExist()
        {
            // Arrange
            var tvShowId = 999999;
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.ImportTvShowAsync(tvShowId, "en-US")
                    .Throws(new InvalidOperationException($"TV show with ID {tvShowId} not found")));

            // Act
            var response = await client.PostAsync($"/api/tmdb/import/tv/{tvShowId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportMovie_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var movieId = 27205;
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.ImportMovieAsync(movieId, "en-US")
                    .Throws(new Exception("Unexpected error")));

            // Act
            var response = await client.PostAsync($"/api/tmdb/import/movie/{movieId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Detail Endpoint Tests with Mocked Service

        [Fact]
        public async Task GetMovieDetails_ShouldReturnMovieDetails_WhenValidIdProvided()
        {
            // Arrange
            var movieId = 27205;
            var expectedMovie = TestDataFactory.CreateTmdbMovieDto(movieId, "Inception");
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieDetailsAsync(movieId, "en-US").Returns(expectedMovie));

            // Act
            var response = await client.GetAsync($"/api/tmdb/movie/{movieId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var movie = await response.Content.ReadFromJsonAsync<TmdbMovieDto>();
            movie.Should().NotBeNull();
            movie!.Title.Should().Be("Inception");
            movie.Id.Should().Be(movieId);
        }

        [Fact]
        public async Task GetTvShowDetails_ShouldReturnTvShowDetails_WhenValidIdProvided()
        {
            // Arrange
            var tvShowId = 1399;
            var expectedTvShow = TestDataFactory.CreateTmdbTvShowDto(tvShowId, "Game of Thrones");
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetTvShowDetailsAsync(tvShowId, "en-US").Returns(expectedTvShow));

            // Act
            var response = await client.GetAsync($"/api/tmdb/tv/{tvShowId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tvShow = await response.Content.ReadFromJsonAsync<TmdbTvShowDto>();
            tvShow.Should().NotBeNull();
            tvShow!.Name.Should().Be("Game of Thrones");
            tvShow.Id.Should().Be(tvShowId);
        }

        [Fact]
        public async Task GetMovieDetails_ShouldReturnNotFound_WhenMovieDoesNotExist()
        {
            // Arrange
            var movieId = 999999;
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieDetailsAsync(movieId, "en-US")
                    .Throws(new InvalidOperationException($"Movie with ID {movieId} not found")));

            // Act
            var response = await client.GetAsync($"/api/tmdb/movie/{movieId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Popular Content Tests

        [Fact]
        public async Task GetPopularMovies_ShouldReturnPopularMovies_WhenCalled()
        {
            // Arrange
            var expectedMovie = TestDataFactory.CreateTmdbMovieDto(27205, "Inception");
            var expectedResult = TestDataFactory.CreateTmdbMovieSearchResultDto(expectedMovie);
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetPopularMoviesAsync(1, "en-US").Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/tmdb/movies/popular");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TmdbMovieSearchResultDto>();
            result.Should().NotBeNull();
            result!.Results.Should().HaveCount(1);
            result.Results[0].Title.Should().Be("Inception");
        }

        [Fact]
        public async Task GetPopularTvShows_ShouldReturnPopularTvShows_WhenCalled()
        {
            // Arrange
            var expectedTvShow = TestDataFactory.CreateTmdbTvShowDto(1399, "Game of Thrones");
            var expectedResult = TestDataFactory.CreateTmdbTvSearchResultDto(expectedTvShow);
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetPopularTvShowsAsync(1, "en-US").Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/tmdb/tv/popular");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TmdbTvSearchResultDto>();
            result.Should().NotBeNull();
            result!.Results.Should().HaveCount(1);
            result.Results[0].Name.Should().Be("Game of Thrones");
        }

        #endregion

        #region Genre Tests

        [Fact]
        public async Task GetMovieGenres_ShouldReturnGenres_WhenCalled()
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
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieGenresAsync("en-US").Returns(expectedGenres));

            // Act
            var response = await client.GetAsync("/api/tmdb/genres/movies");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TmdbGenreListDto>();
            result.Should().NotBeNull();
            result!.Genres.Should().HaveCount(2);
            result.Genres[0].Name.Should().Be("Action");
        }

        [Fact]
        public async Task GetTvGenres_ShouldReturnGenres_WhenCalled()
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
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetTvGenresAsync("en-US").Returns(expectedGenres));

            // Act
            var response = await client.GetAsync("/api/tmdb/genres/tv");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TmdbGenreListDto>();
            result.Should().NotBeNull();
            result!.Genres.Should().HaveCount(2);
            result.Genres[0].Name.Should().Be("Drama");
        }

        #endregion
    }
}
