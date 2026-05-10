using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for ListenNotes API endpoints
    /// Uses WebApplicationFactory which configures mock ListenNotes API server
    /// Mock server: https://listen-api-test.listennotes.com/api/v2/
    /// </summary>
    [Trait("Category", "Integration")]
    public class ListenNotesControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ListenNotesControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        #region Search Endpoint Tests

        [Fact]
        public async Task Search_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var response = await _client.GetAsync("/api/listennotes/search?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Search_ShouldReturnBadRequest_WhenQueryIsMissing()
        {
            // Act
            var response = await _client.GetAsync("/api/listennotes/search");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Search_ShouldReturnOk_WhenValidQueryProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateSearchResultDto();
            
            mockService.SearchAsync("test", null, null, null, null, null, null, null, null, null, null, null, null, null)
                      .Returns(expectedResult);

            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            
            // Replace the service with our mock
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    // Add our mock
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/search?query=test");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Search_ShouldIncludeAllParameters_WhenAllParametersProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateSearchResultDto();
            
            mockService.SearchAsync("test", "podcast", 10, 30, 60, "1,2,3", "2023-01-01", "2022-01-01", "title", "en", "us", "1", "1", "1")
                      .Returns(expectedResult);

            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/search?query=test&type=podcast&offset=10&lenMin=30&lenMax=60&genreIds=1,2,3&publishedBefore=2023-01-01&publishedAfter=2022-01-01&onlyIn=title&language=en&region=us&sortByDate=1&safeMode=1&uniquePodcasts=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            mockService.Received(1).SearchAsync("test", "podcast", 10, 30, 60, "1,2,3", "2023-01-01", "2022-01-01", "title", "en", "us", "1", "1", "1");
        }

        #endregion

        #region Podcast Endpoint Tests

        [Fact]
        public async Task GetPodcast_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreatePodcastSeriesDto();
            
            mockService.GetPodcastByIdAsync("test-id", null)
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/podcasts/test-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetBestPodcasts_ShouldReturnOk_WhenCalled()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateBestPodcastsDto();
            
            mockService.GetBestPodcastsAsync(null, null, null, null, null)
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/best-podcasts");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPodcastRecommendations_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateRecommendationsDto();
            
            mockService.GetPodcastRecommendationsAsync("test-id", null)
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/podcasts/test-id/recommendations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Episode Endpoint Tests

        [Fact]
        public async Task GetEpisode_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreatePodcastEpisodeDto();
            
            mockService.GetEpisodeByIdAsync("test-id")
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/episodes/test-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetEpisodeRecommendations_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateRecommendationsDto();
            
            mockService.GetEpisodeRecommendationsAsync("test-id", null)
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/episodes/test-id/recommendations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Playlist Endpoint Tests

        [Fact]
        public async Task GetPlaylists_ShouldReturnOk_WhenCalled()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreatePlaylistsDto();
            
            mockService.GetPlaylistsAsync()
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/playlists");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPlaylist_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreatePlaylistDto();
            
            mockService.GetPlaylistByIdAsync("test-id")
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/playlists/test-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Genre Endpoint Tests

        [Fact]
        public async Task GetGenres_ShouldReturnOk_WhenCalled()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateGenresDto();
            
            mockService.GetGenresAsync()
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/genres");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Curated Content Endpoint Tests

        [Fact]
        public async Task GetCuratedPodcasts_ShouldReturnOk_WhenCalled()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateCuratedPodcastsDto();
            
            mockService.GetCuratedPodcastsAsync(null)
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/curated-podcasts");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetCuratedPodcast_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreateCuratedPodcastDto();
            
            mockService.GetCuratedPodcastByIdAsync("test-id")
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/listennotes/curated-podcasts/test-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Import Endpoint Tests

        [Fact]
        public async Task ImportPodcast_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var expectedResult = CreatePodcastSeries();
            
            mockService.ImportPodcastSeriesAsync("test-id")
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.PostAsync("/api/listennotes/import/podcast/test-id", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportPodcast_ShouldReturnNotFound_WhenPodcastNotFound()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            
            mockService.ImportPodcastSeriesAsync("invalid-id")
                      .Throws(new InvalidOperationException("Podcast not found"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.PostAsync("/api/listennotes/import/podcast/invalid-id", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportPodcastEpisode_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var seriesId = Guid.NewGuid();
            var expectedResult = CreatePodcastEpisode(seriesId);
            
            mockService.ImportPodcastEpisodeAsync("test-id", seriesId)
                      .Returns(expectedResult);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.PostAsync($"/api/listennotes/import/episode/test-id?seriesId={seriesId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportPodcastEpisode_ShouldReturnNotFound_WhenEpisodeNotFound()
        {
            // Arrange
            var mockService = Substitute.For<IListenNotesService>();
            var seriesId = Guid.NewGuid();
            
            mockService.ImportPodcastEpisodeAsync("invalid-id", seriesId)
                      .Throws(new InvalidOperationException("Episode not found"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListenNotesService));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddSingleton(mockService);
                });
            }).CreateClient();

            // Act
            var response = await client.PostAsync($"/api/listennotes/import/episode/invalid-id?seriesId={seriesId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Test Data Factory Methods

        private static SearchResultDto CreateSearchResultDto()
        {
            return new SearchResultDto
            {
                Count = 10,
                Total = 100,
                NextOffset = 10,
                Results = new List<PodcastSearchDto>
                {
                    new PodcastSearchDto
                    {
                        Id = "test-id",
                        TitleOriginal = "Test Podcast",
                        PublisherOriginal = "Test Publisher",
                        DescriptionOriginal = "Test Description"
                    }
                }
            };
        }

        private static PodcastSeriesDto CreatePodcastSeriesDto()
        {
            return new PodcastSeriesDto
            {
                Id = "test-podcast-id",
                Title = "Test Podcast",
                Publisher = "Test Publisher",
                Description = "Test Description",
                Image = "https://example.com/image.jpg",
                Website = "https://example.com",
                Episodes = new List<PodcastEpisodeDto>()
            };
        }

        private static PodcastEpisodeDto CreatePodcastEpisodeDto()
        {
            return new PodcastEpisodeDto
            {
                Id = "test-episode-id",
                Title = "Test Episode",
                Description = "Test Episode Description",
                AudioUrl = "https://example.com/audio.mp3",
                PublishDateMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DurationInSeconds = 3600
            };
        }

        private static ListenNotesBestPodcastsDto CreateBestPodcastsDto()
        {
            return new ListenNotesBestPodcastsDto
            {
                Id = 1,
                Name = "Best Podcasts",
                Total = 50,
                Podcasts = new List<PodcastSearchDto>()
            };
        }

        private static ListenNotesRecommendationsDto CreateRecommendationsDto()
        {
            return new ListenNotesRecommendationsDto
            {
                Recommendations = new List<PodcastSearchDto>()
            };
        }

        private static ListenNotesPlaylistsDto CreatePlaylistsDto()
        {
            return new ListenNotesPlaylistsDto
            {
                Playlists = new List<ListenNotesPlaylistDto>(),
                Total = 10
            };
        }

        private static ListenNotesPlaylistDto CreatePlaylistDto()
        {
            return new ListenNotesPlaylistDto
            {
                Id = "test-playlist-id",
                Name = "Test Playlist",
                Description = "Test Playlist Description"
            };
        }

        private static ListenNotesGenresDto CreateGenresDto()
        {
            return new ListenNotesGenresDto
            {
                Genres = new List<GenreDto>
                {
                    new GenreDto { Id = 1, Name = "Comedy" },
                    new GenreDto { Id = 2, Name = "News" }
                }
            };
        }

        private static ListenNotesCuratedPodcastsDto CreateCuratedPodcastsDto()
        {
            return new ListenNotesCuratedPodcastsDto
            {
                CuratedLists = new List<ListenNotesCuratedPodcastDto>(),
                Total = 5
            };
        }

        private static ListenNotesCuratedPodcastDto CreateCuratedPodcastDto()
        {
            return new ListenNotesCuratedPodcastDto
            {
                Id = "test-curated-id",
                Title = "Test Curated Podcast",
                Description = "Test Curated Description"
            };
        }

        private static Podcast CreatePodcast()
        {
            return new Podcast
            {
                Id = Guid.NewGuid(),
                Title = "Test Podcast",
                MediaType = MediaType.Podcast,
                PodcastType = PodcastType.Series,
                Publisher = "Test Publisher",
                Notes = "Test Description",
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
        }

        private static PodcastSeries CreatePodcastSeries()
        {
            return new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = "Test Podcast Series",
                Publisher = "Test Publisher",
                Description = "Test Description",
                ExternalId = "test-podcast-id",
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                MediaType = MediaType.Podcast
            };
        }

        private static PodcastEpisode CreatePodcastEpisode(Guid seriesId)
        {
            return new PodcastEpisode
            {
                Id = Guid.NewGuid(),
                Title = "Test Episode",
                SeriesId = seriesId,
                Description = "Test Episode Description",
                ExternalId = "test-episode-id",
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                MediaType = MediaType.Podcast
            };
        }

        #endregion
    }
}
