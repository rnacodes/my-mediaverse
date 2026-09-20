using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Enrichment is feed-fill: the directory resolves a series that has no feed URL, and the feed
    /// supplies the stored metadata. Both are substituted, so no test reaches Apple or a feed host.
    /// </summary>
    [Trait("Category", "Unit")]
    public class PodcastEnrichmentServiceTests : InMemoryDbTestBase
    {
        private const string FeedUrl = "https://feeds.example.com/daily.xml";

        private readonly IPodcastDirectory _directory;
        private readonly IPodcastFeedReader _feedReader;
        private readonly PodcastEnrichmentOptions _options;
        private readonly PodcastEnrichmentService _service;

        public PodcastEnrichmentServiceTests()
        {
            _directory = Substitute.For<IPodcastDirectory>();
            _feedReader = Substitute.For<IPodcastFeedReader>();
            _options = new PodcastEnrichmentOptions { DelayBetweenCallsMs = 0, RetryAfterDays = 7 };
            _service = new PodcastEnrichmentService(
                Context, _directory, _feedReader, Options.Create(_options),
                Substitute.For<ILogger<PodcastEnrichmentService>>());
        }

        #region helpers

        private PodcastSeries Series(
            string title = "The Daily",
            string? feedUrl = null,
            string? applePodcastsId = null,
            string? publisher = null,
            DateTime? enrichedAt = null,
            DateTime? lastAttemptAt = null,
            string metadataSource = PodcastMetadataSources.Manual) => new()
            {
                Id = Guid.NewGuid(),
                Title = title,
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                RssFeedUrl = feedUrl,
                ApplePodcastsId = applePodcastsId,
                Publisher = publisher,
                EnrichedAt = enrichedAt,
                LastEnrichmentAttemptAt = lastAttemptAt,
                MetadataSource = metadataSource
            };

        private async Task<PodcastSeries> Seed(PodcastSeries series)
        {
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();
            return series;
        }

        private static PodcastFeed Feed(
            string? title = "The Daily",
            string? description = "The biggest stories of our time.",
            string? publisher = "The New York Times",
            string? imageUrl = "https://feeds.example.com/art.jpg",
            string? language = "en-us",
            string? podcastGuid = "b1f2a6d0-0000-4000-8000-000000000001",
            int totalItemCount = 2000,
            string[]? categories = null) => new()
            {
                Series = new FeedSeries
                {
                    Title = title,
                    Description = description,
                    Publisher = publisher,
                    ImageUrl = imageUrl,
                    Language = language,
                    PodcastGuid = podcastGuid,
                    Categories = categories ?? new[] { "News", "Daily News" }
                },
                TotalItemCount = totalItemCount
            };

        private void ReaderReturns(PodcastFeed feed) =>
            _feedReader.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(feed);

        private void ReaderThrows(PodcastFeedFailureReason reason = PodcastFeedFailureReason.Unreachable) =>
            _feedReader.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(reason, "The feed's server could not be reached."));

        private static DirectoryPodcast Hit(
            string title = "The Daily",
            string? feedUrl = FeedUrl,
            string? applePodcastsId = "1200361736",
            string? publisher = "The New York Times") => new()
            {
                Title = title,
                Publisher = publisher,
                FeedUrl = feedUrl,
                ApplePodcastsId = applePodcastsId,
                ArtworkUrl = "https://apple.example.com/600.jpg",
                Genres = new[] { "News" },
                Source = DirectoryPodcast.AppleSource
            };

        private async Task<PodcastSeries> Reload(Guid id) =>
            (await Context.PodcastSeries.FindAsync(id))!;

        #endregion

        #region eligibility

        [Fact]
        public async Task GetPodcastsNeedingEnrichmentCountAsync_CountsOnlySeriesNeverFilled()
        {
            await Seed(Series("Pending"));
            await Seed(Series("Done", enrichedAt: DateTime.UtcNow.AddDays(-1)));

            (await _service.GetPodcastsNeedingEnrichmentCountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task GetPodcastsNeedingEnrichmentCountAsync_ExcludesSeriesAttemptedInsideTheRetryWindow()
        {
            await Seed(Series("Tried yesterday", lastAttemptAt: DateTime.UtcNow.AddDays(-1)));
            await Seed(Series("Tried long ago", lastAttemptAt: DateTime.UtcNow.AddDays(-30)));
            await Seed(Series("Never tried"));

            (await _service.GetPodcastsNeedingEnrichmentCountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_NothingPending_ReportsTheContractBody()
        {
            var result = await _service.EnrichPendingPodcastsAsync();

            result.Success.Should().BeTrue();
            result.Operation.Should().Be(PodcastEnrichmentResult.EnrichmentOperation);
            result.TotalProcessed.Should().Be(0);
            result.PendingCount.Should().Be(0);
            result.CompletedAt.Should().NotBeNull();
            result.Duration.Should().NotBeNull();
            _feedReader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_LeavesAlreadyFilledSeriesAlone()
        {
            await Seed(Series("Done", feedUrl: FeedUrl, enrichedAt: DateTime.UtcNow));

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(0);
            _feedReader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_TakesNeverAttemptedSeriesFirst()
        {
            await Seed(Series("Retry", feedUrl: FeedUrl, lastAttemptAt: DateTime.UtcNow.AddDays(-30)));
            var fresh = await Seed(Series("Never tried", feedUrl: "https://feeds.example.com/fresh.xml"));
            ReaderReturns(Feed());

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 1, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            (await Reload(fresh.Id)).EnrichedAt.Should().NotBeNull();
        }

        #endregion

        #region feed fill

        [Fact]
        public async Task EnrichPendingPodcastsAsync_FillsFromTheFeed_AndStampsTheSource()
        {
            var series = await Seed(Series("Podcast at feeds.example.com", feedUrl: FeedUrl));
            ReaderReturns(Feed());

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);
            result.UnchangedCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            result.PendingCount.Should().Be(0);

            var stored = await Reload(series.Id);
            stored.Title.Should().Be("The Daily");
            stored.Description.Should().Be("The biggest stories of our time.");
            stored.Publisher.Should().Be("The New York Times");
            stored.Thumbnail.Should().Be("https://feeds.example.com/art.jpg");
            stored.Language.Should().Be("en-us");
            stored.FeedGuid.Should().Be("b1f2a6d0-0000-4000-8000-000000000001");
            stored.TotalEpisodes.Should().Be(2000);
            stored.MetadataSource.Should().Be(PodcastMetadataSources.Rss);
            stored.EnrichedAt.Should().NotBeNull();
            stored.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_FillOnly_KeepsValuesTheSeriesAlreadyHas()
        {
            var series = Series("My Own Title", feedUrl: FeedUrl, publisher: "My Own Publisher");
            series.Description = "My own description.";
            await Seed(series);
            ReaderReturns(Feed());

            await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            var stored = await Reload(series.Id);
            stored.Title.Should().Be("My Own Title");
            stored.Description.Should().Be("My own description.");
            stored.Publisher.Should().Be("My Own Publisher");
            // Fields it did not have are still filled.
            stored.Language.Should().Be("en-us");
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_ReplacesAPlaceholderTitle()
        {
            var series = await Seed(Series("Podcast at feeds.example.com", feedUrl: FeedUrl));
            ReaderReturns(Feed());

            await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            (await Reload(series.Id)).Title.Should().Be("The Daily");
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_StoresGenresLowercased()
        {
            var series = await Seed(Series(feedUrl: FeedUrl));
            ReaderReturns(Feed(categories: new[] { "News", "Daily News" }));

            await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            var stored = await Context.PodcastSeries
                .Include(p => p.Genres)
                .FirstAsync(p => p.Id == series.Id);
            stored.Genres.Select(g => g.Name).Should().BeEquivalentTo("news", "daily news");
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_FeedWithNothingNew_CountsUnchanged_ButStillMarksItFilled()
        {
            var series = Series("The Daily", feedUrl: FeedUrl, publisher: "The New York Times");
            series.Description = "The biggest stories of our time.";
            series.Thumbnail = "https://feeds.example.com/art.jpg";
            series.Language = "en-us";
            series.FeedGuid = "b1f2a6d0-0000-4000-8000-000000000001";
            series.TotalEpisodes = 2000;
            series.MetadataSource = PodcastMetadataSources.Rss;
            await Seed(series);
            ReaderReturns(Feed(categories: Array.Empty<string>()));

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.UnchangedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            // Without this the row would come back in the queue on every future run.
            (await Reload(series.Id)).EnrichedAt.Should().NotBeNull();
        }

        #endregion

        #region directory resolution

        [Fact]
        public async Task EnrichPendingPodcastsAsync_NoFeedUrl_ResolvesItByAppleId()
        {
            var series = await Seed(Series("The Daily", applePodcastsId: "1200361736"));
            _directory.LookupByAppleIdAsync("1200361736", Arg.Any<CancellationToken>()).Returns(Hit());
            ReaderReturns(Feed());

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);
            var stored = await Reload(series.Id);
            stored.RssFeedUrl.Should().Be(FeedUrl);
            stored.FeedUrlKey.Should().NotBeNullOrEmpty();
            await _feedReader.Received(1).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_NoFeedUrlOrAppleId_SearchesByTitle()
        {
            var series = await Seed(Series("The Daily"));
            _directory.SearchAsync("The Daily", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new[] { Hit() });
            ReaderReturns(Feed());

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);
            var stored = await Reload(series.Id);
            stored.RssFeedUrl.Should().Be(FeedUrl);
            stored.ApplePodcastsId.Should().Be("1200361736");
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_AmbiguousTitleSearch_ResolvesNothing()
        {
            var series = await Seed(Series("The Daily"));
            _directory.SearchAsync("The Daily", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new[] { Hit(applePodcastsId: "1"), Hit(applePodcastsId: "2", publisher: "Someone Else") });

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            // Attaching the wrong feed is worse than leaving the series unfilled.
            result.NotFoundCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            _feedReader.ReceivedCalls().Should().BeEmpty();
            var stored = await Reload(series.Id);
            stored.RssFeedUrl.Should().BeNull();
            stored.EnrichedAt.Should().BeNull();
            stored.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_AmbiguousTitleButOneMatchingPublisher_ResolvesThatOne()
        {
            var series = await Seed(Series("The Daily", publisher: "The New York Times"));
            _directory.SearchAsync("The Daily", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new[] { Hit(applePodcastsId: "2", publisher: "Someone Else", feedUrl: "https://other.example.com/f.xml"), Hit() });
            ReaderReturns(Feed());

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);
            (await Reload(series.Id)).RssFeedUrl.Should().Be(FeedUrl);
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_DirectoryMiss_CountsNotFound_AndRecordsTheAttempt()
        {
            var series = await Seed(Series("Unknown Show", applePodcastsId: "999"));
            _directory.LookupByAppleIdAsync("999", Arg.Any<CancellationToken>()).Returns((DirectoryPodcast?)null);

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.WarningMessage.Should().Contain("no feed URL");
            var stored = await Reload(series.Id);
            stored.EnrichedAt.Should().BeNull();
            stored.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_DirectoryUnavailable_CountsFailure()
        {
            var series = await Seed(Series("The Daily", applePodcastsId: "1200361736"));
            _directory.LookupByAppleIdAsync("1200361736", Arg.Any<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Apple is down"));

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.FailedCount.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().NotContain("Apple is down");
            (await Reload(series.Id)).LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        #endregion

        #region failures

        [Fact]
        public async Task EnrichPendingPodcastsAsync_UnreadableFeed_CountsFailure_AndKeepsTheSeriesUnfilled()
        {
            var series = await Seed(Series(feedUrl: FeedUrl));
            ReaderThrows();

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.FailedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            result.Errors.Should().ContainSingle();
            result.WarningMessage.Should().Contain("retried later");

            var stored = await Reload(series.Id);
            stored.EnrichedAt.Should().BeNull();
            // The attempt is recorded, so the retry window governs the next try.
            stored.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_OneBadFeed_DoesNotCostTheOthersTheirFill()
        {
            var bad = await Seed(Series("Bad", feedUrl: "https://feeds.example.com/bad.xml"));
            var good = await Seed(Series("Good", feedUrl: "https://feeds.example.com/good.xml"));

            _feedReader.ReadAsync("https://feeds.example.com/bad.xml", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.InvalidXml, "Not a feed."));
            _feedReader.ReadAsync("https://feeds.example.com/good.xml", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Feed(title: "Good Show"));

            var result = await _service.EnrichPendingPodcastsAsync(delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(2);
            result.FailedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            (await Reload(bad.Id)).EnrichedAt.Should().BeNull();
            (await Reload(good.Id)).EnrichedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_Cancelled_ReportsIt()
        {
            await Seed(Series(feedUrl: FeedUrl));
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var result = await _service.EnrichPendingPodcastsAsync(
                delayBetweenCallsMs: 0, cancellationToken: cts.Token);

            result.WasCancelled.Should().BeTrue();
            result.WarningMessage.Should().Contain("cancelled");
        }

        #endregion

        #region single series

        [Fact]
        public async Task EnrichSeriesAsync_UnknownSeries_Throws()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.EnrichSeriesAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task EnrichSeriesAsync_AlreadyFilledWithoutForce_SkipsIt()
        {
            var series = await Seed(Series(feedUrl: FeedUrl, enrichedAt: DateTime.UtcNow.AddDays(-2)));

            var result = await _service.EnrichSeriesAsync(series.Id);

            result.SkippedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            result.WarningMessage.Should().Contain("force");
            _feedReader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task EnrichSeriesAsync_NotYetFilled_FillsIt()
        {
            var series = await Seed(Series("Podcast at feeds.example.com", feedUrl: FeedUrl));
            ReaderReturns(Feed());

            var result = await _service.EnrichSeriesAsync(series.Id);

            result.EnrichedCount.Should().Be(1);
            result.TotalProcessed.Should().Be(1);
            (await Reload(series.Id)).Title.Should().Be("The Daily");
        }

        [Fact]
        public async Task EnrichSeriesAsync_Forced_OverwritesWhatIsStored()
        {
            var series = await Seed(Series(
                "A Stale Title", feedUrl: FeedUrl, publisher: "A Stale Publisher",
                enrichedAt: DateTime.UtcNow.AddDays(-30)));
            ReaderReturns(Feed());

            var result = await _service.EnrichSeriesAsync(series.Id, force: true);

            result.EnrichedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);
            var stored = await Reload(series.Id);
            stored.Title.Should().Be("The Daily");
            stored.Publisher.Should().Be("The New York Times");
        }

        #endregion
    }
}
