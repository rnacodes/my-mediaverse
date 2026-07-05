using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastOpmlImportServiceTests
    {
        private readonly IPodcastService _podcastService = Substitute.For<IPodcastService>();
        private readonly ILogger<PodcastOpmlImportService> _logger =
            Substitute.For<ILogger<PodcastOpmlImportService>>();

        // Captures the DTOs handed to CreatePodcastSeriesAsync so tests can assert the OPML→stub mapping.
        private readonly List<CreatePodcastSeriesDto> _created = new();
        private readonly PodcastOpmlImportService _service;

        public PodcastOpmlImportServiceTests()
        {
            // No existing series by default.
            _podcastService.GetAllPodcastSeriesAsync()
                .Returns(Task.FromResult<IEnumerable<PodcastSeries>>(new List<PodcastSeries>()));

            // Record each create and echo back a stub, mirroring what the real service would persist.
            _podcastService.CreatePodcastSeriesAsync(Arg.Any<CreatePodcastSeriesDto>())
                .Returns(ci =>
                {
                    var dto = ci.Arg<CreatePodcastSeriesDto>();
                    _created.Add(dto);
                    return Task.FromResult(new PodcastSeries
                    {
                        Title = dto.Title,
                        RssFeedUrl = dto.RssFeedUrl,
                        ApplePodcastsId = dto.ApplePodcastsId,
                        IsSubscribed = dto.IsSubscribed,
                        Status = dto.Status
                    });
                });

            _service = new PodcastOpmlImportService(_podcastService, _logger);
        }

        private void SeedExisting(params PodcastSeries[] existing) =>
            _podcastService.GetAllPodcastSeriesAsync()
                .Returns(Task.FromResult<IEnumerable<PodcastSeries>>(existing.ToList()));

        #region Parse / mapping

        [Fact]
        public async Task ImportFromOpmlAsync_MapsOpmlAttributesToStubFields()
        {
            var opml = Opml(Feed("The Talk Show", "https://daringfireball.net/thetalkshow/rss", "528458508"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(1);
            result.Imported.Should().Be(1);
            result.Skipped.Should().Be(0);
            result.Failed.Should().Be(0);

            _created.Should().ContainSingle();
            var dto = _created.Single();
            dto.Title.Should().Be("The Talk Show");
            dto.RssFeedUrl.Should().Be("https://daringfireball.net/thetalkshow/rss");
            dto.ApplePodcastsId.Should().Be("528458508");
            dto.IsSubscribed.Should().BeTrue();
            dto.Status.Should().Be(Status.Uncharted);
        }

        [Fact]
        public async Task ImportFromOpmlAsync_FeedWithoutAppleId_ImportsWithNullAppleId()
        {
            var opml = Opml(Feed("No Apple Id", "https://feeds.example.com/x", appleId: null));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Imported.Should().Be(1);
            _created.Single().ApplePodcastsId.Should().BeNull();
            _created.Single().RssFeedUrl.Should().Be("https://feeds.example.com/x");
        }

        [Fact]
        public async Task ImportFromOpmlAsync_DecodesXmlEntitiesInTitle()
        {
            // Overcast exports escape apostrophes as &apos; — the XML parser must decode them.
            var opml = Opml(Feed("I&apos;d Rather Be Writing Podcast", "https://idratherbewriting.com/itunes.rss", "277365275"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Imported.Should().Be(1);
            _created.Single().Title.Should().Be("I'd Rather Be Writing Podcast");
        }

        [Fact]
        public async Task ImportFromOpmlAsync_IgnoresWrapperOutlineWithoutRssType()
        {
            // The outer <outline text="feeds"> wrapper has no type="rss" and must not be treated as a feed.
            var opml = Opml(Feed("Only Real Feed", "https://feeds.example.com/only"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(1);
            result.Imported.Should().Be(1);
        }

        #endregion

        #region Accounting

        [Fact]
        public async Task ImportFromOpmlAsync_MultipleFeeds_ImportsAllAndCountsMatch()
        {
            var opml = Opml(
                Feed("Feed A", "https://feeds.example.com/a", "1"),
                Feed("Feed B", "https://feeds.example.com/b", "2"),
                Feed("Feed C", "https://feeds.example.com/c", "3"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(3);
            result.Imported.Should().Be(3);
            result.Skipped.Should().Be(0);
            result.Failed.Should().Be(0);
            (result.Imported + result.Skipped + result.Failed).Should().Be(result.Total);
            await _podcastService.Received(3).CreatePodcastSeriesAsync(Arg.Any<CreatePodcastSeriesDto>());
        }

        [Fact]
        public async Task ImportFromOpmlAsync_BlankTitle_SkipsAndDoesNotCreate()
        {
            var opml = Opml(Feed("", "https://feeds.example.com/blank"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(1);
            result.Skipped.Should().Be(1);
            result.Imported.Should().Be(0);
            await _podcastService.DidNotReceive().CreatePodcastSeriesAsync(Arg.Any<CreatePodcastSeriesDto>());
        }

        #endregion

        #region Dedup

        [Fact]
        public async Task ImportFromOpmlAsync_ExistingFeedUrl_Skips()
        {
            SeedExisting(new PodcastSeries
            {
                Title = "Different Title",
                RssFeedUrl = "https://feeds.example.com/dup"
            });

            var opml = Opml(Feed("Some Podcast", "https://feeds.example.com/dup", "999"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Skipped.Should().Be(1);
            result.Imported.Should().Be(0);
            await _podcastService.DidNotReceive().CreatePodcastSeriesAsync(Arg.Any<CreatePodcastSeriesDto>());
        }

        [Fact]
        public async Task ImportFromOpmlAsync_ExistingTitle_Skips_WhenNoFeedUrlMatch()
        {
            // Existing series has a matching (case-insensitive) title but a different/absent feed url,
            // so the title fallback must still catch it.
            SeedExisting(new PodcastSeries { Title = "hello internet", RssFeedUrl = null });

            var opml = Opml(Feed("Hello Internet", "https://feeds.example.com/hi", "811377230"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Skipped.Should().Be(1);
            result.Imported.Should().Be(0);
        }

        [Fact]
        public async Task ImportFromOpmlAsync_DuplicateFeedInSameFile_ImportsOnceSkipsRest()
        {
            var opml = Opml(
                Feed("Repeated Show", "https://feeds.example.com/rep", "5"),
                Feed("Repeated Show", "https://feeds.example.com/rep", "5"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(2);
            result.Imported.Should().Be(1);
            result.Skipped.Should().Be(1);
            await _podcastService.Received(1).CreatePodcastSeriesAsync(Arg.Any<CreatePodcastSeriesDto>());
        }

        #endregion

        #region Failure isolation / malformed input

        [Fact]
        public async Task ImportFromOpmlAsync_OneFeedFails_OthersStillImport()
        {
            // The middle feed's create throws; the run must record it as failed and keep going.
            _podcastService.CreatePodcastSeriesAsync(Arg.Is<CreatePodcastSeriesDto>(d => d.Title == "Bad Feed"))
                .Throws(new InvalidOperationException("db exploded"));

            var opml = Opml(
                Feed("Good Feed 1", "https://feeds.example.com/g1", "1"),
                Feed("Bad Feed", "https://feeds.example.com/bad", "2"),
                Feed("Good Feed 2", "https://feeds.example.com/g2", "3"));

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(3);
            result.Imported.Should().Be(2);
            result.Failed.Should().Be(1);
            result.Failures.Should().ContainSingle();
            result.Failures.Single().Title.Should().Be("Bad Feed");
            result.Failures.Single().Reason.Should().Contain("db exploded");
        }

        [Fact]
        public async Task ImportFromOpmlAsync_MalformedXml_ReturnsGracefullyWithFailure()
        {
            var result = await _service.ImportFromOpmlAsync(Stream("this is not xml <<<"));

            result.Imported.Should().Be(0);
            result.Total.Should().Be(0);
            result.Failed.Should().Be(1);
            result.Failures.Should().ContainSingle();
            await _podcastService.DidNotReceive().CreatePodcastSeriesAsync(Arg.Any<CreatePodcastSeriesDto>());
        }

        [Fact]
        public async Task ImportFromOpmlAsync_EmptyOpml_NoFeedsImported()
        {
            var opml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                       "<opml version=\"1.0\"><head><title>Empty</title></head><body></body></opml>";

            var result = await _service.ImportFromOpmlAsync(Stream(opml));

            result.Total.Should().Be(0);
            result.Imported.Should().Be(0);
            result.Skipped.Should().Be(0);
            result.Failed.Should().Be(0);
        }

        #endregion

        #region OPML builders

        private static string Opml(params string[] feedOutlines) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<opml version=\"1.0\"><head><title>Test Subscriptions</title></head><body>" +
            "<outline text=\"feeds\">" + string.Concat(feedOutlines) + "</outline>" +
            "</body></opml>";

        private static string Feed(string title, string? xmlUrl = "https://feeds.example.com/default", string? appleId = "0")
        {
            var attrs = $"type=\"rss\" text=\"{title}\"";
            if (xmlUrl != null)
            {
                attrs += $" xmlUrl=\"{xmlUrl}\"";
            }
            if (appleId != null)
            {
                attrs += $" applePodcastsID=\"{appleId}\"";
            }
            return $"<outline {attrs}/>";
        }

        private static Stream Stream(string content) =>
            new MemoryStream(Encoding.UTF8.GetBytes(content));

        #endregion
    }
}
