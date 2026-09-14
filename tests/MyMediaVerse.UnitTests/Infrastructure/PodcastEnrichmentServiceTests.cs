using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class PodcastEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly IItunesLookupClient _mockItunesLookupClient;
        private readonly ILogger<PodcastEnrichmentService> _mockLogger;
        private readonly PodcastEnrichmentService _service;

        public PodcastEnrichmentServiceTests()
        {
            _mockItunesLookupClient = Substitute.For<IItunesLookupClient>();
            _mockLogger = Substitute.For<ILogger<PodcastEnrichmentService>>();
            _service = new PodcastEnrichmentService(Context, _mockItunesLookupClient, _mockLogger);
        }

        private void MockItunesLookup(string collectionId, ItunesPodcastDto? result) =>
            _mockItunesLookupClient.GetPodcastByCollectionIdAsync(collectionId, Arg.Any<CancellationToken>())
                .Returns(result);

        private PodcastSeries CreateTestPodcastSeries(
            string title,
            string? applePodcastsId = null,
            DateTime? enrichedAt = null,
            DateTime? lastAttemptAt = null)
        {
            return new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = title,
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                ApplePodcastsId = applePodcastsId,
                EnrichedAt = enrichedAt,
                LastEnrichmentAttemptAt = lastAttemptAt
            };
        }

        #region GetPodcastsNeedingEnrichmentCountAsync

        [Fact]
        public async Task GetPodcastsNeedingEnrichmentCountAsync_NoPodcasts_ReturnsZero()
        {
            var result = await _service.GetPodcastsNeedingEnrichmentCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetPodcastsNeedingEnrichmentCountAsync_CountsSeriesNeverEnriched()
        {
            var seriesNeedsEnrichment = CreateTestPodcastSeries("Podcast 1");
            var seriesAlreadyEnriched = CreateTestPodcastSeries("Podcast 2", enrichedAt: DateTime.UtcNow.AddDays(-1));

            Context.PodcastSeries.AddRange(seriesNeedsEnrichment, seriesAlreadyEnriched);
            await Context.SaveChangesAsync();

            var result = await _service.GetPodcastsNeedingEnrichmentCountAsync();

            result.Should().Be(1);
        }

        #endregion

        #region EnrichPendingPodcastsAsync

        [Fact]
        public async Task EnrichPendingPodcastsAsync_NoPodcastsNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.EnrichPendingPodcastsAsync();

            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_SkipsSeriesAlreadyEnriched()
        {
            Context.PodcastSeries.Add(CreateTestPodcastSeries("Done", applePodcastsId: "1", enrichedAt: DateTime.UtcNow));
            await Context.SaveChangesAsync();

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(0);
            _mockItunesLookupClient.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_WithApplePodcastsId_BackfillsFromItunes_AndStampsEnrichedAt()
        {
            var series = CreateTestPodcastSeries("The Daily", applePodcastsId: "1200361736");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            MockItunesLookup("1200361736", new ItunesPodcastDto
            {
                CollectionName = "The Daily",
                ArtistName = "The New York Times",
                FeedUrl = "https://feeds.simplecast.com/thedaily",
                ArtworkUrl600 = "https://example.com/daily600.jpg",
                TrackCount = 2652
            });

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.RssFeedUrl.Should().Be("https://feeds.simplecast.com/thedaily");
            updated.Publisher.Should().Be("The New York Times");
            updated.Thumbnail.Should().Be("https://example.com/daily600.jpg");
            updated.TotalEpisodes.Should().Be(2652);
            updated.EnrichedAt.Should().NotBeNull();
            updated.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_ItunesBackfill_NeverOverwritesPopulatedFields()
        {
            var series = CreateTestPodcastSeries("The Daily", applePodcastsId: "1200361736");
            series.Publisher = "My Publisher";
            series.RssFeedUrl = "https://feeds.example.com/mine";
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            MockItunesLookup("1200361736", new ItunesPodcastDto
            {
                ArtistName = "Apple Publisher",
                FeedUrl = "https://feeds.simplecast.com/thedaily",
                ArtworkUrl600 = "https://example.com/daily600.jpg"
            });

            await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.Publisher.Should().Be("My Publisher");
            updated.RssFeedUrl.Should().Be("https://feeds.example.com/mine");
            updated.Thumbnail.Should().Be("https://example.com/daily600.jpg");
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_WithoutApplePodcastsId_CountsNotFound_AndRecordsTheAttempt()
        {
            var series = CreateTestPodcastSeries("No Apple Id");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            _mockItunesLookupClient.ReceivedCalls().Should().BeEmpty();

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.EnrichedAt.Should().BeNull();
            updated.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_ItunesMiss_DoesNotStampEnrichedAt()
        {
            var series = CreateTestPodcastSeries("Totally Unknown Show", applePodcastsId: "9999999999");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            MockItunesLookup("9999999999", null);

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            Context.PodcastSeries.First(s => s.Id == series.Id).EnrichedAt.Should().BeNull();
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_ItunesError_CountsFailure_AndContinues()
        {
            var failing = CreateTestPodcastSeries("Podcast 1", applePodcastsId: "111");
            var working = CreateTestPodcastSeries("Podcast 2", applePodcastsId: "222");
            Context.PodcastSeries.AddRange(failing, working);
            await Context.SaveChangesAsync();

            _mockItunesLookupClient.GetPodcastByCollectionIdAsync("111", Arg.Any<CancellationToken>())
                .Returns<ItunesPodcastDto?>(_ => throw new HttpRequestException("simulated iTunes outage"));
            MockItunesLookup("222", new ItunesPodcastDto { FeedUrl = "https://feeds.example.com/two" });

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(2);
            result.FailedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().Contain("Podcast 1");
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_PicksNeverAttemptedSeriesBeforeRetries()
        {
            var retried = CreateTestPodcastSeries("Retried", lastAttemptAt: DateTime.UtcNow.AddDays(-1));
            retried.DateAdded = DateTime.UtcNow.AddYears(-1);
            var fresh = CreateTestPodcastSeries("Fresh");
            Context.PodcastSeries.AddRange(retried, fresh);
            await Context.SaveChangesAsync();
            var retriedAttempt = retried.LastEnrichmentAttemptAt;

            var result = await _service.EnrichPendingPodcastsAsync(batchSize: 1, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            Context.PodcastSeries.First(s => s.Id == fresh.Id).LastEnrichmentAttemptAt.Should().NotBeNull();
            Context.PodcastSeries.First(s => s.Id == retried.Id).LastEnrichmentAttemptAt.Should().Be(retriedAttempt);
        }

        [Fact]
        public async Task EnrichPendingPodcastsAsync_RespectsCancellation()
        {
            var series = CreateTestPodcastSeries("Test Podcast");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.EnrichPendingPodcastsAsync(cancellationToken: cts.Token);

            result.WasCancelled.Should().BeTrue();
        }

        #endregion
    }
}
