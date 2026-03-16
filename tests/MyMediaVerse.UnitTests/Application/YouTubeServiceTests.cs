using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyMediaVerse.UnitTests.Application
{
    public class YouTubeServiceTests
    {
        private readonly Mock<IYouTubeApiClient> _mockApiClient;
        private readonly Mock<IYouTubeMappingService> _mockMappingService;
        private readonly Mock<IVideoService> _mockVideoService;
        private readonly Mock<IYouTubeChannelService> _mockChannelService;
        private readonly Mock<ILogger<YouTubeService>> _mockLogger;
        private readonly YouTubeService _service;

        public YouTubeServiceTests()
        {
            _mockApiClient = new Mock<IYouTubeApiClient>();
            _mockMappingService = new Mock<IYouTubeMappingService>();
            _mockVideoService = new Mock<IVideoService>();
            _mockChannelService = new Mock<IYouTubeChannelService>();
            _mockLogger = new Mock<ILogger<YouTubeService>>();

            _mockApiClient
                .Setup(x => x.GetChannelByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((YouTubeChannelDto?)null);

            _mockChannelService
                .Setup(x => x.GetChannelByExternalIdAsync(It.IsAny<string>()))
                .ReturnsAsync((YouTubeChannel?)null);

            _service = new YouTubeService(
                _mockApiClient.Object,
                _mockMappingService.Object,
                _mockVideoService.Object,
                _mockChannelService.Object,
                _mockLogger.Object);
        }

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_WithValidQuery_ShouldReturnSearchResults()
        {
            // Arrange
            var query = "test query";
            var expectedResult = new YouTubeSearchResultDto
            {
                Items = new List<YouTubeSearchItemDto>
                {
                    new YouTubeSearchItemDto
                    {
                        Id = new YouTubeSearchItemIdDto { VideoId = "test_video_id" },
                        Snippet = new YouTubeSearchItemSnippetDto { Title = "Test Video" }
                    }
                }
            };

            _mockApiClient
                .Setup(x => x.SearchAsync(query, "video", 25, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().Snippet.Title.Should().Be("Test Video");
            _mockApiClient.Verify(x => x.SearchAsync(query, "video", 25, null, null), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_WithCustomParameters_ShouldPassParametersToApiClient()
        {
            // Arrange
            var query = "test query";
            var type = "channel";
            var maxResults = 50;
            var pageToken = "next_page";
            var channelId = "test_channel";
            var expectedResult = new YouTubeSearchResultDto();

            _mockApiClient
                .Setup(x => x.SearchAsync(query, type, maxResults, pageToken, channelId))
                .ReturnsAsync(expectedResult);

            // Act
            await _service.SearchAsync(query, type, maxResults, pageToken, channelId);

            // Assert
            _mockApiClient.Verify(x => x.SearchAsync(query, type, maxResults, pageToken, channelId), Times.Once);
        }

        #endregion

        #region GetVideoDetailsAsync Tests

        [Fact]
        public async Task GetVideoDetailsAsync_WithValidVideoId_ShouldReturnVideoDetails()
        {
            // Arrange
            var videoId = "test_video_id";
            var expectedResult = new YouTubeVideoDto
            {
                Id = videoId,
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video" }
            };

            _mockApiClient
                .Setup(x => x.GetVideoDetailsAsync(videoId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetVideoDetailsAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(videoId);
            result.Snippet.Title.Should().Be("Test Video");
            _mockApiClient.Verify(x => x.GetVideoDetailsAsync(videoId), Times.Once);
        }

        [Fact]
        public async Task GetVideoDetailsAsync_WithInvalidVideoId_ShouldReturnNull()
        {
            // Arrange
            var videoId = "invalid_id";

            _mockApiClient
                .Setup(x => x.GetVideoDetailsAsync(videoId))
                .ReturnsAsync((YouTubeVideoDto?)null);

            // Act
            var result = await _service.GetVideoDetailsAsync(videoId);

            // Assert
            result.Should().BeNull();
            _mockApiClient.Verify(x => x.GetVideoDetailsAsync(videoId), Times.Once);
        }

        #endregion

        #region ImportVideoAsync Tests

        [Fact]
        public async Task ImportVideoAsync_WithValidVideoId_ShouldImportAndReturnVideo()
        {
            // Arrange
            var videoId = "test_video_id";
            var videoDto = new YouTubeVideoDto
            {
                Id = videoId,
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video" }
            };
            var mappedVideo = new Video { Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };
            var savedVideo = new Video { Id = Guid.NewGuid(), Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };

            _mockApiClient
                .Setup(x => x.GetVideoDetailsAsync(videoId))
                .ReturnsAsync(videoDto);

            // Setup for channel service calls
            _mockChannelService.Setup(x => x.GetChannelByExternalIdAsync(It.IsAny<string>())).ReturnsAsync((YouTubeChannel?)null);

            _mockMappingService
                .Setup(x => x.MapVideoToEntity(videoDto))
                .Returns(mappedVideo);

            _mockVideoService
                .Setup(x => x.SaveVideoAsync(mappedVideo, true))
                .ReturnsAsync(savedVideo);

            // Act
            var result = await _service.ImportVideoAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(savedVideo);
            _mockApiClient.Verify(x => x.GetVideoDetailsAsync(videoId), Times.Once);
            _mockMappingService.Verify(x => x.MapVideoToEntity(videoDto), Times.Once);
            _mockVideoService.Verify(x => x.SaveVideoAsync(mappedVideo, true), Times.Once);
        }

        [Fact]
        public async Task ImportVideoAsync_WithInvalidVideoId_ShouldThrowException()
        {
            // Arrange
            var videoId = "invalid_id";

            _mockApiClient
                .Setup(x => x.GetVideoDetailsAsync(videoId))
                .ReturnsAsync((YouTubeVideoDto?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ImportVideoAsync(videoId));

            exception.Message.Should().Contain($"Video with ID {videoId} not found");
            _mockApiClient.Verify(x => x.GetVideoDetailsAsync(videoId), Times.Once);
            _mockMappingService.Verify(x => x.MapVideoToEntity(It.IsAny<YouTubeVideoDto>()), Times.Never);
            _mockVideoService.Verify(x => x.SaveVideoAsync(It.IsAny<Video>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task ImportVideoAsync_WhenApiClientThrows_ShouldPropagateException()
        {
            // Arrange
            var videoId = "test_video_id";
            var expectedException = new HttpRequestException("API error");

            _mockApiClient
                .Setup(x => x.GetVideoDetailsAsync(videoId))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => _service.ImportVideoAsync(videoId));

            exception.Should().BeSameAs(expectedException);
        }

        #endregion

        #region ImportPlaylistAsync Tests

        [Fact]
        public async Task ImportPlaylistAsync_WithValidPlaylistId_ShouldImportAllVideos()
        {
            // Arrange
            var playlistId = "test_playlist_id";
            var playlistDto = new YouTubePlaylistDto
            {
                Id = playlistId,
                Snippet = new YouTubePlaylistSnippetDto { Title = "Test Playlist" }
            };

            var playlistItems = new List<YouTubePlaylistItemDto>
            {
                new YouTubePlaylistItemDto
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        ResourceId = new YouTubeResourceIdDto { VideoId = "video1" }
                    }
                },
                new YouTubePlaylistItemDto
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        ResourceId = new YouTubeResourceIdDto { VideoId = "video2" }
                    }
                }
            };

            var videoDetails = new List<YouTubeVideoDto>
            {
                new YouTubeVideoDto { Id = "video1", Snippet = new YouTubeVideoSnippetDto { Title = "Video 1" } },
                new YouTubeVideoDto { Id = "video2", Snippet = new YouTubeVideoSnippetDto { Title = "Video 2" } }
            };

            var mappedVideos = new List<Video>
            {
                new Video { Title = "Video 1", MediaType = MediaType.Video, Platform = "YouTube" },
                new Video { Title = "Video 2", MediaType = MediaType.Video, Platform = "YouTube" }
            };

            var savedVideos = new List<Video>
            {
                new Video { Id = Guid.NewGuid(), Title = "Video 1", MediaType = MediaType.Video, Platform = "YouTube" },
                new Video { Id = Guid.NewGuid(), Title = "Video 2", MediaType = MediaType.Video, Platform = "YouTube" }
            };

            _mockApiClient
                .Setup(x => x.GetPlaylistDetailsAsync(playlistId))
                .ReturnsAsync(playlistDto);

            _mockApiClient
                .Setup(x => x.GetAllPlaylistItemsAsync(playlistId))
                .ReturnsAsync(playlistItems);

            _mockApiClient
                .Setup(x => x.GetVideosAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(videoDetails);

            // Setup for AutoLinkChannelToVideo to prevent NullReferenceException
            _mockChannelService.Setup(x => x.GetChannelByExternalIdAsync(It.IsAny<string>())).ReturnsAsync((YouTubeChannel?)null);
            _mockChannelService.Setup(x => x.ImportChannelFromYouTubeAsync(It.IsAny<string>())).ReturnsAsync(new YouTubeChannel { Id = Guid.NewGuid(), Title = "Imported Channel", ChannelExternalId = "channel_id", MediaType = MediaType.Channel });

            _mockMappingService
                .Setup(x => x.MapVideoToEntity(videoDetails[0]))
                .Returns(mappedVideos[0]);

            _mockMappingService
                .Setup(x => x.MapVideoToEntity(videoDetails[1]))
                .Returns(mappedVideos[1]);

            _mockMappingService
                .Setup(x => x.MapPlaylistItemsToVideoEntities(It.IsAny<List<YouTubePlaylistItemDto>>(), It.IsAny<List<YouTubeVideoDto>>()))
                .Returns(mappedVideos);

            _mockVideoService
                .Setup(x => x.SaveVideoAsync(mappedVideos[0], true))
                .ReturnsAsync(savedVideos[0]);

            _mockVideoService
                .Setup(x => x.SaveVideoAsync(mappedVideos[1], true))
                .ReturnsAsync(savedVideos[1]);

            // Act
            var result = await _service.ImportPlaylistAsync(playlistId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(savedVideos[0]);
            result.Should().Contain(savedVideos[1]);

            _mockApiClient.Verify(x => x.GetPlaylistDetailsAsync(playlistId), Times.Once);
            _mockApiClient.Verify(x => x.GetAllPlaylistItemsAsync(playlistId), Times.Once);
            _mockApiClient.Verify(x => x.GetVideosAsync(It.Is<List<string>>(l => l.Contains("video1") && l.Contains("video2"))), Times.Once);
        }

        [Fact]
        public async Task ImportPlaylistAsync_WithInvalidPlaylistId_ShouldThrowException()
        {
            // Arrange
            var playlistId = "invalid_playlist_id";

            _mockApiClient
                .Setup(x => x.GetPlaylistDetailsAsync(playlistId))
                .ReturnsAsync((YouTubePlaylistDto?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ImportPlaylistAsync(playlistId));

            exception.Message.Should().Contain($"Playlist with ID {playlistId} not found");
            _mockApiClient.Verify(x => x.GetPlaylistDetailsAsync(playlistId), Times.Once);
            _mockApiClient.Verify(x => x.GetAllPlaylistItemsAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region ImportFromUrlAsync Tests

        [Fact]
        public async Task ImportFromUrlAsync_WithVideoUrl_ShouldImportVideo()
        {
            // Arrange
            var videoId = "test_video1"; // Must be 11 chars for regex match
            var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
            var videoDto = new YouTubeVideoDto
            {
                Id = videoId,
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video", ChannelId = "channel_id" }
            };
            var mappedVideo = new Video { Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };
            var savedVideo = new Video { Id = Guid.NewGuid(), Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };

            _mockApiClient
                .Setup(x => x.GetVideoDetailsAsync(videoId))
                .ReturnsAsync(videoDto);

            // Setup for channel service calls
            _mockChannelService.Setup(x => x.GetChannelByExternalIdAsync(It.IsAny<string>())).ReturnsAsync((YouTubeChannel?)null);
            _mockChannelService.Setup(x => x.ImportChannelFromYouTubeAsync(It.IsAny<string>())).ReturnsAsync(new YouTubeChannel { Id = Guid.NewGuid(), Title = "Imported Channel", ChannelExternalId = "channel_id", MediaType = MediaType.Channel });

            _mockMappingService
                .Setup(x => x.MapVideoToEntity(videoDto))
                .Returns(mappedVideo);

            _mockVideoService
                .Setup(x => x.SaveVideoAsync(mappedVideo, true))
                .ReturnsAsync(savedVideo);

            // Act
            var result = await _service.ImportFromUrlAsync(videoUrl);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(savedVideo);
            _mockApiClient.Verify(x => x.GetVideoDetailsAsync(videoId), Times.Once);
        }

        [Fact]
        public async Task ImportFromUrlAsync_WithInvalidUrl_ShouldThrowException()
        {
            // Arrange
            var invalidUrl = "https://example.com/not-youtube";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ImportFromUrlAsync(invalidUrl));

            exception.Message.Should().Contain("Unable to extract valid YouTube ID from URL");
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public async Task GetPlaylistItemsAsync_ShouldCallApiClient()
        {
            // Arrange
            var playlistId = "test_playlist";
            var maxResults = 25;
            var pageToken = "test_token";
            var expectedResult = new List<YouTubePlaylistItemDto>();

            _mockApiClient
                .Setup(x => x.GetPlaylistItemsAsync(playlistId, maxResults, pageToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetPlaylistItemsAsync(playlistId, maxResults, pageToken);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockApiClient.Verify(x => x.GetPlaylistItemsAsync(playlistId, maxResults, pageToken), Times.Once);
        }

        [Fact]
        public async Task GetChannelDetailsAsync_ShouldCallApiClient()
        {
            // Arrange
            var channelId = "test_channel";
            var expectedResult = new YouTubeChannelDto
            {
                Id = channelId,
                Snippet = new YouTubeChannelSnippetDto { Title = "Test Channel" }
            };

            _mockApiClient
                .Setup(x => x.GetChannelDetailsAsync(channelId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetChannelDetailsAsync(channelId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockApiClient.Verify(x => x.GetChannelDetailsAsync(channelId), Times.Once);
        }

        #endregion
    }
}
