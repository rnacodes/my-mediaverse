using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastFeedMapperTests
    {
        private static PodcastFeed Feed(FeedSeries? channel = null, int totalItems = 42) => new()
        {
            Series = channel ?? new FeedSeries
            {
                Title = "Feed Title",
                Description = "Feed description",
                Publisher = "Feed Publisher",
                ImageUrl = "https://feed.example.com/art.jpg",
                Link = "https://feed.example.com/",
                Language = "en-us",
                PodcastGuid = "guid-1",
                Categories = new[] { "Technology", " News ", "technology" }
            },
            TotalItemCount = totalItems
        };

        private static DirectoryPodcast AppleHit() => new()
        {
            Title = "Apple Title",
            Publisher = "Apple Publisher",
            ArtworkUrl = "https://apple.example.com/600.jpg",
            ApplePodcastsId = "123",
            Genres = new[] { "Tech News", "News" },
            EpisodeCount = 99,
            Source = DirectoryPodcast.AppleSource
        };

        private static PodcastSeries NewSeries(string title = "Podcast at feed.example.com") =>
            new() { Title = title, MediaType = MediaType.Podcast };

        [Fact]
        public void ApplyToSeries_Overwrite_TakesFeedValues()
        {
            var series = NewSeries("Old Title");
            series.Description = "Old description";
            series.Publisher = "Old Publisher";

            var genres = PodcastFeedMapper.ApplyToSeries(series, Feed(), directoryHit: null, fillOnly: false);

            series.Title.Should().Be("Feed Title");
            series.Description.Should().Be("Feed description");
            series.Publisher.Should().Be("Feed Publisher");
            series.Thumbnail.Should().Be("https://feed.example.com/art.jpg");
            series.Link.Should().Be("https://feed.example.com/");
            series.Language.Should().Be("en-us");
            series.FeedGuid.Should().Be("guid-1");
            series.TotalEpisodes.Should().Be(42);
            genres.Should().Equal("technology", "news");
        }

        [Fact]
        public void ApplyToSeries_FillOnly_KeepsExistingValues()
        {
            var series = NewSeries("My Own Title");
            series.Description = "Mine";
            series.Thumbnail = "https://mine.example.com/art.jpg";

            PodcastFeedMapper.ApplyToSeries(series, Feed(), directoryHit: null, fillOnly: true);

            series.Title.Should().Be("My Own Title");
            series.Description.Should().Be("Mine");
            series.Thumbnail.Should().Be("https://mine.example.com/art.jpg");
            series.Publisher.Should().Be("Feed Publisher");
            series.Language.Should().Be("en-us");
        }

        [Fact]
        public void ApplyToSeries_FillOnly_ReplacesAPlaceholderTitle()
        {
            var series = NewSeries(PodcastFeedMapper.PlaceholderTitle("https://feed.example.com/rss"));

            PodcastFeedMapper.ApplyToSeries(series, Feed(), directoryHit: null, fillOnly: true);

            series.Title.Should().Be("Feed Title");
        }

        [Fact]
        public void ApplyToSeries_NeverReplacesAnExistingFeedGuid()
        {
            var series = NewSeries();
            series.FeedGuid = "original-guid";

            PodcastFeedMapper.ApplyToSeries(series, Feed(), directoryHit: null, fillOnly: false);

            series.FeedGuid.Should().Be("original-guid");
        }

        [Fact]
        public void ApplyToSeries_FeedArtworkWins_AppleFillsGaps()
        {
            var withFeedArt = NewSeries();
            PodcastFeedMapper.ApplyToSeries(withFeedArt, Feed(), AppleHit(), fillOnly: false);
            withFeedArt.Thumbnail.Should().Be("https://feed.example.com/art.jpg");
            withFeedArt.Publisher.Should().Be("Feed Publisher");

            var bareChannel = new FeedSeries { Title = null, Categories = new[] { "News" } };
            var withoutFeedArt = NewSeries();
            var genres = PodcastFeedMapper.ApplyToSeries(withoutFeedArt, Feed(bareChannel, totalItems: 0), AppleHit(), fillOnly: false);

            withoutFeedArt.Thumbnail.Should().Be("https://apple.example.com/600.jpg");
            withoutFeedArt.Publisher.Should().Be("Apple Publisher");
            withoutFeedArt.Title.Should().Be("Apple Title");
            withoutFeedArt.TotalEpisodes.Should().Be(99);
            genres.Should().Equal("news", "tech news");
        }

        [Fact]
        public void ApplyToSeries_SkipsValuesTooLongForTheirColumns()
        {
            var channel = new FeedSeries
            {
                Title = new string('t', 600),
                Language = "this-is-not-a-language-code",
                PodcastGuid = new string('g', 101),
                ImageUrl = "https://feed.example.com/" + new string('a', 2000)
            };
            var series = NewSeries();

            PodcastFeedMapper.ApplyToSeries(series, Feed(channel), directoryHit: null, fillOnly: false);

            series.Title.Should().HaveLength(500);
            series.Language.Should().BeNull();
            series.FeedGuid.Should().BeNull();
            series.Thumbnail.Should().BeNull();
        }

        [Fact]
        public void ApplyDirectory_WithoutAHit_ChangesNothing()
        {
            var series = NewSeries("Title");

            PodcastFeedMapper.ApplyDirectory(series, null).Should().BeEmpty();
            series.Title.Should().Be("Title");
        }

        [Theory]
        [InlineData("https://www.feeds.example.com/rss", "Podcast at feeds.example.com")]
        [InlineData(null, "Podcast at an unknown address")]
        public void PlaceholderTitle_NamesTheHost(string? feedUrl, string expected)
        {
            PodcastFeedMapper.PlaceholderTitle(feedUrl).Should().Be(expected);
            PodcastFeedMapper.IsPlaceholderTitle(expected).Should().BeTrue();
            PodcastFeedMapper.IsPlaceholderTitle("Darknet Diaries").Should().BeFalse();
        }
    }
}
