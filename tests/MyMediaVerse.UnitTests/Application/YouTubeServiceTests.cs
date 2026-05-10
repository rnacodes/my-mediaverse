using Xunit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
    [Trait("Category", "Unit")]
    public class YouTubeServiceTests
    {
        private readonly IYouTubeApiClient _mockApiClient;
        private readonly IYouTubeMappingService _mockMappingService;
        private readonly IVideoService _mockVideoService;
        private readonly IYouTubeChannelService _mockChannelService;
        private readonly ILogger<YouTubeService> _mockLogger;
        private readonly YouTubeService _service;

        public YouTubeServiceTests()
        {
            _mockApiClient = Substitute.For<IYouTubeApiClient>();
            _mockMappingService = Substitute.For<IYouTubeMappingService>();
            _mockVideoService = Substitute.For<IVideoService>();
            _mockChannelService = Substitute.For<IYouTubeChannelService>();
            _mockLogger = Substitute.For<ILogger<YouTubeService>>();

            _mockApiClient
                .GetChannelByUsernameAsync(Arg.Any<string>())
                .Returns((YouTubeChannelDto?)null);

            _mockChannelService
                .GetChannelByExternalIdAsync(Arg.Any<string>())
                .Returns((YouTubeChannel?)null);

            _service = new YouTubeService(
                _mockApiClient,
                _mockMappingService,
                _mockVideoService,
                _mockChannelService,
                _mockLogger);
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
                .SearchAsync(query, "video", 25, null, null)
                .Returns(expectedResult);

            // Act
            var result = await _service.SearchAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().Snippet.Title.Should().Be("Test Video");
            _mockApiClient.Received(1).SearchAsync(query, "video", 25, null, null);
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
                .SearchAsync(query, type, maxResults, pageToken, channelId)
                .Returns(expectedResult);

            // Act
            await _service.SearchAsync(query, type, maxResults, pageToken, channelId);

            // Assert
            _mockApiClient.Received(1).SearchAsync(query, type, maxResults, pageToken, channelId);
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
                .GetVideoDetailsAsync(videoId)
                .Returns(expectedResult);

            // Act
            var result = await _service.GetVideoDetailsAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(videoId);
            result.Snippet.Title.Should().Be("Test Video");
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
        }

        [Fact]
        public async Task GetVideoDetailsAsync_WithInvalidVideoId_ShouldReturnNull()
        {
            // Arrange
            var videoId = "invalid_id";

            _mockApiClient
                .GetVideoDetailsAsync(videoId)
                .Returns((YouTubeVideoDto?)null);

            // Act
            var result = await _service.GetVideoDetailsAsync(videoId);

            // Assert
            result.Should().BeNull();
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
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
                .GetVideoDetailsAsync(videoId)
                .Returns(videoDto);

            // Setup for channel service calls
            _mockChannelService.GetChannelByExternalIdAsync(Arg.Any<string>()).Returns((YouTubeChannel?)null);

            _mockMappingService
                .MapVideoToEntity(videoDto)
                .Returns(mappedVideo);

            _mockVideoService
                .SaveVideoAsync(mappedVideo, true)
                .Returns(savedVideo);

            // Act
            var result = await _service.ImportVideoAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(savedVideo);
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
            _mockMappingService.Received(1).MapVideoToEntity(videoDto);
            _mockVideoService.Received(1).SaveVideoAsync(mappedVideo, true);
        }

        [Fact]
        public async Task ImportVideoAsync_WithInvalidVideoId_ShouldThrowException()
        {
            // Arrange
            var videoId = "invalid_id";

            _mockApiClient
                .GetVideoDetailsAsync(videoId)
                .Returns((YouTubeVideoDto?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ImportVideoAsync(videoId));

            exception.Message.Should().Contain($"Video with ID {videoId} not found");
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
            _mockMappingService.DidNotReceive().MapVideoToEntity(Arg.Any<YouTubeVideoDto>());
            _mockVideoService.DidNotReceive().SaveVideoAsync(Arg.Any<Video>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task ImportVideoAsync_WhenApiClientThrows_ShouldPropagateException()
        {
            // Arrange
            var videoId = "test_video_id";
            var expectedException = new HttpRequestException("API error");

            _mockApiClient
                .GetVideoDetailsAsync(videoId)
                .Throws(expectedException);

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
                .GetPlaylistDetailsAsync(playlistId)
                .Returns(playlistDto);

            _mockApiClient
                .GetAllPlaylistItemsAsync(playlistId)
                .Returns(playlistItems);

            _mockApiClient
                .GetVideosAsync(Arg.Any<List<string>>())
                .Returns(videoDetails);

            // Setup for AutoLinkChannelToVideo to prevent NullReferenceException
            _mockChannelService.GetChannelByExternalIdAsync(Arg.Any<string>()).Returns((YouTubeChannel?)null);
            _mockChannelService.ImportChannelFromYouTubeAsync(Arg.Any<string>()).Returns(new YouTubeChannel { Id = Guid.NewGuid(), Title = "Imported Channel", ChannelExternalId = "channel_id", MediaType = MediaType.Channel });

            _mockMappingService
                .MapVideoToEntity(videoDetails[0])
                .Returns(mappedVideos[0]);

            _mockMappingService
                .MapVideoToEntity(videoDetails[1])
                .Returns(mappedVideos[1]);

            _mockMappingService
                .MapPlaylistItemsToVideoEntities(Arg.Any<List<YouTubePlaylistItemDto>>(), Arg.Any<List<YouTubeVideoDto>>())
                .Returns(mappedVideos);

            _mockVideoService
                .SaveVideoAsync(mappedVideos[0], true)
                .Returns(savedVideos[0]);

            _mockVideoService
                .SaveVideoAsync(mappedVideos[1], true)
                .Returns(savedVideos[1]);

            // Act
            var result = await _service.ImportPlaylistAsync(playlistId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(savedVideos[0]);
            result.Should().Contain(savedVideos[1]);

            _mockApiClient.Received(1).GetPlaylistDetailsAsync(playlistId);
            _mockApiClient.Received(1).GetAllPlaylistItemsAsync(playlistId);
            _mockApiClient.Received(1).GetVideosAsync(Arg.Is<List<string>>(l => l.Contains("video1") && l.Contains("video2")));
        }

        [Fact]
        public async Task ImportPlaylistAsync_WithInvalidPlaylistId_ShouldThrowException()
        {
            // Arrange
            var playlistId = "invalid_playlist_id";

            _mockApiClient
                .GetPlaylistDetailsAsync(playlistId)
                .Returns((YouTubePlaylistDto?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ImportPlaylistAsync(playlistId));

            exception.Message.Should().Contain($"Playlist with ID {playlistId} not found");
            _mockApiClient.Received(1).GetPlaylistDetailsAsync(playlistId);
            _mockApiClient.DidNotReceive().GetAllPlaylistItemsAsync(Arg.Any<string>());
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
                .GetVideoDetailsAsync(videoId)
                .Returns(videoDto);

            // Setup for channel service calls
            _mockChannelService.GetChannelByExternalIdAsync(Arg.Any<string>()).Returns((YouTubeChannel?)null);
            _mockChannelService.ImportChannelFromYouTubeAsync(Arg.Any<string>()).Returns(new YouTubeChannel { Id = Guid.NewGuid(), Title = "Imported Channel", ChannelExternalId = "channel_id", MediaType = MediaType.Channel });

            _mockMappingService
                .MapVideoToEntity(videoDto)
                .Returns(mappedVideo);

            _mockVideoService
                .SaveVideoAsync(mappedVideo, true)
                .Returns(savedVideo);

            // Act
            var result = await _service.ImportFromUrlAsync(videoUrl);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(savedVideo);
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
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
                .GetPlaylistItemsAsync(playlistId, maxResults, pageToken)
                .Returns(expectedResult);

            // Act
            var result = await _service.GetPlaylistItemsAsync(playlistId, maxResults, pageToken);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockApiClient.Received(1).GetPlaylistItemsAsync(playlistId, maxResults, pageToken);
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
                .GetChannelDetailsAsync(channelId)
                .Returns(expectedResult);

            // Act
            var result = await _service.GetChannelDetailsAsync(channelId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockApiClient.Received(1).GetChannelDetailsAsync(channelId);
        }

        #endregion
    }
}
