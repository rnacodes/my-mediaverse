using Xunit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
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
        private readonly IYouTubePlaylistService _mockPlaylistService;
        private readonly ILogger<YouTubeService> _mockLogger;
        private readonly YouTubeService _service;

        public YouTubeServiceTests()
        {
            _mockApiClient = Substitute.For<IYouTubeApiClient>();
            _mockMappingService = Substitute.For<IYouTubeMappingService>();
            _mockVideoService = Substitute.For<IVideoService>();
            _mockChannelService = Substitute.For<IYouTubeChannelService>();
            _mockPlaylistService = Substitute.For<IYouTubePlaylistService>();
            _mockLogger = Substitute.For<ILogger<YouTubeService>>();

            _mockApiClient
                .GetChannelByUsernameAsync(Arg.Any<string>())
                .Returns((YouTubeChannelDto?)null);

            _mockChannelService
                .GetChannelByExternalIdAsync(Arg.Any<string>())
                .Returns((YouTubeChannel?)null);

            _mockVideoService
                .GetVideoByExternalIdAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns((Video?)null);

            _service = new YouTubeService(
                _mockApiClient,
                _mockMappingService,
                _mockVideoService,
                _mockChannelService,
                _mockPlaylistService,
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
                .SaveVideoAsync(mappedVideo)
                .Returns(new VideoCreationResult(savedVideo, true));

            // Act
            var result = await _service.ImportVideoAsync(videoId);

            // Assert
            result.Item.Should().BeSameAs(savedVideo);
            result.Created.Should().BeTrue();
            result.AutoImportedChannel.Should().BeNull();
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
            _mockMappingService.Received(1).MapVideoToEntity(videoDto);
            _mockVideoService.Received(1).SaveVideoAsync(mappedVideo);
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
            var exception = await Assert.ThrowsAsync<YouTubeResourceNotFoundException>(
                () => _service.ImportVideoAsync(videoId));

            exception.ResourceType.Should().Be("video");
            exception.Identifier.Should().Be(videoId);
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
            _mockMappingService.DidNotReceive().MapVideoToEntity(Arg.Any<YouTubeVideoDto>());
            _mockVideoService.DidNotReceive().SaveVideoAsync(Arg.Any<Video>());
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

        [Fact]
        public async Task ImportVideoAsync_WhenTheIdIsAlreadyStored_ReturnsTheStoredVideoWithoutCallingYouTube()
        {
            var stored = new Video { Id = Guid.NewGuid(), Title = "Stored", MediaType = MediaType.Video, Platform = "YouTube", ExternalId = "abc123DEF45" };
            _mockVideoService.GetVideoByExternalIdAsync("YouTube", "abc123DEF45").Returns(stored);

            var result = await _service.ImportVideoAsync("abc123DEF45");

            result.Item.Should().BeSameAs(stored);
            result.Created.Should().BeFalse();
            result.AutoImportedChannel.Should().BeNull();
            await _mockApiClient.DidNotReceive().GetVideoDetailsAsync(Arg.Any<string>());
            await _mockChannelService.DidNotReceive().ImportChannelFromYouTubeAsync(Arg.Any<string>());
            await _mockVideoService.DidNotReceive().SaveVideoAsync(Arg.Any<Video>());
        }

        [Fact]
        public async Task ImportVideoAsync_WhenItBringsInANewChannel_ReportsTheChannelAndLinksTheVideo()
        {
            var videoDto = new YouTubeVideoDto
            {
                Id = "video_with_channel",
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video", ChannelId = "UC_new_channel" }
            };
            var mappedVideo = new Video { Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };
            var newChannel = new YouTubeChannel { Id = Guid.NewGuid(), Title = "New Channel", ChannelExternalId = "UC_new_channel", MediaType = MediaType.Channel };
            _mockApiClient.GetVideoDetailsAsync("video_with_channel").Returns(videoDto);
            _mockChannelService.ImportChannelFromYouTubeAsync("UC_new_channel").Returns(new YouTubeChannelCreationResult(newChannel, true));
            _mockMappingService.MapVideoToEntity(videoDto).Returns(mappedVideo);
            _mockVideoService.SaveVideoAsync(mappedVideo).Returns(new VideoCreationResult(mappedVideo, true));

            var result = await _service.ImportVideoAsync("video_with_channel");

            result.Created.Should().BeTrue();
            result.AutoImportedChannel.Should().BeSameAs(newChannel);
            ((Video)result.Item).ChannelId.Should().Be(newChannel.Id);
        }

        [Fact]
        public async Task ImportVideoAsync_WhenTheChannelIsAlreadyStored_LinksItWithoutReportingAnImport()
        {
            var videoDto = new YouTubeVideoDto
            {
                Id = "video_with_channel",
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video", ChannelId = "UC_stored_channel" }
            };
            var mappedVideo = new Video { Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };
            var storedChannel = new YouTubeChannel { Id = Guid.NewGuid(), Title = "Stored Channel", ChannelExternalId = "UC_stored_channel", MediaType = MediaType.Channel };
            _mockApiClient.GetVideoDetailsAsync("video_with_channel").Returns(videoDto);
            _mockChannelService.GetChannelByExternalIdAsync("UC_stored_channel").Returns(storedChannel);
            _mockMappingService.MapVideoToEntity(videoDto).Returns(mappedVideo);
            _mockVideoService.SaveVideoAsync(mappedVideo).Returns(new VideoCreationResult(mappedVideo, true));

            var result = await _service.ImportVideoAsync("video_with_channel");

            result.AutoImportedChannel.Should().BeNull();
            ((Video)result.Item).ChannelId.Should().Be(storedChannel.Id);
            await _mockChannelService.DidNotReceive().ImportChannelFromYouTubeAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task ImportVideoAsync_WhenTheSaveFindsTheVideoAlreadyStored_ReportsItAsNotCreated()
        {
            var videoDto = new YouTubeVideoDto
            {
                Id = "legacy_video",
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video" }
            };
            var mappedVideo = new Video { Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };
            var legacyRow = new Video { Id = Guid.NewGuid(), Title = "My Title", MediaType = MediaType.Video, Platform = "YouTube" };
            _mockApiClient.GetVideoDetailsAsync("legacy_video").Returns(videoDto);
            _mockMappingService.MapVideoToEntity(videoDto).Returns(mappedVideo);
            _mockVideoService.SaveVideoAsync(mappedVideo).Returns(new VideoCreationResult(legacyRow, false));

            var result = await _service.ImportVideoAsync("legacy_video");

            result.Item.Should().BeSameAs(legacyRow);
            result.Created.Should().BeFalse();
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
            _mockChannelService.ImportChannelFromYouTubeAsync(Arg.Any<string>()).Returns(new YouTubeChannelCreationResult(
                new YouTubeChannel { Id = Guid.NewGuid(), Title = "Imported Channel", ChannelExternalId = "channel_id", MediaType = MediaType.Channel }, true));

            _mockMappingService
                .MapVideoToEntity(videoDto)
                .Returns(mappedVideo);

            _mockVideoService
                .SaveVideoAsync(mappedVideo)
                .Returns(new VideoCreationResult(savedVideo, true));

            // Act
            var result = await _service.ImportFromUrlAsync(videoUrl);

            // Assert
            result.Item.Should().BeSameAs(savedVideo);
            _mockApiClient.Received(1).GetVideoDetailsAsync(videoId);
        }

        [Fact]
        public async Task ImportFromUrlAsync_WithPlaylistUrl_ShouldImportPlaylistContainer()
        {
            // Arrange
            var playlistId = "PLtest_playlist_id";
            var playlistUrl = $"https://www.youtube.com/playlist?list={playlistId}";
            var importedPlaylist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Test Playlist",
                PlaylistExternalId = playlistId,
                MediaType = MediaType.Playlist
            };

            _mockPlaylistService
                .ImportPlaylistFromYouTubeAsync(playlistId)
                .Returns(new YouTubePlaylistCreationResult(importedPlaylist, true));

            // Act
            var result = await _service.ImportFromUrlAsync(playlistUrl);

            // Assert
            result.Item.Should().BeSameAs(importedPlaylist);
            result.Created.Should().BeTrue();
            await _mockPlaylistService.Received(1).ImportPlaylistFromYouTubeAsync(playlistId);
        }

        [Fact]
        public async Task ImportFromUrlAsync_WithChannelUrl_ShouldImportChannelContainer()
        {
            // Arrange
            var channelId = "UC_test_channel_id";
            var channelUrl = $"https://www.youtube.com/channel/{channelId}";
            var importedChannel = new YouTubeChannel
            {
                Id = Guid.NewGuid(),
                Title = "Test Channel",
                ChannelExternalId = channelId,
                MediaType = MediaType.Channel
            };

            _mockChannelService
                .ImportChannelFromYouTubeAsync(channelId)
                .Returns(new YouTubeChannelCreationResult(importedChannel, false));

            // Act
            var result = await _service.ImportFromUrlAsync(channelUrl);

            // Assert
            result.Item.Should().BeSameAs(importedChannel);
            result.Created.Should().BeFalse();
            await _mockChannelService.Received(1).ImportChannelFromYouTubeAsync(channelId);
        }

        [Fact]
        public async Task ImportFromUrlAsync_WhenTheChannelHandleResolvesToNothing_ThrowsNotFound()
        {
            _mockApiClient.GetChannelByHandleAsync(Arg.Any<string>()).Returns((YouTubeChannelDto?)null);
            _mockApiClient.GetChannelDetailsAsync(Arg.Any<string>()).Returns((YouTubeChannelDto?)null);

            var exception = await Assert.ThrowsAsync<YouTubeResourceNotFoundException>(
                () => _service.ImportFromUrlAsync("https://www.youtube.com/@NoSuchChannel"));

            exception.ResourceType.Should().Be("channel");
            await _mockChannelService.DidNotReceive().ImportChannelFromYouTubeAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task ImportVideoAsync_WhenTheQuotaRunsOutDuringChannelImport_StopsWithoutSavingTheVideo()
        {
            var videoDto = new YouTubeVideoDto
            {
                Id = "video_with_channel",
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video", ChannelId = "UC_new_channel" }
            };
            _mockApiClient.GetVideoDetailsAsync("video_with_channel").Returns(videoDto);
            _mockChannelService
                .ImportChannelFromYouTubeAsync("UC_new_channel")
                .ThrowsAsync(new YouTubeQuotaExceededException("quota used up", "quotaExceeded"));

            await Assert.ThrowsAsync<YouTubeQuotaExceededException>(
                () => _service.ImportVideoAsync("video_with_channel"));

            await _mockVideoService.DidNotReceive().SaveVideoAsync(Arg.Any<Video>());
        }

        [Fact]
        public async Task ImportVideoAsync_WhenChannelImportFailsForAnotherReason_SavesTheVideoWithoutAChannel()
        {
            var videoDto = new YouTubeVideoDto
            {
                Id = "video_with_channel",
                Snippet = new YouTubeVideoSnippetDto { Title = "Test Video", ChannelId = "UC_new_channel" }
            };
            var mappedVideo = new Video { Title = "Test Video", MediaType = MediaType.Video, Platform = "YouTube" };
            _mockApiClient.GetVideoDetailsAsync("video_with_channel").Returns(videoDto);
            _mockChannelService
                .ImportChannelFromYouTubeAsync("UC_new_channel")
                .ThrowsAsync(new HttpRequestException("connection reset"));
            _mockMappingService.MapVideoToEntity(videoDto).Returns(mappedVideo);
            _mockVideoService.SaveVideoAsync(mappedVideo).Returns(new VideoCreationResult(mappedVideo, true));

            var result = await _service.ImportVideoAsync("video_with_channel");

            ((Video)result.Item).ChannelId.Should().BeNull();
            result.AutoImportedChannel.Should().BeNull();
            await _mockVideoService.Received(1).SaveVideoAsync(mappedVideo);
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
            var expectedResult = new YouTubePlaylistItemListResponseDto { Items = new List<YouTubePlaylistItemDto>() };

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
