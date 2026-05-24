using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class YouTubePlaylistTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var playlist = new YouTubePlaylist { Title = "", PlaylistExternalId = "" };

            // Assert
            playlist.Id.Should().NotBeEmpty();
            playlist.Title.Should().Be("");
            playlist.PlaylistExternalId.Should().Be("");
            playlist.ChannelExternalId.Should().BeNull();
            playlist.LinkedYouTubeChannelId.Should().BeNull();
            playlist.VideoCount.Should().BeNull();
            playlist.PublishedAt.Should().BeNull();
            playlist.LastSyncedAt.Should().BeNull();
            playlist.PrivacyStatus.Should().BeNull();
            playlist.LinkedYouTubeChannel.Should().BeNull();
            playlist.PlaylistVideos.Should().NotBeNull().And.BeEmpty();
            playlist.Topics.Should().NotBeNull().And.BeEmpty();
            playlist.Genres.Should().NotBeNull().And.BeEmpty();
            playlist.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var playlist = new YouTubePlaylist { Title = "", PlaylistExternalId = "" };
            var testDate = DateTime.UtcNow;
            var channelId = Guid.NewGuid();

            // Act
            playlist.Title = "Linear Algebra Playlist";
            playlist.PlaylistExternalId = "PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab";
            playlist.ChannelExternalId = "UCYO_jab_esuFRV4b17AJtAw";
            playlist.LinkedYouTubeChannelId = channelId;
            playlist.VideoCount = 16;
            playlist.PublishedAt = testDate.AddYears(-5);
            playlist.LastSyncedAt = testDate;
            playlist.PrivacyStatus = "public";
            playlist.Description = "Essence of Linear Algebra series";

            // Assert
            playlist.Title.Should().Be("Linear Algebra Playlist");
            playlist.PlaylistExternalId.Should().Be("PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab");
            playlist.ChannelExternalId.Should().Be("UCYO_jab_esuFRV4b17AJtAw");
            playlist.LinkedYouTubeChannelId.Should().Be(channelId);
            playlist.VideoCount.Should().Be(16);
            playlist.PublishedAt.Should().Be(testDate.AddYears(-5));
            playlist.LastSyncedAt.Should().Be(testDate);
            playlist.PrivacyStatus.Should().Be("public");
            playlist.Description.Should().Be("Essence of Linear Algebra series");
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void LinkedYouTubeChannel_CanBeSetAndRetrieved()
        {
            // Arrange
            var channel = new YouTubeChannel { Title = "3Blue1Brown", ChannelExternalId = "UC_test" };
            var playlist = new YouTubePlaylist { Title = "Test", PlaylistExternalId = "PL_test" };

            // Act
            playlist.LinkedYouTubeChannel = channel;
            playlist.LinkedYouTubeChannelId = channel.Id;

            // Assert
            playlist.LinkedYouTubeChannel.Should().NotBeNull();
            playlist.LinkedYouTubeChannel!.Title.Should().Be("3Blue1Brown");
        }

        [Fact]
        public void PlaylistVideos_CanBeAddedAndRetrieved()
        {
            // Arrange
            var playlist = new YouTubePlaylist { Title = "Test", PlaylistExternalId = "PL_test" };
            var video = new Video { Title = "Video 1", Platform = "YouTube", VideoType = VideoType.Episode };
            var playlistVideo = new YouTubePlaylistVideo
            {
                YouTubePlaylistId = playlist.Id,
                VideoId = video.Id,
                Position = 0
            };

            // Act
            playlist.PlaylistVideos.Add(playlistVideo);

            // Assert
            playlist.PlaylistVideos.Should().ContainSingle().Which.Position.Should().Be(0);
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var playlist = new YouTubePlaylist { Title = "Test", PlaylistExternalId = "PL_test" };

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(playlist);
        }

        #endregion
    }
}
