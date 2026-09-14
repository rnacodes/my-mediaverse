using AwesomeAssertions;
using MyMediaVerse.Infrastructure.Services.Podcasts;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class ApplePodcastDirectoryTests
    {
        private readonly IItunesLookupClient _itunesClient = Substitute.For<IItunesLookupClient>();
        private readonly ApplePodcastDirectory _directory;

        public ApplePodcastDirectoryTests()
        {
            _directory = new ApplePodcastDirectory(_itunesClient);
        }

        private static ItunesPodcastDto Darknet() => new()
        {
            Kind = "podcast",
            CollectionId = 1296350485,
            CollectionName = " Darknet Diaries ",
            ArtistName = "Jack Rhysider",
            FeedUrl = "https://podcast.darknetdiaries.com",
            ArtworkUrl600 = "https://example.com/600x600bb.jpg",
            TrackCount = 198,
            CollectionViewUrl = "https://podcasts.apple.com/us/podcast/darknet-diaries/id1296350485",
            PrimaryGenreName = "Technology",
            Genres = new List<string> { "Technology", "Podcasts" },
            ReleaseDate = new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public void Map_CopiesFieldsAndDropsTheCatchAllGenre()
        {
            var podcast = ApplePodcastDirectory.Map(Darknet());

            podcast.Should().NotBeNull();
            podcast!.Title.Should().Be("Darknet Diaries");
            podcast.Publisher.Should().Be("Jack Rhysider");
            podcast.FeedUrl.Should().Be("https://podcast.darknetdiaries.com");
            podcast.ApplePodcastsId.Should().Be("1296350485");
            podcast.PodcastIndexId.Should().BeNull();
            podcast.ArtworkUrl.Should().Be("https://example.com/600x600bb.jpg");
            podcast.Genres.Should().Equal("Technology");
            podcast.EpisodeCount.Should().Be(198);
            podcast.StoreUrl.Should().Be("https://podcasts.apple.com/us/podcast/darknet-diaries/id1296350485");
            podcast.LatestReleaseDate.Should().Be(new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc));
            podcast.Source.Should().Be(DirectoryPodcast.AppleSource);
        }

        [Fact]
        public void Map_FallsBackToPrimaryGenre_AndBlankFieldsBecomeNull()
        {
            var dto = Darknet();
            dto.Genres = null;
            dto.FeedUrl = "  ";
            dto.ArtistName = "";

            var podcast = ApplePodcastDirectory.Map(dto)!;

            podcast.Genres.Should().Equal("Technology");
            podcast.FeedUrl.Should().BeNull();
            podcast.Publisher.Should().BeNull();
        }

        [Theory]
        [InlineData(0, "Darknet Diaries")]
        [InlineData(1296350485, null)]
        [InlineData(1296350485, " ")]
        public void Map_ReturnsNull_WithoutIdOrTitle(long collectionId, string? title)
        {
            var dto = Darknet();
            dto.CollectionId = collectionId;
            dto.CollectionName = title;

            ApplePodcastDirectory.Map(dto).Should().BeNull();
        }

        [Fact]
        public async Task SearchAsync_MapsResultsAndSkipsUnusableOnes()
        {
            var unusable = Darknet();
            unusable.CollectionName = null;
            _itunesClient.SearchPodcastsAsync("darknet", 5, Arg.Any<CancellationToken>())
                .Returns(new List<ItunesPodcastDto> { Darknet(), unusable });

            var results = await _directory.SearchAsync("darknet", 5);

            results.Should().ContainSingle().Which.ApplePodcastsId.Should().Be("1296350485");
        }

        [Fact]
        public async Task LookupByAppleIdAsync_MapsTheShow_OrReturnsNull()
        {
            _itunesClient.GetPodcastByCollectionIdAsync("1296350485", Arg.Any<CancellationToken>()).Returns(Darknet());
            _itunesClient.GetPodcastByCollectionIdAsync("1", Arg.Any<CancellationToken>()).Returns((ItunesPodcastDto?)null);

            (await _directory.LookupByAppleIdAsync("1296350485"))!.Title.Should().Be("Darknet Diaries");
            (await _directory.LookupByAppleIdAsync("1")).Should().BeNull();
        }

        [Fact]
        public async Task LookupByFeedUrlAsync_IsNotSupportedByApple()
        {
            (await _directory.LookupByFeedUrlAsync("https://podcast.darknetdiaries.com")).Should().BeNull();
            _itunesClient.ReceivedCalls().Should().BeEmpty();
        }
    }
}
