using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class TraktControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public TraktControllerIntegrationTests(WebApplicationFactory factory)
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

        #region Status Tests

        [Fact]
        public async Task GetStatus_WhenConnected_ShouldReturnOkWithConnectedStatus()
        {
            // Arrange
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.GetStatusAsync())
                .ReturnsAsync(new TraktConnectionStatusDto { Connected = true, Username = "testuser" });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.GetStatusAsync())
                .ReturnsAsync(new TraktConnectionStatusDto { Connected = false, Username = null });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockApiClient.Setup(c => c.GetDeviceCodeAsync())
                .ReturnsAsync(new TraktDeviceCodeDto
                {
                    DeviceCode = "test-device-code",
                    UserCode = "ABCD1234",
                    VerificationUrl = "https://trakt.tv/activate",
                    ExpiresIn = 600,
                    Interval = 5
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockApiClient.Setup(c => c.GetDeviceCodeAsync())
                .ThrowsAsync(new Exception("API error"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockApiClient.Setup(c => c.PollDeviceTokenAsync("test-code"))
                .ReturnsAsync((TraktOAuthTokenDto?)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();

            var token = new TraktOAuthTokenDto
            {
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token",
                TokenType = "Bearer",
                ExpiresIn = 7776000,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            mockApiClient.Setup(c => c.PollDeviceTokenAsync("test-code"))
                .ReturnsAsync(token);
            mockSyncService.Setup(s => s.SaveTokenAsync(It.IsAny<TraktOAuthTokenDto>()))
                .Returns(Task.CompletedTask);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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

            mockSyncService.Verify(s => s.SaveTokenAsync(It.IsAny<TraktOAuthTokenDto>()), Times.Once);
        }

        [Fact]
        public async Task PollDeviceToken_WhenCodeExpired_ShouldReturnBadRequest()
        {
            // Arrange
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockApiClient.Setup(c => c.PollDeviceTokenAsync("test-code"))
                .ThrowsAsync(new InvalidOperationException("Device code has expired"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.DisconnectAsync())
                .Returns(Task.CompletedTask);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.PostAsync("/api/trakt/disconnect", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            mockSyncService.Verify(s => s.DisconnectAsync(), Times.Once);
        }

        #endregion

        #region Sync Tests

        [Fact]
        public async Task SyncWatched_ShouldReturnOkWithResult()
        {
            // Arrange
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.SyncWatchedAsync())
                .ReturnsAsync(new TraktSyncResultDto
                {
                    Success = true,
                    MoviesCreated = 5,
                    ShowsCreated = 3,
                    EpisodesCreated = 20,
                    StartedAt = DateTime.UtcNow.AddSeconds(-10),
                    CompletedAt = DateTime.UtcNow
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.SyncWatchlistAsync())
                .ReturnsAsync(new TraktSyncResultDto
                {
                    Success = true,
                    WatchlistItemsProcessed = 10,
                    StartedAt = DateTime.UtcNow.AddSeconds(-5),
                    CompletedAt = DateTime.UtcNow
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.SyncRatingsAsync())
                .ReturnsAsync(new TraktSyncResultDto
                {
                    Success = true,
                    RatingsProcessed = 15,
                    StartedAt = DateTime.UtcNow.AddSeconds(-3),
                    CompletedAt = DateTime.UtcNow
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.SyncAllAsync())
                .ReturnsAsync(new TraktSyncResultDto
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
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
            var mockSyncService = new Mock<ITraktSyncService>();
            var mockApiClient = new Mock<ITraktApiClient>();
            mockSyncService.Setup(s => s.SyncWatchedAsync())
                .ThrowsAsync(new Exception("Sync failed"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockSyncService.Object);
                    services.AddScoped(_ => mockApiClient.Object);
                });
            }).CreateClient();

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
