using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class TraktControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public TraktControllerIntegrationTests(ApiFactory factory)
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

        #region Status Tests

        [Fact]
        public async Task GetStatus_WhenConnected_ShouldReturnOkWithConnectedStatus()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.GetStatusAsync()
                    .Returns(new TraktConnectionStatusDto { Connected = true, Username = "testuser" }),
                null);

            // Act
            var response = await client.GetAsync("/api/trakt/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<TraktConnectionStatusDto>(content, _jsonOptions);
            Assert.NotNull(status);
            Assert.True(status.Connected);
            Assert.Equal("testuser", status.Username);
        }

        [Fact]
        public async Task GetStatus_WhenDisconnected_ShouldReturnOkWithDisconnectedStatus()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.GetStatusAsync()
                    .Returns(new TraktConnectionStatusDto { Connected = false, Username = null }),
                null);

            // Act
            var response = await client.GetAsync("/api/trakt/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<TraktConnectionStatusDto>(content, _jsonOptions);
            Assert.NotNull(status);
            Assert.False(status.Connected);
            Assert.Null(status.Username);
        }

        #endregion

        #region Device Auth Tests

        [Fact]
        public async Task StartDeviceAuth_ShouldReturnOkWithDeviceCode()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                null,
                api => api.GetDeviceCodeAsync().Returns(new TraktDeviceCodeDto
                {
                    DeviceCode = "test-device-code",
                    UserCode = "ABCD1234",
                    VerificationUrl = "https://trakt.tv/activate",
                    ExpiresIn = 600,
                    Interval = 5
                }));

            // Act
            var response = await client.PostAsync("/api/trakt/auth/device-code", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var deviceCode = JsonSerializer.Deserialize<TraktDeviceCodeDto>(content, _jsonOptions);
            Assert.NotNull(deviceCode);
            Assert.Equal("test-device-code", deviceCode.DeviceCode);
            Assert.Equal("ABCD1234", deviceCode.UserCode);
            Assert.Equal("https://trakt.tv/activate", deviceCode.VerificationUrl);
        }

        [Fact]
        public async Task StartDeviceAuth_WhenApiThrows_ShouldReturn500()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                null,
                api => api.GetDeviceCodeAsync().Throws(new Exception("API error")));

            // Act
            var response = await client.PostAsync("/api/trakt/auth/device-code", null);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        #endregion

        #region Poll Tests

        [Fact]
        public async Task PollDeviceToken_WhenPending_ShouldReturnOkWithPendingStatus()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                null,
                api => api.PollDeviceTokenAsync("test-code").Returns((TraktOAuthTokenDto?)null));

            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { deviceCode = "test-code" }, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await client.PostAsync("/api/trakt/auth/poll", requestBody);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            Assert.Equal("pending", doc.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task PollDeviceToken_WhenAuthorized_ShouldReturnOkWithAuthorizedStatus()
        {
            // Arrange
            var token = new TraktOAuthTokenDto
            {
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token",
                TokenType = "Bearer",
                ExpiresIn = 7776000,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var (client, mockSyncService, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.SaveTokenAsync(Arg.Any<TraktOAuthTokenDto>()).Returns(Task.CompletedTask),
                api => api.PollDeviceTokenAsync("test-code").Returns(token));

            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { deviceCode = "test-code" }, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await client.PostAsync("/api/trakt/auth/poll", requestBody);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            Assert.Equal("authorized", doc.RootElement.GetProperty("status").GetString());

            await mockSyncService.Received(1).SaveTokenAsync(Arg.Any<TraktOAuthTokenDto>());
        }

        [Fact]
        public async Task PollDeviceToken_WhenCodeExpired_ShouldReturnBadRequest()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                null,
                api => api.PollDeviceTokenAsync("test-code")
                    .Throws(new InvalidOperationException("Device code has expired")));

            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { deviceCode = "test-code" }, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await client.PostAsync("/api/trakt/auth/poll", requestBody);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Disconnect Tests

        [Fact]
        public async Task Disconnect_ShouldReturnOk()
        {
            // Arrange
            var (client, mockSyncService, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.DisconnectAsync().Returns(Task.CompletedTask),
                null);

            // Act
            var response = await client.PostAsync("/api/trakt/disconnect", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await mockSyncService.Received(1).DisconnectAsync();
        }

        #endregion

        #region Sync Tests

        [Fact]
        public async Task SyncWatched_ShouldReturnOkWithResult()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.SyncWatchedAsync().Returns(new TraktSyncResultDto
                {
                    Success = true,
                    MoviesCreated = 5,
                    ShowsCreated = 3,
                    EpisodesCreated = 20,
                    StartedAt = DateTime.UtcNow.AddSeconds(-10),
                    CompletedAt = DateTime.UtcNow
                }),
                null);

            // Act
            var response = await client.PostAsync("/api/trakt/sync/watched", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TraktSyncResultDto>(content, _jsonOptions);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(5, result.MoviesCreated);
            Assert.Equal(3, result.ShowsCreated);
            Assert.Equal(20, result.EpisodesCreated);
        }

        [Fact]
        public async Task SyncWatchlist_ShouldReturnOkWithResult()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.SyncWatchlistAsync().Returns(new TraktSyncResultDto
                {
                    Success = true,
                    WatchlistItemsProcessed = 10,
                    StartedAt = DateTime.UtcNow.AddSeconds(-5),
                    CompletedAt = DateTime.UtcNow
                }),
                null);

            // Act
            var response = await client.PostAsync("/api/trakt/sync/watchlist", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TraktSyncResultDto>(content, _jsonOptions);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(10, result.WatchlistItemsProcessed);
        }

        [Fact]
        public async Task SyncRatings_ShouldReturnOkWithResult()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.SyncRatingsAsync().Returns(new TraktSyncResultDto
                {
                    Success = true,
                    RatingsProcessed = 15,
                    StartedAt = DateTime.UtcNow.AddSeconds(-3),
                    CompletedAt = DateTime.UtcNow
                }),
                null);

            // Act
            var response = await client.PostAsync("/api/trakt/sync/ratings", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TraktSyncResultDto>(content, _jsonOptions);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(15, result.RatingsProcessed);
        }

        [Fact]
        public async Task SyncAll_ShouldReturnOkWithCombinedResult()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.SyncAllAsync().Returns(new TraktSyncResultDto
                {
                    Success = true,
                    MoviesCreated = 5,
                    MoviesUpdated = 2,
                    ShowsCreated = 3,
                    ShowsUpdated = 1,
                    EpisodesCreated = 20,
                    EpisodesUpdated = 5,
                    WatchlistItemsProcessed = 10,
                    RatingsProcessed = 15,
                    StartedAt = DateTime.UtcNow.AddSeconds(-30),
                    CompletedAt = DateTime.UtcNow
                }),
                null);

            // Act
            var response = await client.PostAsync("/api/trakt/sync/all", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TraktSyncResultDto>(content, _jsonOptions);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(5, result.MoviesCreated);
            Assert.Equal(2, result.MoviesUpdated);
            Assert.Equal(3, result.ShowsCreated);
            Assert.Equal(1, result.ShowsUpdated);
            Assert.Equal(20, result.EpisodesCreated);
            Assert.Equal(5, result.EpisodesUpdated);
            Assert.Equal(10, result.WatchlistItemsProcessed);
            Assert.Equal(15, result.RatingsProcessed);
        }

        [Fact]
        public async Task SyncWatched_WhenServiceThrows_ShouldReturn500()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<ITraktSyncService, ITraktApiClient>(
                sync => sync.SyncWatchedAsync().Throws(new Exception("Sync failed")),
                null);

            // Act
            var response = await client.PostAsync("/api/trakt/sync/watched", null);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TraktSyncResultDto>(content, _jsonOptions);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Sync failed", result.Errors);
        }

        #endregion
    }
}
