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
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

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

        #region ImportLatestUploads

        [Fact]
        public async Task ImportLatestUploads_WhenTheDailyQuotaIsUsedUp_ShouldReturnServiceUnavailableWithTheQuotaFlag()
        {
            var id = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.ImportLatestUploadsAsync(id, Arg.Any<int>()).Throws(new YouTubeQuotaExceededException("quota used up", "quotaExceeded")));

            var response = await client.PostAsync($"/api/youtubechannel/{id}/import-latest", null);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("quotaExceeded").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task ImportLatestUploads_WhenTheChannelIsUnknown_ShouldReturnNotFound()
        {
            var id = Guid.NewGuid();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeChannelService>(mock =>
                mock.ImportLatestUploadsAsync(id, Arg.Any<int>()).Throws(new ArgumentException("not found")));

            var response = await client.PostAsync($"/api/youtubechannel/{id}/import-latest", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportLatestUploads_WhenVideosWereAdded_ShouldReturnTheResult_AndReindexLast()
        {
            var id = Guid.NewGuid();
            var (client, service, reindex) = _factory.CreateClientWithSubstitutes<IYouTubeChannelService, IImportReindexService>(svc =>
                svc.ImportLatestUploadsAsync(id, Arg.Any<int>()).Returns(call => new YouTubeChannelImportResultDto
                {
                    ChannelId = id,
                    ChannelTitle = "Contract Channel",
                    RequestedCount = call.ArgAt<int>(1),
                    CreatedCount = 2,
                    LinkedCount = 1,
                    SkippedCount = 3,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                }));

            var response = await client.PostAsync($"/api/youtubechannel/{id}/import-latest?count=10", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("youtube-channel-import-latest");
            body.GetProperty("channelId").GetGuid().Should().Be(id);
            body.GetProperty("requestedCount").GetInt32().Should().Be(10, "the query count reaches the service");
            body.GetProperty("createdCount").GetInt32().Should().Be(2);
            body.GetProperty("linkedCount").GetInt32().Should().Be(1);
            body.GetProperty("totalProcessed").GetInt32().Should().Be(6);
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeTrue();
            await service.Received(1).ImportLatestUploadsAsync(id, 10);
            await reindex.Received(1).ReindexAfterImportAsync(3, Arg.Any<string>());
        }

        #endregion

        #region Shared delete

        [Fact]
        public async Task DeleteChannel_KeepsItsVideos_UnlinksThem_AndCleansTheSearchIndex()
        {
            // Against real Postgres: a channel's delete hands off to the shared delete. Its videos
            // stay in the library (they may sit in mixlists or playlists) and only lose their link.
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();
            var channel = new YouTubeChannel { Title = "Channel to delete", ChannelExternalId = "UCdelete", MediaType = MediaType.Channel };
            var video = new Video { Title = "Keeps living", Platform = "YouTube", ExternalId = "keep0000001", Channel = channel };
            var mixlist = new Mixlist { Name = "Holds the channel" };
            mixlist.MediaItems.Add(channel);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
                db.AddRange(channel, video, mixlist);
                await db.SaveChangesAsync();
            }

            var delete = await client.DeleteAsync($"/api/youtubechannel/{channel.Id}");

            delete.IsSuccessStatusCode.Should().BeTrue();
            (await client.GetAsync("/api/media")).StatusCode.Should().Be(HttpStatusCode.OK);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
                (await db.MediaItems.AnyAsync(m => m.Id == channel.Id)).Should().BeFalse();
                var stored = await db.Videos.SingleAsync(v => v.Id == video.Id);
                stored.ChannelId.Should().BeNull();
                var storedMixlist = await db.Mixlists.Include(m => m.MediaItems).SingleAsync(m => m.Id == mixlist.Id);
                storedMixlist.MediaItems.Should().BeEmpty();
            }
            await typesense.Received(1).DeleteMediaItemAsync(channel.Id);
            await typesense.DidNotReceive().DeleteMediaItemAsync(video.Id);
        }

        #endregion
    }
}
