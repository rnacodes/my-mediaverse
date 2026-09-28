using AwesomeAssertions;
using MyMediaVerse.Application.Helpers;

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
    }
}
