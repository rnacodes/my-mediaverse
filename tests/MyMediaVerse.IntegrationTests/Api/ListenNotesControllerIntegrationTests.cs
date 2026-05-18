using System.Net;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class ListenNotesControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;

        public ListenNotesControllerIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region Search Endpoint Tests

        [Fact]
        public async Task Search_ShouldReturnBadRequest_WhenQueryIsEmpty()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/listennotes/search?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Search_ShouldReturnBadRequest_WhenQueryIsMissing()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/listennotes/search");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Search_ShouldReturnOk_WhenValidQueryProvided()
        {
            // Arrange
            var expectedResult = CreateSearchResultDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.SearchAsync("test", null, null, null, null, null, null, null, null, null, null, null, null, null)
                    .Returns(expectedResult));

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
            var expectedResult = CreateSearchResultDto();
            var (client, mockService) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.SearchAsync("test", "podcast", 10, 30, 60, "1,2,3", "2023-01-01", "2022-01-01", "title", "en", "us", "1", "1", "1")
                    .Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/listennotes/search?query=test&type=podcast&offset=10&lenMin=30&lenMax=60&genreIds=1,2,3&publishedBefore=2023-01-01&publishedAfter=2022-01-01&onlyIn=title&language=en&region=us&sortByDate=1&safeMode=1&uniquePodcasts=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await mockService.Received(1).SearchAsync("test", "podcast", 10, 30, 60, "1,2,3", "2023-01-01", "2022-01-01", "title", "en", "us", "1", "1", "1");
        }

        #endregion

        #region Podcast Endpoint Tests

        [Fact]
        public async Task GetPodcast_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var expectedResult = CreatePodcastSeriesDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetPodcastByIdAsync("test-id", null).Returns(expectedResult));

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
            var expectedResult = CreateBestPodcastsDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetBestPodcastsAsync(null, null, null, null, null).Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/listennotes/best-podcasts");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPodcastRecommendations_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var expectedResult = CreateRecommendationsDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetPodcastRecommendationsAsync("test-id", null).Returns(expectedResult));

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
            var expectedResult = CreatePodcastEpisodeDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetEpisodeByIdAsync("test-id").Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/listennotes/episodes/test-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetEpisodeRecommendations_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var expectedResult = CreateRecommendationsDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetEpisodeRecommendationsAsync("test-id", null).Returns(expectedResult));

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
            var expectedResult = CreatePlaylistsDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetPlaylistsAsync().Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/listennotes/playlists");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetPlaylist_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var expectedResult = CreatePlaylistDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetPlaylistByIdAsync("test-id").Returns(expectedResult));

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
            var expectedResult = CreateGenresDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetGenresAsync().Returns(expectedResult));

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
            var expectedResult = CreateCuratedPodcastsDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetCuratedPodcastsAsync(null).Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/listennotes/curated-podcasts");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetCuratedPodcast_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var expectedResult = CreateCuratedPodcastDto();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.GetCuratedPodcastByIdAsync("test-id").Returns(expectedResult));

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
            var expectedResult = CreatePodcastSeries();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.ImportPodcastSeriesAsync("test-id").Returns(expectedResult));

            // Act
            var response = await client.PostAsync("/api/listennotes/import/podcast/test-id", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportPodcast_ShouldReturnNotFound_WhenPodcastNotFound()
        {
            // Arrange
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.ImportPodcastSeriesAsync("invalid-id")
                    .Throws(new InvalidOperationException("Podcast not found")));

            // Act
            var response = await client.PostAsync("/api/listennotes/import/podcast/invalid-id", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportPodcastEpisode_ShouldReturnOk_WhenValidIdProvided()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var expectedResult = CreatePodcastEpisode(seriesId);
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.ImportPodcastEpisodeAsync("test-id", seriesId).Returns(expectedResult));

            // Act
            var response = await client.PostAsync($"/api/listennotes/import/episode/test-id?seriesId={seriesId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportPodcastEpisode_ShouldReturnNotFound_WhenEpisodeNotFound()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IListenNotesService>(mock =>
                mock.ImportPodcastEpisodeAsync("invalid-id", seriesId)
                    .Throws(new InvalidOperationException("Episode not found")));

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
