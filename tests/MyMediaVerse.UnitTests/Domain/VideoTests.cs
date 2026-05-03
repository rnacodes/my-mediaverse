using Xunit;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using System;
using System.Collections.Generic;

namespace MyMediaVerse.UnitTests.Domain
{
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
            video.VideoType.Should().Be(VideoType.Series); // Default value
            video.LengthInSeconds.Should().Be(0);
            video.Episodes.Should().NotBeNull();
            video.Episodes.Should().BeEmpty();
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
            var parentVideoId = Guid.NewGuid();
            var dateAdded = DateTime.UtcNow;
            var dateCompleted = DateTime.UtcNow.AddDays(-1);

            // Act
            var video = new Video
            {
                Id = videoId,
                Title = "Test Video",
                Platform = "YouTube",
                VideoType = VideoType.Episode,
                ParentVideoId = parentVideoId,
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
            video.VideoType.Should().Be(VideoType.Episode);
            video.ParentVideoId.Should().Be(parentVideoId);
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

            var parentVideo = new Video
            {
                Title = "Parent Series",
                Platform = "YouTube",
                Thumbnail = "https://example.com/parent-thumb.jpg"
            };

            video.ParentVideo = parentVideo;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/video-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasNoThumbnailButParentDoes_ShouldReturnParentThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = null
            };

            var parentVideo = new Video
            {
                Title = "Parent Series",
                Platform = "YouTube",
                Thumbnail = "https://example.com/parent-thumb.jpg"
            };

            video.ParentVideo = parentVideo;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/parent-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasEmptyThumbnailButParentDoes_ShouldReturnParentThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = ""
            };

            var parentVideo = new Video
            {
                Title = "Parent Series",
                Platform = "YouTube",
                Thumbnail = "https://example.com/parent-thumb.jpg"
            };

            video.ParentVideo = parentVideo;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/parent-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenNeitherVideoNorParentHasThumbnail_ShouldReturnNull()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = null
            };

            var parentVideo = new Video
            {
                Title = "Parent Series",
                Platform = "YouTube",
                Thumbnail = null
            };

            video.ParentVideo = parentVideo;

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasNoParent_ShouldReturnVideoThumbnail()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = "https://example.com/video-thumb.jpg",
                ParentVideo = null
            };

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().Be("https://example.com/video-thumb.jpg");
        }

        [Fact]
        public void GetEffectiveThumbnail_WhenVideoHasNoParentAndNoThumbnail_ShouldReturnNull()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                Thumbnail = null,
                ParentVideo = null
            };

            // Act
            var result = video.GetEffectiveThumbnail();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region IsSeries Property Tests

        [Fact]
        public void IsSeries_WhenVideoTypeIsSeries_ShouldReturnTrue()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Series",
                Platform = "YouTube",
                VideoType = VideoType.Series
            };

            // Act & Assert
            video.IsSeries.Should().BeTrue();
        }

        [Fact]
        public void IsSeries_WhenVideoTypeIsEpisode_ShouldReturnFalse()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Episode",
                Platform = "YouTube",
                VideoType = VideoType.Episode
            };

            // Act & Assert
            video.IsSeries.Should().BeFalse();
        }

        [Fact]
        public void IsSeries_WhenVideoTypeIsChannel_ShouldReturnFalse()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Channel",
                Platform = "YouTube",
                VideoType = VideoType.Channel
            };

            // Act & Assert
            video.IsSeries.Should().BeFalse();
        }

        #endregion

        #region IsEpisode Property Tests

        [Fact]
        public void IsEpisode_WhenVideoTypeIsEpisode_ShouldReturnTrue()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Episode",
                Platform = "YouTube",
                VideoType = VideoType.Episode
            };

            // Act & Assert
            video.IsEpisode.Should().BeTrue();
        }

        [Fact]
        public void IsEpisode_WhenVideoTypeIsSeries_ShouldReturnFalse()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Series",
                Platform = "YouTube",
                VideoType = VideoType.Series
            };

            // Act & Assert
            video.IsEpisode.Should().BeFalse();
        }

        [Fact]
        public void IsEpisode_WhenVideoTypeIsChannel_ShouldReturnFalse()
        {
            // Arrange
            var video = new Video
            {
                Title = "Test Channel",
                Platform = "YouTube",
                VideoType = VideoType.Channel
            };

            // Act & Assert
            video.IsEpisode.Should().BeFalse();
        }

        #endregion

        #region Navigation Properties Tests

        [Fact]
        public void Video_ShouldSupportParentChildRelationship()
        {
            // Arrange
            var parentSeries = new Video
            {
                Id = Guid.NewGuid(),
                Title = "Parent Series",
                Platform = "YouTube",
                VideoType = VideoType.Series
            };

            var episode1 = new Video
            {
                Id = Guid.NewGuid(),
                Title = "Episode 1",
                Platform = "YouTube",
                VideoType = VideoType.Episode,
                ParentVideoId = parentSeries.Id,
                ParentVideo = parentSeries
            };

            var episode2 = new Video
            {
                Id = Guid.NewGuid(),
                Title = "Episode 2",
                Platform = "YouTube",
                VideoType = VideoType.Episode,
                ParentVideoId = parentSeries.Id,
                ParentVideo = parentSeries
            };

            // Act
            parentSeries.Episodes.Add(episode1);
            parentSeries.Episodes.Add(episode2);

            // Assert
            parentSeries.Episodes.Should().HaveCount(2);
            parentSeries.Episodes.Should().Contain(episode1);
            parentSeries.Episodes.Should().Contain(episode2);
            
            episode1.ParentVideo.Should().Be(parentSeries);
            episode1.ParentVideoId.Should().Be(parentSeries.Id);
            
            episode2.ParentVideo.Should().Be(parentSeries);
            episode2.ParentVideoId.Should().Be(parentSeries.Id);
        }

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

        #region VideoType Enum Tests

        [Fact]
        public void VideoType_ShouldHaveExpectedValues()
        {
            // Assert
            Enum.GetValues<VideoType>().Should().Contain(VideoType.Series);
            Enum.GetValues<VideoType>().Should().Contain(VideoType.Episode);
            Enum.GetValues<VideoType>().Should().Contain(VideoType.Channel);
        }

        [Theory]
        [InlineData(VideoType.Series)]
        [InlineData(VideoType.Episode)]
        [InlineData(VideoType.Channel)]
        public void VideoType_ShouldBeAssignableToVideo(VideoType videoType)
        {
            // Arrange & Act
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                VideoType = videoType
            };

            // Assert
            video.VideoType.Should().Be(videoType);
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
                Platform = "YouTube",
                VideoType = VideoType.Series
            };

            // Assert
            video.Title.Should().NotBeNullOrEmpty();
            video.Platform.Should().NotBeNullOrEmpty();
            video.VideoType.Should().BeDefined();
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
            video.LengthInSeconds.Should().BeGreaterOrEqualTo(0);
        }

        #endregion
    }
}
