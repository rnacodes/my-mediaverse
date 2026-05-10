using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    public class YouTubePlaylistControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubePlaylistControllerIntegrationTests(WebApplicationFactory factory)
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

        #region GetAllPlaylists

        [Fact]
        public async Task GetAllPlaylists_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/youtubeplaylist");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var playlists = await response.Content.ReadFromJsonAsync<IEnumerable<YouTubePlaylistResponseDto>>(_jsonOptions);
            playlists.Should().NotBeNull();
        }

        #endregion

        #region GetPlaylist

        [Fact]
        public async Task GetPlaylist_ShouldReturnNotFound_WhenPlaylistDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/youtubeplaylist/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetPlaylistByExternalId

        [Fact]
        public async Task GetPlaylistByExternalId_ShouldReturnNotFound_WhenPlaylistDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/youtubeplaylist/by-external/PLnonexistent{Guid.NewGuid():N}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetPlaylistVideos

        [Fact]
        public async Task GetPlaylistVideos_ShouldReturnOk_WhenPlaylistDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/youtubeplaylist/{Guid.NewGuid()}/videos");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region DeletePlaylist

        [Fact]
        public async Task DeletePlaylist_ShouldReturnNotFound_WhenPlaylistDoesNotExist()
        {
            var response = await _client.DeleteAsync($"/api/youtubeplaylist/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion
    }
}
