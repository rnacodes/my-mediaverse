using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class YouTubeChannelServiceTests : InMemoryDbTestBase
    {
        private readonly IYouTubeApiClient _mockYouTubeApiClient;
        private readonly IYouTubeMappingService _mockMappingService;
        private readonly ILogger<YouTubeChannelService> _mockLogger;
        private readonly YouTubeChannelService _service;

        public YouTubeChannelServiceTests()
        {
            _mockYouTubeApiClient = Substitute.For<IYouTubeApiClient>();
            _mockMappingService = Substitute.For<IYouTubeMappingService>();
            _mockLogger = Substitute.For<ILogger<YouTubeChannelService>>();
            _service = new YouTubeChannelService(Context, _mockYouTubeApiClient, _mockMappingService, _mockLogger);
        }

        private YouTubeChannel CreateTestChannel(string title = "Test Channel", string externalId = "UCtest123")
        {
            return new YouTubeChannel
            {
                Title = title,
                ChannelExternalId = externalId,
                MediaType = MediaType.Channel,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Videos = new List<Video>(),
                Mixlists = new List<Mixlist>()
            };
        }

        #region GetAllChannelsAsync Tests

        [Fact]
        public async Task GetAllChannelsAsync_ShouldReturnAllChannels()
        {
            // Arrange
            Context.YouTubeChannels.AddRange(
                CreateTestChannel("Channel 1", "UC001"),
                CreateTestChannel("Channel 2", "UC002")
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllChannelsAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllChannelsAsync_WhenEmpty_ShouldReturnEmptyList()
        {
            // Act
            var result = await _service.GetAllChannelsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetChannelByIdAsync Tests

        [Fact]
        public async Task GetChannelByIdAsync_WhenExists_ShouldReturnChannel()
        {
            // Arrange
            var channel = CreateTestChannel();
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetChannelByIdAsync(channel.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Channel");
        }

        [Fact]
        public async Task GetChannelByIdAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetChannelByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetChannelByExternalIdAsync Tests

        [Fact]
        public async Task GetChannelByExternalIdAsync_WhenExists_ShouldReturnChannel()
        {
            // Arrange
            var channel = CreateTestChannel("My Channel", "UCabc123");
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetChannelByExternalIdAsync("UCabc123");

            // Assert
            result.Should().NotBeNull();
            result!.ChannelExternalId.Should().Be("UCabc123");
        }

        [Fact]
        public async Task GetChannelByExternalIdAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetChannelByExternalIdAsync("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetChannelVideosAsync Tests

        [Fact]
        public async Task GetChannelVideosAsync_ShouldReturnVideosForChannel()
        {
            // Arrange
            var channel = CreateTestChannel();
            Context.YouTubeChannels.Add(channel);

            var video1 = new Video
            {
                Title = "Video 1",
                Platform = "YouTube",
                ChannelId = channel.Id,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            var video2 = new Video
            {
                Title = "Video 2",
                Platform = "YouTube",
                ChannelId = channel.Id,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            var otherVideo = new Video
            {
                Title = "Other Video",
                Platform = "YouTube",
                ChannelId = Guid.NewGuid(),
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };

            Context.Videos.AddRange(video1, video2, otherVideo);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetChannelVideosAsync(channel.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(v => v.ChannelId == channel.Id);
        }

        [Fact]
        public async Task GetChannelVideosAsync_WhenNoVideos_ShouldReturnEmptyList()
        {
            // Arrange
            var channel = CreateTestChannel();
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetChannelVideosAsync(channel.Id);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region CreateChannelAsync Tests

        [Fact]
        public async Task CreateChannelAsync_WithValidDto_ShouldCreateChannel()
        {
            // Arrange
            var dto = new CreateYouTubeChannelDto
            {
                Title = "New Channel",
                ChannelExternalId = "UCnew456",
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateChannelAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Channel");
            result.ChannelExternalId.Should().Be("UCnew456");
            result.MediaType.Should().Be(MediaType.Channel);
            Context.YouTubeChannels.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateChannelAsync_WhenDuplicateExternalId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var existing = CreateTestChannel("Existing", "UCdupe");
            Context.YouTubeChannels.Add(existing);
            await Context.SaveChangesAsync();

            var dto = new CreateYouTubeChannelDto
            {
                Title = "Duplicate",
                ChannelExternalId = "UCdupe",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateChannelAsync(dto));
        }

        [Fact]
        public async Task CreateChannelAsync_WithTopics_ShouldNormalizeToLowercase()
        {
            // Arrange
            var dto = new CreateYouTubeChannelDto
            {
                Title = "Tagged Channel",
                ChannelExternalId = "UCtag",
                Topics = new[] { "Technology", " GAMING " },
                Genres = new[] { "Entertainment" }
            };

            // Act
            var result = await _service.CreateChannelAsync(dto);

            // Assert
            result.Topics.Select(t => t.Name).Should().Contain("technology");
            result.Topics.Select(t => t.Name).Should().Contain("gaming");
            result.Genres.Select(g => g.Name).Should().Contain("entertainment");
        }

        [Fact]
        public async Task CreateChannelAsync_ShouldSetDateAddedAndLastSyncedAt()
        {
            // Arrange
            var before = DateTime.UtcNow;
            var dto = new CreateYouTubeChannelDto
            {
                Title = "Timed Channel",
                ChannelExternalId = "UCtime",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateChannelAsync(dto);

            // Assert
            result.DateAdded.Should().BeOnOrAfter(before);
            result.LastSyncedAt.Should().NotBeNull();
        }

        #endregion

        #region DeleteChannelAsync Tests

        [Fact]
        public async Task DeleteChannelAsync_WhenExists_ShouldReturnTrue()
        {
            // Arrange
            var channel = CreateTestChannel();
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteChannelAsync(channel.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteChannelAsync_WhenNotExists_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteChannelAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ChannelExistsAsync Tests

        [Fact]
        public async Task ChannelExistsAsync_WhenExists_ShouldReturnTrue()
        {
            // Arrange
            var channel = CreateTestChannel("Existing", "UCexists");
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.ChannelExistsAsync("UCexists");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ChannelExistsAsync_WhenNotExists_ShouldReturnFalse()
        {
            // Act
            var result = await _service.ChannelExistsAsync("UCmissing");

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
