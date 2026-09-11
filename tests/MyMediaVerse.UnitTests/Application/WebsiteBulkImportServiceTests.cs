using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Application
{
    /// <summary>
    /// EF in-memory only; nothing is fetched. The rules under test: every entry is keyed like the
    /// single-URL paths so library and in-file duplicates are caught, a match gains only topics,
    /// new rows are stubs awaiting enrichment, and folders/tags become lowercase topics per the
    /// options.
    /// </summary>
    [Trait("Category", "Unit")]
    public class WebsiteBulkImportServiceTests : InMemoryDbTestBase
    {
        private readonly WebsiteBulkImportService _service;

        public WebsiteBulkImportServiceTests()
        {
            _service = new WebsiteBulkImportService(Context, Substitute.For<ILogger<WebsiteBulkImportService>>());
        }

        #region Helpers

        private static ParsedBookmark Bookmark(
            string url, string? title = null, string[]? folders = null, string[]? tags = null, DateTime? addedAt = null) =>
            new(url, title, addedAt, folders ?? Array.Empty<string>(), tags ?? Array.Empty<string>());

        private static BookmarkParseResult Parsed(params ParsedBookmark[] bookmarks) =>
            new(bookmarks, 0, bookmarks.SelectMany(b => b.FolderPath.Count > 0 ? new[] { string.Join("/", b.FolderPath) } : Array.Empty<string>()).Distinct().ToList());

        private static BookmarkParseResult Parsed(int nonWebLinks, params ParsedBookmark[] bookmarks) =>
            new(bookmarks, nonWebLinks, Array.Empty<string>());

        private Task<WebsiteBulkImportResultDto> Import(BookmarkParseResult parsed, BookmarkImportOptionsDto? options = null) =>
            _service.ImportAsync(parsed, options ?? new BookmarkImportOptionsDto(), WebsiteBulkImportResultDto.BookmarkImportOperation);

        private async Task<Website> SeedWebsite(string url, string title = "Existing", bool legacy = false, params string[] topics)
        {
            var website = TestDataFactory.CreateWebsite(title, url);
            website.UrlKey = legacy ? null : MyMediaVerse.Application.Utilities.UrlNormalizer.GetComparisonKey(url);
            foreach (var topic in topics) website.Topics.Add(new Topic { Name = topic });
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return website;
        }

        private Task<Website> Reload(Guid id) =>
            Context.Websites.AsNoTracking().Include(w => w.Topics).Include(w => w.Genres).SingleAsync(w => w.Id == id);

        #endregion

        #region PreviewAsync

        [Fact]
        public async Task PreviewAsync_CountsNewExistingInFileDuplicatesAndInvalid_WithoutWriting()
        {
            await SeedWebsite("https://example.com/known");
            var parsed = new BookmarkParseResult(new[]
            {
                Bookmark("https://example.com/new-1", "One", new[] { "Dev" }),
                Bookmark("https://example.com/new-2"),
                Bookmark("http://www.example.com/new-1/?utm_source=x", "One again"),
                Bookmark("https://example.com/known"),
                Bookmark("http://")
            }, NonWebLinkCount: 2, Folders: new[] { "Dev" });

            var preview = await _service.PreviewAsync(parsed);

            preview.TotalCount.Should().Be(5);
            preview.ValidCount.Should().Be(4);
            preview.InvalidCount.Should().Be(1);
            preview.NonWebLinkCount.Should().Be(2);
            preview.DuplicateInFileCount.Should().Be(1);
            preview.AlreadyInLibraryCount.Should().Be(1);
            preview.NewCount.Should().Be(2);
            preview.Folders.Should().Equal("Dev");
            preview.Sample.Should().HaveCount(3, "in-file duplicates are not listed twice");
            preview.Sample[0].Url.Should().Be("https://example.com/new-1");
            preview.Sample[0].FolderPath.Should().Be("Dev");
            preview.Sample.Single(s => s.Url == "https://example.com/known").AlreadyInLibrary.Should().BeTrue();
            (await Context.Websites.CountAsync()).Should().Be(1, "preview writes nothing");
        }

        #endregion

        #region ImportAsync — new rows

        [Fact]
        public async Task ImportAsync_CreatesStubs_WithIdentityDateTopicsAndNoEnrichment()
        {
            var added = new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc);
            var parsed = Parsed(Bookmark("https://WWW.Example.com/Dev/?utm_medium=x#top", "Dev & Tools", new[] { "Dev", "Rust Lang" }, new[] { "Reading" }, added));

            var result = await Import(parsed);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("bookmark-import");
            result.CreatedCount.Should().Be(1);
            result.FoldersFound.Should().Be(1);
            result.TopicsCreatedCount.Should().Be(3);
            result.PendingEnrichmentCount.Should().Be(1);
            result.CompletedAt.Should().NotBeNull();

            var saved = await Context.Websites.AsNoTracking().Include(w => w.Topics).SingleAsync();
            saved.Title.Should().Be("Dev & Tools");
            saved.Link.Should().Be("https://example.com/Dev", "the host is lowercased, the path keeps its case");
            saved.UrlKey.Should().Be("example.com/dev", "the key is case-insensitive");
            saved.Domain.Should().Be("example.com");
            saved.MediaType.Should().Be(MediaType.Website);
            saved.Status.Should().Be(Status.Uncharted);
            saved.DateAdded.Should().Be(added);
            saved.EnrichedAt.Should().BeNull("stubs are filled by the enrichment run");
            saved.Description.Should().BeNull();
            saved.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "dev", "rust lang", "reading" });
        }

        [Fact]
        public async Task ImportAsync_FallsBackToTheDomainAsTitle_AndToNowAsDateAdded()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);

            await Import(Parsed(Bookmark("https://example.com/untitled")));

            var saved = await Context.Websites.AsNoTracking().SingleAsync();
            saved.Title.Should().Be("example.com");
            saved.DateAdded.Should().BeAfter(before);
        }

        [Fact]
        public async Task ImportAsync_HonorsTheFolderAndTagToggles_AndAlwaysAppliesExtras()
        {
            var parsed = Parsed(Bookmark("https://example.com/a", "A", new[] { "Dev" }, new[] { "later" }));
            var options = new BookmarkImportOptionsDto
            {
                FoldersAsTopics = false,
                TagsAsTopics = false,
                DefaultStatus = Status.ActivelyExploring,
                ExtraTopics = new List<string> { " Imported ", "imported" },
                ExtraGenres = new List<string> { "Blog" }
            };

            await Import(parsed, options);

            var saved = await Context.Websites.AsNoTracking().Include(w => w.Topics).Include(w => w.Genres).SingleAsync();
            saved.Topics.Select(t => t.Name).Should().Equal("imported");
            saved.Genres.Select(g => g.Name).Should().Equal("blog");
            saved.Status.Should().Be(Status.ActivelyExploring);
        }

        [Fact]
        public async Task ImportAsync_SharesOneTopicAndGenreRow_AcrossRowsInTheSameImport()
        {
            var parsed = Parsed(
                Bookmark("https://example.com/a", "A", new[] { "Dev" }),
                Bookmark("https://example.com/b", "B", new[] { "Dev" }));

            var result = await Import(parsed, new BookmarkImportOptionsDto { ExtraGenres = new List<string> { "blog" } });

            result.CreatedCount.Should().Be(2);
            result.TopicsCreatedCount.Should().Be(1);
            (await Context.Topics.CountAsync(t => t.Name == "dev")).Should().Be(1);
            (await Context.Genres.CountAsync(g => g.Name == "blog")).Should().Be(1);
        }

        [Fact]
        public async Task ImportAsync_SavesEveryRow_AcrossBatchBoundaries()
        {
            var parsed = Parsed(Enumerable.Range(1, 250).Select(i => Bookmark($"https://example.com/page-{i}", $"Page {i}")).ToArray());

            var result = await Import(parsed);

            result.CreatedCount.Should().Be(250);
            (await Context.Websites.CountAsync()).Should().Be(250);
        }

        #endregion

        #region ImportAsync — duplicates and matches

        [Fact]
        public async Task ImportAsync_SkipsRepeatsWithinTheFile_FirstOccurrenceWins()
        {
            var parsed = Parsed(
                Bookmark("https://example.com/page", "First"),
                Bookmark("http://www.example.com/page/", "Second"));

            var result = await Import(parsed);

            result.CreatedCount.Should().Be(1);
            result.SkippedCount.Should().Be(1);
            (await Context.Websites.SingleAsync()).Title.Should().Be("First");
        }

        [Fact]
        public async Task ImportAsync_AddsTopicsToAMatchingWebsite_AndCountsItAsUpdated()
        {
            var existing = await SeedWebsite("https://example.com/known", "Known", legacy: false, "existing");
            var parsed = Parsed(Bookmark("https://example.com/known?utm_source=x", "Different title", new[] { "Dev" }, new[] { "existing" }));

            var result = await Import(parsed);

            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1);
            var saved = await Reload(existing.Id);
            saved.Title.Should().Be("Known", "a bookmark title never overwrites the library");
            saved.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "existing", "dev" });
        }

        [Fact]
        public async Task ImportAsync_MatchesEveryExistingRow_WhenTheKeysSpanMoreThanOneLookupChunk()
        {
            // 520 keys → two 500-key lookup queries; a real browser export is this size or larger.
            const int count = 520;
            var seeded = Enumerable.Range(1, count).Select(i =>
            {
                var url = $"https://example.com/page-{i}";
                var website = TestDataFactory.CreateWebsite($"Page {i}", url);
                website.UrlKey = MyMediaVerse.Application.Utilities.UrlNormalizer.GetComparisonKey(url);
                return website;
            }).ToList();
            Context.Websites.AddRange(seeded);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            // Same pages, written the way a browser would (www + trailing slash + tracking query).
            var parsed = Parsed(Enumerable.Range(1, count)
                .Select(i => Bookmark($"http://www.example.com/page-{i}/?utm_source=export", $"Page {i}", new[] { "Imported" }))
                .ToArray());

            var preview = await _service.PreviewAsync(parsed);
            preview.AlreadyInLibraryCount.Should().Be(count);
            preview.NewCount.Should().Be(0);

            var result = await Import(parsed);

            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(count, "each match gains the folder topic");
            result.SkippedCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            (await Context.Websites.CountAsync()).Should().Be(count, "no row was duplicated across the chunk boundary");
            (await Context.Websites.CountAsync(w => w.Topics.Any(t => t.Name == "imported"))).Should().Be(count);
        }

        [Fact]
        public async Task ImportAsync_CountsAMatchWithNothingNewAsSkipped()
        {
            await SeedWebsite("https://example.com/known", "Known", legacy: false, "dev");

            var result = await Import(Parsed(Bookmark("https://example.com/known", "Known", new[] { "Dev" })));

            result.UpdatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(1);
        }

        [Fact]
        public async Task ImportAsync_MatchesLegacyRowsByLink_AndBackfillsTheirKey()
        {
            var legacy = await SeedWebsite("https://example.com/old", "Legacy", legacy: true);

            var result = await Import(Parsed(Bookmark("http://www.example.com/old/", "Legacy again")));

            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1, "the identity backfill counts as a change");
            (await Reload(legacy.Id)).UrlKey.Should().Be("example.com/old");
            (await Context.Websites.CountAsync()).Should().Be(1);
        }

        #endregion

        #region ImportAsync — failures, warnings, cancellation

        [Fact]
        public async Task ImportAsync_CountsUnusableUrlsAsFailed_AndReportsIgnoredNonWebLinks()
        {
            var parsed = Parsed(3, Bookmark("http://"), Bookmark("https://example.com/ok"));

            var result = await Import(parsed);

            result.Success.Should().BeTrue();
            result.TotalProcessed.Should().Be(2);
            result.CreatedCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
            result.NonWebLinkCount.Should().Be(3);
            result.Errors.Should().ContainSingle();
            result.WarningMessage.Should().Contain("3 entries were not web links").And.Contain("1 entry could not be imported");
        }

        [Fact]
        public async Task ImportAsync_WordsTheWarningForASingleIgnoredEntry()
        {
            var result = await Import(Parsed(1, Bookmark("https://example.com/ok")));

            result.WarningMessage.Should().Be("1 entry was not a web link and was ignored.");
        }

        [Fact]
        public async Task ImportAsync_StopsAtTheToken_AndKeepsWhatWasSaved()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.ImportAsync(
                Parsed(Bookmark("https://example.com/a")), new BookmarkImportOptionsDto(),
                WebsiteBulkImportResultDto.UrlListImportOperation, cts.Token);

            result.WasCancelled.Should().BeTrue();
            result.Success.Should().BeTrue();
            result.Operation.Should().Be("url-list-import");
            result.CreatedCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportAsync_ReportsAFatalFailure_WhenTheDatabaseIsUnavailable()
        {
            var context = Substitute.For<IApplicationDbContext>();
            context.Websites.Returns(_ => throw new InvalidOperationException("database unavailable"));
            var service = new WebsiteBulkImportService(context, Substitute.For<ILogger<WebsiteBulkImportService>>());

            var result = await service.ImportAsync(Parsed(Bookmark("https://example.com/a")), new BookmarkImportOptionsDto(), "bookmark-import");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("database unavailable");
            result.CompletedAt.Should().NotBeNull();
        }

        #endregion
    }
}
