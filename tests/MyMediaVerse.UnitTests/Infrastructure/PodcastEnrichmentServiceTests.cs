using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class PodcastEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<IListenNotesApiClient> _mockListenNotesClient;
        private readonly Mock<ILogger<PodcastEnrichmentService>> _mockLogger;
        private readonly PodcastEnrichmentService _service;

        public PodcastEnrichmentServiceTests()
        {
            _mockListenNotesClient = new Mock<IListenNotesApiClient>();
            _mockLogger = new Mock<ILogger<PodcastEnrichmentService>>();
            _service = new PodcastEnrichmentService(Context, _mockListenNotesClient.Object, _mockLogger.Object);
        }

        private PodcastSeries CreateTestPodcastSeries(string title, string? externalId = null)
        {
            return new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = title,
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                ExternalId = externalId
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

            _mockListenNotesClient.Setup(c => c.SearchAsync(
                "The Daily", "podcast", It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(new SearchResultDto
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

            _mockListenNotesClient.Setup(c => c.GetPodcastByIdAsync("ln_daily_123", It.IsAny<string?>()))
                .ReturnsAsync(new PodcastSeriesDto
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

            _mockListenNotesClient.Setup(c => c.SearchAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(new SearchResultDto
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
            _mockListenNotesClient.Setup(c => c.SearchAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns<string, string?, int?, int?, int?, string?, string?, string?, string?, string?, string?, string?, string?, string?>(
                    (query, type, offset, lenMin, lenMax, genreIds, publishedBefore, publishedAfter, onlyIn, language, region, sortByDate, safeMode, uniquePodcasts) =>
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
    }
}
