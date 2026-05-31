using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Infrastructure.Clients.YouTube;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class YouTubeApiClientTests
    {
        private readonly TestHttpMessageHandler _mockHttpMessageHandler;
        private readonly ILogger<YouTubeApiClient> _mockLogger;
        private readonly IConfiguration _mockConfiguration;
        private readonly HttpClient _httpClient;
        private readonly YouTubeApiClient _youtubeApiClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeApiClientTests()
        {
            _mockHttpMessageHandler = new TestHttpMessageHandler();
            _mockLogger = Substitute.For<ILogger<YouTubeApiClient>>();
            _mockConfiguration = Substitute.For<IConfiguration>();

            _httpClient = new HttpClient(_mockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/")
            };

            // Setup configuration to return test API key
            _mockConfiguration["ApiKeys:YouTube"].Returns("test-api-key");

            _youtubeApiClient = new YouTubeApiClient(_httpClient, _mockLogger, _mockConfiguration);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_ShouldReturnSearchResults_WhenValidResponseReceived()
        {
            // Arrange
            var query = "test video";
            var expectedResult = CreateSearchResultDto();
            var jsonResponse = JsonSerializer.Serialize(expectedResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.SearchAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeNull();
            result.Items.Should().HaveCountGreaterThan(0);
            
            VerifyHttpRequest("GET", "search?");
        }

        [Fact]
        public async Task SearchAsync_ShouldIncludeAllParameters_WhenAllParametersProvided()
        {
            // Arrange
            var query = "test";
            var type = "video";
            var maxResults = 10;
            var pageToken = "CAUQAA";
            var channelId = "UCtest123";
            var expectedResult = CreateSearchResultDto();
            var jsonResponse = JsonSerializer.Serialize(expectedResult, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.SearchAsync(query, type, maxResults, pageToken, channelId);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest("GET", "search?");
        }

        [Fact]
        public async Task SearchAsync_ShouldThrowException_WhenHttpRequestFails()
        {
            // Arrange
            var query = "test";
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act & Assert
            await _youtubeApiClient.Invoking(c => c.SearchAsync(query))
                .Should().ThrowAsync<HttpRequestException>();
        }

        #endregion

        #region GetVideoDetailsAsync Tests

        [Fact]
        public async Task GetVideoDetailsAsync_ShouldReturnVideoDetails_WhenValidResponseReceived()
        {
            // Arrange
            var videoId = "test-video-id";
            var videoDto = CreateVideoDto(videoId, "Test Video");
            var videoListResponse = new YouTubeVideoListResponseDto
            {
                Items = new List<YouTubeVideoDto> { videoDto }
            };
            var jsonResponse = JsonSerializer.Serialize(videoListResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetVideoDetailsAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(videoId);
            result.Snippet.Title.Should().Be("Test Video");

            VerifyHttpRequest("GET", "videos?");
        }

        [Fact]
        public async Task GetVideoDetailsAsync_ShouldReturnNull_WhenVideoNotFound()
        {
            // Arrange
            var videoId = "non-existent-id";
            var emptyResponse = new YouTubeVideoListResponseDto
            {
                Items = new List<YouTubeVideoDto>()
            };
            var jsonResponse = JsonSerializer.Serialize(emptyResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetVideoDetailsAsync(videoId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetVideosAsync Tests

        [Fact]
        public async Task GetVideosAsync_ShouldReturnMultipleVideos_WhenValidResponseReceived()
        {
            // Arrange
            var videoIds = new List<string> { "video1", "video2", "video3" };
            var videos = new List<YouTubeVideoDto>
            {
                CreateVideoDto("video1", "Video 1"),
                CreateVideoDto("video2", "Video 2"),
                CreateVideoDto("video3", "Video 3")
            };
            var videoListResponse = new YouTubeVideoListResponseDto { Items = videos };
            var jsonResponse = JsonSerializer.Serialize(videoListResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetVideosAsync(videoIds);

            // Assert
            result.Should().HaveCount(3);
            result.Select(v => v.Id).Should().Contain(videoIds);
        }

        [Fact]
        public async Task GetVideosAsync_ShouldReturnEmptyList_WhenNoVideoIdsProvided()
        {
            // Arrange
            var emptyList = new List<string>();

            // Act
            var result = await _youtubeApiClient.GetVideosAsync(emptyList);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetPlaylistDetailsAsync Tests

        [Fact]
        public async Task GetPlaylistDetailsAsync_ShouldReturnPlaylistDetails_WhenValidResponseReceived()
        {
            // Arrange
            var playlistId = "test-playlist-id";
            var playlistDto = CreatePlaylistDto(playlistId, "Test Playlist");
            var playlistListResponse = new YouTubePlaylistListResponseDto
            {
                Items = new List<YouTubePlaylistDto> { playlistDto }
            };
            var jsonResponse = JsonSerializer.Serialize(playlistListResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetPlaylistDetailsAsync(playlistId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(playlistId);
            result.Snippet.Title.Should().Be("Test Playlist");

            VerifyHttpRequest("GET", "playlists?");
        }

        [Fact]
        public async Task GetPlaylistDetailsAsync_ShouldReturnNull_WhenPlaylistNotFound()
        {
            // Arrange
            var playlistId = "non-existent-id";
            var emptyResponse = new YouTubePlaylistListResponseDto
            {
                Items = new List<YouTubePlaylistDto>()
            };
            var jsonResponse = JsonSerializer.Serialize(emptyResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetPlaylistDetailsAsync(playlistId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetPlaylistItemsAsync Tests

        [Fact]
        public async Task GetPlaylistItemsAsync_ShouldReturnPlaylistItems_WhenValidResponseReceived()
        {
            // Arrange
            var playlistId = "test-playlist-id";
            var playlistItems = new List<YouTubePlaylistItemDto>
            {
                CreatePlaylistItemDto("item1", "Video 1"),
                CreatePlaylistItemDto("item2", "Video 2")
            };
            var playlistItemListResponse = new YouTubePlaylistItemListResponseDto
            {
                Items = playlistItems
            };
            var jsonResponse = JsonSerializer.Serialize(playlistItemListResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetPlaylistItemsAsync(playlistId);

            // Assert
            result.Should().HaveCount(2);
            result.Select(i => i.Id).Should().Contain(new[] { "item1", "item2" });

            VerifyHttpRequest("GET", "playlistItems?");
        }

        #endregion

        #region GetChannelDetailsAsync Tests

        [Fact]
        public async Task GetChannelDetailsAsync_ShouldReturnChannelDetails_WhenValidResponseReceived()
        {
            // Arrange
            var channelId = "test-channel-id";
            var channelDto = CreateChannelDto(channelId, "Test Channel");
            var channelListResponse = new YouTubeChannelListResponseDto
            {
                Items = new List<YouTubeChannelDto> { channelDto }
            };
            var jsonResponse = JsonSerializer.Serialize(channelListResponse, _jsonOptions);

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _youtubeApiClient.GetChannelDetailsAsync(channelId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(channelId);
            result.Snippet.Title.Should().Be("Test Channel");

            VerifyHttpRequest("GET", "channels?");
        }

        #endregion

        #region Helper Methods

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
            => _mockHttpMessageHandler.RespondWith(statusCode, content);

        private void VerifyHttpRequest(string method, string expectedUriPart)
        {
            _mockHttpMessageHandler.Requests.Should().ContainSingle(req =>
                req.Method.ToString() == method &&
                req.RequestUri!.ToString().Contains(expectedUriPart));
        }

        private static YouTubeSearchResultDto CreateSearchResultDto()
        {
            return new YouTubeSearchResultDto
            {
                Items = new List<YouTubeSearchItemDto>
                {
                    new YouTubeSearchItemDto
                    {
                        Id = new YouTubeSearchItemIdDto { VideoId = "test-id", Kind = "youtube#video" },
                        Snippet = new YouTubeSearchItemSnippetDto { Title = "Test Video", Description = "Test Description" }
                    }
                }
            };
        }

        private static YouTubeVideoDto CreateVideoDto(string id, string title)
        {
            return new YouTubeVideoDto
            {
                Id = id,
                Snippet = new YouTubeVideoSnippetDto { Title = title, Description = "Test Description" },
                ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT10M30S" },
                Statistics = new YouTubeVideoStatisticsDto { ViewCount = "1000", LikeCount = "100" }
            };
        }

        private static YouTubePlaylistDto CreatePlaylistDto(string id, string title)
        {
            return new YouTubePlaylistDto
            {
                Id = id,
                Snippet = new YouTubePlaylistSnippetDto { Title = title, Description = "Test Playlist Description" },
                ContentDetails = new YouTubePlaylistContentDetailsDto { ItemCount = 10 }
            };
        }

        private static YouTubePlaylistItemDto CreatePlaylistItemDto(string id, string title)
        {
            return new YouTubePlaylistItemDto
            {
                Id = id,
                Snippet = new YouTubePlaylistItemSnippetDto 
                { 
                    Title = title, 
                    Description = "Test Description",
                    ResourceId = new YouTubeResourceIdDto { VideoId = id, Kind = "youtube#video" }
                }
            };
        }

        private static YouTubeChannelDto CreateChannelDto(string id, string title)
        {
            return new YouTubeChannelDto
            {
                Id = id,
                Snippet = new YouTubeChannelSnippetDto { Title = title, Description = "Test Channel Description" },
                Statistics = new YouTubeChannelStatisticsDto 
                { 
                    SubscriberCount = "10000", 
                    VideoCount = "100", 
                    ViewCount = "1000000" 
                }
            };
        }

        #endregion
    }
}

