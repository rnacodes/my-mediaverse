using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
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
        private readonly IThumbnailStorageService _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
        private readonly ITypesenseService _mockTypesense = Substitute.For<ITypesenseService>();

        public YouTubeChannelServiceTests()
        {
            _mockYouTubeApiClient = Substitute.For<IYouTubeApiClient>();
            _mockMappingService = Substitute.For<IYouTubeMappingService>();
            _mockLogger = Substitute.For<ILogger<YouTubeChannelService>>();
            // Deletes go through the real shared delete path, so its effects are asserted here too.
            var mediaService = new MediaService(
                Context, Substitute.For<ILogger<MediaService>>(), _mockThumbnailStorage, _mockTypesense);
            _service = new YouTubeChannelService(Context, _mockYouTubeApiClient, _mockMappingService, _mockLogger, mediaService);
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
        public async Task DeleteChannelAsync_ShouldDetachLinks_KeepItsVideos_AndRemoveTheChannelFromTheSearchIndex()
        {
            var channel = CreateTestChannel();
            var video = new Video { Title = "Upload", Platform = "YouTube", ExternalId = "upload00001", Channel = channel };
            var mixlist = TestDataFactory.CreateMixlist("Favorites");
            mixlist.MediaItems.Add(channel);
            Context.Mixlists.Add(mixlist);
            Context.YouTubeChannels.Add(channel);
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();

            var result = await _service.DeleteChannelAsync(channel.Id);

            result.Should().BeTrue();
            Context.MediaItems.Any(m => m.Id == channel.Id).Should().BeFalse();
            Context.Mixlists.Single().MediaItems.Should().BeEmpty();
            Context.Videos.Any(v => v.Id == video.Id).Should().BeTrue("deleting a channel keeps its videos");
            await _mockTypesense.Received(1).DeleteMediaItemAsync(channel.Id);
            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(video.Id);
        }

        [Fact]
        public async Task DeleteChannelAsync_ShouldReturnFalse_AndDeleteNothing_WhenTheIdBelongsToAnotherMediaType()
        {
            var video = new Video { Title = "Not a channel", Platform = "YouTube" };
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();

            var result = await _service.DeleteChannelAsync(video.Id);

            result.Should().BeFalse();
            Context.Videos.Any(v => v.Id == video.Id).Should().BeTrue();
            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(Arg.Any<Guid>());
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

        #region Import result Tests

        [Fact]
        public async Task ImportChannelFromYouTubeAsync_ForANewChannel_SavesItAndReportsItAsCreated()
        {
            var channelDto = new YouTubeChannelDto { Id = "UCnew", Snippet = new YouTubeChannelSnippetDto { Title = "New Channel" } };
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCnew").Returns(channelDto);
            _mockMappingService.MapChannelToYouTubeChannelEntity(channelDto).Returns(CreateTestChannel("New Channel", "UCnew"));

            var result = await _service.ImportChannelFromYouTubeAsync("UCnew");

            result.Created.Should().BeTrue();
            result.Channel.ChannelExternalId.Should().Be("UCnew");
            Context.YouTubeChannels.Should().ContainSingle();
        }

        [Fact]
        public async Task ImportChannelFromYouTubeAsync_WhenTheChannelIsAlreadyStored_ReturnsItWithoutCallingYouTube()
        {
            var stored = CreateTestChannel("Stored Channel", "UCstored");
            Context.YouTubeChannels.Add(stored);
            await Context.SaveChangesAsync();

            var result = await _service.ImportChannelFromYouTubeAsync("UCstored");

            result.Created.Should().BeFalse();
            result.Channel.Id.Should().Be(stored.Id);
            Context.YouTubeChannels.Should().ContainSingle();
            await _mockYouTubeApiClient.DidNotReceive().GetChannelDetailsAsync(Arg.Any<string>());
        }

        #endregion

        #region SyncChannelMetadataAsync Tests

        [Fact]
        public async Task SyncChannelMetadataAsync_AppliesYouTubeValues_AndStampsTheSync()
        {
            var channel = CreateTestChannel("Old name", "UCsync");
            channel.SubscriberCount = 10;
            channel.Notes = "My notes";
            channel.Rating = Rating.Like;
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCsync").Returns(new YouTubeChannelDto
            {
                Id = "UCsync",
                Snippet = new YouTubeChannelSnippetDto
                {
                    Title = "New name",
                    Description = "New description",
                    PublishedAt = new DateTime(2015, 3, 4, 0, 0, 0, DateTimeKind.Unspecified),
                    Thumbnails = new YouTubeThumbnailsDto { High = new YouTubeThumbnailDto { Url = "https://yt3.ggpht.com/new" } }
                },
                Statistics = new YouTubeChannelStatisticsDto { SubscriberCount = "2000", VideoCount = "50", ViewCount = "123456" }
            });

            await _service.SyncChannelMetadataAsync(channel.Id);

            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubeChannels.FindAsync(channel.Id);
            stored!.Title.Should().Be("New name");
            stored.Description.Should().Be("New description");
            stored.SubscriberCount.Should().Be(2000);
            stored.VideoCount.Should().Be(50);
            stored.ViewCount.Should().Be(123456);
            stored.Thumbnail.Should().Be("https://yt3.ggpht.com/new");
            stored.PublishedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
            stored.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            stored.Notes.Should().Be("My notes");
            stored.Rating.Should().Be(Rating.Like);
        }

        [Fact]
        public async Task SyncChannelMetadataAsync_WhenYouTubeSendsNoThumbnail_KeepsTheStoredOne()
        {
            var channel = CreateTestChannel("Channel", "UCsync");
            channel.Thumbnail = "https://yt3.ggpht.com/stored";
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCsync").Returns(new YouTubeChannelDto
            {
                Id = "UCsync",
                Snippet = new YouTubeChannelSnippetDto { Title = "Channel" }
            });

            await _service.SyncChannelMetadataAsync(channel.Id);

            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubeChannels.FindAsync(channel.Id);
            stored!.Thumbnail.Should().Be("https://yt3.ggpht.com/stored");
        }

        [Fact]
        public async Task SyncChannelMetadataAsync_KeepsAThumbnailChosenByHand()
        {
            var channel = CreateTestChannel("Channel", "UCsync");
            channel.Thumbnail = "https://cdn.example.com/my-avatar.jpg";
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCsync").Returns(new YouTubeChannelDto
            {
                Id = "UCsync",
                Snippet = new YouTubeChannelSnippetDto
                {
                    Title = "Channel",
                    Thumbnails = new YouTubeThumbnailsDto { High = new YouTubeThumbnailDto { Url = "https://yt3.ggpht.com/new" } }
                }
            });

            await _service.SyncChannelMetadataAsync(channel.Id);

            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubeChannels.FindAsync(channel.Id);
            stored!.Thumbnail.Should().Be("https://cdn.example.com/my-avatar.jpg");
        }

        #endregion

        #region YouTube not-found Tests

        [Fact]
        public async Task ImportChannelFromYouTubeAsync_WhenYouTubeHasNoSuchChannel_ThrowsNotFound_AndStoresNothing()
        {
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCgone").Returns((YouTubeChannelDto?)null);

            var exception = await Assert.ThrowsAsync<YouTubeResourceNotFoundException>(
                () => _service.ImportChannelFromYouTubeAsync("UCgone"));

            exception.ResourceType.Should().Be("channel");
            exception.Identifier.Should().Be("UCgone");
            Context.YouTubeChannels.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncChannelMetadataAsync_WhenTheChannelLeftYouTube_ThrowsNotFound_AndKeepsTheStoredRow()
        {
            var channel = CreateTestChannel("Stored Channel", "UCgone");
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCgone").Returns((YouTubeChannelDto?)null);

            await Assert.ThrowsAsync<YouTubeResourceNotFoundException>(
                () => _service.SyncChannelMetadataAsync(channel.Id));

            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubeChannels.FindAsync(channel.Id);
            stored!.Title.Should().Be("Stored Channel");
        }

        #endregion
    }
}
