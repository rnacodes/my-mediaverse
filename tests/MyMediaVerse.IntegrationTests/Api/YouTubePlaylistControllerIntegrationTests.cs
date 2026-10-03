using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Exceptions;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class YouTubePlaylistControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubePlaylistControllerIntegrationTests(ApiFactory factory)
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

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

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

        #region YouTube failures on import and sync

        [Fact]
        public async Task ImportPlaylist_WhenYouTubeHasNoSuchPlaylist_ShouldReturnNotFoundWithAnErrorObject()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubePlaylistService>(mock =>
                mock.ImportPlaylistFromYouTubeAsync("PLgone").Throws(new YouTubeResourceNotFoundException("playlist", "PLgone")));

            var response = await client.PostAsync("/api/youtubeplaylist/import/PLgone", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("error").GetString().Should().Contain("PLgone");
        }

        [Fact]
        public async Task ImportPlaylist_WhenTheDailyQuotaIsUsedUp_ShouldReturnServiceUnavailableWithTheQuotaFlag()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubePlaylistService>(mock =>
                mock.ImportPlaylistFromYouTubeAsync("PLany").Throws(new YouTubeQuotaExceededException("quota used up", "quotaExceeded")));

            var response = await client.PostAsync("/api/youtubeplaylist/import/PLany", null);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("quotaExceeded").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task SyncPlaylist_WhenThePlaylistIsNotStored_ShouldReturnNotFoundWithAnErrorObject()
        {
            var id = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubePlaylistService>(mock =>
                mock.SyncPlaylistVideosAsync(id).Throws(new InvalidOperationException($"Playlist with ID {id} not found")));

            var response = await client.PostAsync($"/api/youtubeplaylist/{id}/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("error").GetString().Should().Contain(id.ToString());
        }

        [Fact]
        public async Task SyncPlaylist_WhenTheDailyQuotaIsUsedUp_ShouldReturnServiceUnavailableWithTheQuotaFlag()
        {
            var id = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubePlaylistService>(mock =>
                mock.SyncPlaylistVideosAsync(id).Throws(new YouTubeQuotaExceededException("quota used up", "quotaExceeded")));

            var response = await client.PostAsync($"/api/youtubeplaylist/{id}/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("quotaExceeded").GetBoolean().Should().BeTrue();
        }

        #endregion

        #region Shared delete

        [Fact]
        public async Task DeletePlaylist_KeepsItsVideos_DropsTheLinks_AndCleansTheSearchIndex()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();
            var playlist = new YouTubePlaylist { Title = "Playlist to delete", PlaylistExternalId = "PLdelete", MediaType = MediaType.Playlist };
            var video = new Video { Title = "Keeps living", Platform = "YouTube", ExternalId = "keep0000002" };
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
                db.AddRange(playlist, video);
                db.Add(new YouTubePlaylistVideo { YouTubePlaylistId = playlist.Id, VideoId = video.Id, Position = 0 });
                await db.SaveChangesAsync();
            }

            var delete = await client.DeleteAsync($"/api/youtubeplaylist/{playlist.Id}");

            delete.IsSuccessStatusCode.Should().BeTrue();
            (await client.GetAsync("/api/media")).StatusCode.Should().Be(HttpStatusCode.OK);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
                (await db.MediaItems.AnyAsync(m => m.Id == playlist.Id)).Should().BeFalse();
                (await db.Set<YouTubePlaylistVideo>().AnyAsync(link => link.YouTubePlaylistId == playlist.Id)).Should().BeFalse();
                (await db.Videos.AnyAsync(v => v.Id == video.Id)).Should().BeTrue();
            }
            await typesense.Received(1).DeleteMediaItemAsync(playlist.Id);
            await typesense.DidNotReceive().DeleteMediaItemAsync(video.Id);
        }

        #endregion
    }
}
