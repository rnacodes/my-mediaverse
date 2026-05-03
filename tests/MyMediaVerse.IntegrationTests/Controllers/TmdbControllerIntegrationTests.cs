using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class TmdbControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public TmdbControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        #region Search Endpoint Tests

        [Fact]
        public async Task SearchMovies_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/tmdb/search/movies?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchMovies_ShouldReturnBadRequest_WhenQueryIsMissing()
        {
            // Act
            var response = await _client.GetAsync("/api/tmdb/search/movies");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchTvShows_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/tmdb/search/tv?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchMulti_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/tmdb/search/multi?query=");

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

            // Act
            var response = await _client.GetAsync($"/api/tmdb/image?imagePath={imagePath}&size={size}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var imageUrl = await response.Content.ReadAsStringAsync();
            imageUrl.Should().Contain("https://image.tmdb.org/t/p/w500/test-image.jpg");
        }

        [Fact]
        public async Task GetImageUrl_ShouldReturnBadRequest_WhenImagePathIsEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/tmdb/image?imagePath=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetImageUrl_ShouldReturnBadRequest_WhenImagePathIsMissing()
        {
            // Act
            var response = await _client.GetAsync("/api/tmdb/image");

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.ImportMovieAsync(movieId, "en-US"))
                        .ReturnsAsync(expectedMovie);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.ImportTvShowAsync(tvShowId, "en-US"))
                        .ReturnsAsync(expectedTvShow);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.ImportMovieAsync(movieId, "en-US"))
                        .ThrowsAsync(new InvalidOperationException($"Movie with ID {movieId} not found"));
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.ImportTvShowAsync(tvShowId, "en-US"))
                        .ThrowsAsync(new InvalidOperationException($"TV show with ID {tvShowId} not found"));
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.ImportMovieAsync(movieId, "en-US"))
                        .ThrowsAsync(new Exception("Unexpected error"));
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetMovieDetailsAsync(movieId, "en-US"))
                        .ReturnsAsync(expectedMovie);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetTvShowDetailsAsync(tvShowId, "en-US"))
                        .ReturnsAsync(expectedTvShow);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetMovieDetailsAsync(movieId, "en-US"))
                        .ThrowsAsync(new InvalidOperationException($"Movie with ID {movieId} not found"));
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetPopularMoviesAsync(1, "en-US"))
                        .ReturnsAsync(expectedResult);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetPopularTvShowsAsync(1, "en-US"))
                        .ReturnsAsync(expectedResult);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetMovieGenresAsync("en-US"))
                        .ReturnsAsync(expectedGenres);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
            
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real TMDB service with a mock
                    var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITmdbService));
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                    var mockTmdbService = new Mock<ITmdbService>();
                    mockTmdbService
                        .Setup(x => x.GetTvGenresAsync("en-US"))
                        .ReturnsAsync(expectedGenres);
                    
                    services.AddSingleton(mockTmdbService.Object);
                });
            });

            var client = factory.CreateClient();

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
