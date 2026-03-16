using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class YouTubeChannelControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeChannelControllerIntegrationTests(WebApplicationFactory factory)
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
    }
}
