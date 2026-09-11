using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Every collaborator that reaches the network (scraper, screenshot service, Wayback, link
    /// checker) is substituted; the database is EF in-memory. The rules under test: fill-only
    /// writes, placeholder-only title replacement, quota-gated screenshots, unreachable pages
    /// recorded rather than retried forever, and one website's failure never aborting the run.
    /// </summary>
    [Trait("Category", "Unit")]
    public class WebsiteEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly IWebsiteScraperService _scraper = Substitute.For<IWebsiteScraperService>();
        private readonly IWebsiteScreenshotService _screenshots = Substitute.For<IWebsiteScreenshotService>();
        private readonly IScreenshotQuota _quota = Substitute.For<IScreenshotQuota>();
        private readonly IWaybackMachineClient _wayback = Substitute.For<IWaybackMachineClient>();
        private readonly ILinkChecker _linkChecker = Substitute.For<ILinkChecker>();
        private readonly IThumbnailStorageService _storage = Substitute.For<IThumbnailStorageService>();
        private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
        private readonly WebsiteEnrichmentService _service;

        public WebsiteEnrichmentServiceTests()
        {
            _quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(100);
            _scraper.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => Scraped(call.Arg<string>()));
            _screenshots.CaptureScreenshotAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
            _wayback.FindLatestSnapshotAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

            _service = CreateService(Context);
        }

        private WebsiteEnrichmentService CreateService(IApplicationDbContext context) => new(
            context, _scraper, _screenshots, _quota, _wayback, _linkChecker, _storage, _syncState,
            Options.Create(new WebsiteEnrichmentOptions { WaybackDelayMs = 0, MaxLimit = 200 }),
            Substitute.For<ILogger<WebsiteEnrichmentService>>());

        #region Helpers

        private static ScrapedWebsiteDataDto Scraped(string url, string? title = "Scraped Title") => new()
        {
            Url = url,
            Title = title,
            Description = "Scraped description",
            ImageUrl = "https://cdn.example.com/og.png",
            RssFeedUrl = "https://example.com/feed.xml",
            Author = "Scraped Author",
            Publication = "Scraped Publication",
            Domain = "example.com"
        };

        private async Task<Website> SeedStub(
            string url = "https://example.com/page",
            string? title = null,
            DateTime? dateAdded = null,
            Action<Website>? configure = null)
        {
            var website = TestDataFactory.CreateWebsite(title ?? "example.com", url, "example.com");
            website.UrlKey = null;
            website.DateAdded = dateAdded ?? DateTime.UtcNow;
            website.LastCheckedDate = null;
            configure?.Invoke(website);
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            return website;
        }

        private Task<Website> Reload(Guid id) => Context.Websites.AsNoTracking().SingleAsync(w => w.Id == id);

        #endregion

        #region EnrichPendingAsync — time budget

        [Fact]
        public async Task EnrichPendingAsync_StopsStartingWebsites_OnceTheTimeBudgetIsSpent()
        {
            // Three stubs, a one-second budget, and a scraper that takes longer than that: the first
            // website is started (the budget is checked before each start), the other two stay pending.
            await SeedStub("https://example.com/a", dateAdded: DateTime.UtcNow.AddMinutes(-3));
            await SeedStub("https://example.com/b", dateAdded: DateTime.UtcNow.AddMinutes(-2));
            await SeedStub("https://example.com/c", dateAdded: DateTime.UtcNow.AddMinutes(-1));
            _scraper.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(async call =>
            {
                await Task.Delay(1100);
                return Scraped(call.Arg<string>());
            });
            var service = new WebsiteEnrichmentService(
                Context, _scraper, _screenshots, _quota, _wayback, _linkChecker, _storage, _syncState,
                Options.Create(new WebsiteEnrichmentOptions { WaybackDelayMs = 0, MaxLimit = 200, RunTimeBudgetSeconds = 1 }),
                Substitute.For<ILogger<WebsiteEnrichmentService>>());

            var result = await service.EnrichPendingAsync(50);

            result.Success.Should().BeTrue();
            result.TimeBudgetReached.Should().BeTrue();
            result.TotalProcessed.Should().Be(1, "only websites actually attempted are counted");
            result.EnrichedCount.Should().Be(1);
            result.PendingCount.Should().Be(2, "the websites the budget did not reach stay pending for the next call");
            result.WasCancelled.Should().BeFalse();
            await _scraper.Received(1).ScrapeWebsiteAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task EnrichPendingAsync_ProcessesTheWholePage_WhenTheBudgetIsDisabled()
        {
            await SeedStub("https://example.com/a");
            await SeedStub("https://example.com/b");
            var service = new WebsiteEnrichmentService(
                Context, _scraper, _screenshots, _quota, _wayback, _linkChecker, _storage, _syncState,
                Options.Create(new WebsiteEnrichmentOptions { WaybackDelayMs = 0, MaxLimit = 200, RunTimeBudgetSeconds = 0 }),
                Substitute.For<ILogger<WebsiteEnrichmentService>>());

            var result = await service.EnrichPendingAsync(50);

            result.TimeBudgetReached.Should().BeFalse();
            result.TotalProcessed.Should().Be(2);
            result.PendingCount.Should().Be(0);
        }

        #endregion

        #region GetPendingCountAsync

        [Fact]
        public async Task GetPendingCountAsync_CountsOnlyWebsitesNeverEnriched()
        {
            await SeedStub("https://example.com/a");
            await SeedStub("https://example.com/b");
            await SeedStub("https://example.com/c", configure: w => w.EnrichedAt = DateTime.UtcNow);

            (await _service.GetPendingCountAsync()).Should().Be(2);
        }

        #endregion

        #region EnrichPendingAsync

        [Fact]
        public async Task EnrichPendingAsync_FillsEveryEmptyField_AndStampsTheRow()
        {
            var stub = await SeedStub();
            _wayback.FindLatestSnapshotAsync(stub.Link!, Arg.Any<CancellationToken>())
                .Returns("https://web.archive.org/web/20240101000000/https://example.com/page");

            var result = await _service.EnrichPendingAsync(10);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("website-enrichment");
            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.PendingCount.Should().Be(0);
            result.CompletedAt.Should().NotBeNull();

            var saved = await Reload(stub.Id);
            saved.Title.Should().Be("Scraped Title", "the stub title was the bare domain");
            saved.Description.Should().Be("Scraped description");
            saved.Thumbnail.Should().Be("https://cdn.example.com/og.png");
            saved.RssFeedUrl.Should().Be("https://example.com/feed.xml");
            saved.Author.Should().Be("Scraped Author");
            saved.Publication.Should().Be("Scraped Publication");
            saved.WaybackUrl.Should().Be("https://web.archive.org/web/20240101000000/https://example.com/page");
            saved.UrlKey.Should().Be("example.com/page", "the identity is backfilled on legacy rows");
            saved.EnrichedAt.Should().NotBeNull();
            saved.LastCheckedDate.Should().NotBeNull();
            saved.LastHttpStatus.Should().Be(200);

            await _syncState.Received(1).MarkSyncSucceededAsync("website-enrichment", result.StartedAt);
        }

        [Fact]
        public async Task EnrichPendingAsync_NeverOverwritesValuesTheRowAlreadyHas()
        {
            var stub = await SeedStub(title: "My Own Title", configure: w =>
            {
                w.Description = "My description";
                w.Thumbnail = "https://mine.example/thumb.png";
                w.Author = "Me";
                w.WaybackUrl = "https://web.archive.org/web/1/https://example.com/page";
            });

            var result = await _service.EnrichPendingAsync(10);

            var saved = await Reload(stub.Id);
            saved.Title.Should().Be("My Own Title");
            saved.Description.Should().Be("My description");
            saved.Thumbnail.Should().Be("https://mine.example/thumb.png");
            saved.Author.Should().Be("Me");
            saved.WaybackUrl.Should().Be("https://web.archive.org/web/1/https://example.com/page");
            saved.RssFeedUrl.Should().Be("https://example.com/feed.xml", "only the empty fields are filled");
            saved.Publication.Should().Be("Scraped Publication");
            result.EnrichedCount.Should().Be(1);
            await _wayback.DidNotReceiveWithAnyArgs().FindLatestSnapshotAsync(default!, default);
        }

        [Fact]
        public async Task EnrichPendingAsync_ReplacesOnlyPlaceholderTitles()
        {
            var domainTitle = await SeedStub("https://example.com/a", title: "example.com");
            var untitled = await SeedStub("https://example.com/b", title: "Untitled Website");
            var real = await SeedStub("https://example.com/c", title: "A Title Someone Typed");

            await _service.EnrichPendingAsync(10);

            (await Reload(domainTitle.Id)).Title.Should().Be("Scraped Title");
            (await Reload(untitled.Id)).Title.Should().Be("Scraped Title");
            (await Reload(real.Id)).Title.Should().Be("A Title Someone Typed");
        }

        [Fact]
        public async Task EnrichPendingAsync_CountsACompleteRowAsUnchanged_ButStillMarksIt()
        {
            var full = await SeedStub(title: "Full", configure: w =>
            {
                w.Description = "d";
                w.Thumbnail = "t";
                w.RssFeedUrl = "https://example.com/feed";
                w.Author = "a";
                w.Publication = "p";
                w.WaybackUrl = "w";
                w.UrlKey = "example.com/page";
            });

            var result = await _service.EnrichPendingAsync(10);

            result.EnrichedCount.Should().Be(0);
            result.UnchangedCount.Should().Be(1);
            (await Reload(full.Id)).EnrichedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingAsync_ProcessesOldestFirst_AndHonorsTheLimit()
        {
            var oldest = await SeedStub("https://example.com/old", dateAdded: DateTime.UtcNow.AddDays(-3));
            var middle = await SeedStub("https://example.com/mid", dateAdded: DateTime.UtcNow.AddDays(-2));
            var newest = await SeedStub("https://example.com/new", dateAdded: DateTime.UtcNow.AddDays(-1));

            var result = await _service.EnrichPendingAsync(2);

            result.TotalProcessed.Should().Be(2);
            result.PendingCount.Should().Be(1);
            (await Reload(oldest.Id)).EnrichedAt.Should().NotBeNull();
            (await Reload(middle.Id)).EnrichedAt.Should().NotBeNull();
            (await Reload(newest.Id)).EnrichedAt.Should().BeNull();
        }

        [Fact]
        public async Task EnrichPendingAsync_SkipsRowsAlreadyEnriched()
        {
            await SeedStub(configure: w => w.EnrichedAt = DateTime.UtcNow.AddDays(-1));

            var result = await _service.EnrichPendingAsync(10);

            result.TotalProcessed.Should().Be(0);
            await _scraper.DidNotReceiveWithAnyArgs().ScrapeWebsiteAsync(default!);
        }

        [Fact]
        public async Task EnrichPendingAsync_RendersAScreenshot_OnlyWhenThePageOffersNoImage()
        {
            var noImage = await SeedStub("https://example.com/plain");
            var withImage = await SeedStub("https://example.com/rich");
            _scraper.ScrapeWebsiteAsync(noImage.Link!).Returns(new ScrapedWebsiteDataDto { Url = noImage.Link!, Title = "Plain" });
            _screenshots.CaptureScreenshotAsync(noImage.Link!, Arg.Any<CancellationToken>())
                .Returns("https://bucket.example/screenshots/plain.png");

            var result = await _service.EnrichPendingAsync(10);

            result.ScreenshotsRendered.Should().Be(1);
            (await Reload(noImage.Id)).Thumbnail.Should().Be("https://bucket.example/screenshots/plain.png");
            (await Reload(withImage.Id)).Thumbnail.Should().Be("https://cdn.example.com/og.png");
            await _screenshots.Received(1).CaptureScreenshotAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichPendingAsync_StopsAskingForScreenshots_OnceTheQuotaIsGone()
        {
            _scraper.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => new ScrapedWebsiteDataDto { Url = call.Arg<string>(), Title = "Plain" });
            var first = await SeedStub("https://example.com/1", dateAdded: DateTime.UtcNow.AddDays(-3));
            var second = await SeedStub("https://example.com/2", dateAdded: DateTime.UtcNow.AddDays(-2));
            var third = await SeedStub("https://example.com/3", dateAdded: DateTime.UtcNow.AddDays(-1));
            _quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(1, 0);
            _screenshots.CaptureScreenshotAsync(first.Link!, Arg.Any<CancellationToken>()).Returns("https://bucket.example/screenshots/1.png");

            var result = await _service.EnrichPendingAsync(10);

            result.QuotaReached.Should().BeTrue();
            result.ScreenshotsRendered.Should().Be(1);
            result.WarningMessage.Should().Contain("quota").And.Contain("2 website(s)");
            await _screenshots.Received(1).CaptureScreenshotAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _quota.Received(2).RemainingAsync(Arg.Any<CancellationToken>());

            // Metadata enrichment carried on for the websites that got no screenshot.
            (await Reload(second.Id)).EnrichedAt.Should().NotBeNull();
            (await Reload(third.Id)).EnrichedAt.Should().NotBeNull();
            (await Reload(third.Id)).Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task EnrichPendingAsync_RecordsAnUnreachablePage_AndDoesNotRetryItForever()
        {
            var gone = await SeedStub("https://example.com/gone");
            _scraper.ScrapeWebsiteAsync(gone.Link!)
                .Throws(new HttpRequestException("not found", null, System.Net.HttpStatusCode.NotFound));

            var result = await _service.EnrichPendingAsync(10);

            result.Success.Should().BeTrue();
            result.SkippedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            result.PendingCount.Should().Be(0);

            var saved = await Reload(gone.Id);
            saved.LastHttpStatus.Should().Be(404);
            saved.EnrichedAt.Should().NotBeNull();
            saved.LastCheckedDate.Should().NotBeNull();
            saved.Description.Should().BeNull();
            await _screenshots.DidNotReceiveWithAnyArgs().CaptureScreenshotAsync(default!, default);
        }

        [Fact]
        public async Task EnrichPendingAsync_LinksAnArchivedCopy_ForAPageThatIsGone()
        {
            var gone = await SeedStub("https://example.com/gone");
            _scraper.ScrapeWebsiteAsync(gone.Link!)
                .Throws(new HttpRequestException("gone", null, System.Net.HttpStatusCode.Gone));
            _wayback.FindLatestSnapshotAsync(gone.Link!, Arg.Any<CancellationToken>())
                .Returns("https://web.archive.org/web/20240101000000/https://example.com/gone");

            var result = await _service.EnrichPendingAsync(10);

            result.SkippedCount.Should().Be(1, "a dead page is still counted as unreachable");
            var saved = await Reload(gone.Id);
            saved.LastHttpStatus.Should().Be(410);
            saved.WaybackUrl.Should().Be("https://web.archive.org/web/20240101000000/https://example.com/gone");
        }

        [Fact]
        public async Task EnrichPendingAsync_TreatsAScraperTimeoutAsUnreachable_AndKeepsGoing()
        {
            var hanging = await SeedStub("https://example.com/hangs", dateAdded: DateTime.UtcNow.AddDays(-2));
            var healthy = await SeedStub("https://example.com/healthy", dateAdded: DateTime.UtcNow.AddDays(-1));
            _scraper.ScrapeWebsiteAsync(hanging.Link!)
                .Throws(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 15 seconds elapsing."));

            var result = await _service.EnrichPendingAsync(10);

            await _linkChecker.DidNotReceiveWithAnyArgs().CheckAsync(default!, default);

            result.Success.Should().BeTrue();
            result.WasCancelled.Should().BeFalse();
            result.TotalProcessed.Should().Be(2);
            result.SkippedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.PendingCount.Should().Be(0);

            var saved = await Reload(hanging.Id);
            saved.LastHttpStatus.Should().Be(0);
            saved.EnrichedAt.Should().NotBeNull("the row must not be retried on every page");
            (await Reload(healthy.Id)).Description.Should().Be("Scraped description");
        }

        [Fact]
        public async Task EnrichPendingAsync_RecordsStatusZero_WhenTheHostNeverAnswered()
        {
            var dead = await SeedStub("https://example.com/dead");
            _scraper.ScrapeWebsiteAsync(dead.Link!).Throws(new HttpRequestException("name not resolved"));
            _linkChecker.CheckAsync(dead.Link!, Arg.Any<CancellationToken>()).Returns(0);

            await _service.EnrichPendingAsync(10);

            (await Reload(dead.Id)).LastHttpStatus.Should().Be(0);
        }

        [Fact]
        public async Task EnrichPendingAsync_AsksTheLinkChecker_WhenTheScraperDroppedTheStatus()
        {
            // The scraper wraps HTTP failures in a new exception; the status survives only on the inner one,
            // and some wrappers drop it entirely.
            var wrapped = await SeedStub("https://example.com/wrapped", dateAdded: DateTime.UtcNow.AddDays(-2));
            var dropped = await SeedStub("https://example.com/dropped", dateAdded: DateTime.UtcNow.AddDays(-1));
            _scraper.ScrapeWebsiteAsync(wrapped.Link!).Throws(new HttpRequestException("Failed to fetch URL",
                new HttpRequestException("gone", null, System.Net.HttpStatusCode.Gone)));
            _scraper.ScrapeWebsiteAsync(dropped.Link!).Throws(new HttpRequestException("Failed to fetch URL"));
            _linkChecker.CheckAsync(dropped.Link!, Arg.Any<CancellationToken>()).Returns(404);

            var result = await _service.EnrichPendingAsync(10);

            result.SkippedCount.Should().Be(2);
            (await Reload(wrapped.Id)).LastHttpStatus.Should().Be(410);
            (await Reload(dropped.Id)).LastHttpStatus.Should().Be(404);
            await _linkChecker.Received(1).CheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichPendingAsync_AsksTheLinkChecker_WhenTheScraperStoppedAtARedirect()
        {
            // The scraper's client refuses an https-to-http hop and reports the 301; the link checker
            // follows it and finds a live page.
            var redirected = await SeedStub("https://example.com/moved");
            _scraper.ScrapeWebsiteAsync(redirected.Link!).Throws(new HttpRequestException("Failed to fetch URL",
                new HttpRequestException("moved", null, System.Net.HttpStatusCode.MovedPermanently)));
            _linkChecker.CheckAsync(redirected.Link!, Arg.Any<CancellationToken>()).Returns(200);

            var result = await _service.EnrichPendingAsync(10);

            result.SkippedCount.Should().Be(1);
            (await Reload(redirected.Id)).LastHttpStatus.Should().Be(200);
        }

        [Fact]
        public async Task EnrichPendingAsync_IsolatesOneWebsitesFailure_FromTheRest()
        {
            var broken = await SeedStub("https://example.com/broken", dateAdded: DateTime.UtcNow.AddDays(-2));
            var fine = await SeedStub("https://example.com/fine", dateAdded: DateTime.UtcNow.AddDays(-1));
            _scraper.ScrapeWebsiteAsync(broken.Link!).Throws(new InvalidOperationException("parser exploded"));

            var result = await _service.EnrichPendingAsync(10);

            result.Success.Should().BeTrue();
            result.FailedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().Contain("parser exploded");
            result.WarningMessage.Should().Contain("1 of 2");
            (await Reload(broken.Id)).EnrichedAt.Should().BeNull("a failed website stays pending for the next run");
            (await Reload(fine.Id)).EnrichedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichPendingAsync_StopsAtTheToken_AndKeepsFinishedRows()
        {
            var first = await SeedStub("https://example.com/1", dateAdded: DateTime.UtcNow.AddDays(-2));
            var second = await SeedStub("https://example.com/2", dateAdded: DateTime.UtcNow.AddDays(-1));
            using var cts = new CancellationTokenSource();
            _wayback.FindLatestSnapshotAsync(first.Link!, Arg.Any<CancellationToken>())
                .Returns(_ => { cts.Cancel(); return (string?)null; });

            var result = await _service.EnrichPendingAsync(10, cts.Token);

            result.WasCancelled.Should().BeTrue();
            result.Success.Should().BeTrue();
            result.PendingCount.Should().Be(2, "an interrupted run still reports what is left (the row it was on was never saved) so callers do not think the queue is empty");
            await _syncState.DidNotReceiveWithAnyArgs().MarkSyncSucceededAsync(default!, default);
            (await Reload(second.Id)).EnrichedAt.Should().BeNull();
        }

        [Fact]
        public async Task EnrichPendingAsync_ReportsAFatalFailure_WhenTheQueryItselfThrows()
        {
            var context = Substitute.For<IApplicationDbContext>();
            context.Websites.Returns(_ => throw new InvalidOperationException("database unavailable"));
            var service = CreateService(context);

            var result = await service.EnrichPendingAsync(10);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("database unavailable");
            result.CompletedAt.Should().NotBeNull();
            await _syncState.DidNotReceiveWithAnyArgs().MarkSyncSucceededAsync(default!, default);
        }

        #endregion

        #region EnrichByIdAsync

        [Fact]
        public async Task EnrichByIdAsync_ReportsNotFound()
        {
            var result = await _service.EnrichByIdAsync(Guid.NewGuid(), force: false);

            result.NotFound.Should().BeTrue();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task EnrichByIdAsync_LeavesAnEnrichedRowAlone_UnlessForced()
        {
            var done = await SeedStub(configure: w => w.EnrichedAt = DateTime.UtcNow.AddDays(-1));

            var untouched = await _service.EnrichByIdAsync(done.Id, force: false);
            var forced = await _service.EnrichByIdAsync(done.Id, force: true);

            untouched.AlreadyEnriched.Should().BeTrue();
            untouched.FilledFields.Should().BeEmpty();
            forced.AlreadyEnriched.Should().BeFalse();
            forced.FilledFields.Should().Contain("description");
            await _scraper.Received(1).ScrapeWebsiteAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task EnrichByIdAsync_ListsTheFieldsItFilled()
        {
            var stub = await SeedStub(configure: w => w.Description = "kept");
            _wayback.FindLatestSnapshotAsync(stub.Link!, Arg.Any<CancellationToken>()).Returns("https://web.archive.org/web/1/x");

            var result = await _service.EnrichByIdAsync(stub.Id, force: false);

            result.Success.Should().BeTrue();
            result.Title.Should().Be("Scraped Title");
            result.FilledFields.Should().BeEquivalentTo(new[] { "urlKey", "title", "thumbnail", "rssFeedUrl", "author", "publication", "waybackUrl" });
            result.LastHttpStatus.Should().Be(200);
        }

        [Fact]
        public async Task EnrichByIdAsync_ReportsAnUnreachablePage()
        {
            var stub = await SeedStub();
            _scraper.ScrapeWebsiteAsync(stub.Link!)
                .Throws(new HttpRequestException("gone", null, System.Net.HttpStatusCode.Gone));

            var result = await _service.EnrichByIdAsync(stub.Id, force: false);

            result.Success.Should().BeTrue();
            result.Unreachable.Should().BeTrue();
            result.LastHttpStatus.Should().Be(410);
            result.WarningMessage.Should().Contain("410");
        }

        [Fact]
        public async Task EnrichByIdAsync_ReportsTheQuota_WhenAScreenshotWasWanted()
        {
            var stub = await SeedStub();
            _scraper.ScrapeWebsiteAsync(stub.Link!).Returns(new ScrapedWebsiteDataDto { Url = stub.Link!, Title = "Plain" });
            _quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(0);

            var result = await _service.EnrichByIdAsync(stub.Id, force: false);

            result.QuotaReached.Should().BeTrue();
            result.ScreenshotRendered.Should().BeFalse();
            result.WarningMessage.Should().Contain("quota");
            (await Reload(stub.Id)).EnrichedAt.Should().NotBeNull("metadata enrichment still completes without a screenshot");
        }

        #endregion

        #region CheckLinksAsync

        [Fact]
        public async Task CheckLinksAsync_RecordsStatuses_AndReportsNewlyBrokenAndRecovered()
        {
            var healthy = await SeedStub("https://example.com/ok", configure: w => { w.LastHttpStatus = 200; w.WaybackUrl = "https://web.archive.org/web/1/ok"; });
            var nowDead = await SeedStub("https://example.com/dead", configure: w => w.LastHttpStatus = 200);
            var recovered = await SeedStub("https://example.com/back", configure: w => w.LastHttpStatus = 503);
            var neverChecked = await SeedStub("https://example.com/new");
            _linkChecker.CheckAsync(healthy.Link!, Arg.Any<CancellationToken>()).Returns(200);
            _linkChecker.CheckAsync(nowDead.Link!, Arg.Any<CancellationToken>()).Returns(404);
            _linkChecker.CheckAsync(recovered.Link!, Arg.Any<CancellationToken>()).Returns(200);
            _linkChecker.CheckAsync(neverChecked.Link!, Arg.Any<CancellationToken>()).Returns(0);

            var result = await _service.CheckLinksAsync(10, olderThanDays: 30);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("website-link-check");
            result.TotalProcessed.Should().Be(4);
            result.BrokenCount.Should().Be(2);
            result.RecoveredCount.Should().Be(1);
            result.ChangedCount.Should().Be(3);
            result.NewlyBroken.Select(b => b.Id).Should().BeEquivalentTo(new[] { nowDead.Id, neverChecked.Id });
            result.NewlyBroken.Single(b => b.Id == nowDead.Id).PreviousStatus.Should().Be(200);
            result.NewlyBroken.Single(b => b.Id == nowDead.Id).Status.Should().Be(404);

            (await Reload(nowDead.Id)).LastHttpStatus.Should().Be(404);
            (await Reload(recovered.Id)).LastHttpStatus.Should().Be(200);
            (await Reload(neverChecked.Id)).LastCheckedDate.Should().NotBeNull();
            await _syncState.Received(1).MarkSyncSucceededAsync("website-link-check", result.StartedAt);
        }

        [Fact]
        public async Task CheckLinksAsync_OnlyRevisitsStaleOrNeverCheckedLinks()
        {
            var fresh = await SeedStub("https://example.com/fresh", configure: w => { w.LastHttpStatus = 200; w.LastCheckedDate = DateTime.UtcNow.AddDays(-1); });
            var stale = await SeedStub("https://example.com/stale", configure: w => { w.LastHttpStatus = 200; w.LastCheckedDate = DateTime.UtcNow.AddDays(-40); });
            var never = await SeedStub("https://example.com/never", configure: w => w.LastCheckedDate = DateTime.UtcNow);
            _linkChecker.CheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(200);

            var result = await _service.CheckLinksAsync(10, olderThanDays: 30);

            result.TotalProcessed.Should().Be(2);
            await _linkChecker.Received(1).CheckAsync(stale.Link!, Arg.Any<CancellationToken>());
            await _linkChecker.Received(1).CheckAsync(never.Link!, Arg.Any<CancellationToken>());
            await _linkChecker.DidNotReceive().CheckAsync(fresh.Link!, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckLinksAsync_IsolatesACheckerFailure()
        {
            var bad = await SeedStub("https://example.com/bad", configure: w => w.LastHttpStatus = 200);
            var good = await SeedStub("https://example.com/good", configure: w => w.LastHttpStatus = 200);
            _linkChecker.CheckAsync(bad.Link!, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("boom"));
            _linkChecker.CheckAsync(good.Link!, Arg.Any<CancellationToken>()).Returns(200);

            var result = await _service.CheckLinksAsync(10, olderThanDays: 0);

            result.Success.Should().BeTrue();
            result.FailedCount.Should().Be(1);
            result.WarningMessage.Should().Contain("1 of 2");
            (await Reload(bad.Id)).LastHttpStatus.Should().Be(200, "a failed check leaves the previous status alone");
        }

        #endregion

        #region RepairThumbnailsAsync

        [Fact]
        public async Task RepairThumbnailsAsync_TargetsProviderUrlsAndAnimatedPlaceholders_AndReplacesThem()
        {
            var providerUrl = await SeedStub("https://example.com/a", configure: w => w.Thumbnail = "https://image.thum.io/get/width/1200/https://example.com/a");
            var storedGif = await SeedStub("https://example.com/b", configure: w => w.Thumbnail = "https://bucket.example/screenshots/abc.gif");
            var healthy = await SeedStub("https://example.com/c", configure: w => w.Thumbnail = "https://bucket.example/screenshots/def.png");
            var ogImage = await SeedStub("https://example.com/d", configure: w => w.Thumbnail = "https://cdn.example.com/og.png");
            _screenshots.CaptureScreenshotAsync(providerUrl.Link!, Arg.Any<CancellationToken>()).Returns("https://bucket.example/screenshots/new-a.png");
            _screenshots.CaptureScreenshotAsync(storedGif.Link!, Arg.Any<CancellationToken>()).Returns("https://bucket.example/screenshots/new-b.png");

            var result = await _service.RepairThumbnailsAsync(10);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("website-thumbnail-repair");
            result.TotalProcessed.Should().Be(2);
            result.EnrichedCount.Should().Be(2);
            result.ScreenshotsRendered.Should().Be(2);
            (await Reload(providerUrl.Id)).Thumbnail.Should().Be("https://bucket.example/screenshots/new-a.png");
            (await Reload(storedGif.Id)).Thumbnail.Should().Be("https://bucket.example/screenshots/new-b.png");
            (await Reload(healthy.Id)).Thumbnail.Should().Be("https://bucket.example/screenshots/def.png");
            (await Reload(ogImage.Id)).Thumbnail.Should().Be("https://cdn.example.com/og.png");
            await _storage.Received(1).DeleteAsync("https://bucket.example/screenshots/abc.gif");
            await _storage.Received(1).DeleteAsync("https://image.thum.io/get/width/1200/https://example.com/a");
        }

        [Fact]
        public async Task RepairThumbnailsAsync_ClearsAThumbnail_WhenNothingUsableCanBeRendered()
        {
            var bad = await SeedStub(configure: w => w.Thumbnail = "https://image.thum.io/get/https://example.com/page");

            var result = await _service.RepairThumbnailsAsync(10);

            result.SkippedCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
            result.WarningMessage.Should().Contain("cleared");
            (await Reload(bad.Id)).Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task RepairThumbnailsAsync_LeavesTheRestForLater_WhenTheQuotaIsGone()
        {
            var first = await SeedStub("https://example.com/1", dateAdded: DateTime.UtcNow.AddDays(-2), configure: w => w.Thumbnail = "https://image.thum.io/get/1");
            var second = await SeedStub("https://example.com/2", dateAdded: DateTime.UtcNow.AddDays(-1), configure: w => w.Thumbnail = "https://image.thum.io/get/2");
            _quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(1, 0);
            _screenshots.CaptureScreenshotAsync(first.Link!, Arg.Any<CancellationToken>()).Returns("https://bucket.example/screenshots/1.png");

            var result = await _service.RepairThumbnailsAsync(10);

            result.QuotaReached.Should().BeTrue();
            result.EnrichedCount.Should().Be(1);
            result.WarningMessage.Should().Contain("1 thumbnail(s) were left");
            (await Reload(second.Id)).Thumbnail.Should().Be("https://image.thum.io/get/2", "untouched rows are still found by the next run");
        }

        #endregion
    }
}
