using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class PodcastEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly IListenNotesApiClient _mockListenNotesClient;
        private readonly IItunesLookupClient _mockItunesLookupClient;
        private readonly ILogger<PodcastEnrichmentService> _mockLogger;
        private readonly PodcastEnrichmentService _service;

        public PodcastEnrichmentServiceTests()
        {
            _mockListenNotesClient = Substitute.For<IListenNotesApiClient>();
            _mockItunesLookupClient = Substitute.For<IItunesLookupClient>();
            _mockLogger = Substitute.For<ILogger<PodcastEnrichmentService>>();
            _service = new PodcastEnrichmentService(
                Context, _mockListenNotesClient, _mockItunesLookupClient, _mockLogger);
        }

        private void MockItunesLookup(string collectionId, ItunesPodcastDto? result) =>
            _mockItunesLookupClient.GetPodcastByCollectionIdAsync(collectionId, Arg.Any<CancellationToken>())
                .Returns(result);

        private PodcastSeries CreateTestPodcastSeries(
            string title,
            string? externalId = null,
            string? applePodcastsId = null,
            string? rssFeedUrl = null)
        {
            return new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = title,
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                ExternalId = externalId,
                ApplePodcastsId = applePodcastsId,
                RssFeedUrl = rssFeedUrl
            };
        }

        private void MockSearch(SearchResultDto result) =>
            _mockListenNotesClient.SearchAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(result);

        #region GetPodcastsNeedingEnrichmentCountAsync

        [Fact]
        public async Task GetPodcastsNeedingEnrichmentCountAsync_NoPodcasts_ReturnsZero()
        {
            var result = await _service.GetPodcastsNeedingEnrichmentCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetPodcastsNeedingEnrichmentCountAsync_PodcastsWithoutExternalId_ReturnsCount()
        {
            var seriesNeedsEnrichment = CreateTestPodcastSeries("Podcast 1");
            var seriesAlreadyEnriched = CreateTestPodcastSeries("Podcast 2", "abc123");

            Context.PodcastSeries.AddRange(seriesNeedsEnrichment, seriesAlreadyEnriched);
            await Context.SaveChangesAsync();

            var result = await _service.GetPodcastsNeedingEnrichmentCountAsync();

            result.Should().Be(1);
        }

        #endregion

        #region EnrichPodcastsWithoutListenNotesDataAsync

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_NoPodcastsNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync();

            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_WithPodcast_SearchesAndEnriches()
        {
            var series = CreateTestPodcastSeries("The Daily");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            _mockListenNotesClient.SearchAsync(
                "The Daily", "podcast", Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(new SearchResultDto
                {
                    Count = 1,
                    Total = 1,
                    Results = new List<PodcastSearchDto>
                    {
                        new PodcastSearchDto
                        {
                            Id = "ln_daily_123",
                            TitleOriginal = "The Daily",
                            PublisherOriginal = "The New York Times",
                            DescriptionOriginal = "This is what the news should sound like.",
                            Thumbnail = "https://example.com/daily.jpg",
                            TotalEpisodes = 2000
                        }
                    }
                });

            _mockListenNotesClient.GetPodcastByIdAsync("ln_daily_123", Arg.Any<string?>())
                .Returns(new PodcastSeriesDto
                {
                    Id = "ln_daily_123",
                    Title = "The Daily",
                    Publisher = "The New York Times",
                    Description = "This is what the news should sound like.",
                    Image = "https://example.com/daily_full.jpg",
                    Thumbnail = "https://example.com/daily.jpg",
                    Episodes = new List<PodcastEpisodeDto>()
                });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);

            var updatedSeries = Context.PodcastSeries.First(s => s.Id == series.Id);
            updatedSeries.ExternalId.Should().Be("ln_daily_123");
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_NoSearchResults_IncrementsNotFound()
        {
            var series = CreateTestPodcastSeries("Unknown Podcast XYZ");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            _mockListenNotesClient.SearchAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(new SearchResultDto
                {
                    Count = 0,
                    Total = 0,
                    Results = new List<PodcastSearchDto>()
                });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_RespectsCancellation()
        {
            var series = CreateTestPodcastSeries("Test Podcast");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(cancellationToken: cts.Token);

            result.WasCancelled.Should().BeTrue();
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_ApiError_ContinuesProcessing()
        {
            var series1 = CreateTestPodcastSeries("Podcast 1");
            var series2 = CreateTestPodcastSeries("Podcast 2");

            Context.PodcastSeries.AddRange(series1, series2);
            await Context.SaveChangesAsync();

            var callCount = 0;
            _mockListenNotesClient.SearchAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(callInfo =>
                {
                    callCount++;
                    if (callCount == 1) throw new Exception("API error");
                    return Task.FromResult(new SearchResultDto
                    {
                        Count = 0,
                        Total = 0,
                        Results = new List<PodcastSearchDto>()
                    });
                });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(2);
            result.Errors.Should().NotBeEmpty();
        }

        #endregion

        #region Match-key precedence (RAS-187)

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_WithApplePodcastsId_BackfillsRssFromItunes_ThenMatchesBySearch()
        {
            // An OPML stub with only an Apple id (no RSS) — iTunes lookup resolves the feed url,
            // which then disambiguates the ListenNotes search to the correct show.
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

            MockSearch(new SearchResultDto
            {
                Count = 2,
                Total = 2,
                Results = new List<PodcastSearchDto>
                {
                    // Wrong show first — only the RSS feed url (from iTunes) distinguishes them.
                    new PodcastSearchDto { Id = "ln_wrong", TitleOriginal = "The Daily", Rss = "https://feeds.simplecast.com/other" },
                    new PodcastSearchDto { Id = "ln_daily", TitleOriginal = "The Daily", Rss = "https://feeds.simplecast.com/thedaily" }
                }
            });

            _mockListenNotesClient.GetPodcastByIdAsync("ln_daily", Arg.Any<string?>())
                .Returns(new PodcastSeriesDto { Id = "ln_daily", Title = "The Daily" });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.ExternalId.Should().Be("ln_daily");
            // RSS feed url was backfilled from the iTunes lookup.
            updated.RssFeedUrl.Should().Be("https://feeds.simplecast.com/thedaily");

            await _mockItunesLookupClient.Received(1)
                .GetPodcastByCollectionIdAsync("1200361736", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_WithRssFeedUrl_DisambiguatesTitleSearchByFeed()
        {
            // Two podcasts share the title "The Daily"; only the RSS feed url distinguishes them.
            var series = CreateTestPodcastSeries("The Daily", rssFeedUrl: "https://feeds.example.com/thedaily");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            MockSearch(new SearchResultDto
            {
                Count = 2,
                Total = 2,
                Results = new List<PodcastSearchDto>
                {
                    // Wrong show listed first — first-result-wins would pick this one.
                    new PodcastSearchDto
                    {
                        Id = "ln_wrong",
                        TitleOriginal = "The Daily",
                        Rss = "https://feeds.example.com/some-other-daily"
                    },
                    new PodcastSearchDto
                    {
                        Id = "ln_right",
                        TitleOriginal = "The Daily",
                        Rss = "https://feeds.example.com/thedaily/"
                    }
                }
            });

            _mockListenNotesClient.GetPodcastByIdAsync("ln_right", Arg.Any<string?>())
                .Returns(new PodcastSeriesDto { Id = "ln_right", Title = "The Daily" });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.ExternalId.Should().Be("ln_right");
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_RssFeedUrlNotAmongResults_FallsBackToTitleMatch()
        {
            // RSS present but no result matches it → fall back to the title/publisher heuristic.
            var series = CreateTestPodcastSeries("The Daily", rssFeedUrl: "https://feeds.example.com/unindexed");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            MockSearch(new SearchResultDto
            {
                Count = 1,
                Total = 1,
                Results = new List<PodcastSearchDto>
                {
                    new PodcastSearchDto
                    {
                        Id = "ln_title_match",
                        TitleOriginal = "The Daily",
                        Rss = "https://feeds.example.com/thedaily"
                    }
                }
            });

            _mockListenNotesClient.GetPodcastByIdAsync("ln_title_match", Arg.Any<string?>())
                .Returns(new PodcastSeriesDto { Id = "ln_title_match", Title = "The Daily" });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.ExternalId.Should().Be("ln_title_match");
        }

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_NoMatchByAnyKey_LeavesStubUntouched()
        {
            var series = CreateTestPodcastSeries("Totally Unknown Show", applePodcastsId: "9999999999");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            // Apple iTunes lookup misses...
            MockItunesLookup("9999999999", null);
            // ...and the title search also comes up empty.
            MockSearch(new SearchResultDto { Count = 0, Total = 0, Results = new List<PodcastSearchDto>() });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.ExternalId.Should().BeNull();
        }

        #endregion

        #region Apple id lookup resilience

        [Fact]
        public async Task EnrichPodcastsWithoutListenNotesDataAsync_ItunesLookupThrows_FallsBackToTitleSearch()
        {
            // If the Apple iTunes lookup fails for any reason, enrichment must degrade to the
            // plain title search — not fail the whole podcast.
            var series = CreateTestPodcastSeries("The Daily", applePodcastsId: "1200361736");
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();

            _mockItunesLookupClient.GetPodcastByCollectionIdAsync("1200361736", Arg.Any<CancellationToken>())
                .Returns<ItunesPodcastDto?>(_ => throw new HttpRequestException("simulated iTunes outage"));

            MockSearch(new SearchResultDto
            {
                Count = 1,
                Total = 1,
                Results = new List<PodcastSearchDto>
                {
                    new PodcastSearchDto { Id = "ln_via_search", TitleOriginal = "The Daily" }
                }
            });

            _mockListenNotesClient.GetPodcastByIdAsync("ln_via_search", Arg.Any<string?>())
                .Returns(new PodcastSeriesDto { Id = "ln_via_search", Title = "The Daily" });

            var result = await _service.EnrichPodcastsWithoutListenNotesDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            // Enriched via the fallback path, and not counted as a failure.
            result.EnrichedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);

            var updated = Context.PodcastSeries.First(s => s.Id == series.Id);
            updated.ExternalId.Should().Be("ln_via_search");
        }

        #endregion
    }
}
