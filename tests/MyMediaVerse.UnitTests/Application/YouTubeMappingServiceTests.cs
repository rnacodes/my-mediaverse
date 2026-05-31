using AwesomeAssertions;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class YouTubeMappingServiceTests
    {
        private readonly YouTubeMappingService _service;

        public YouTubeMappingServiceTests()
        {
            _service = new YouTubeMappingService();
        }

        #region MapVideoToEntity

        [Fact]
        public void MapVideoToEntity_ValidDto_MapsAllProperties()
        {
            var videoDto = CreateTestVideoDto("vid123", "Test Video", "A test description");

            var result = _service.MapVideoToEntity(videoDto);

            result.Should().NotBeNull();
            result.Title.Should().Be("Test Video");
            result.Description.Should().Be("A test description");
            result.Link.Should().Be("https://www.youtube.com/watch?v=vid123");
            result.Platform.Should().Be("YouTube");
            result.ExternalId.Should().Be("vid123");
            result.MediaType.Should().Be(MediaType.Video);
            result.VideoType.Should().Be(VideoType.Episode);
            result.DateAdded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void MapVideoToEntity_WithThumbnails_UsesBestQuality()
        {
            var videoDto = CreateTestVideoDto("vid123", "Test");
            videoDto.Snippet.Thumbnails = new YouTubeThumbnailsDto
            {
                Default = new YouTubeThumbnailDto { Url = "http://default.jpg" },
                Medium = new YouTubeThumbnailDto { Url = "http://medium.jpg" },
                High = new YouTubeThumbnailDto { Url = "http://high.jpg" },
                Standard = new YouTubeThumbnailDto { Url = "http://standard.jpg" },
                Maxres = new YouTubeThumbnailDto { Url = "http://maxres.jpg" }
            };

            var result = _service.MapVideoToEntity(videoDto);

            result.Thumbnail.Should().Be("http://maxres.jpg");
        }

        [Fact]
        public void MapVideoToEntity_WithDuration_ParsesSeconds()
        {
            var videoDto = CreateTestVideoDto("vid123", "Test");
            videoDto.ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT4M13S" };

            var result = _service.MapVideoToEntity(videoDto);

            result.LengthInSeconds.Should().Be(253); // 4*60 + 13
        }

        [Fact]
        public void MapVideoToEntity_NullTitle_DefaultsToUnknownTitle()
        {
            var videoDto = CreateTestVideoDto("vid123", null);

            var result = _service.MapVideoToEntity(videoDto);

            result.Title.Should().Be("Unknown Title");
        }

        [Fact]
        public void MapVideoToEntity_NullDto_ThrowsArgumentException()
        {
            Action act = () => _service.MapVideoToEntity(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MapVideoToEntity_NullSnippet_ThrowsArgumentException()
        {
            var videoDto = new YouTubeVideoDto { Id = "vid123", Snippet = null };

            Action act = () => _service.MapVideoToEntity(videoDto);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region MapPlaylistToEntity

        [Fact]
        public void MapPlaylistToEntity_ValidDto_MapsCorrectly()
        {
            var playlistDto = CreateTestPlaylistDto("PL123", "My Playlist", "Playlist description");

            var result = _service.MapPlaylistToEntity(playlistDto);

            result.Title.Should().Be("My Playlist");
            result.Description.Should().Be("Playlist description");
            result.Link.Should().Be("https://www.youtube.com/playlist?list=PL123");
            result.ExternalId.Should().Be("PL123");
            result.VideoType.Should().Be(VideoType.Series);
            result.MediaType.Should().Be(MediaType.Video);
        }

        [Fact]
        public void MapPlaylistToEntity_NullDto_ThrowsArgumentException()
        {
            Action act = () => _service.MapPlaylistToEntity(null!);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region MapChannelToEntity

        [Fact]
        public void MapChannelToEntity_ValidDto_MapsCorrectly()
        {
            var channelDto = CreateTestChannelDto("UC123", "Test Channel");

            var result = _service.MapChannelToEntity(channelDto);

            result.Title.Should().Be("Test Channel");
            result.Link.Should().Be("https://www.youtube.com/channel/UC123");
            result.ExternalId.Should().Be("UC123");
            result.VideoType.Should().Be(VideoType.Channel);
        }

        [Fact]
        public void MapChannelToEntity_NullDto_ThrowsArgumentException()
        {
            Action act = () => _service.MapChannelToEntity(null!);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region MapChannelToYouTubeChannelEntity

        [Fact]
        public void MapChannelToYouTubeChannelEntity_ValidDto_MapsAllProperties()
        {
            var channelDto = CreateTestChannelDto("UC123", "My Channel");
            channelDto.Snippet.CustomUrl = "@mychannel";
            channelDto.Snippet.Country = "US";
            channelDto.Snippet.PublishedAt = new DateTime(2020, 1, 1);
            channelDto.Statistics = new YouTubeChannelStatisticsDto
            {
                SubscriberCount = "1000",
                VideoCount = "50",
                ViewCount = "100000"
            };
            channelDto.ContentDetails = new YouTubeChannelContentDetailsDto
            {
                RelatedPlaylists = new YouTubeRelatedPlaylistsDto
                {
                    Uploads = "UU123"
                }
            };

            var result = _service.MapChannelToYouTubeChannelEntity(channelDto);

            result.Title.Should().Be("My Channel");
            result.ChannelExternalId.Should().Be("UC123");
            result.CustomUrl.Should().Be("@mychannel");
            result.MediaType.Should().Be(MediaType.Channel);
            result.Country.Should().Be("US");
            result.SubscriberCount.Should().Be(1000);
            result.VideoCount.Should().Be(50);
            result.ViewCount.Should().Be(100000);
            result.UploadsPlaylistId.Should().Be("UU123");
            result.Link.Should().Be("https://www.youtube.com/channel/UC123");
            result.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void MapChannelToYouTubeChannelEntity_NullStatistics_SkipsStatistics()
        {
            var channelDto = CreateTestChannelDto("UC123", "Test");
            channelDto.Statistics = null;

            var result = _service.MapChannelToYouTubeChannelEntity(channelDto);

            result.SubscriberCount.Should().BeNull();
            result.VideoCount.Should().BeNull();
            result.ViewCount.Should().BeNull();
        }

        [Fact]
        public void MapChannelToYouTubeChannelEntity_NullChannelId_ThrowsArgumentException()
        {
            var channelDto = CreateTestChannelDto(null!, "Test");

            Action act = () => _service.MapChannelToYouTubeChannelEntity(channelDto);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region MapPlaylistToYouTubePlaylistEntity

        [Fact]
        public void MapPlaylistToYouTubePlaylistEntity_ValidDto_MapsAllProperties()
        {
            var playlistDto = CreateTestPlaylistDto("PL123", "My Playlist");
            playlistDto.Snippet.ChannelId = "UC456";
            playlistDto.Snippet.PublishedAt = new DateTime(2021, 6, 15);
            playlistDto.ContentDetails = new YouTubePlaylistContentDetailsDto { ItemCount = 25 };
            playlistDto.Status = new YouTubePlaylistStatusDto { PrivacyStatus = "public" };

            var result = _service.MapPlaylistToYouTubePlaylistEntity(playlistDto);

            result.Title.Should().Be("My Playlist");
            result.PlaylistExternalId.Should().Be("PL123");
            result.ChannelExternalId.Should().Be("UC456");
            result.MediaType.Should().Be(MediaType.Playlist);
            result.VideoCount.Should().Be(25);
            result.PrivacyStatus.Should().Be("public");
            result.Link.Should().Be("https://www.youtube.com/playlist?list=PL123");
        }

        [Fact]
        public void MapPlaylistToYouTubePlaylistEntity_NullPlaylistId_ThrowsArgumentException()
        {
            var playlistDto = CreateTestPlaylistDto(null!, "Test");

            Action act = () => _service.MapPlaylistToYouTubePlaylistEntity(playlistDto);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region MapPlaylistItemToVideoEntity

        [Fact]
        public void MapPlaylistItemToVideoEntity_ValidDto_MapsCorrectly()
        {
            var playlistItemDto = new YouTubePlaylistItemDto
            {
                Id = "item1",
                Snippet = new YouTubePlaylistItemSnippetDto
                {
                    Title = "Video in Playlist",
                    Description = "Description",
                    ResourceId = new YouTubeResourceIdDto { VideoId = "vid789" }
                }
            };

            var result = _service.MapPlaylistItemToVideoEntity(playlistItemDto);

            result.Title.Should().Be("Video in Playlist");
            result.ExternalId.Should().Be("vid789");
            result.Link.Should().Be("https://www.youtube.com/watch?v=vid789");
            result.VideoType.Should().Be(VideoType.Episode);
        }

        [Fact]
        public void MapPlaylistItemToVideoEntity_WithVideoDetails_EnhancesEntity()
        {
            var playlistItemDto = new YouTubePlaylistItemDto
            {
                Id = "item1",
                Snippet = new YouTubePlaylistItemSnippetDto
                {
                    Title = "Video",
                    ResourceId = new YouTubeResourceIdDto { VideoId = "vid789" }
                }
            };

            var videoDetails = CreateTestVideoDto("vid789", "Detailed Video", "Better description");
            videoDetails.ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT10M" };

            var result = _service.MapPlaylistItemToVideoEntity(playlistItemDto, videoDetails);

            result.Description.Should().Be("Better description");
            result.LengthInSeconds.Should().Be(600); // 10 minutes
        }

        [Fact]
        public void MapPlaylistItemToVideoEntity_NullDto_ThrowsArgumentException()
        {
            Action act = () => _service.MapPlaylistItemToVideoEntity(null!);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region MapPlaylistItemsToVideoEntities

        [Fact]
        public void MapPlaylistItemsToVideoEntities_MultipleItems_MapsAll()
        {
            var items = new List<YouTubePlaylistItemDto>
            {
                new YouTubePlaylistItemDto
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        Title = "Video 1",
                        ResourceId = new YouTubeResourceIdDto { VideoId = "v1" }
                    }
                },
                new YouTubePlaylistItemDto
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        Title = "Video 2",
                        ResourceId = new YouTubeResourceIdDto { VideoId = "v2" }
                    }
                }
            };

            var result = _service.MapPlaylistItemsToVideoEntities(items);

            result.Should().HaveCount(2);
            result[0].Title.Should().Be("Video 1");
            result[1].Title.Should().Be("Video 2");
        }

        [Fact]
        public void MapPlaylistItemsToVideoEntities_ItemWithNullSnippet_SkipsIt()
        {
            var items = new List<YouTubePlaylistItemDto>
            {
                new YouTubePlaylistItemDto
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        Title = "Good Video",
                        ResourceId = new YouTubeResourceIdDto { VideoId = "v1" }
                    }
                },
                new YouTubePlaylistItemDto { Snippet = null } // Bad item
            };

            var result = _service.MapPlaylistItemsToVideoEntities(items);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Good Video");
        }

        [Fact]
        public void MapPlaylistItemsToVideoEntities_WithVideoDetails_MatchesById()
        {
            var items = new List<YouTubePlaylistItemDto>
            {
                new YouTubePlaylistItemDto
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        Title = "Video 1",
                        ResourceId = new YouTubeResourceIdDto { VideoId = "v1" }
                    }
                }
            };

            var details = new List<YouTubeVideoDto>
            {
                CreateTestVideoDto("v1", "Detailed Video 1"),
            };
            details[0].ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT5M" };

            var result = _service.MapPlaylistItemsToVideoEntities(items, details);

            result.Should().HaveCount(1);
            result[0].LengthInSeconds.Should().Be(300);
        }

        #endregion

        #region Helper Methods

        private static YouTubeVideoDto CreateTestVideoDto(string id, string? title, string? description = null)
        {
            return new YouTubeVideoDto
            {
                Id = id,
                Snippet = new YouTubeVideoSnippetDto
                {
                    Title = title,
                    Description = description
                }
            };
        }

        private static YouTubePlaylistDto CreateTestPlaylistDto(string id, string title, string? description = null)
        {
            return new YouTubePlaylistDto
            {
                Id = id,
                Snippet = new YouTubePlaylistSnippetDto
                {
                    Title = title,
                    Description = description
                }
            };
        }

        private static YouTubeChannelDto CreateTestChannelDto(string id, string title)
        {
            return new YouTubeChannelDto
            {
                Id = id,
                Snippet = new YouTubeChannelSnippetDto
                {
                    Title = title
                }
            };
        }

        #endregion
    }
}
