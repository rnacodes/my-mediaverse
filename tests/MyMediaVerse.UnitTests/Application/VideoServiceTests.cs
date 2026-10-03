using Xunit;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class VideoServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<VideoService> _mockLogger;
        private readonly VideoService _service;
        private readonly IThumbnailStorageService _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
        private readonly ITypesenseService _mockTypesense = Substitute.For<ITypesenseService>();

        public VideoServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<VideoService>>();
            // Deletes go through the real shared delete path, so its effects are asserted here too.
            var mediaService = new MediaService(
                Context, Substitute.For<ILogger<MediaService>>(), _mockThumbnailStorage, _mockTypesense);
            _service = new VideoService(Context, _mockLogger, mediaService);
        }

        #region GetAllVideosAsync Tests

        [Fact]
        public async Task GetAllVideosAsync_ShouldReturnAllVideos()
        {
            // Arrange
            var videos = new List<Video>
            {
                new Video { Id = Guid.NewGuid(), Title = "Video 1", Platform = "YouTube", Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Video { Id = Guid.NewGuid(), Title = "Video 2", Platform = "Vimeo", Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.Videos.AddRange(videos);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllVideosAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Title.Should().Be("Video 1");
        }

        [Fact]
        public async Task GetAllVideosAsync_WhenExceptionOccurs_ShouldLogErrorAndThrow()
        {
            // Arrange
            // Dispose the context to force an exception
            Context.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.GetAllVideosAsync());
        }

        #endregion

        #region GetVideoByIdAsync Tests

        [Fact]
        public async Task GetVideoByIdAsync_WithValidId_ShouldReturnVideo()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var video = new Video { Id = videoId, Title = "Test Video", Platform = "YouTube", Topics = new List<Topic>(), Genres = new List<Genre>() };
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetVideoByIdAsync(videoId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(videoId);
            result.Title.Should().Be("Test Video");
        }

        [Fact]
        public async Task GetVideoByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var videoId = Guid.NewGuid();

            // Act
            var result = await _service.GetVideoByIdAsync(videoId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateVideoAsync Tests

        [Fact]
        public async Task CreateVideoAsync_WithValidDto_ShouldCreateVideo()
        {
            // Arrange
            var dto = new CreateVideoDto
            {
                Title = "Test Video",
                Platform = "YouTube",
                Status = Status.Uncharted,
                Topics = new[] { "technology", "programming" },
                Genres = new[] { "educational", "tutorial" }
            };

            // Pre-create one topic and genre to test reuse
            var existingTopic = new Topic { Name = "technology" };
            var existingGenre = new Genre { Name = "educational" };
            Context.Topics.Add(existingTopic);
            Context.Genres.Add(existingGenre);
            await Context.SaveChangesAsync();

            // Act
            var result = (await _service.CreateVideoAsync(dto)).Video;

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Video");
            result.Platform.Should().Be("YouTube");
            result.MediaType.Should().Be(MediaType.Video);
            result.Topics.Should().HaveCount(2);
            result.Genres.Should().HaveCount(2);
            
            // Verify saved to database
            var savedVideo = await Context.Videos.FindAsync(result.Id);
            savedVideo.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateVideoAsync_WithExistingTopicsAndGenres_ShouldReuseExisting()
        {
            // Arrange
            var dto = new CreateVideoDto
            {
                Title = "Test Video",
                Platform = "YouTube",
                Status = Status.Uncharted,
                Topics = new[] { "technology" },
                Genres = new[] { "educational" }
            };

            var existingTopic = new Topic { Name = "technology" };
            var existingGenre = new Genre { Name = "educational" };
            Context.Topics.Add(existingTopic);
            Context.Genres.Add(existingGenre);
            await Context.SaveChangesAsync();

            var initialTopicCount = Context.Topics.Count();
            var initialGenreCount = Context.Genres.Count();

            // Act
            var result = (await _service.CreateVideoAsync(dto)).Video;

            // Assert
            result.Topics.Should().HaveCount(1);
            result.Genres.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("technology");
            result.Genres.First().Name.Should().Be("educational");
            
            // Verify no duplicates were created
            Context.Topics.Count().Should().Be(initialTopicCount);
            Context.Genres.Count().Should().Be(initialGenreCount);
        }

        #endregion

        #region UpdateVideoAsync Tests

        [Fact]
        public async Task UpdateVideoAsync_WithValidId_ShouldUpdateVideo()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var existingVideo = new Video 
            { 
                Id = videoId, 
                Title = "Old Title", 
                Platform = "YouTube",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Videos.Add(existingVideo);
            await Context.SaveChangesAsync();

            var dto = new CreateVideoDto
            {
                Title = "Updated Title",
                Platform = "Vimeo",
                Status = Status.ActivelyExploring
            };

            // Act
            var result = await _service.UpdateVideoAsync(videoId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Platform.Should().Be("Vimeo");
            result.Status.Should().Be(Status.ActivelyExploring);
            
            // Clear tracker and reload from database to verify persistence
            Context.ChangeTracker.Clear();
            var updatedVideo = await Context.Videos.FindAsync(videoId);
            updatedVideo.Should().NotBeNull();
            updatedVideo!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateVideoAsync_WhenTheRequestLeavesOutRelatedNotes_KeepsTheStoredOnes()
        {
            var videoId = Guid.NewGuid();
            Context.Videos.Add(new Video
            {
                Id = videoId,
                Title = "Old Title",
                Platform = "YouTube",
                RelatedNotes = "[[video-notes]]",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();

            await _service.UpdateVideoAsync(videoId, new CreateVideoDto
            {
                Title = "Updated Title",
                Platform = "YouTube",
                Status = Status.Uncharted
            });

            Context.ChangeTracker.Clear();
            var stored = await Context.Videos.FindAsync(videoId);
            stored!.Title.Should().Be("Updated Title");
            stored.RelatedNotes.Should().Be("[[video-notes]]");
        }

        [Fact]
        public async Task UpdateVideoAsync_WithInvalidId_ShouldThrowArgumentException()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var dto = new CreateVideoDto { Title = "Test", Platform = "YouTube", Status = Status.Uncharted };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVideoAsync(videoId, dto));
            exception.Message.Should().Contain($"Video with ID {videoId} not found");
        }

        #endregion

        #region DeleteVideoAsync Tests

        [Fact]
        public async Task DeleteVideoAsync_ShouldDetachLinks_AndRemoveTheVideoFromTheSearchIndex()
        {
            var video = new Video { Id = Guid.NewGuid(), Title = "Test Video", Platform = "YouTube", Topics = new List<Topic>(), Genres = new List<Genre>() };
            video.Topics.Add(new Topic { Name = "learning" });
            var mixlist = TestDataFactory.CreateMixlist("Favorites");
            mixlist.MediaItems.Add(video);
            Context.Mixlists.Add(mixlist);
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();

            var result = await _service.DeleteVideoAsync(video.Id);

            result.Should().BeTrue();
            Context.MediaItems.Any(m => m.Id == video.Id).Should().BeFalse();
            Context.Mixlists.Single().MediaItems.Should().BeEmpty();
            Context.Topics.Any(t => t.Name == "learning").Should().BeTrue("topics are shared and outlive the item");
            await _mockTypesense.Received(1).DeleteMediaItemAsync(video.Id);
        }

        [Fact]
        public async Task DeleteVideoAsync_ShouldReturnFalse_AndDeleteNothing_WhenTheIdBelongsToAnotherMediaType()
        {
            var channel = TestDataFactory.CreateYouTubeChannel("A Channel", "UCother");
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            var result = await _service.DeleteVideoAsync(channel.Id);

            result.Should().BeFalse();
            Context.YouTubeChannels.Any(c => c.Id == channel.Id).Should().BeTrue();
            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task DeleteVideoAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var videoId = Guid.NewGuid();

            // Act
            var result = await _service.DeleteVideoAsync(videoId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetVideosByChannelAsync Tests

        [Fact]
        public async Task GetVideosByChannelAsync_WithValidChannel_ShouldReturnVideos()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var otherChannelId = Guid.NewGuid();
            var videos = new List<Video>
            {
                new Video { Id = Guid.NewGuid(), Title = "Video 1", Platform = "YouTube", ChannelId = channelId, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Video { Id = Guid.NewGuid(), Title = "Video 2", Platform = "YouTube", ChannelId = otherChannelId, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Video { Id = Guid.NewGuid(), Title = "Video 3", Platform = "YouTube", ChannelId = channelId, Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.Videos.AddRange(videos);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetVideosByChannelAsync(channelId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(v => v.ChannelId == channelId).Should().BeTrue();
        }

        #endregion

        #region Identity Tests

        private Video AddVideo(string title, string platform = "YouTube", string? externalId = null,
            string? link = null, Guid? channelId = null)
        {
            var video = new Video
            {
                Id = Guid.NewGuid(),
                Title = title,
                Platform = platform,
                ExternalId = externalId,
                Link = link,
                ChannelId = channelId,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Videos.Add(video);
            return video;
        }

        private YouTubeChannel AddChannel(string externalId = "UC_channel")
        {
            var channel = new YouTubeChannel
            {
                Id = Guid.NewGuid(),
                Title = "Channel",
                ChannelExternalId = externalId,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.YouTubeChannels.Add(channel);
            return channel;
        }

        [Fact]
        public async Task CreateVideoAsync_ForANewVideo_ReportsItAsCreated()
        {
            var result = await _service.CreateVideoAsync(new CreateVideoDto
            {
                Title = "New Video",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ExternalId = "abc123DEF45"
            });

            result.Created.Should().BeTrue();
            Context.Videos.Count().Should().Be(1);
        }

        [Fact]
        public async Task CreateVideoAsync_WhenTheIdIsAlreadyStored_ReturnsTheStoredRowUntouched()
        {
            var stored = AddVideo("Stored Title", externalId: "abc123DEF45");
            stored.Description = "Stored description";
            await Context.SaveChangesAsync();

            var result = await _service.CreateVideoAsync(new CreateVideoDto
            {
                Title = "A Different Title",
                Platform = "YouTube",
                Status = Status.Completed,
                ExternalId = "abc123DEF45",
                Description = "Incoming description",
                Notes = "Incoming notes"
            });

            result.Created.Should().BeFalse();
            result.Video.Id.Should().Be(stored.Id);
            result.Video.Title.Should().Be("Stored Title");
            result.Video.Description.Should().Be("Stored description");
            result.Video.Notes.Should().BeNull();
            result.Video.Status.Should().Be(Status.Uncharted);
            Context.Videos.Count().Should().Be(1);
        }

        [Fact]
        public async Task SaveVideoAsync_WhenTheIdIsAlreadyStored_DoesNotEvenFillBlankFields()
        {
            var stored = AddVideo("Stored Title", externalId: "abc123DEF45");
            await Context.SaveChangesAsync();

            var result = await _service.SaveVideoAsync(new Video
            {
                Title = "Title From YouTube",
                Platform = "YouTube",
                ExternalId = "abc123DEF45",
                Link = "https://www.youtube.com/watch?v=abc123DEF45",
                Description = "Description from YouTube",
                LengthInSeconds = 212
            });

            result.Created.Should().BeFalse();
            Context.ChangeTracker.Clear();
            var row = await Context.Videos.FindAsync(stored.Id);
            row!.Link.Should().BeNull();
            row.Description.Should().BeNull();
            row.LengthInSeconds.Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateVideoAsync_StoresABlankIdAsNull(string blankId)
        {
            var result = await _service.CreateVideoAsync(new CreateVideoDto
            {
                Title = "No Id",
                Platform = "Vimeo",
                Status = Status.Uncharted,
                ExternalId = blankId
            });

            result.Video.ExternalId.Should().BeNull();
        }

        [Fact]
        public async Task CreateVideoAsync_TrimsTheId()
        {
            var result = await _service.CreateVideoAsync(new CreateVideoDto
            {
                Title = "Padded Id",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ExternalId = "  abc123DEF45 "
            });

            result.Video.ExternalId.Should().Be("abc123DEF45");
        }

        [Fact]
        public async Task CreateVideoAsync_TwoVideosSharingOnlyATitle_AreBothSaved()
        {
            var dto = new CreateVideoDto { Title = "Introduction", Platform = "YouTube", Status = Status.Uncharted };

            var first = await _service.CreateVideoAsync(dto);
            var second = await _service.CreateVideoAsync(dto);

            first.Created.Should().BeTrue();
            second.Created.Should().BeTrue();
            Context.Videos.Count().Should().Be(2);
        }

        [Fact]
        public async Task SaveVideoAsync_WhenARowIsFoundByItsLink_GivesItTheIdAndFillsBlankFieldsOnly()
        {
            var channel = AddChannel();
            var legacy = AddVideo("My Own Title", link: "https://youtu.be/abc123DEF45");
            legacy.Notes = "My notes";
            legacy.Description = "My description";
            await Context.SaveChangesAsync();

            var result = await _service.SaveVideoAsync(new Video
            {
                Title = "Title From YouTube",
                Platform = "YouTube",
                ExternalId = "abc123DEF45",
                Link = "https://www.youtube.com/watch?v=abc123DEF45",
                ChannelId = channel.Id,
                Description = "Description from YouTube",
                Thumbnail = "https://i.ytimg.com/vi/abc123DEF45/hqdefault.jpg",
                LengthInSeconds = 212
            });

            result.Created.Should().BeFalse();
            Context.Videos.Count().Should().Be(1);

            Context.ChangeTracker.Clear();
            var stored = await Context.Videos.FindAsync(legacy.Id);
            stored!.ExternalId.Should().Be("abc123DEF45");
            stored.ChannelId.Should().Be(channel.Id);
            stored.Thumbnail.Should().Be("https://i.ytimg.com/vi/abc123DEF45/hqdefault.jpg");
            stored.LengthInSeconds.Should().Be(212);
            stored.Title.Should().Be("My Own Title");
            stored.Description.Should().Be("My description");
            stored.Notes.Should().Be("My notes");
            stored.Link.Should().Be("https://youtu.be/abc123DEF45");
        }

        [Fact]
        public async Task SaveVideoAsync_ForANewVideo_SavesItAndInheritsTheChannelTopics()
        {
            var topic = new Topic { Name = "technology" };
            Context.Topics.Add(topic);
            var channel = AddChannel();
            channel.Topics.Add(topic);
            await Context.SaveChangesAsync();

            var result = await _service.SaveVideoAsync(new Video
            {
                Title = "New Video",
                Platform = "YouTube",
                ExternalId = "abc123DEF45",
                ChannelId = channel.Id
            });

            result.Created.Should().BeTrue();
            result.Video.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "technology" });
        }

        [Fact]
        public async Task GetVideoByExternalIdAsync_MatchesTheIdOnThatPlatformOnly()
        {
            var youTube = AddVideo("On YouTube", platform: "YouTube", externalId: "12345678901");
            AddVideo("On Vimeo", platform: "Vimeo", externalId: "12345678901");
            AddVideo("Link only", link: "https://youtu.be/abc123DEF45");
            await Context.SaveChangesAsync();

            (await _service.GetVideoByExternalIdAsync("YouTube", "12345678901"))!.Id.Should().Be(youTube.Id);
            (await _service.GetVideoByExternalIdAsync("YouTube", "abc123DEF45")).Should().BeNull();
            (await _service.GetVideoByExternalIdAsync("YouTube", " ")).Should().BeNull();
        }

        [Fact]
        public async Task Reads_IncludeTheChannel()
        {
            var channel = AddChannel();
            var video = AddVideo("With Channel", externalId: "abc123DEF45", channelId: channel.Id);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            (await _service.GetVideoByIdAsync(video.Id))!.Channel!.Id.Should().Be(channel.Id);
            (await _service.GetAllVideosAsync()).Single().Channel!.Id.Should().Be(channel.Id);
            (await _service.GetVideoByExternalIdAsync("YouTube", "abc123DEF45"))!.Channel!.Id.Should().Be(channel.Id);
        }

        [Fact]
        public async Task UpdateVideoAsync_WhenTheRequestLeavesOutTheChannel_KeepsTheStoredLink()
        {
            var channel = AddChannel();
            var video = AddVideo("Old Title", externalId: "abc123DEF45", channelId: channel.Id);
            await Context.SaveChangesAsync();

            await _service.UpdateVideoAsync(video.Id, new CreateVideoDto
            {
                Title = "Updated Title",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ExternalId = "abc123DEF45"
            });

            Context.ChangeTracker.Clear();
            var stored = await Context.Videos.FindAsync(video.Id);
            stored!.Title.Should().Be("Updated Title");
            stored.ChannelId.Should().Be(channel.Id);
        }

        [Fact]
        public async Task UpdateVideoAsync_StoresABlankIdAsNull()
        {
            var video = AddVideo("Title", externalId: "abc123DEF45");
            await Context.SaveChangesAsync();

            await _service.UpdateVideoAsync(video.Id, new CreateVideoDto
            {
                Title = "Title",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ExternalId = "  "
            });

            Context.ChangeTracker.Clear();
            (await Context.Videos.FindAsync(video.Id))!.ExternalId.Should().BeNull();
        }

        [Fact]
        public async Task UpdateVideoAsync_WhenAnotherVideoHoldsTheId_ThrowsAConflictAndChangesNothing()
        {
            AddVideo("Owner", externalId: "abc123DEF45");
            var other = AddVideo("Other", externalId: "zzz999YYY88");
            await Context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<VideoIdentityConflictException>(() =>
                _service.UpdateVideoAsync(other.Id, new CreateVideoDto
                {
                    Title = "Renamed",
                    Platform = "YouTube",
                    Status = Status.Uncharted,
                    ExternalId = "abc123DEF45"
                }));

            exception.ExternalId.Should().Be("abc123DEF45");
            Context.ChangeTracker.Clear();
            var stored = await Context.Videos.FindAsync(other.Id);
            stored!.Title.Should().Be("Other");
            stored.ExternalId.Should().Be("zzz999YYY88");
        }

        [Fact]
        public async Task UpdateVideoAsync_KeepingItsOwnId_IsNotAConflict()
        {
            var video = AddVideo("Title", externalId: "abc123DEF45");
            await Context.SaveChangesAsync();

            var updated = await _service.UpdateVideoAsync(video.Id, new CreateVideoDto
            {
                Title = "Renamed",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ExternalId = "abc123DEF45"
            });

            updated.Title.Should().Be("Renamed");
        }

        #endregion

        #region Topic/Genre Inheritance Tests

        [Fact]
        public async Task CreateVideoAsync_WithNoTopicsOrGenres_ShouldInheritFromChannel()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var channelTopic = new Topic { Name = "technology" };
            var channelGenre = new Genre { Name = "educational" };
            Context.Topics.Add(channelTopic);
            Context.Genres.Add(channelGenre);
            await Context.SaveChangesAsync();

            var channel = new YouTubeChannel
            {
                Id = channelId,
                Title = "Tech Channel",
                ChannelExternalId = "UC_test123",
                Topics = new List<Topic> { channelTopic },
                Genres = new List<Genre> { channelGenre }
            };
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            var dto = new CreateVideoDto
            {
                Title = "Test Video",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ChannelId = channelId
                // No topics or genres provided
            };

            // Act
            var result = (await _service.CreateVideoAsync(dto)).Video;

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("technology");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("educational");
        }

        [Fact]
        public async Task CreateVideoAsync_WithExplicitTopics_ShouldNotInheritFromChannel()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var channelTopic = new Topic { Name = "technology" };
            Context.Topics.Add(channelTopic);
            await Context.SaveChangesAsync();

            var channel = new YouTubeChannel
            {
                Id = channelId,
                Title = "Tech Channel",
                ChannelExternalId = "UC_test456",
                Topics = new List<Topic> { channelTopic },
                Genres = new List<Genre>()
            };
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();

            var dto = new CreateVideoDto
            {
                Title = "Test Video",
                Platform = "YouTube",
                Status = Status.Uncharted,
                ChannelId = channelId,
                Topics = new[] { "science" },
                Genres = new[] { "documentary" }
            };

            // Act
            var result = (await _service.CreateVideoAsync(dto)).Video;

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("science");
            result.Topics.Select(t => t.Name).Should().NotContain("technology");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("documentary");
        }

        #endregion
    }
}
