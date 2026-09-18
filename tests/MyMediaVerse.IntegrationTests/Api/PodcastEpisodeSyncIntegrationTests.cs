using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.DTOs;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Episode sync, sync-all, the feed-episodes browser and single-episode import over HTTP against
    /// real Postgres. The feed reader is substituted in every test, so nothing reaches a feed host.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastEpisodeSyncIntegrationTests : IAsyncLifetime
    {
        private const string FeedUrl = "https://feeds.example.com/darknet.xml";
        private const string OtherFeedUrl = "https://feeds.example.com/other.xml";

        // One sync imports at most 50 episodes; a 60-item feed therefore leaves a backlog of 10.
        private const int FeedItemCount = 60;
        private const int MaxPerSync = 50;

        private static readonly DateTime NewestPublished = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastEpisodeSyncIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region helpers

        private Task<HttpResponseMessage> PostJson(HttpClient client, string url, object body) =>
            client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json"));

        private async Task<T> Read<T>(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), _jsonOptions)!;

        /// <summary>Item 1 is the newest; each later number is a day older, as a real feed is ordered.</summary>
        private static FeedEpisode Item(int number) => new()
        {
            Title = $"Episode {number}",
            Description = $"What happened in episode {number}.",
            EnclosureUrl = $"https://feeds.example.com/audio/{number}.mp3",
            EnclosureType = "audio/mpeg",
            PublishedAt = NewestPublished.AddDays(-(number - 1)),
            Guid = $"item-{number}",
            DurationSeconds = 1800 + number,
            EpisodeNumber = number,
            Link = $"https://darknetdiaries.com/episode/{number}/"
        };

        private static PodcastFeed Feed(int itemCount = FeedItemCount) => new()
        {
            Series = new FeedSeries
            {
                Title = "Darknet Diaries",
                Publisher = "Jack Rhysider",
                PodcastGuid = "ac631a54-a629-5367-a1a5-c2e2f7c25054",
                Categories = new[] { "Technology" }
            },
            Episodes = Enumerable.Range(1, itemCount).Select(Item).ToList(),
            TotalItemCount = itemCount
        };

        private (HttpClient Client, IPodcastFeedReader Reader) ClientReading(PodcastFeed feed) =>
            _factory.CreateClientWithSubstitute<IPodcastFeedReader>(r =>
                r.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(feed));

        private (HttpClient Client, IPodcastFeedReader Reader) ClientFailingToRead(PodcastFeedFailureReason reason, string message) =>
            _factory.CreateClientWithSubstitute<IPodcastFeedReader>(r =>
                r.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new PodcastFeedException(reason, message)));

        private async Task<PodcastSeriesResponseDto> CreateSeries(
            HttpClient client, string title, string? feedUrl = FeedUrl, bool subscribed = false)
        {
            var response = await PostJson(client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = title, RssFeedUrl = feedUrl });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var series = await Read<PodcastSeriesResponseDto>(response);

            if (subscribed)
            {
                (await client.PostAsync($"/api/podcast/series/{series.Id}/subscribe", null))
                    .IsSuccessStatusCode.Should().BeTrue();
            }

            return series;
        }

        private Task<HttpResponseMessage> Sync(HttpClient client, Guid seriesId) =>
            client.PostAsync($"/api/podcast/series/{seriesId}/sync", null);

        private async Task<List<PodcastEpisodeResponseDto>> StoredEpisodes(HttpClient client, Guid seriesId) =>
            await Read<List<PodcastEpisodeResponseDto>>(
                await client.GetAsync($"/api/podcast/series/{seriesId}/episodes"));

        private async Task<T> FromDatabase<T>(Func<MediaLibraryDbContext, Task<T>> query)
        {
            using var scope = _factory.Services.CreateScope();
            return await query(scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>());
        }

        #endregion

        #region series sync

        [Fact]
        public async Task SyncSeries_FirstSync_ImportsTheNewestEpisodesAndCountsTheRestAsBacklog()
        {
            var (client, _) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");

            var response = await Sync(client, series.Id);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await Read<PodcastEpisodeSyncResultDto>(response);
            result.Success.Should().BeTrue();
            result.Operation.Should().Be(PodcastEpisodeSyncResultDto.SyncOperation);
            result.SeriesId.Should().Be(series.Id);
            result.SeriesTitle.Should().Be("Darknet Diaries");
            result.CreatedCount.Should().Be(MaxPerSync);
            result.BacklogCount.Should().Be(FeedItemCount - MaxPerSync);
            result.SkippedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(0);
            result.IgnoredCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            result.FeedItemCount.Should().Be(FeedItemCount);
            result.TotalProcessed.Should().Be(MaxPerSync);
            result.ErrorMessage.Should().BeNull();
            result.WarningMessage.Should().BeNull();
            result.LastSyncDate.Should().NotBeNull();
            result.CompletedAt.Should().NotBeNull();
            result.ReindexTriggered.Should().BeTrue();

            var stored = await StoredEpisodes(client, series.Id);
            stored.Should().HaveCount(MaxPerSync);
            var titles = stored.Select(e => e.Title).ToList();
            titles.Should().Contain("Episode 1");
            titles.Should().Contain($"Episode {MaxPerSync}");
            titles.Should().NotContain($"Episode {MaxPerSync + 1}");
            stored.Should().AllSatisfy(e => e.SeriesId.Should().Be(series.Id));
        }

        [Fact]
        public async Task SyncSeries_RunAgainOnAnUnchangedFeed_CreatesNothing()
        {
            var (client, reader) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");
            await Sync(client, series.Id);

            var response = await Sync(client, series.Id);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await Read<PodcastEpisodeSyncResultDto>(response);
            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(MaxPerSync);
            // The older items stay in the backlog: they are not newer than what is already stored.
            result.BacklogCount.Should().Be(FeedItemCount - MaxPerSync);
            result.WarningMessage.Should().BeNull();
            result.ReindexTriggered.Should().BeFalse();

            (await StoredEpisodes(client, series.Id)).Should().HaveCount(MaxPerSync);
            await reader.Received(2).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SyncSeries_UnknownSeries_Returns404WithErrorObject()
        {
            var (client, reader) = ClientReading(Feed());

            var response = await Sync(client, Guid.NewGuid());

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
            reader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task SyncSeries_SeriesWithoutAFeedUrl_Returns400WithErrorObject()
        {
            var (client, reader) = ClientReading(Feed());
            var series = await CreateSeries(client, "Added By Hand", feedUrl: null);

            var response = await Sync(client, series.Id);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
            reader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task SyncSeries_FeedCannotBeRead_Returns500WithTheResultBody()
        {
            var (client, _) = ClientFailingToRead(
                PodcastFeedFailureReason.Unreachable, "The feed's server could not be reached.");
            var series = await CreateSeries(client, "Dead Feed");

            var response = await Sync(client, series.Id);

            // A feed host being down is reported in the body, not as an exception page.
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var result = await Read<PodcastEpisodeSyncResultDto>(response);
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("The feed's server could not be reached.");
            result.CreatedCount.Should().Be(0);
            result.ReindexTriggered.Should().BeFalse();
            (await StoredEpisodes(client, series.Id)).Should().BeEmpty();
        }

        [Fact]
        public async Task SyncSeries_Anonymous_Returns401()
        {
            var seeded = await CreateSeries(_factory.CreateClient(), "Darknet Diaries");

            var response = await _factory.CreateAnonymousClient()
                .PostAsync($"/api/podcast/series/{seeded.Id}/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region sync-all

        [Fact]
        public async Task SyncAll_SyncsOnlySubscribedSeriesThatHaveAFeed()
        {
            var (client, _) = ClientReading(Feed());
            var subscribed = await CreateSeries(client, "Subscribed With Feed", FeedUrl, subscribed: true);
            var noFeed = await CreateSeries(client, "Subscribed Without Feed", feedUrl: null, subscribed: true);
            var unsubscribed = await CreateSeries(client, "Unsubscribed With Feed", OtherFeedUrl);

            var response = await client.PostAsync("/api/podcast/series/sync-all", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await Read<PodcastSyncAllResultDto>(response);
            result.Success.Should().BeTrue();
            result.Operation.Should().Be(PodcastSyncAllResultDto.SyncAllOperation);
            result.SeriesChecked.Should().Be(1);
            result.SeriesSucceeded.Should().Be(1);
            result.SeriesFailed.Should().Be(0);
            result.PendingSeriesCount.Should().Be(0);
            result.CreatedCount.Should().Be(MaxPerSync);
            result.WasCancelled.Should().BeFalse();
            result.TimeBudgetReached.Should().BeFalse();
            result.WarningMessage.Should().BeNull();
            result.Errors.Should().BeEmpty();
            result.ReindexTriggered.Should().BeTrue();

            result.Series.Should().HaveCount(1);
            result.Series[0].SeriesId.Should().Be(subscribed.Id);
            result.Series[0].SeriesTitle.Should().Be("Subscribed With Feed");
            result.Series[0].Success.Should().BeTrue();
            result.Series[0].CreatedCount.Should().Be(MaxPerSync);

            (await StoredEpisodes(client, noFeed.Id)).Should().BeEmpty();
            (await StoredEpisodes(client, unsubscribed.Id)).Should().BeEmpty();
        }

        [Fact]
        public async Task SyncAll_CleanRun_RecordsItsSuccessfulSync()
        {
            var (client, _) = ClientReading(Feed());
            await CreateSeries(client, "Subscribed With Feed", FeedUrl, subscribed: true);

            var response = await client.PostAsync("/api/podcast/series/sync-all", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var state = await FromDatabase(context => context.SyncStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == PodcastSyncAllResultDto.SyncAllOperation));
            state.Should().NotBeNull();
            state!.LastSuccessfulSyncAt.Should().NotBeNull();
        }

        [Fact]
        public async Task SyncAll_Anonymous_Returns401()
        {
            var response = await _factory.CreateAnonymousClient().PostAsync("/api/podcast/series/sync-all", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region feed-episodes browser

        [Fact]
        public async Task FeedEpisodes_PagesTheFeedAndMarksItemsAlreadyStored()
        {
            var (client, _) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");
            await Sync(client, series.Id);

            var firstPage = await Read<PodcastFeedEpisodesPageDto>(
                await client.GetAsync($"/api/podcast/series/{series.Id}/feed-episodes?offset=0&limit=5"));

            firstPage.SeriesId.Should().Be(series.Id);
            firstPage.FeedItemCount.Should().Be(FeedItemCount);
            firstPage.Offset.Should().Be(0);
            firstPage.Limit.Should().Be(5);
            firstPage.Items.Should().HaveCount(5);
            firstPage.Items[0].Title.Should().Be("Episode 1");
            firstPage.Items[0].Guid.Should().Be("item-1");
            firstPage.Items[0].AudioUrl.Should().Be("https://feeds.example.com/audio/1.mp3");
            // Everything on the first page was imported by the sync above.
            firstPage.Items.Should().AllSatisfy(item =>
            {
                item.Importable.Should().BeTrue();
                item.ExistingEpisodeId.Should().NotBeNull();
            });

            var backlogPage = await Read<PodcastFeedEpisodesPageDto>(
                await client.GetAsync($"/api/podcast/series/{series.Id}/feed-episodes?offset={MaxPerSync}&limit=10"));

            backlogPage.Offset.Should().Be(MaxPerSync);
            backlogPage.Items.Should().HaveCount(FeedItemCount - MaxPerSync);
            backlogPage.Items[0].Title.Should().Be($"Episode {MaxPerSync + 1}");
            backlogPage.Items.Should().AllSatisfy(item =>
            {
                item.Importable.Should().BeTrue();
                item.ExistingEpisodeId.Should().BeNull();
            });
        }

        [Fact]
        public async Task FeedEpisodes_LimitAboveTheMaximum_IsClampedToThePageSize()
        {
            var (client, _) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");

            var page = await Read<PodcastFeedEpisodesPageDto>(
                await client.GetAsync($"/api/podcast/series/{series.Id}/feed-episodes?limit=500"));

            page.Limit.Should().Be(100);
            page.Items.Should().HaveCount(FeedItemCount);
        }

        [Fact]
        public async Task FeedEpisodes_FeedCannotBeRead_Returns502()
        {
            var (client, _) = ClientFailingToRead(
                PodcastFeedFailureReason.InvalidXml, "The feed could not be read as XML.");
            var series = await CreateSeries(client, "Broken Feed");

            var response = await client.GetAsync($"/api/podcast/series/{series.Id}/feed-episodes");

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task FeedEpisodes_UnknownSeries_Returns404()
        {
            var (client, _) = ClientReading(Feed());

            var response = await client.GetAsync($"/api/podcast/series/{Guid.NewGuid()}/feed-episodes");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        #endregion

        #region single-episode import

        [Fact]
        public async Task ImportEpisodeFromFeed_BacklogItem_Returns201ThenTheSameEpisodeWith200()
        {
            var (client, _) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");
            await Sync(client, series.Id);
            var backlogGuid = $"item-{FeedItemCount}";

            var first = await PostJson(client, "/api/podcast/episodes/from-feed",
                new ImportPodcastEpisodeFromFeedDto { SeriesId = series.Id, Guid = backlogGuid });

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await Read<PodcastEpisodeResponseDto>(first);
            created.Title.Should().Be($"Episode {FeedItemCount}");
            created.SeriesId.Should().Be(series.Id);
            created.RssGuid.Should().Be(backlogGuid);
            created.AudioLink.Should().Be($"https://feeds.example.com/audio/{FeedItemCount}.mp3");

            var second = await PostJson(client, "/api/podcast/episodes/from-feed",
                new ImportPodcastEpisodeFromFeedDto { SeriesId = series.Id, Guid = backlogGuid });

            second.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Read<PodcastEpisodeResponseDto>(second)).Id.Should().Be(created.Id);
            (await StoredEpisodes(client, series.Id)).Should().HaveCount(MaxPerSync + 1);
        }

        [Fact]
        public async Task ImportEpisodeFromFeed_ItemNotInTheFeed_Returns404()
        {
            var (client, _) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");

            var response = await PostJson(client, "/api/podcast/episodes/from-feed",
                new ImportPodcastEpisodeFromFeedDto { SeriesId = series.Id, Guid = "not-in-this-feed" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task ImportEpisodeFromFeed_WithoutASeriesId_Returns400()
        {
            var (client, reader) = ClientReading(Feed());

            var response = await PostJson(client, "/api/podcast/episodes/from-feed",
                new ImportPodcastEpisodeFromFeedDto { Guid = "item-1" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
            reader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ImportEpisodeFromFeed_Anonymous_Returns401()
        {
            var seeded = await CreateSeries(_factory.CreateClient(), "Darknet Diaries");

            var response = await PostJson(_factory.CreateAnonymousClient(), "/api/podcast/episodes/from-feed",
                new ImportPodcastEpisodeFromFeedDto { SeriesId = seeded.Id, Guid = "item-1" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        [Fact]
        public async Task SyncedEpisodes_AppearInAllMedia_AndAreRemovedWithTheSeries()
        {
            var (client, _) = ClientReading(Feed());
            var series = await CreateSeries(client, "Darknet Diaries");
            await Sync(client, series.Id);
            var stored = await StoredEpisodes(client, series.Id);

            var allMedia = await Read<List<MediaItemResponseDto>>(await client.GetAsync("/api/media"));

            allMedia.Select(m => m.Id).Should().Contain(stored.Select(e => e.Id));
            allMedia.Select(m => m.Id).Should().Contain(series.Id);

            var delete = await client.DeleteAsync($"/api/podcast/series/{series.Id}");

            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var afterDelete = await client.GetAsync("/api/media");
            afterDelete.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Read<List<MediaItemResponseDto>>(afterDelete)).Should().BeEmpty();
        }
    }
}
