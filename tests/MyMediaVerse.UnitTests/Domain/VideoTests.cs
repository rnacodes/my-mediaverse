using Xunit;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using System;
using System.Collections.Generic;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class VideoTests
    {
        #region Constructor Tests

        [Fact]
        public void Video_ShouldInitializeWithDefaultValues()
        {
            // Act
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube"
            };

            // Assert
            video.Should().NotBeNull();
            video.Title.Should().Be("Test Video");
            video.Platform.Should().Be("YouTube");
            video.LengthInSeconds.Should().Be(0);
            video.Topics.Should().NotBeNull();
            video.Genres.Should().NotBeNull();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Video_ShouldAllowSettingAllProperties()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var dateAdded = DateTime.UtcNow;
            var dateCompleted = DateTime.UtcNow.AddDays(-1);

            // Act
            var video = new Video
            {
                Id = videoId,
                Title = "Test Video",
                Platform = "YouTube",
                ChannelId = Guid.NewGuid(),
                LengthInSeconds = 3600,
                ExternalId = "external123",
                MediaType = MediaType.Video,
                Status = Status.ActivelyExploring,
                DateAdded = dateAdded,
                DateCompleted = dateCompleted,
                Rating = Rating.Like,
                OwnershipStatus = OwnershipStatus.Own,
                Description = "Test description",
                Notes = "Test notes",
                RelatedNotes = "Related notes",
                Thumbnail = "https://example.com/thumb.jpg",
                Link = "https://example.com/video"
            };

            // Assert
            video.Id.Should().Be(videoId);
            video.Title.Should().Be("Test Video");
            video.Platform.Should().Be("YouTube");
            video.ChannelId.Should().NotBeNull();
            video.LengthInSeconds.Should().Be(3600);
            video.ExternalId.Should().Be("external123");
            video.MediaType.Should().Be(MediaType.Video);
            video.Status.Should().Be(Status.ActivelyExploring);
            video.DateAdded.Should().Be(dateAdded);
            video.DateCompleted.Should().Be(dateCompleted);
            video.Rating.Should().Be(Rating.Like);
            video.OwnershipStatus.Should().Be(OwnershipStatus.Own);
            video.Description.Should().Be("Test description");
            video.Notes.Should().Be("Test notes");
            video.RelatedNotes.Should().Be("Related notes");
            video.Thumbnail.Should().Be("https://example.com/thumb.jpg");
            video.Link.Should().Be("https://example.com/video");
        }

        #endregion

        #region GetEffectiveThumbnail Tests

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasThumbnail_ShouldReturnVideoThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = "https://example.com/video-thumb.jpg"
            };

            var channel = new YouTubeChannel
            {
                Title = "Parent Channel",
                ChannelExternalId = "UC_test_channel",
                MediaType = MediaType.Channel,
                Thumbnail = "https://example.com/channel-thumb.jpg"
            };

            video.Channel = channel;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/video-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasNoThumbnailButChannelDoes_ShouldReturnChannelThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = null
            };

            var channel = new YouTubeChannel
            {
                Title = "Parent Channel",
                ChannelExternalId = "UC_test_channel",
                MediaType = MediaType.Channel,
                Thumbnail = "https://example.com/channel-thumb.jpg"
            };

            video.Channel = channel;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/channel-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasEmptyThumbnailButChannelDoes_ShouldReturnChannelThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = ""
            };

            var channel = new YouTubeChannel
            {
                Title = "Parent Channel",
                ChannelExternalId = "UC_test_channel",
                MediaType = MediaType.Channel,
                Thumbnail = "https://example.com/channel-thumb.jpg"
            };

            video.Channel = channel;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/channel-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenNeitherVideoNorChannelHasThumbnail_ShouldReturnNull()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = null
            };

            var channel = new YouTubeChannel
            {
                Title = "Parent Channel",
                ChannelExternalId = "UC_test_channel",
                MediaType = MediaType.Channel,
                Thumbnail = null
            };

            video.Channel = channel;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasNoChannel_ShouldReturnVideoThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = "https://example.com/video-thumb.jpg",
                Channel = null
            };

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/video-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasNoChannelAndNoThumbnail_ShouldReturnNull()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = null,
                Channel = null
            };

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Navigation Properties Tests

        [Fact]
        public void Video_ShouldSupportTopicsAndGenres()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube"
            };

            var topic1 = new Topic { Name = "technology" };
            var topic2 = new Topic { Name = "programming" };
            var genre1 = new Genre { Name = "educational" };
            var genre2 = new Genre { Name = "tutorial" };

            // Act
            video.Topics.Add(topic1);
            video.Topics.Add(topic2);
            video.Genres.Add(genre1);
            video.Genres.Add(genre2);

            // Assert
            video.Topics.Should().HaveCount(2);
            video.Topics.Should().Contain(topic1);
            video.Topics.Should().Contain(topic2);
            
            video.Genres.Should().HaveCount(2);
            video.Genres.Should().Contain(genre1);
            video.Genres.Should().Contain(genre2);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void Video_WithRequiredFields_ShouldBeValid()
        {
            // Arrange & Act
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube"
            };

            // Assert
            video.Title.Should().NotBeNullOrEmpty();
            video.Platform.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3600)]
        [InlineData(7200)]
        public void Video_WithValidLengthInSeconds_ShouldAcceptValue(int lengthInSeconds)
        {
            // Arrange & Act
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                LengthInSeconds = lengthInSeconds
            };

            // Assert
            video.LengthInSeconds.Should().Be(lengthInSeconds);
            video.LengthInSeconds.Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion
    }
}
