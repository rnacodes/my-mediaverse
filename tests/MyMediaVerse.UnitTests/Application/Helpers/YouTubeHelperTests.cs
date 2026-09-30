using AwesomeAssertions;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.UnitTests.Application.Helpers
{
    [Trait("Category", "Unit")]
    public class YouTubeHelperTests
    {
        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&t=42s")]
        [InlineData("https://www.youtube.com/watch?list=PL123&v=dQw4w9WgXcQ")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
        [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
        [InlineData("https://youtube.com/shorts/dQw4w9WgXcQ?feature=share")]
        [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
        [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("dQw4w9WgXcQ")]
        public void ExtractVideoIdFromUrl_ReturnsTheId_ForEveryUrlShape(string url)
        {
            YouTubeHelper.ExtractVideoIdFromUrl(url).Should().Be("dQw4w9WgXcQ");
        }

        [Theory]
        [InlineData("")]
        [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")]
        [InlineData("https://www.youtube.com/channel/UC_x5XG1OV2P6uZZ5FSM9Ttw")]
        [InlineData("https://www.youtube.com/@TheTaleFoundry")]
        [InlineData("https://vimeo.com/123456789")]
        [InlineData("too-short")]
        public void ExtractVideoIdFromUrl_ReturnsNull_WhenTheUrlHoldsNoVideoId(string url)
        {
            YouTubeHelper.ExtractVideoIdFromUrl(url).Should().BeNull();
        }

        [Theory]
        [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf", "PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PL123_abc-XYZ", "PL123_abc-XYZ")]
        public void ExtractPlaylistIdFromUrl_ReturnsTheListId(string url, string expected)
        {
            YouTubeHelper.ExtractPlaylistIdFromUrl(url).Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        public void ExtractPlaylistIdFromUrl_ReturnsNull_WhenThereIsNoList(string url)
        {
            YouTubeHelper.ExtractPlaylistIdFromUrl(url).Should().BeNull();
        }

        [Theory]
        [InlineData("https://www.youtube.com/channel/UC_x5XG1OV2P6uZZ5FSM9Ttw", "UC_x5XG1OV2P6uZZ5FSM9Ttw")]
        [InlineData("https://www.youtube.com/c/GoogleDevelopers", "GoogleDevelopers")]
        [InlineData("https://www.youtube.com/user/GoogleDevelopers", "GoogleDevelopers")]
        [InlineData("https://www.youtube.com/@TheTaleFoundry", "TheTaleFoundry")]
        public void ExtractChannelIdFromUrl_ReturnsTheIdentifier_ForEveryUrlShape(string url, string expected)
        {
            YouTubeHelper.ExtractChannelIdFromUrl(url).Should().Be(expected);
        }

        [Theory]
        [InlineData("PT4M13S", 253)]
        [InlineData("PT1H2M3S", 3723)]
        [InlineData("PT45S", 45)]
        [InlineData("P0D", 0)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("not-a-duration", 0)]
        public void ParseDurationToSeconds_ReadsIso8601_AndReturnsZeroWhenItCannot(string? duration, int expected)
        {
            YouTubeHelper.ParseDurationToSeconds(duration).Should().Be(expected);
        }

        [Theory]
        [InlineData("dQw4w9WgXcQ", true)]
        [InlineData("a-b_c-d_e-f", true)]
        [InlineData("short", false)]
        [InlineData("twelve_chars", false)]
        [InlineData("has.a.dot.x", false)]
        [InlineData("has a space", false)]
        [InlineData("https://youtu.be/dQw4w9WgXcQ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidVideoId_AcceptsOnlyTheShapeOfAVideoId(string? videoId, bool expected)
        {
            YouTubeHelper.IsValidVideoId(videoId).Should().Be(expected);
        }

        [Fact]
        public void GetBestThumbnailUrl_PrefersTheLargestSize_AndFallsBackInOrder()
        {
            var thumbnails = new YouTubeThumbnailsDto
            {
                Default = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/default.jpg" },
                Medium = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/medium.jpg" },
                High = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/high.jpg" },
                Standard = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/standard.jpg" },
                Maxres = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/maxres.jpg" }
            };

            YouTubeHelper.GetBestThumbnailUrl(thumbnails).Should().Be("https://i.ytimg.com/maxres.jpg");

            thumbnails.Maxres = null;
            YouTubeHelper.GetBestThumbnailUrl(thumbnails).Should().Be("https://i.ytimg.com/standard.jpg");

            thumbnails.Standard = null;
            YouTubeHelper.GetBestThumbnailUrl(thumbnails).Should().Be("https://i.ytimg.com/high.jpg");

            thumbnails.High = null;
            YouTubeHelper.GetBestThumbnailUrl(thumbnails).Should().Be("https://i.ytimg.com/medium.jpg");

            thumbnails.Medium = null;
            YouTubeHelper.GetBestThumbnailUrl(thumbnails).Should().Be("https://i.ytimg.com/default.jpg");
        }

        [Fact]
        public void GetBestThumbnailUrl_ReturnsNull_WhenYouTubeOffersNone()
        {
            YouTubeHelper.GetBestThumbnailUrl(null).Should().BeNull();
            YouTubeHelper.GetBestThumbnailUrl(new YouTubeThumbnailsDto()).Should().BeNull();
        }

        [Theory]
        [InlineData("https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg", true)]
        [InlineData("https://I.YTIMG.COM/vi/dQw4w9WgXcQ/hqdefault.jpg", true)]
        [InlineData("https://yt3.ggpht.com/avatar", true)]
        [InlineData("https://yt3.googleusercontent.com/avatar", true)]
        [InlineData("https://cdn.example.com/my-image.jpg", false)]
        [InlineData("https://notytimg.com/image.jpg", false)]
        [InlineData("https://example.com/ytimg.com/image.jpg", false)]
        [InlineData("/uploads/my-image.jpg", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsYouTubeImageUrl_RecognizesYouTubeImageHostsOnly(string? url, bool expected)
        {
            YouTubeHelper.IsYouTubeImageUrl(url).Should().Be(expected);
        }
    }
}
