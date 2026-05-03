using Xunit;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class VideoServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ILogger<VideoService>> _mockLogger;
        private readonly VideoService _service;

        public VideoServiceTests()
        {
            _mockLogger = new Mock<ILogger<VideoService>>();
            _service = new VideoService(Context, _mockLogger.Object);
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
                VideoType = VideoType.Series,
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
            var result = await _service.CreateVideoAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Video");
            result.Platform.Should().Be("YouTube");
            result.VideoType.Should().Be(VideoType.Series);
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
                VideoType = VideoType.Series,
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
            var result = await _service.CreateVideoAsync(dto);

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
                VideoType = VideoType.Episode,
                Status = Status.ActivelyExploring
            };

            // Act
            var result = await _service.UpdateVideoAsync(videoId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Platform.Should().Be("Vimeo");
            result.VideoType.Should().Be(VideoType.Episode);
            result.Status.Should().Be(Status.ActivelyExploring);
            
            // Clear tracker and reload from database to verify persistence
            Context.ChangeTracker.Clear();
            var updatedVideo = await Context.Videos.FindAsync(videoId);
            updatedVideo.Should().NotBeNull();
            updatedVideo!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateVideoAsync_WithInvalidId_ShouldThrowArgumentException()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var dto = new CreateVideoDto { Title = "Test", Platform = "YouTube", VideoType = VideoType.Series, Status = Status.Uncharted };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVideoAsync(videoId, dto));
            exception.Message.Should().Contain($"Video with ID {videoId} not found");
        }

        #endregion

        #region DeleteVideoAsync Tests

        [Fact]
        public async Task DeleteVideoAsync_WithValidId_ShouldDeleteVideo()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var video = new Video { Id = videoId, Title = "Test Video", Platform = "YouTube", Topics = new List<Topic>(), Genres = new List<Genre>() };
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();
            
            // Clear change tracker to avoid entity tracking conflicts
            Context.ChangeTracker.Clear();

            // Act
            var result = await _service.DeleteVideoAsync(videoId);

            // Assert
            result.Should().BeTrue();
            
            // Verify deleted from database
            var deletedVideo = await Context.Videos.FindAsync(videoId);
            deletedVideo.Should().BeNull();
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

        #region GetVideoSeriesAsync Tests

        [Fact]
        public async Task GetVideoSeriesAsync_ShouldReturnOnlySeriesVideos()
        {
            // Arrange
            var videos = new List<Video>
            {
                new Video { Id = Guid.NewGuid(), Title = "Series 1", Platform = "YouTube", VideoType = VideoType.Series, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Video { Id = Guid.NewGuid(), Title = "Episode 1", Platform = "YouTube", VideoType = VideoType.Episode, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new Video { Id = Guid.NewGuid(), Title = "Series 2", Platform = "YouTube", VideoType = VideoType.Series, Topics = new List<Topic>(), Genres = new List<Genre>() }
            };
            Context.Videos.AddRange(videos);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetVideoSeriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(v => v.VideoType == VideoType.Series).Should().BeTrue();
        }

        #endregion

        #region VideoExistsAsync Tests

        [Fact]
        public async Task VideoExistsAsync_WithExistingVideo_ShouldReturnTrue()
        {
            // Arrange
            var title = "Test Video";
            var channelId = Guid.NewGuid();
            var video = new Video { Id = Guid.NewGuid(), Title = "Test Video", Platform = "YouTube", ChannelId = channelId, Topics = new List<Topic>(), Genres = new List<Genre>() };
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.VideoExistsAsync(title, channelId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task VideoExistsAsync_WithNonExistingVideo_ShouldReturnFalse()
        {
            // Arrange
            var title = "Non-existing Video";
            var channelId = Guid.NewGuid();

            // Act
            var result = await _service.VideoExistsAsync(title, channelId);

            // Assert
            result.Should().BeFalse();
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
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                ChannelId = channelId
                // No topics or genres provided
            };

            // Act
            var result = await _service.CreateVideoAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("technology");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("educational");
        }

        [Fact]
        public async Task CreateVideoAsync_WithNoTopicsOrGenres_ShouldInheritFromParentVideo()
        {
            // Arrange
            var parentTopic = new Topic { Name = "programming" };
            var parentGenre = new Genre { Name = "tutorial" };
            Context.Topics.Add(parentTopic);
            Context.Genres.Add(parentGenre);
            await Context.SaveChangesAsync();

            var parentVideo = new Video
            {
                Id = Guid.NewGuid(),
                Title = "Parent Series",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Topics = new List<Topic> { parentTopic },
                Genres = new List<Genre> { parentGenre }
            };
            Context.Videos.Add(parentVideo);
            await Context.SaveChangesAsync();

            var dto = new CreateVideoDto
            {
                Title = "Episode 1",
                Platform = "YouTube",
                VideoType = VideoType.Episode,
                Status = Status.Uncharted,
                ParentVideoId = parentVideo.Id
                // No topics or genres, no channel
            };

            // Act
            var result = await _service.CreateVideoAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("programming");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("tutorial");
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
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                ChannelId = channelId,
                Topics = new[] { "science" },
                Genres = new[] { "documentary" }
            };

            // Act
            var result = await _service.CreateVideoAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("science");
            result.Topics.Select(t => t.Name).Should().NotContain("technology");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("documentary");
        }

        [Fact]
        public async Task CreateVideoAsync_ChannelInheritanceTakesPrecedenceOverParentVideo()
        {
            // Arrange
            var channelId = Guid.NewGuid();
            var channelTopic = new Topic { Name = "channel-topic" };
            var parentTopic = new Topic { Name = "parent-topic" };
            Context.Topics.AddRange(channelTopic, parentTopic);
            await Context.SaveChangesAsync();

            var channel = new YouTubeChannel
            {
                Id = channelId,
                Title = "Channel",
                ChannelExternalId = "UC_test789",
                Topics = new List<Topic> { channelTopic },
                Genres = new List<Genre>()
            };
            Context.YouTubeChannels.Add(channel);

            var parentVideo = new Video
            {
                Id = Guid.NewGuid(),
                Title = "Parent Series",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Topics = new List<Topic> { parentTopic },
                Genres = new List<Genre>()
            };
            Context.Videos.Add(parentVideo);
            await Context.SaveChangesAsync();

            var dto = new CreateVideoDto
            {
                Title = "Episode",
                Platform = "YouTube",
                VideoType = VideoType.Episode,
                Status = Status.Uncharted,
                ChannelId = channelId,
                ParentVideoId = parentVideo.Id
                // No topics/genres â€” should inherit from channel, not parent
            };

            // Act
            var result = await _service.CreateVideoAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("channel-topic");
        }

        #endregion
    }
}
