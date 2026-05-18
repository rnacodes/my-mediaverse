using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.YouTube;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class YouTubeControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeControllerIntegrationTests(ApiFactory factory)
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

        #region Search Endpoints Tests

        [Fact]
        public async Task Search_WithValidQuery_ShouldReturnOk()
        {
            // Arrange
            var expectedResult = CreateSearchResult();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.SearchAsync("test", "video", 25, null, null).Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/YouTube/search?query=test");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var searchResult = await response.Content.ReadFromJsonAsync<YouTubeSearchResultDto>(_jsonOptions);
            searchResult.Should().NotBeNull();
        }

        [Fact]
        public async Task Search_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/YouTube/search?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Search_WithCustomParameters_ShouldReturnOk()
        {
            // Arrange
            var expectedResult = CreateSearchResult();
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.SearchAsync("test", "channel", 10, null, null).Returns(expectedResult));

            // Act
            var response = await client.GetAsync("/api/YouTube/search?query=test&type=channel&maxResults=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var searchResult = await response.Content.ReadFromJsonAsync<YouTubeSearchResultDto>(_jsonOptions);
            searchResult.Should().NotBeNull();
        }

        #endregion

        #region Video Details Tests

        [Fact]
        public async Task GetVideoDetails_WithValidVideoId_ShouldReturnOk()
        {
            // Arrange
            var videoId = "dQw4w9WgXcQ";
            var expectedVideo = CreateVideoDto(videoId, "Never Gonna Give You Up");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetVideoDetailsAsync(videoId).Returns(expectedVideo));

            // Act
            var response = await client.GetAsync($"/api/YouTube/videos/{videoId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var video = await response.Content.ReadFromJsonAsync<YouTubeVideoDto>(_jsonOptions);
            video.Should().NotBeNull();
            video!.Id.Should().Be(videoId);
        }

        [Fact]
        public async Task GetVideoDetails_WithInvalidVideoId_ShouldReturnNotFound()
        {
            // Arrange
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetVideoDetailsAsync("invalid_video_id").Returns((YouTubeVideoDto?)null));

            // Act
            var response = await client.GetAsync("/api/YouTube/videos/invalid_video_id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Playlist Tests

        [Fact]
        public async Task GetPlaylistDetails_WithValidPlaylistId_ShouldReturnOk()
        {
            // Arrange
            var playlistId = "PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI";
            var expectedPlaylist = CreatePlaylistDto(playlistId, "Test Playlist");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetPlaylistDetailsAsync(playlistId).Returns(expectedPlaylist));

            // Act
            var response = await client.GetAsync($"/api/YouTube/playlists/{playlistId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var playlist = await response.Content.ReadFromJsonAsync<YouTubePlaylistDto>(_jsonOptions);
            playlist.Should().NotBeNull();
            playlist!.Id.Should().Be(playlistId);
        }

        [Fact]
        public async Task GetPlaylistItems_WithValidPlaylistId_ShouldReturnOk()
        {
            // Arrange
            var playlistId = "PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI";
            var expectedItems = new List<YouTubePlaylistItemDto>
            {
                CreatePlaylistItemDto("item1", "Test Video 1"),
                CreatePlaylistItemDto("item2", "Test Video 2")
            };
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetPlaylistItemsAsync(playlistId, 50, null).Returns(expectedItems));

            // Act
            var response = await client.GetAsync($"/api/YouTube/playlists/{playlistId}/items");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await response.Content.ReadFromJsonAsync<List<YouTubePlaylistItemDto>>(_jsonOptions);
            items.Should().NotBeNull();
        }

        #endregion

        #region Channel Tests

        [Fact]
        public async Task GetChannelDetails_WithValidChannelId_ShouldReturnOk()
        {
            // Arrange
            var channelId = "UCuAXFkgsw1L7xaCfnd5JJOw";
            var expectedChannel = CreateChannelDto(channelId, "Test Channel");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetChannelDetailsAsync(channelId).Returns(expectedChannel));

            // Act
            var response = await client.GetAsync($"/api/YouTube/channels/{channelId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var channel = await response.Content.ReadFromJsonAsync<YouTubeChannelDto>(_jsonOptions);
            channel.Should().NotBeNull();
            channel!.Id.Should().Be(channelId);
        }

        [Fact]
        public async Task GetChannelByUsername_WithValidUsername_ShouldReturnOk()
        {
            // Arrange
            var username = "YouTube";
            var expectedChannel = CreateChannelDto("UC_test_id", "YouTube");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetChannelByUsernameAsync(username).Returns(expectedChannel));

            // Act
            var response = await client.GetAsync($"/api/YouTube/channels/by-username/{username}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var channel = await response.Content.ReadFromJsonAsync<YouTubeChannelDto>(_jsonOptions);
            channel.Should().NotBeNull();
        }

        [Fact]
        public async Task GetChannelByUsername_WithInvalidUsername_ShouldReturnNotFound()
        {
            // Arrange
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.GetChannelByUsernameAsync("nonexistent_user_12345").Returns((YouTubeChannelDto?)null));

            // Act
            var response = await client.GetAsync("/api/YouTube/channels/by-username/nonexistent_user_12345");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Import Tests

        [Fact]
        public async Task ImportVideo_WithValidVideoId_ShouldReturnCreated()
        {
            // Arrange
            var videoId = "dQw4w9WgXcQ";
            var expectedVideo = CreateVideoEntity(videoId, "Never Gonna Give You Up");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportVideoAsync(videoId).Returns(expectedVideo));

            // Act
            var response = await client.PostAsync($"/api/YouTube/import/video/{videoId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var importedVideo = await response.Content.ReadFromJsonAsync<Video>(_jsonOptions);
            importedVideo.Should().NotBeNull();
            importedVideo!.Platform.Should().Be("YouTube");
            importedVideo.MediaType.Should().Be(MediaType.Video);
        }

        [Fact]
        public async Task ImportVideo_WithInvalidVideoId_ShouldReturnNotFound()
        {
            // Arrange
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportVideoAsync("invalid_video_id").Throws(new InvalidOperationException("Video not found")));

            // Act
            var response = await client.PostAsync("/api/YouTube/import/video/invalid_video_id", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportFromUrl_WithValidVideoUrl_ShouldReturnCreated()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var expectedVideo = CreateVideoEntity("dQw4w9WgXcQ", "Never Gonna Give You Up");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportFromUrlAsync(videoUrl).Returns(expectedVideo));

            var requestBody = JsonSerializer.Serialize(new { url = videoUrl }, _jsonOptions);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/YouTube/import/url", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var importedVideo = await response.Content.ReadFromJsonAsync<Video>(_jsonOptions);
            importedVideo.Should().NotBeNull();
            importedVideo!.Platform.Should().Be("YouTube");
            importedVideo.MediaType.Should().Be(MediaType.Video);
        }

        [Fact]
        public async Task ImportFromUrl_WithInvalidUrl_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidUrl = "https://example.com/not-youtube";
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportFromUrlAsync(invalidUrl).Throws(new ArgumentException("Invalid YouTube URL")));

            var requestBody = JsonSerializer.Serialize(new { url = invalidUrl }, _jsonOptions);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/YouTube/import/url", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ImportPlaylist_WithValidPlaylistId_ShouldReturnOk()
        {
            // Arrange
            var playlistId = "PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI";
            var expectedVideos = new List<Video>
            {
                CreateVideoEntity("vid1", "Video 1"),
                CreateVideoEntity("vid2", "Video 2")
            };
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportPlaylistAsync(playlistId, false).Returns(expectedVideos));

            // Act
            var response = await client.PostAsync($"/api/YouTube/import/playlist/{playlistId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var importedVideos = await response.Content.ReadFromJsonAsync<List<Video>>(_jsonOptions);
            importedVideos.Should().NotBeNull();
            importedVideos!.Count.Should().BeGreaterThan(0);

            foreach (var video in importedVideos)
            {
                video.Platform.Should().Be("YouTube");
                video.MediaType.Should().Be(MediaType.Video);
            }
        }

        [Fact]
        public async Task ImportChannel_WithValidChannelId_ShouldReturnCreated()
        {
            // Arrange
            var channelId = "UCuAXFkgsw1L7xaCfnd5JJOw";
            var expectedChannel = CreateVideoEntity(channelId, "Test Channel");
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportChannelAsync(channelId).Returns(expectedChannel));

            // Act
            var response = await client.PostAsync($"/api/YouTube/import/channel/{channelId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var importedChannel = await response.Content.ReadFromJsonAsync<Video>(_jsonOptions);
            importedChannel.Should().NotBeNull();
            importedChannel!.Platform.Should().Be("YouTube");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetVideoDetails_WithNullVideoId_ShouldReturnBadRequest()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/YouTube/videos/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Search_WithMissingQuery_ShouldReturnBadRequest()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/YouTube/search");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ImportVideo_WithEmptyVideoId_ShouldReturnNotFound()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/YouTube/import/video/", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Route not matched
        }

        #endregion

        #region Helper Methods

        private static YouTubeSearchResultDto CreateSearchResult()
        {
            return new YouTubeSearchResultDto
            {
                Kind = "youtube#searchListResponse",
                PageInfo = new YouTubePageInfoDto
                {
                    TotalResults = 1,
                    ResultsPerPage = 25
                },
                Items = new List<YouTubeSearchItemDto>
                {
                    new YouTubeSearchItemDto
                    {
                        Kind = "youtube#searchResult",
                        Id = new YouTubeSearchItemIdDto
                        {
                            Kind = "youtube#video",
                            VideoId = "test-video-id"
                        },
                        Snippet = new YouTubeSearchItemSnippetDto
                        {
                            Title = "Test Video",
                            Description = "A test video description",
                            ChannelTitle = "Test Channel"
                        }
                    }
                }
            };
        }

        private static YouTubeVideoDto CreateVideoDto(string id, string title)
        {
            return new YouTubeVideoDto
            {
                Kind = "youtube#video",
                Id = id,
                Snippet = new YouTubeVideoSnippetDto
                {
                    Title = title,
                    Description = "A test video description",
                    ChannelTitle = "Test Channel",
                    ChannelId = "UC_test_channel"
                },
                ContentDetails = new YouTubeVideoContentDetailsDto
                {
                    Duration = "PT5M30S"
                },
                Statistics = new YouTubeVideoStatisticsDto
                {
                    ViewCount = "1000000",
                    LikeCount = "50000"
                }
            };
        }

        private static YouTubePlaylistDto CreatePlaylistDto(string id, string title)
        {
            return new YouTubePlaylistDto
            {
                Kind = "youtube#playlist",
                Id = id,
                Snippet = new YouTubePlaylistSnippetDto
                {
                    Title = title,
                    Description = "A test playlist description",
                    ChannelTitle = "Test Channel"
                },
                ContentDetails = new YouTubePlaylistContentDetailsDto
                {
                    ItemCount = 10
                }
            };
        }

        private static YouTubePlaylistItemDto CreatePlaylistItemDto(string id, string title)
        {
            return new YouTubePlaylistItemDto
            {
                Kind = "youtube#playlistItem",
                Id = id,
                Snippet = new YouTubePlaylistItemSnippetDto
                {
                    Title = title,
                    Description = "A test playlist item description"
                },
                ContentDetails = new YouTubePlaylistItemContentDetailsDto
                {
                    VideoId = $"vid_{id}"
                }
            };
        }

        private static YouTubeChannelDto CreateChannelDto(string id, string title)
        {
            return new YouTubeChannelDto
            {
                Kind = "youtube#channel",
                Id = id,
                Snippet = new YouTubeChannelSnippetDto
                {
                    Title = title,
                    Description = "A test channel description"
                },
                Statistics = new YouTubeChannelStatisticsDto
                {
                    SubscriberCount = "100000",
                    VideoCount = "500",
                    ViewCount = "50000000"
                }
            };
        }

        private static Video CreateVideoEntity(string externalId, string title)
        {
            return new Video
            {
                Id = Guid.NewGuid(),
                Title = title,
                ExternalId = externalId,
                Platform = "YouTube",
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>()
            };
        }

        #endregion
    }
}
