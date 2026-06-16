using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class YouTubeChannelTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var channel = new YouTubeChannel { Title = "", ChannelExternalId = "" };

            // Assert
            channel.Id.Should().NotBeEmpty();
            channel.Title.Should().Be("");
            channel.ChannelExternalId.Should().Be("");
            channel.CustomUrl.Should().BeNull();
            channel.SubscriberCount.Should().BeNull();
            channel.VideoCount.Should().BeNull();
            channel.ViewCount.Should().BeNull();
            channel.UploadsPlaylistId.Should().BeNull();
            channel.Country.Should().BeNull();
            channel.PublishedAt.Should().BeNull();
            channel.LastSyncedAt.Should().BeNull();
            channel.Videos.Should().NotBeNull().And.BeEmpty();
            channel.Topics.Should().NotBeNull().And.BeEmpty();
            channel.Genres.Should().NotBeNull().And.BeEmpty();
            channel.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var channel = new YouTubeChannel { Title = "", ChannelExternalId = "" };
            var testDate = DateTime.UtcNow;

            // Act
            channel.Title = "3Blue1Brown";
            channel.ChannelExternalId = "UCYO_jab_esuFRV4b17AJtAw";
            channel.CustomUrl = "@3blue1brown";
            channel.Description = "Math visualizations and explanations";
            channel.SubscriberCount = 6000000;
            channel.VideoCount = 150;
            channel.ViewCount = 500000000;
            channel.UploadsPlaylistId = "UU_abc123";
            channel.Country = "US";
            channel.PublishedAt = testDate.AddYears(-10);
            channel.LastSyncedAt = testDate;
            channel.Thumbnail = "https://yt3.googleusercontent.com/channel.jpg";

            // Assert
            channel.Title.Should().Be("3Blue1Brown");
            channel.ChannelExternalId.Should().Be("UCYO_jab_esuFRV4b17AJtAw");
            channel.CustomUrl.Should().Be("@3blue1brown");
            channel.Description.Should().Be("Math visualizations and explanations");
            channel.SubscriberCount.Should().Be(6000000);
            channel.VideoCount.Should().Be(150);
            channel.ViewCount.Should().Be(500000000);
            channel.UploadsPlaylistId.Should().Be("UU_abc123");
            channel.Country.Should().Be("US");
            channel.PublishedAt.Should().Be(testDate.AddYears(-10));
            channel.LastSyncedAt.Should().Be(testDate);
            channel.Thumbnail.Should().Be("https://yt3.googleusercontent.com/channel.jpg");
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_VideosCanBeAddedAndRetrieved()
        {
            // Arrange
            var channel = new YouTubeChannel { Title = "Test Channel", ChannelExternalId = "UC_test" };
            var video = new Video
            {
                Title = "Test Video",
                Platform = "YouTube",
                ChannelId = channel.Id
            };

            // Act
            channel.Videos.Add(video);

            // Assert
            channel.Videos.Should().ContainSingle().Which.Title.Should().Be("Test Video");
        }

        [Fact]
        public void NavigationProperties_TopicsCanBeAddedAndRetrieved()
        {
            // Arrange
            var channel = new YouTubeChannel { Title = "Test Channel", ChannelExternalId = "UC_test" };
            var topic = new Topic { Name = "mathematics" };

            // Act
            channel.Topics.Add(topic);

            // Assert
            channel.Topics.Should().ContainSingle().Which.Name.Should().Be("mathematics");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var channel = new YouTubeChannel { Title = "Test", ChannelExternalId = "UC_test" };

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(channel);
        }

        #endregion
    }
}
