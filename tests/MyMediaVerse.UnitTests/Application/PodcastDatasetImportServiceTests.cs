using System.Text;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    /// <summary>
    /// The importer runs against the real podcast service on an in-memory database, so the mapping
    /// and the duplicate handling are checked together. Nothing here reaches Listen Notes, Apple or a feed.
    /// </summary>
    [Trait("Category", "Unit")]
    public class PodcastDatasetImportServiceTests : InMemoryDbTestBase
    {
        private const string PodcastsHeader = "title,publisher,description,website,artwork_thumbnail,explicit";

        private const string EpisodesHeader =
            "title,podcast_title,publisher,description,audio,pub_date,audio_length_seconds,link,website," +
            "artwork_thumbnail,podcast_artwork_thumbnail,explicit,rss,itunes_id,listennotes_id,listennotes_podcast_id";

        private readonly PodcastService _podcastService;
        private readonly PodcastDatasetImportService _service;

        public PodcastDatasetImportServiceTests()
        {
            _podcastService = new PodcastService(
                Context, Substitute.For<ITypesenseService>(), Substitute.For<ILogger<PodcastService>>());
            _service = new PodcastDatasetImportService(
                _podcastService, Substitute.For<ILogger<PodcastDatasetImportService>>());
        }

        private static Stream Csv(params string[] lines) =>
            new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines)));

        private Task<List<PodcastSeries>> AllSeries() => Context.PodcastSeries.AsNoTracking().ToListAsync();

        private static string EpisodeRow(
            string title = "Episode One",
            string podcastTitle = "Weird Studies",
            string rss = "https://feeds.example.com/weird.xml",
            string itunesId = "1343819177",
            string listennotesId = "ep-1") =>
            $"{title},{podcastTitle},SpectreVision Radio,<p>About it</p>,https://audio.example.com/{listennotesId}.mp3," +
            $"2025-09-07T07:29:00.028000-07:00,120,https://example.com/{listennotesId},https://www.weirdstudies.com," +
            $"https://cdn.example.com/ep.jpg,https://cdn.example.com/show.jpg,yes,{rss},{itunesId},{listennotesId},show-1";

        #region podcasts file

        [Fact]
        public async Task PodcastsFile_CreatesUnsubscribedDatasetStubs()
        {
            var result = await _service.ImportFromCsvAsync(Csv(
                PodcastsHeader,
                "The Apocalypse Players,The Apocalypse Players,A Call of Cthulhu actual play podcast.,https://www.apocalypseplayers.com,https://cdn.example.com/ap.jpg,yes"));

            var report = result.Report;
            report.Success.Should().BeTrue();
            report.Operation.Should().Be(PodcastDatasetImportResultDto.DatasetImportOperation);
            report.FileKind.Should().Be(PodcastDatasetImportResultDto.PodcastsFile);
            report.TotalProcessed.Should().Be(1);
            report.CreatedCount.Should().Be(1);
            report.SeriesCreatedCount.Should().Be(1);
            report.EpisodesCreatedCount.Should().Be(0);
            result.ChangedIds.Should().ContainSingle();

            var stored = (await AllSeries()).Single();
            stored.Title.Should().Be("The Apocalypse Players");
            stored.Publisher.Should().Be("The Apocalypse Players");
            stored.Description.Should().Be("A Call of Cthulhu actual play podcast.");
            stored.Link.Should().Be("https://www.apocalypseplayers.com");
            stored.Thumbnail.Should().Be("https://cdn.example.com/ap.jpg");
            stored.MetadataSource.Should().Be(PodcastMetadataSources.ListenNotesDataset);
            stored.IsSubscribed.Should().BeFalse();
            stored.Status.Should().Be(Status.Uncharted);
            // No identity in this export: stored as NULL, never "", so the unique indexes stay happy.
            stored.RssFeedUrl.Should().BeNull();
            stored.FeedUrlKey.Should().BeNull();
            stored.ApplePodcastsId.Should().BeNull();
            stored.ExternalId.Should().BeNull();
            stored.EnrichedAt.Should().BeNull();
        }

        [Fact]
        public async Task PodcastsFile_MultilineQuotedDescription_IsReadAsOneRow()
        {
            var report = (await _service.ImportFromCsvAsync(Csv(
                PodcastsHeader,
                "The Overwhelmed Brain,Paul Colaianni,\"First paragraph.",
                "",
                "Second \"\"quoted\"\" paragraph.\",http://sites.libsyn.com/49002/site,https://cdn.example.com/ob.jpg,no"))).Report;

            report.TotalProcessed.Should().Be(1);
            (await AllSeries()).Single().Description.Should().Be("First paragraph.\n\nSecond \"quoted\" paragraph.");
        }

        [Fact]
        public async Task PodcastsFile_FullExportColumns_AreMapped()
        {
            await _service.ImportFromCsvAsync(Csv(
                "title,publisher,artwork_thumbnail,artwork_image,language,genres,rss,itunes_id,listennotes_id",
                "Weird Studies,SpectreVision Radio,https://cdn.example.com/small.jpg,https://cdn.example.com/big.jpg,English,\"Philosophy, Society & Culture,philosophy\",https://feeds.example.com/weird.xml,1343819177,ln-123"));

            var stored = await Context.PodcastSeries.Include(s => s.Genres).SingleAsync();
            stored.Thumbnail.Should().Be("https://cdn.example.com/big.jpg");
            stored.Language.Should().Be("English");
            stored.RssFeedUrl.Should().Be("https://feeds.example.com/weird.xml");
            stored.ApplePodcastsId.Should().Be("1343819177");
            stored.ExternalId.Should().Be("ln-123");
            stored.Genres.Select(g => g.Name).Should().BeEquivalentTo("philosophy", "society & culture");
        }

        [Fact]
        public async Task PodcastsFile_SecondUpload_SkipsEveryRow()
        {
            string[] file =
            {
                PodcastsHeader,
                "Show A,Publisher A,,,,no",
                "Show B,Publisher B,,,,no"
            };
            await _service.ImportFromCsvAsync(Csv(file));

            var second = await _service.ImportFromCsvAsync(Csv(file));

            second.Report.CreatedCount.Should().Be(0);
            second.Report.SkippedCount.Should().Be(2);
            second.ChangedIds.Should().BeEmpty();
            (await AllSeries()).Should().HaveCount(2);
        }

        [Fact]
        public async Task PodcastsFile_ShowAlreadyStoredUnderTheFeedsPublisher_IsStillSkipped()
        {
            // After enrichment the feed's publisher replaces the dataset's, so title + publisher no
            // longer matches. The title alone must still stop a re-upload from adding it twice.
            await _podcastService.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto
            {
                Title = "The Apocalypse Players",
                Publisher = "Apocalypse Players Ltd",
                RssFeedUrl = "https://www.apocalypseplayers.com/feed.xml"
            }, PodcastMetadataSources.Rss);

            var report = (await _service.ImportFromCsvAsync(Csv(
                PodcastsHeader,
                "the apocalypse players,The Apocalypse Players,,,,yes"))).Report;

            report.SkippedCount.Should().Be(1);
            (await AllSeries()).Should().ContainSingle();
        }

        [Fact]
        public async Task PodcastsFile_SameShowTwiceInTheFile_IsAddedOnce()
        {
            var report = (await _service.ImportFromCsvAsync(Csv(
                PodcastsHeader,
                "Show A,Publisher A,,,,no",
                "Show A,Publisher A,,,,no"))).Report;

            report.CreatedCount.Should().Be(1);
            report.SkippedCount.Should().Be(1);
            (await AllSeries()).Should().ContainSingle();
        }

        [Fact]
        public async Task PodcastsFile_BlankTitle_IsSkipped()
        {
            var report = (await _service.ImportFromCsvAsync(Csv(
                PodcastsHeader,
                ",Publisher A,,,,no"))).Report;

            report.SkippedCount.Should().Be(1);
            (await AllSeries()).Should().BeEmpty();
        }

        [Fact]
        public async Task PodcastsFile_BadFeedUrl_FailsThatRowOnly()
        {
            var report = (await _service.ImportFromCsvAsync(Csv(
                "title,publisher,rss",
                "Broken Show,Publisher A,not a url",
                "Good Show,Publisher B,https://feeds.example.com/good.xml"))).Report;

            report.CreatedCount.Should().Be(1);
            report.FailedCount.Should().Be(1);
            report.Failures.Should().ContainSingle(f => f.Title == "Broken Show");
            report.WarningMessage.Should().NotBeNull();
            (await AllSeries()).Single().Title.Should().Be("Good Show");
        }

        #endregion

        #region episodes file

        [Fact]
        public async Task EpisodesFile_CreatesTheShowAndTheEpisode()
        {
            var result = await _service.ImportFromCsvAsync(Csv(EpisodesHeader, EpisodeRow()));

            var report = result.Report;
            report.FileKind.Should().Be(PodcastDatasetImportResultDto.EpisodesFile);
            report.CreatedCount.Should().Be(1);
            report.SeriesCreatedCount.Should().Be(1);
            report.EpisodesCreatedCount.Should().Be(1);
            result.ChangedIds.Should().HaveCount(2);

            var series = (await AllSeries()).Single();
            series.Title.Should().Be("Weird Studies");
            series.RssFeedUrl.Should().Be("https://feeds.example.com/weird.xml");
            series.ApplePodcastsId.Should().Be("1343819177");
            series.ExternalId.Should().Be("show-1");
            series.Thumbnail.Should().Be("https://cdn.example.com/show.jpg");
            series.MetadataSource.Should().Be(PodcastMetadataSources.ListenNotesDataset);
            series.IsSubscribed.Should().BeFalse();

            var episode = await Context.PodcastEpisodes.AsNoTracking().SingleAsync();
            episode.SeriesId.Should().Be(series.Id);
            episode.Title.Should().Be("Episode One");
            episode.AudioLink.Should().Be("https://audio.example.com/ep-1.mp3");
            episode.ReleaseDate.Should().Be(new DateTime(2025, 9, 7, 14, 29, 0, 28, DateTimeKind.Utc));
            episode.DurationInSeconds.Should().Be(120);
            episode.Link.Should().Be("https://example.com/ep-1");
            episode.ExternalId.Should().Be("ep-1");
        }

        [Fact]
        public async Task EpisodesFile_TwoEpisodesOfOneShow_ShareOneShow()
        {
            var report = (await _service.ImportFromCsvAsync(Csv(
                EpisodesHeader,
                EpisodeRow("Episode One", listennotesId: "ep-1"),
                EpisodeRow("Episode Two", listennotesId: "ep-2")))).Report;

            report.SeriesCreatedCount.Should().Be(1);
            report.EpisodesCreatedCount.Should().Be(2);
            (await AllSeries()).Should().ContainSingle();
        }

        [Fact]
        public async Task EpisodesFile_ShowAlreadyStored_AttachesTheEpisodeToIt()
        {
            var existing = (await _podcastService.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto
            {
                Title = "Weird Studies",
                RssFeedUrl = "https://feeds.example.com/weird.xml"
            }, PodcastMetadataSources.Rss)).Series;

            var report = (await _service.ImportFromCsvAsync(Csv(EpisodesHeader, EpisodeRow()))).Report;

            report.SeriesCreatedCount.Should().Be(0);
            report.EpisodesCreatedCount.Should().Be(1);
            (await AllSeries()).Should().ContainSingle();
            (await Context.PodcastEpisodes.SingleAsync()).SeriesId.Should().Be(existing.Id);
        }

        [Fact]
        public async Task EpisodesFile_SecondUpload_SkipsTheEpisode()
        {
            await _service.ImportFromCsvAsync(Csv(EpisodesHeader, EpisodeRow()));

            var second = (await _service.ImportFromCsvAsync(Csv(EpisodesHeader, EpisodeRow()))).Report;

            second.CreatedCount.Should().Be(0);
            second.SkippedCount.Should().Be(1);
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(1);
        }

        #endregion

        #region unusable files

        [Fact]
        public async Task FileWithoutATitleColumn_IsRejected()
        {
            var report = (await _service.ImportFromCsvAsync(Csv("name,url", "Something,https://example.com"))).Report;

            report.Success.Should().BeFalse();
            report.ErrorMessage.Should().Contain("title");
        }

        [Fact]
        public async Task EmptyFile_IsRejected()
        {
            var report = (await _service.ImportFromCsvAsync(Csv())).Report;

            report.Success.Should().BeFalse();
        }

        #endregion
    }
}
