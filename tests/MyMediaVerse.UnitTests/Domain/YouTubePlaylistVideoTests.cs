using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class YouTubePlaylistVideoTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var playlistVideo = new YouTubePlaylistVideo();

            // Assert
            playlistVideo.YouTubePlaylistId.Should().Be(Guid.Empty);
            playlistVideo.VideoId.Should().Be(Guid.Empty);
            playlistVideo.Position.Should().BeNull();
            playlistVideo.AddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            playlistVideo.VideoPublishedAt.Should().BeNull();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var playlistId = Guid.NewGuid();
            var videoId = Guid.NewGuid();
            var testDate = DateTime.UtcNow;

            // Act
            var playlistVideo = new YouTubePlaylistVideo
            {
                YouTubePlaylistId = playlistId,
                VideoId = videoId,
                Position = 5,
                AddedAt = testDate,
                VideoPublishedAt = testDate.AddDays(-30)
            };

            // Assert
            playlistVideo.YouTubePlaylistId.Should().Be(playlistId);
            playlistVideo.VideoId.Should().Be(videoId);
            playlistVideo.Position.Should().Be(5);
            playlistVideo.AddedAt.Should().Be(testDate);
            playlistVideo.VideoPublishedAt.Should().Be(testDate.AddDays(-30));
        }

        [Fact]
        public void Position_CanBeZeroIndexed()
        {
            // Arrange & Act
            var playlistVideo = new YouTubePlaylistVideo
            {
                YouTubePlaylistId = Guid.NewGuid(),
                VideoId = Guid.NewGuid(),
                Position = 0
            };

            // Assert
            playlistVideo.Position.Should().Be(0);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_CanLinkPlaylistAndVideo()
        {
            // Arrange
            var playlist = new YouTubePlaylist { Title = "Math Series", PlaylistExternalId = "PL_test" };
            var video = new Video { Title = "Linear Algebra", Platform = "YouTube" };

            // Act
            var playlistVideo = new YouTubePlaylistVideo
            {
                YouTubePlaylistId = playlist.Id,
                YouTubePlaylist = playlist,
                VideoId = video.Id,
                Video = video,
                Position = 0
            };

            // Assert
            playlistVideo.YouTubePlaylist.Title.Should().Be("Math Series");
            playlistVideo.Video.Title.Should().Be("Linear Algebra");
        }

        #endregion
    }
}
