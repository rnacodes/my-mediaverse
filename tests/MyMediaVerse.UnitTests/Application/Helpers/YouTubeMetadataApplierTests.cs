using AwesomeAssertions;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Application.Helpers
{
    /// <summary>
    /// What a fresh YouTube payload may change on a stored video, channel or playlist: which
    /// values it overwrites, which it only fills, and which it never touches.
    /// </summary>
    [Trait("Category", "Unit")]
    public class YouTubeMetadataApplierTests
    {
        private static YouTubeThumbnailsDto Thumbnails(string url) => new()
        {
            High = new YouTubeThumbnailDto { Url = url }
        };

        #region Video

        [Fact]
        public void Apply_Video_OverwritesWhatYouTubeOwns()
        {
            var video = TestDataFactory.CreateVideo("Old title");
            video.Description = "Old description";
            video.LengthInSeconds = 100;
            video.PublishedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            video.Thumbnail = "https://i.ytimg.com/vi/abc/old.jpg";
            var published = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc);

            var changed = YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto
                {
                    Title = "New title",
                    Description = "New description",
                    PublishedAt = published,
                    Thumbnails = Thumbnails("https://i.ytimg.com/vi/abc/new.jpg")
                },
                ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT4M13S" }
            });

            changed.Should().BeTrue();
            video.Title.Should().Be("New title");
            video.Description.Should().Be("New description");
            video.LengthInSeconds.Should().Be(253);
            video.PublishedAt.Should().Be(published);
            video.Thumbnail.Should().Be("https://i.ytimg.com/vi/abc/new.jpg");
        }

        [Fact]
        public void Apply_Video_AnEmptyIncomingValueNeverOverwrites()
        {
            var published = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var video = TestDataFactory.CreateVideo("Stored title");
            video.Description = "Stored description";
            video.LengthInSeconds = 100;
            video.PublishedAt = published;
            video.Thumbnail = "https://i.ytimg.com/vi/abc/stored.jpg";

            // A live or upcoming video reports a zero duration.
            var changed = YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto { Title = "", Description = null },
                ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "P0D" }
            });

            changed.Should().BeFalse();
            video.Title.Should().Be("Stored title");
            video.Description.Should().Be("Stored description");
            video.LengthInSeconds.Should().Be(100);
            video.PublishedAt.Should().Be(published);
            video.Thumbnail.Should().Be("https://i.ytimg.com/vi/abc/stored.jpg");
        }

        [Fact]
        public void Apply_Video_WithNoSnippetAtAll_ChangesNothing()
        {
            var video = TestDataFactory.CreateVideo("Stored title");

            var changed = YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto { Id = "abc" });

            changed.Should().BeFalse();
            video.Title.Should().Be("Stored title");
        }

        [Fact]
        public void Apply_Video_ReportsNoChange_WhenYouTubeSendsWhatIsStored()
        {
            var published = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc);
            var video = TestDataFactory.CreateVideo("Same title");
            video.Description = "Same description";
            video.LengthInSeconds = 253;
            video.PublishedAt = published;
            video.Thumbnail = "https://i.ytimg.com/vi/abc/same.jpg";

            var changed = YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto
                {
                    Title = "Same title",
                    Description = "Same description",
                    PublishedAt = published,
                    Thumbnails = Thumbnails("https://i.ytimg.com/vi/abc/same.jpg")
                },
                ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT4M13S" }
            });

            changed.Should().BeFalse();
        }

        [Fact]
        public void Apply_Video_StoresThePublishedDateAsUtc()
        {
            var video = TestDataFactory.CreateVideo();

            YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto
                {
                    PublishedAt = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Unspecified)
                }
            });

            video.PublishedAt.Should().Be(new DateTime(2021, 5, 6, 7, 8, 9));
            video.PublishedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void Apply_Video_NeverTouchesPersonalFields()
        {
            var completed = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var video = TestDataFactory.CreateVideo("Stored title");
            video.Status = Status.Completed;
            video.Rating = Rating.SuperLike;
            video.Notes = "Watched twice";
            video.RelatedNotes = "related";
            video.DateCompleted = completed;
            video.Link = "https://www.youtube.com/watch?v=abc";
            video.ExternalId = "abc";
            video.Topics.Add(new Topic { Name = "music" });
            video.Genres.Add(new Genre { Name = "pop" });
            var dateAdded = video.DateAdded;

            YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto
                {
                    Title = "New title",
                    Tags = new List<string> { "tag" },
                    CategoryId = "10"
                }
            });

            video.Status.Should().Be(Status.Completed);
            video.Rating.Should().Be(Rating.SuperLike);
            video.Notes.Should().Be("Watched twice");
            video.RelatedNotes.Should().Be("related");
            video.DateCompleted.Should().Be(completed);
            video.DateAdded.Should().Be(dateAdded);
            video.Link.Should().Be("https://www.youtube.com/watch?v=abc");
            video.ExternalId.Should().Be("abc");
            video.Platform.Should().Be("YouTube");
            video.Topics.Select(t => t.Name).Should().Equal("music");
            video.Genres.Select(g => g.Name).Should().Equal("pop");
        }

        [Fact]
        public void Apply_Video_LinksAStoredChannel_OnlyWhenTheVideoHasNone()
        {
            var storedChannelId = Guid.NewGuid();
            var ownChannelId = Guid.NewGuid();
            var unlinked = TestDataFactory.CreateVideo("Unlinked");
            var linked = TestDataFactory.CreateVideo("Linked");
            linked.ChannelId = ownChannelId;
            var youTube = new YouTubeVideoDto { Id = "abc", Snippet = new YouTubeVideoSnippetDto() };

            var unlinkedChanged = YouTubeMetadataApplier.Apply(unlinked, youTube, storedChannelId);
            var linkedChanged = YouTubeMetadataApplier.Apply(linked, youTube, storedChannelId);

            unlinkedChanged.Should().BeTrue();
            unlinked.ChannelId.Should().Be(storedChannelId);
            linkedChanged.Should().BeFalse();
            linked.ChannelId.Should().Be(ownChannelId);
        }

        #endregion

        #region Thumbnail

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://i.ytimg.com/vi/abc/hqdefault.jpg")]
        [InlineData("https://yt3.ggpht.com/old-avatar")]
        [InlineData("https://yt3.googleusercontent.com/old-avatar")]
        public void Apply_ReplacesAThumbnail_ThatIsEmptyOrAlreadyFromYouTube(string? stored)
        {
            var video = TestDataFactory.CreateVideo();
            video.Thumbnail = stored;

            YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto { Thumbnails = Thumbnails("https://i.ytimg.com/vi/abc/new.jpg") }
            });

            video.Thumbnail.Should().Be("https://i.ytimg.com/vi/abc/new.jpg");
        }

        [Theory]
        [InlineData("https://cdn.example.com/my-image.jpg")]
        [InlineData("https://notytimg.com/vi/abc/hqdefault.jpg")]
        [InlineData("https://example.com/ytimg.com/image.jpg")]
        public void Apply_KeepsAThumbnail_ThatWasChosenByHand(string stored)
        {
            var video = TestDataFactory.CreateVideo();
            video.Thumbnail = stored;

            var changed = YouTubeMetadataApplier.Apply(video, new YouTubeVideoDto
            {
                Id = "abc",
                Snippet = new YouTubeVideoSnippetDto { Thumbnails = Thumbnails("https://i.ytimg.com/vi/abc/new.jpg") }
            });

            changed.Should().BeFalse();
            video.Thumbnail.Should().Be(stored);
        }

        [Fact]
        public void Apply_KeepsTheStoredThumbnail_WhenYouTubeSendsNone()
        {
            var channel = TestDataFactory.CreateYouTubeChannel();
            channel.Thumbnail = "https://yt3.ggpht.com/avatar";

            YouTubeMetadataApplier.Apply(channel, new YouTubeChannelDto
            {
                Id = "UC1",
                Snippet = new YouTubeChannelSnippetDto { Title = "Channel" }
            });

            channel.Thumbnail.Should().Be("https://yt3.ggpht.com/avatar");
        }

        #endregion

        #region Channel

        [Fact]
        public void Apply_Channel_OverwritesWhatYouTubeOwns()
        {
            var channel = TestDataFactory.CreateYouTubeChannel("Old name", "UC1");
            channel.Description = "Old description";
            channel.CustomUrl = "@old";
            channel.Country = "GB";
            channel.SubscriberCount = 10;
            channel.VideoCount = 1;
            channel.ViewCount = 100;
            channel.UploadsPlaylistId = "UUold";
            channel.Thumbnail = "https://yt3.ggpht.com/old";
            var published = new DateTime(2015, 3, 4, 0, 0, 0, DateTimeKind.Utc);

            var changed = YouTubeMetadataApplier.Apply(channel, new YouTubeChannelDto
            {
                Id = "UC1",
                Snippet = new YouTubeChannelSnippetDto
                {
                    Title = "New name",
                    Description = "New description",
                    CustomUrl = "@new",
                    Country = "US",
                    PublishedAt = published,
                    Thumbnails = Thumbnails("https://yt3.ggpht.com/new")
                },
                Statistics = new YouTubeChannelStatisticsDto
                {
                    SubscriberCount = "2000",
                    VideoCount = "50",
                    ViewCount = "123456"
                },
                ContentDetails = new YouTubeChannelContentDetailsDto
                {
                    RelatedPlaylists = new YouTubeRelatedPlaylistsDto { Uploads = "UUnew" }
                }
            });

            changed.Should().BeTrue();
            channel.Title.Should().Be("New name");
            channel.Description.Should().Be("New description");
            channel.CustomUrl.Should().Be("@new");
            channel.Country.Should().Be("US");
            channel.PublishedAt.Should().Be(published);
            channel.SubscriberCount.Should().Be(2000);
            channel.VideoCount.Should().Be(50);
            channel.ViewCount.Should().Be(123456);
            channel.UploadsPlaylistId.Should().Be("UUnew");
            channel.Thumbnail.Should().Be("https://yt3.ggpht.com/new");
            channel.ChannelExternalId.Should().Be("UC1");
        }

        [Fact]
        public void Apply_Channel_KeepsTheStoredCount_WhenYouTubeHidesIt()
        {
            var channel = TestDataFactory.CreateYouTubeChannel("Channel", "UC1");
            channel.SubscriberCount = 10;

            YouTubeMetadataApplier.Apply(channel, new YouTubeChannelDto
            {
                Id = "UC1",
                Snippet = new YouTubeChannelSnippetDto { Title = "Channel" },
                Statistics = new YouTubeChannelStatisticsDto { HiddenSubscriberCount = true, VideoCount = "5" }
            });

            channel.SubscriberCount.Should().Be(10);
            channel.VideoCount.Should().Be(5);
        }

        #endregion

        #region Playlist

        [Fact]
        public void Apply_Playlist_OverwritesWhatYouTubeOwns()
        {
            var playlist = TestDataFactory.CreateYouTubePlaylist("Old name", "PL1");
            playlist.Description = "Old description";
            playlist.VideoCount = 3;
            playlist.PrivacyStatus = "unlisted";
            playlist.Thumbnail = "https://i.ytimg.com/vi/abc/old.jpg";
            var published = new DateTime(2019, 8, 9, 0, 0, 0, DateTimeKind.Utc);

            var changed = YouTubeMetadataApplier.Apply(playlist, new YouTubePlaylistDto
            {
                Id = "PL1",
                Snippet = new YouTubePlaylistSnippetDto
                {
                    Title = "New name",
                    Description = "New description",
                    PublishedAt = published,
                    Thumbnails = Thumbnails("https://i.ytimg.com/vi/abc/new.jpg")
                },
                ContentDetails = new YouTubePlaylistContentDetailsDto { ItemCount = 12 },
                Status = new YouTubePlaylistStatusDto { PrivacyStatus = "public" }
            });

            changed.Should().BeTrue();
            playlist.Title.Should().Be("New name");
            playlist.Description.Should().Be("New description");
            playlist.PublishedAt.Should().Be(published);
            playlist.VideoCount.Should().Be(12);
            playlist.PrivacyStatus.Should().Be("public");
            playlist.Thumbnail.Should().Be("https://i.ytimg.com/vi/abc/new.jpg");
            playlist.PlaylistExternalId.Should().Be("PL1");
        }

        [Fact]
        public void Apply_Playlist_TakesAnEmptyPlaylistAsZeroVideos()
        {
            var playlist = TestDataFactory.CreateYouTubePlaylist("Playlist", "PL1");
            playlist.VideoCount = 3;

            YouTubeMetadataApplier.Apply(playlist, new YouTubePlaylistDto
            {
                Id = "PL1",
                Snippet = new YouTubePlaylistSnippetDto { Title = "Playlist" },
                ContentDetails = new YouTubePlaylistContentDetailsDto { ItemCount = 0 }
            });

            playlist.VideoCount.Should().Be(0);
        }

        #endregion
    }
}
