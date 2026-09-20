using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.UnitTests.Application
{
    public partial class PodcastServiceTests
    {
        #region Identity and duplicate handling

        [Fact]
        public async Task CreatePodcastSeriesAsync_SameFeedInAnyVariant_ReturnsExistingRowAndAbsorbsIds()
        {
            var first = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto
            {
                Title = "Darknet Diaries",
                RssFeedUrl = "https://feeds.megaphone.fm/darknetdiaries"
            });

            var second = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto
            {
                Title = "Darknet Diaries (again)",
                RssFeedUrl = "http://www.feeds.megaphone.fm/darknetdiaries/",
                ApplePodcastsId = "1296350485",
                Description = "True stories from the dark side of the Internet.",
                Topics = new[] { "Security" }
            });

            first.Created.Should().BeTrue();
            second.Created.Should().BeFalse();
            second.Series.Id.Should().Be(first.Series.Id);

            var stored = await Context.PodcastSeries.Include(p => p.Topics).SingleAsync();
            stored.Title.Should().Be("Darknet Diaries");
            stored.RssFeedUrl.Should().Be("https://feeds.megaphone.fm/darknetdiaries");
            stored.FeedUrlKey.Should().Be("feeds.megaphone.fm/darknetdiaries");
            stored.ApplePodcastsId.Should().Be("1296350485");
            stored.Description.Should().Be("True stories from the dark side of the Internet.");
            stored.Topics.Select(t => t.Name).Should().BeEquivalentTo("security");
        }

        [Fact]
        public async Task CreatePodcastSeriesAsync_StampsMetadataSource_DefaultingToManual()
        {
            var manual = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "By hand" });
            var fromFeed = await _service.CreatePodcastSeriesAsync(
                new CreatePodcastSeriesDto { Title = "From feed", RssFeedUrl = "https://example.com/feed.xml" },
                PodcastMetadataSources.Rss);

            manual.Series.MetadataSource.Should().Be(PodcastMetadataSources.Manual);
            fromFeed.Series.MetadataSource.Should().Be(PodcastMetadataSources.Rss);
        }

        [Theory]
        [InlineData("not a url")]
        [InlineData("ftp://example.com/feed.xml")]
        public async Task CreatePodcastSeriesAsync_InvalidFeedUrl_ThrowsArgumentException(string feedUrl)
        {
            var act = () => _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "Bad", RssFeedUrl = feedUrl });

            await act.Should().ThrowAsync<ArgumentException>();
            (await Context.PodcastSeries.AnyAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task CreatePodcastSeriesAsync_RepeatedTopicNames_ResolveToOneTopic()
        {
            var result = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto
            {
                Title = "Show",
                Topics = new[] { "History", " history ", "HISTORY" }
            });

            result.Series.Topics.Should().ContainSingle().Which.Name.Should().Be("history");
        }

        [Fact]
        public async Task UpdatePodcastSeriesAsync_FeedUrlOwnedByAnotherSeries_ThrowsInvalidOperation()
        {
            await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "A", RssFeedUrl = "https://a.example.com/feed" });
            var b = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "B", RssFeedUrl = "https://b.example.com/feed" });

            var act = () => _service.UpdatePodcastSeriesAsync(b.Series.Id,
                new CreatePodcastSeriesDto { Title = "B", RssFeedUrl = "http://a.example.com/feed/" });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*feed URL*");
        }

        [Fact]
        public async Task UpdatePodcastSeriesAsync_AppleIdOwnedByAnotherSeries_ThrowsInvalidOperation()
        {
            await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "A", ApplePodcastsId = "111" });
            var b = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "B" });

            var act = () => _service.UpdatePodcastSeriesAsync(b.Series.Id,
                new CreatePodcastSeriesDto { Title = "B", ApplePodcastsId = "111" });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Apple*");
        }

        [Fact]
        public async Task UpdatePodcastSeriesAsync_ChangingTheFeedUrl_RefreshesTheKey()
        {
            var created = await _service.CreatePodcastSeriesAsync(
                new CreatePodcastSeriesDto { Title = "Show", RssFeedUrl = "https://old.example.com/feed" });

            await _service.UpdatePodcastSeriesAsync(created.Series.Id,
                new CreatePodcastSeriesDto { Title = "Show", RssFeedUrl = "https://new.example.com/feed" });

            var stored = await Context.PodcastSeries.AsNoTracking().SingleAsync();
            stored.FeedUrlKey.Should().Be("new.example.com/feed");
        }

        [Fact]
        public async Task SeriesWithoutAFeed_HasNoFeedUrlKey_OnCreateAndOnUpdate()
        {
            // The unique index on the key exempts NULL only; an empty key would let one feedless series exist.
            var a = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "A" });
            var b = await _service.CreatePodcastSeriesAsync(
                new CreatePodcastSeriesDto { Title = "B", RssFeedUrl = "https://b.example.com/feed" });

            await _service.UpdatePodcastSeriesAsync(b.Series.Id, new CreatePodcastSeriesDto { Title = "B" });

            var stored = await Context.PodcastSeries.AsNoTracking().ToListAsync();
            stored.Should().HaveCount(2);
            stored.Should().OnlyContain(s => s.FeedUrlKey == null);
            a.Created.Should().BeTrue();
        }

        [Fact]
        public async Task DeletePodcastSeriesAsync_RemovesEpisodesCompletely_AndCleansTheSearchIndex()
        {
            var series = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "Show" });
            var ep1 = await _service.CreatePodcastEpisodeAsync(new CreatePodcastEpisodeDto { Title = "One", SeriesId = series.Series.Id, RssGuid = "1" });
            var ep2 = await _service.CreatePodcastEpisodeAsync(new CreatePodcastEpisodeDto { Title = "Two", SeriesId = series.Series.Id, RssGuid = "2" });
            Context.ChangeTracker.Clear();

            var deleted = await _service.DeletePodcastSeriesAsync(series.Series.Id);

            deleted.Should().BeTrue();
            (await Context.MediaItems.AnyAsync()).Should().BeFalse();
            await _mockTypesenseService.Received(1).DeleteMediaItemAsync(series.Series.Id);
            await _mockTypesenseService.Received(1).DeleteMediaItemAsync(ep1.Episode.Id);
            await _mockTypesenseService.Received(1).DeleteMediaItemAsync(ep2.Episode.Id);
        }

        [Fact]
        public async Task DeletePodcastSeriesAsync_SearchIndexFailure_DoesNotFailTheDelete()
        {
            var series = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "Show" });
            _mockTypesenseService.DeleteMediaItemAsync(Arg.Any<Guid>())
                .Returns(Task.FromException(new HttpRequestException("search down")));

            var deleted = await _service.DeletePodcastSeriesAsync(series.Series.Id);

            deleted.Should().BeTrue();
            (await Context.PodcastSeries.AnyAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task DeletePodcastEpisodeAsync_CleansTheSearchIndex()
        {
            var series = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "Show" });
            var episode = await _service.CreatePodcastEpisodeAsync(new CreatePodcastEpisodeDto { Title = "One", SeriesId = series.Series.Id });

            (await _service.DeletePodcastEpisodeAsync(episode.Episode.Id)).Should().BeTrue();

            await _mockTypesenseService.Received(1).DeleteMediaItemAsync(episode.Episode.Id);
        }

        [Fact]
        public async Task CreatePodcastEpisodeAsync_SameGuidInSeries_ReturnsExistingEpisode()
        {
            var series = await _service.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto { Title = "Show" });

            var first = await _service.CreatePodcastEpisodeAsync(
                new CreatePodcastEpisodeDto { Title = "Episode 1", SeriesId = series.Series.Id, RssGuid = "guid-1" });
            var second = await _service.CreatePodcastEpisodeAsync(
                new CreatePodcastEpisodeDto { Title = "Episode 1 (renamed)", SeriesId = series.Series.Id, RssGuid = "guid-1" });

            first.Created.Should().BeTrue();
            second.Created.Should().BeFalse();
            second.Episode.Id.Should().Be(first.Episode.Id);
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(1);
        }

        #endregion
    }
}
