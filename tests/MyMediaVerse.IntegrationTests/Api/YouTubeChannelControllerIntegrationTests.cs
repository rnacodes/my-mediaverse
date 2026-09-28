using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Exceptions;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class YouTubeChannelControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeChannelControllerIntegrationTests(ApiFactory factory)
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

        private CreateYouTubeChannelDto CreateValidChannelDto(string? suffix = null)
        {
            suffix ??= Guid.NewGuid().ToString()[..8];
            return new CreateYouTubeChannelDto
            {
                Title = $"Test Channel {suffix}",
                ChannelExternalId = $"UC{suffix}",
                Description = "A test YouTube channel",
                Status = Status.Uncharted
            };
        }

        #region GetAllChannels

        [Fact]
        public async Task GetAllChannels_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/youtubechannel");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var channels = await response.Content.ReadFromJsonAsync<IEnumerable<YouTubeChannelResponseDto>>(_jsonOptions);
            channels.Should().NotBeNull();
        }

        #endregion

        #region CreateChannel

        [Fact]
        public async Task CreateChannel_ShouldReturnCreated_WhenValidDataProvided()
        {
            var dto = CreateValidChannelDto();

            var response = await _client.PostAsJsonAsync("/api/youtubechannel", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.Title.Should().Be(dto.Title);
            created.ChannelExternalId.Should().Be(dto.ChannelExternalId);
        }

        [Fact]
        public async Task CreateChannel_ShouldReturnBadRequest_WhenTitleIsMissing()
        {
            var dto = new CreateYouTubeChannelDto
            {
                Title = "",
                ChannelExternalId = "UCtest123"
            };

            var response = await _client.PostAsJsonAsync("/api/youtubechannel", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetChannel

        [Fact]
        public async Task GetChannel_ShouldReturnOk_WhenChannelExists()
        {
            var dto = CreateValidChannelDto();
            var createResponse = await _client.PostAsJsonAsync("/api/youtubechannel", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/youtubechannel/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var channel = await response.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);
            channel.Should().NotBeNull();
            channel!.Id.Should().Be(created.Id);
            channel.Title.Should().Be(dto.Title);
        }

        [Fact]
        public async Task GetChannel_ShouldReturnNotFound_WhenChannelDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/youtubechannel/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetChannelByExternalId

        [Fact]
        public async Task GetChannelByExternalId_ShouldReturnOk_WhenChannelExists()
        {
            var dto = CreateValidChannelDto();
            var createResponse = await _client.PostAsJsonAsync("/api/youtubechannel", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/youtubechannel/by-external/{dto.ChannelExternalId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var channel = await response.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);
            channel.Should().NotBeNull();
            channel!.ChannelExternalId.Should().Be(dto.ChannelExternalId);
        }

        #endregion

        #region UpdateChannel

        [Fact]
        public async Task UpdateChannel_ShouldReturnOk_WhenChannelExists()
        {
            var dto = CreateValidChannelDto();
            var createResponse = await _client.PostAsJsonAsync("/api/youtubechannel", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);

            var updateDto = new UpdateYouTubeChannelDto
            {
                Title = "Updated Channel Title",
                Description = "Updated description"
            };

            var response = await _client.PutAsJsonAsync($"/api/youtubechannel/{created!.Id}", updateDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);
            updated.Should().NotBeNull();
            updated!.Title.Should().Be("Updated Channel Title");
        }

        #endregion

        #region DeleteChannel

        [Fact]
        public async Task DeleteChannel_ShouldDeleteChannel_WhenChannelExists()
        {
            var dto = CreateValidChannelDto();
            var createResponse = await _client.PostAsJsonAsync("/api/youtubechannel", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);

            var response = await _client.DeleteAsync($"/api/youtubechannel/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/youtubechannel/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region CheckChannelExists

        [Fact]
        public async Task CheckChannelExists_ShouldReturnTrue_WhenChannelExists()
        {
            var dto = CreateValidChannelDto();
            await _client.PostAsJsonAsync("/api/youtubechannel", dto);

            var response = await _client.GetAsync($"/api/youtubechannel/exists/{dto.ChannelExternalId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.GetProperty("exists").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task CheckChannelExists_ShouldReturnFalse_WhenChannelDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/youtubechannel/exists/UCnonexistent{Guid.NewGuid():N}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.GetProperty("exists").GetBoolean().Should().BeFalse();
        }

        #endregion

        #region GetChannelVideos

        [Fact]
        public async Task GetChannelVideos_ShouldReturnOk_WhenChannelExists()
        {
            var dto = CreateValidChannelDto();
            var createResponse = await _client.PostAsJsonAsync("/api/youtubechannel", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<YouTubeChannelResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/youtubechannel/{created!.Id}/videos");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var videos = await response.Content.ReadFromJsonAsync<IEnumerable<VideoResponseDto>>(_jsonOptions);
            videos.Should().NotBeNull();
        }

        #endregion

        #region YouTube failures on import and sync

        [Fact]
        public async Task ImportChannel_WhenYouTubeHasNoSuchChannel_ShouldReturnNotFoundWithAnErrorObject()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.ImportChannelFromYouTubeAsync("UCgone").Throws(new YouTubeResourceNotFoundException("channel", "UCgone")));

            var response = await client.PostAsync("/api/youtubechannel/import/UCgone", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("error").GetString().Should().Contain("UCgone");
        }

        [Fact]
        public async Task ImportChannel_WhenTheDailyQuotaIsUsedUp_ShouldReturnServiceUnavailableWithTheQuotaFlag()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.ImportChannelFromYouTubeAsync("UCany").Throws(new YouTubeQuotaExceededException("quota used up", "quotaExceeded")));

            var response = await client.PostAsync("/api/youtubechannel/import/UCany", null);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("quotaExceeded").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task ImportChannel_WhenTheApiKeyIsNotConfigured_ShouldReturnServerError_NotBadRequest()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.ImportChannelFromYouTubeAsync("UCany").Throws(new YouTubeNotConfiguredException()));

            var response = await client.PostAsync("/api/youtubechannel/import/UCany", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task SyncChannelMetadata_WhenTheChannelLeftYouTube_ShouldReturnNotFoundWithAnErrorObject()
        {
            var id = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.SyncChannelMetadataAsync(id).Throws(new YouTubeResourceNotFoundException("channel", "UCgone")));

            var response = await client.PostAsync($"/api/youtubechannel/{id}/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("error").GetString().Should().Contain("UCgone");
        }

        [Fact]
        public async Task SyncChannelMetadata_WhenTheDailyQuotaIsUsedUp_ShouldReturnServiceUnavailableWithTheQuotaFlag()
        {
            var id = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.SyncChannelMetadataAsync(id).Throws(new YouTubeQuotaExceededException("quota used up", "quotaExceeded")));

            var response = await client.PostAsync($"/api/youtubechannel/{id}/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("quotaExceeded").GetBoolean().Should().BeTrue();
        }

        #endregion
    }
}
