using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastSeriesDuplicateFinderTests : InMemoryDbTestBase
    {
        private PodcastSeries AddSeries(
            string title = "Some Show",
            string? feedUrl = null,
            string? feedUrlKey = null,
            string? feedGuid = null,
            string? appleId = null,
            long? podcastIndexId = null,
            string? externalId = null,
            string? publisher = null)
        {
            var series = new PodcastSeries
            {
                Title = title,
                MediaType = MediaType.Podcast,
                RssFeedUrl = feedUrl,
                FeedUrlKey = feedUrlKey,
                FeedGuid = feedGuid,
                ApplePodcastsId = appleId,
                PodcastIndexId = podcastIndexId,
                ExternalId = externalId,
                Publisher = publisher
            };
            Context.PodcastSeries.Add(series);
            return series;
        }

        [Theory]
        [InlineData("https://feeds.example.com/show")]
        [InlineData("http://feeds.example.com/show/")]
        [InlineData("HTTPS://www.Feeds.Example.com/show")]
        public async Task FindExistingAsync_MatchesOnFeedUrlKey_IgnoringSchemeWwwAndTrailingSlash(string probe)
        {
            var target = AddSeries(feedUrl: "https://feeds.example.com/show", feedUrlKey: "feeds.example.com/show");
            AddSeries(feedUrl: "https://feeds.example.com/other", feedUrlKey: "feeds.example.com/other");
            await Context.SaveChangesAsync();

            var match = await PodcastSeriesDuplicateFinder.FindExistingAsync(
                Context.PodcastSeries, new PodcastSeriesIdentity { FeedUrl = probe });

            match!.Id.Should().Be(target.Id);
        }

        [Theory]
        [InlineData("http://feeds.example.com/show")]
        [InlineData("https://feeds.example.com/show/")]
        public async Task FindExistingAsync_LegacyRowWithoutFeedUrlKey_MatchesOnStoredFeedUrl(string probe)
        {
            // Rows saved before the key column existed keep only the raw feed URL.
            var legacy = AddSeries(feedUrl: "https://Feeds.Example.com/show", feedUrlKey: null);
            await Context.SaveChangesAsync();

            var match = await PodcastSeriesDuplicateFinder.FindExistingAsync(
                Context.PodcastSeries, new PodcastSeriesIdentity { FeedUrl = probe });

            match!.Id.Should().Be(legacy.Id);
        }

        [Fact]
        public async Task FindExistingAsync_FeedGuidWinsOverFeedUrl()
        {
            // A show that moved hosts keeps its podcast:guid; the guid match must win even when
            // another row still holds the incoming URL.
            var byGuid = AddSeries(title: "Moved", feedGuid: "guid-123", feedUrl: "https://old.example.com/feed", feedUrlKey: "old.example.com/feed");
            AddSeries(title: "Other", feedUrl: "https://new.example.com/feed", feedUrlKey: "new.example.com/feed");
            await Context.SaveChangesAsync();

            var match = await PodcastSeriesDuplicateFinder.FindExistingAsync(
                Context.PodcastSeries,
                new PodcastSeriesIdentity { FeedGuid = "guid-123", FeedUrl = "https://new.example.com/feed" });

            match!.Id.Should().Be(byGuid.Id);
        }

        [Fact]
        public async Task FindExistingAsync_FallsBackToDirectoryIds_InPriorityOrder()
        {
            var byApple = AddSeries(title: "Apple", appleId: "1047332516");
            var byIndex = AddSeries(title: "Index", podcastIndexId: 42);
            var byExternal = AddSeries(title: "ListenNotes", externalId: "ln-1");
            await Context.SaveChangesAsync();

            (await PodcastSeriesDuplicateFinder.FindExistingAsync(Context.PodcastSeries,
                new PodcastSeriesIdentity { ApplePodcastsId = "1047332516", PodcastIndexId = 42 }))!.Id.Should().Be(byApple.Id);
            (await PodcastSeriesDuplicateFinder.FindExistingAsync(Context.PodcastSeries,
                new PodcastSeriesIdentity { PodcastIndexId = 42, ExternalId = "ln-1" }))!.Id.Should().Be(byIndex.Id);
            (await PodcastSeriesDuplicateFinder.FindExistingAsync(Context.PodcastSeries,
                new PodcastSeriesIdentity { ExternalId = "ln-1" }))!.Id.Should().Be(byExternal.Id);
        }

        [Fact]
        public async Task FindExistingAsync_TitleAndPublisher_RequireBoth()
        {
            var target = AddSeries(title: "Darknet Diaries", publisher: "Jack Rhysider");
            await Context.SaveChangesAsync();

            (await PodcastSeriesDuplicateFinder.FindExistingAsync(Context.PodcastSeries,
                new PodcastSeriesIdentity { Title = " darknet diaries ", Publisher = "JACK RHYSIDER" }))!.Id.Should().Be(target.Id);
            (await PodcastSeriesDuplicateFinder.FindExistingAsync(Context.PodcastSeries,
                new PodcastSeriesIdentity { Title = "Darknet Diaries" })).Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_ReturnsNull_WhenNothingMatches()
        {
            AddSeries(feedUrl: "https://feeds.example.com/show", feedUrlKey: "feeds.example.com/show", appleId: "1");
            await Context.SaveChangesAsync();

            var match = await PodcastSeriesDuplicateFinder.FindExistingAsync(Context.PodcastSeries,
                new PodcastSeriesIdentity { FeedUrl = "https://feeds.example.com/elsewhere", ApplePodcastsId = "2" });

            match.Should().BeNull();
        }

        [Fact]
        public void FillIdentity_SetsFeedUrlKey_OnlyWhenMissing()
        {
            var series = new PodcastSeries { Title = "x", RssFeedUrl = "https://www.Feeds.example.com/show/" };

            PodcastSeriesDuplicateFinder.FillIdentity(series).Should().BeTrue();
            series.FeedUrlKey.Should().Be("feeds.example.com/show");
            PodcastSeriesDuplicateFinder.FillIdentity(series).Should().BeFalse();
        }

        [Fact]
        public async Task AbsorbIdentityAsync_FillsMissingIds_AndNeverOverwrites()
        {
            var existing = AddSeries(title: "Show", appleId: "111");
            await Context.SaveChangesAsync();

            var changed = await PodcastSeriesDuplicateFinder.AbsorbIdentityAsync(Context.PodcastSeries, existing,
                new PodcastSeriesIdentity
                {
                    FeedUrl = "https://feeds.example.com/show",
                    FeedGuid = "guid-1",
                    ApplePodcastsId = "999",
                    PodcastIndexId = 7
                });

            changed.Should().BeTrue();
            existing.RssFeedUrl.Should().Be("https://feeds.example.com/show");
            existing.FeedUrlKey.Should().Be("feeds.example.com/show");
            existing.FeedGuid.Should().Be("guid-1");
            existing.PodcastIndexId.Should().Be(7);
            existing.ApplePodcastsId.Should().Be("111");
        }

        [Fact]
        public async Task AbsorbIdentityAsync_SkipsAnIdAnotherRowOwns()
        {
            var existing = AddSeries(title: "Show", feedUrl: "https://a.example.com/feed", feedUrlKey: "a.example.com/feed");
            AddSeries(title: "Other", appleId: "555");
            await Context.SaveChangesAsync();

            var changed = await PodcastSeriesDuplicateFinder.AbsorbIdentityAsync(Context.PodcastSeries, existing,
                new PodcastSeriesIdentity { ApplePodcastsId = "555" });

            changed.Should().BeFalse();
            existing.ApplePodcastsId.Should().BeNull();
        }

        [Fact]
        public void AbsorbMetadata_FillsBlanksOnly_AndMergesTags()
        {
            var shared = new Topic { Name = "security" };
            var existing = new PodcastSeries { Title = "Keep", Publisher = "Original", Topics = { shared } };
            var incoming = new PodcastSeries
            {
                Title = "Ignored",
                Publisher = "Ignored",
                Description = "About the show",
                Thumbnail = "https://img.example.com/a.jpg",
                Language = "en",
                Topics = { new Topic { Name = "security" }, new Topic { Name = "true crime" } },
                Genres = { new Genre { Name = "technology" } }
            };

            PodcastSeriesDuplicateFinder.AbsorbMetadata(existing, incoming).Should().BeTrue();

            existing.Title.Should().Be("Keep");
            existing.Publisher.Should().Be("Original");
            existing.Description.Should().Be("About the show");
            existing.Thumbnail.Should().Be("https://img.example.com/a.jpg");
            existing.Language.Should().Be("en");
            existing.Topics.Select(t => t.Name).Should().BeEquivalentTo("security", "true crime");
            existing.Genres.Select(g => g.Name).Should().BeEquivalentTo("technology");
        }
    }
}
