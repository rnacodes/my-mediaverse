using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class WebsiteServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<WebsiteService> _mockLogger;
        private readonly IWebsiteScraperService _mockScraperService;
        private readonly ITypesenseService _mockTypesenseService;
        private readonly IThumbnailStorageService _mockThumbnailStorage;
        private readonly WebsiteService _websiteService;

        public WebsiteServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<WebsiteService>>();
            _mockScraperService = Substitute.For<IWebsiteScraperService>();
            _mockTypesenseService = Substitute.For<ITypesenseService>();
            _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
            _websiteService = new WebsiteService(
                Context, _mockScraperService, _mockTypesenseService, _mockThumbnailStorage, _mockLogger);
        }

        private static ScrapedWebsiteDataDto Scraped(string url, string title = "Scraped Title") => new()
        {
            Url = url,
            Title = title,
            Domain = "test.com"
        };

        #region Reads

        [Fact]
        public async Task GetAllWebsitesAsync_ShouldReturnAllWebsites()
        {
            // Arrange
            var websites = TestDataFactory.CreateWebsites(3);
            Context.Websites.AddRange(websites);
            await Context.SaveChangesAsync();

            // Act
            var result = await _websiteService.GetAllWebsitesAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(websites, options => options.Excluding(w => w.Topics).Excluding(w => w.Genres));
        }

        [Fact]
        public async Task GetWebsiteByIdAsync_ShouldReturnWebsite_WhenWebsiteExists()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite("Test Website", "https://test.com", "test.com");
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();

            // Act
            var result = await _websiteService.GetWebsiteByIdAsync(website.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Website");
            result.Link.Should().Be("https://test.com");
            result.Domain.Should().Be("test.com");
        }

        [Fact]
        public async Task GetWebsiteByIdAsync_ShouldReturnNull_WhenWebsiteDoesNotExist()
        {
            var result = await _websiteService.GetWebsiteByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetWebsitesByDomainAsync_ShouldReturnWebsitesFromDomain()
        {
            // Arrange
            var websites = new[]
            {
                TestDataFactory.CreateWebsite("Site 1", "https://example.com/page1", "example.com"),
                TestDataFactory.CreateWebsite("Site 2", "https://example.com/page2", "example.com"),
                TestDataFactory.CreateWebsite("Site 3", "https://other.com", "other.com")
            };
            Context.Websites.AddRange(websites);
            await Context.SaveChangesAsync();

            // Act
            var result = await _websiteService.GetWebsitesByDomainAsync("example.com");

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(w => w.Domain == "example.com");
        }

        [Fact]
        public async Task GetWebsitesWithRssFeedsAsync_ShouldReturnOnlyWebsitesWithRss()
        {
            // Arrange
            var websites = new[]
            {
                TestDataFactory.CreateWebsite("With RSS 1", "https://rss1.com", "rss1.com"),
                TestDataFactory.CreateWebsite("With RSS 2", "https://rss2.com", "rss2.com"),
                TestDataFactory.CreateWebsite("Without RSS", "https://norss.com", "norss.com")
            };
            websites[0].RssFeedUrl = "https://rss1.com/feed";
            websites[1].RssFeedUrl = "https://rss2.com/feed";
            websites[2].RssFeedUrl = null;

            Context.Websites.AddRange(websites);
            await Context.SaveChangesAsync();

            // Act
            var result = await _websiteService.GetWebsitesWithRssFeedsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(w => !string.IsNullOrEmpty(w.RssFeedUrl));
        }

        #endregion

        #region Create

        [Fact]
        public async Task CreateWebsiteAsync_ShouldCreateNewWebsite()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("New Website", "https://newsite.com");

            // Act
            var result = await _websiteService.CreateWebsiteAsync(dto);

            // Assert
            result.Created.Should().BeTrue();
            var website = result.Website;
            website.Title.Should().Be("New Website");
            website.Link.Should().Be("https://newsite.com");
            website.Domain.Should().Be("newsite.com");
            website.UrlKey.Should().Be("newsite.com");
            website.MediaType.Should().Be(MediaType.Website);
            website.Status.Should().Be(Status.Uncharted);

            var savedWebsite = await Context.Websites.FindAsync(website.Id);
            savedWebsite.Should().NotBeNull();
            savedWebsite!.Title.Should().Be("New Website");
        }

        [Fact]
        public async Task CreateWebsiteAsync_StoresNormalizedLinkKeyAndDomain()
        {
            var dto = TestDataFactory.CreateWebsiteDto("Site", "https://WWW.Example.com/Path/?utm_source=x#frag");

            var result = await _websiteService.CreateWebsiteAsync(dto);

            result.Website.Link.Should().Be("https://example.com/path");
            result.Website.UrlKey.Should().Be("example.com/path");
            result.Website.Domain.Should().Be("example.com");
        }

        [Fact]
        public async Task CreateWebsiteAsync_AppliesStatusRatingAndDateCompleted()
        {
            var dto = TestDataFactory.CreateWebsiteDto("Site", "https://example.com");
            dto.Status = Status.Completed;
            dto.Rating = Rating.Like;
            dto.DateCompleted = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            var result = await _websiteService.CreateWebsiteAsync(dto);

            result.Website.Status.Should().Be(Status.Completed);
            result.Website.Rating.Should().Be(Rating.Like);
            result.Website.DateCompleted.Should().Be(dto.DateCompleted);
        }

        [Fact]
        public async Task CreateWebsiteAsync_ShouldCreateTopicsAndGenres_TrimmedAndLowercased()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("Website with Tags", "https://tagged.com");
            dto.Topics = new List<string> { " Technology ", "Programming", "", "technology" };
            dto.Genres = new List<string> { "News ", " Tutorial", "   " };

            // Act
            var result = await _websiteService.CreateWebsiteAsync(dto);

            // Assert
            result.Website.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "technology", "programming" });
            result.Website.Genres.Select(g => g.Name).Should().BeEquivalentTo(new[] { "news", "tutorial" });
        }

        [Theory]
        [InlineData("not a url")]
        [InlineData("ftp://example.com/file")]
        [InlineData("")]
        public async Task CreateWebsiteAsync_InvalidUrl_ThrowsArgumentException(string url)
        {
            var dto = TestDataFactory.CreateWebsiteDto("Site", url);

            await _websiteService.Invoking(s => s.CreateWebsiteAsync(dto))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateWebsiteAsync_ExistingUrl_ReturnsExistingRowAndAbsorbsMetadata()
        {
            // Arrange: first save has no description or topics
            var first = TestDataFactory.CreateWebsiteDto("Original Title", "https://Example.com/Page?utm_source=x");
            first.Description = null;
            var created = await _websiteService.CreateWebsiteAsync(first);

            var second = TestDataFactory.CreateWebsiteDto("Different Title", "http://www.example.com/page/");
            second.Description = "Filled in later";
            second.Topics = new List<string> { "News" };

            // Act
            var result = await _websiteService.CreateWebsiteAsync(second);

            // Assert
            result.Created.Should().BeFalse();
            result.Website.Id.Should().Be(created.Website.Id);
            result.Website.Title.Should().Be("Original Title");
            result.Website.Description.Should().Be("Filled in later");
            result.Website.Topics.Select(t => t.Name).Should().Contain("news");
            (await Context.Websites.CountAsync()).Should().Be(1);
        }

        #endregion

        #region Import from URL

        [Fact]
        public async Task ImportWebsiteFromUrlAsync_ShouldScrapeAndImportWebsite()
        {
            // Arrange
            var importDto = new ImportWebsiteDto
            {
                Url = "https://test.com",
                Notes = "Test notes",
                Topics = new List<string> { "tech" },
                Genres = new List<string> { "blog" }
            };

            var scrapedData = new ScrapedWebsiteDataDto
            {
                Url = "https://test.com",
                Title = "Scraped Title",
                Description = "Scraped Description",
                ImageUrl = "https://test.com/image.jpg",
                RssFeedUrl = "https://test.com/feed",
                Domain = "test.com",
                Author = "Test Author",
                Publication = "Test Publication"
            };

            _mockScraperService
                .ScrapeWebsiteAsync(importDto.Url)
                .Returns(scrapedData);

            // Act
            var result = await _websiteService.ImportWebsiteFromUrlAsync(importDto);

            // Assert
            result.Created.Should().BeTrue();
            var website = result.Website;
            website.Title.Should().Be("Scraped Title");
            website.Description.Should().Be("Scraped Description");
            website.Thumbnail.Should().Be("https://test.com/image.jpg");
            website.RssFeedUrl.Should().Be("https://test.com/feed");
            website.Domain.Should().Be("test.com");
            website.Author.Should().Be("Test Author");
            website.Publication.Should().Be("Test Publication");
            website.Notes.Should().Be("Test notes");
            website.Topics.Should().HaveCount(1);
            website.Genres.Should().HaveCount(1);

            await _mockScraperService.Received(1).ScrapeWebsiteAsync(importDto.Url);
        }

        [Fact]
        public async Task ImportWebsiteFromUrlAsync_ShouldUseTitleOverride_WhenProvided()
        {
            // Arrange
            var importDto = new ImportWebsiteDto
            {
                Url = "https://test.com",
                TitleOverride = "My Custom Title"
            };

            _mockScraperService
                .ScrapeWebsiteAsync(importDto.Url)
                .Returns(Scraped(importDto.Url));

            // Act
            var result = await _websiteService.ImportWebsiteFromUrlAsync(importDto);

            // Assert
            result.Website.Title.Should().Be("My Custom Title");
        }

        [Fact]
        public async Task ImportWebsiteFromUrlAsync_LeavesTheThumbnailEmpty_WhenNoImageAndNoUsableScreenshot()
        {
            // The old screenshot service handed back the render provider's own URL on failure.
            var screenshots = Substitute.For<IWebsiteScreenshotService>();
            screenshots.CaptureScreenshotAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
            var service = new WebsiteService(
                Context, _mockScraperService, _mockTypesenseService, _mockThumbnailStorage, _mockLogger, screenshots);
            _mockScraperService.ScrapeWebsiteAsync("https://test.com/plain")
                .Returns(new ScrapedWebsiteDataDto { Url = "https://test.com/plain", Title = "Plain", ImageUrl = null });

            var result = await service.ImportWebsiteFromUrlAsync(new ImportWebsiteDto { Url = "https://test.com/plain" });

            result.Created.Should().BeTrue();
            result.Website.Thumbnail.Should().BeNull();
            await screenshots.Received(1).CaptureScreenshotAsync("https://test.com/plain", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportWebsiteFromUrlAsync_ExistingUrl_ReturnsExistingWithoutScraping()
        {
            // Arrange: a legacy row with no UrlKey
            var legacy = TestDataFactory.CreateWebsite("Known Site", "https://test.com", "test.com");
            legacy.UrlKey = null;
            Context.Websites.Add(legacy);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            // Act
            var result = await _websiteService.ImportWebsiteFromUrlAsync(new ImportWebsiteDto { Url = "http://www.test.com/" });

            // Assert
            result.Created.Should().BeFalse();
            result.Website.Id.Should().Be(legacy.Id);
            result.Website.UrlKey.Should().Be("test.com", "the identity is filled in on the way past");
            await _mockScraperService.DidNotReceiveWithAnyArgs().ScrapeWebsiteAsync(default!);
            (await Context.Websites.CountAsync()).Should().Be(1);
        }

        #endregion

        #region Preview

        [Fact]
        public async Task ScrapeWebsitePreviewAsync_ReportsExistingWebsite_WhenUrlIsKnown()
        {
            var known = TestDataFactory.CreateWebsite("Known Site", "https://test.com/page", "test.com");
            known.UrlKey = "test.com/page";
            Context.Websites.Add(known);
            await Context.SaveChangesAsync();
            _mockScraperService.ScrapeWebsiteAsync("https://www.test.com/page/").Returns(Scraped("https://www.test.com/page/"));

            var preview = await _websiteService.ScrapeWebsitePreviewAsync("https://www.test.com/page/");

            preview.Title.Should().Be("Scraped Title");
            preview.ExistingWebsiteId.Should().Be(known.Id);
            preview.ExistingTitle.Should().Be("Known Site");
        }

        [Fact]
        public async Task ScrapeWebsitePreviewAsync_LeavesExistingFieldsNull_WhenUrlIsNew()
        {
            _mockScraperService.ScrapeWebsiteAsync("https://new.com").Returns(Scraped("https://new.com"));

            var preview = await _websiteService.ScrapeWebsitePreviewAsync("https://new.com");

            preview.ExistingWebsiteId.Should().BeNull();
            preview.ExistingTitle.Should().BeNull();
        }

        #endregion

        #region Update

        [Fact]
        public async Task UpdateWebsiteAsync_ShouldUpdateExistingWebsite_AsATrackedSave()
        {
            // Arrange
            var existingWebsite = TestDataFactory.CreateWebsite("Old Title", "https://old.com", "old.com");
            Context.Websites.Add(existingWebsite);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            var updateDto = TestDataFactory.CreateWebsiteDto("Updated Title", "https://updated.com");
            updateDto.Description = "Updated Description";

            // Act
            var result = await _websiteService.UpdateWebsiteAsync(existingWebsite.Id, updateDto);

            // Assert
            result.Title.Should().Be("Updated Title");
            result.Link.Should().Be("https://updated.com");
            result.Domain.Should().Be("updated.com");
            result.UrlKey.Should().Be("updated.com");
            result.Description.Should().Be("Updated Description");

            Context.ChangeTracker.Clear();
            var reloaded = await Context.Websites.SingleAsync(w => w.Id == existingWebsite.Id);
            reloaded.Title.Should().Be("Updated Title");
            reloaded.UrlKey.Should().Be("updated.com");
        }

        [Fact]
        public async Task UpdateWebsiteAsync_NullEnumFields_LeaveExistingValues()
        {
            var existing = TestDataFactory.CreateWebsite("Site", "https://site.com", "site.com");
            existing.Status = Status.Completed;
            existing.Rating = Rating.SuperLike;
            Context.Websites.Add(existing);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            var dto = TestDataFactory.CreateWebsiteDto("Site", "https://site.com");
            dto.Status = null;
            dto.Rating = null;

            var result = await _websiteService.UpdateWebsiteAsync(existing.Id, dto);

            result.Status.Should().Be(Status.Completed);
            result.Rating.Should().Be(Rating.SuperLike);
        }

        [Fact]
        public async Task UpdateWebsiteAsync_ReplacesTopicsAndGenres()
        {
            var existing = TestDataFactory.CreateWebsite("Site", "https://site.com", "site.com");
            existing.Topics.Add(new Topic { Name = "old" });
            Context.Websites.Add(existing);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            var dto = TestDataFactory.CreateWebsiteDto("Site", "https://site.com");
            dto.Topics = new List<string> { " New " };
            dto.Genres = new List<string> { "Blog" };

            var result = await _websiteService.UpdateWebsiteAsync(existing.Id, dto);

            result.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "new" });
            result.Genres.Select(g => g.Name).Should().BeEquivalentTo(new[] { "blog" });
        }

        [Fact]
        public async Task UpdateWebsiteAsync_UrlOwnedByAnotherWebsite_ThrowsInvalidOperation()
        {
            var first = TestDataFactory.CreateWebsite("First", "https://first.com", "first.com");
            first.UrlKey = "first.com";
            var second = TestDataFactory.CreateWebsite("Second", "https://second.com", "second.com");
            second.UrlKey = "second.com";
            Context.Websites.AddRange(first, second);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            var dto = TestDataFactory.CreateWebsiteDto("Second", "http://www.first.com/");

            await _websiteService.Invoking(s => s.UpdateWebsiteAsync(second.Id, dto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*{first.Id}*");
        }

        [Fact]
        public async Task UpdateWebsiteAsync_ShouldThrowKeyNotFoundException_WhenWebsiteDoesNotExist()
        {
            var nonExistentId = Guid.NewGuid();
            var updateDto = TestDataFactory.CreateWebsiteDto("Title", "https://test.com");

            await _websiteService.Invoking(s => s.UpdateWebsiteAsync(nonExistentId, updateDto))
                .Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Website with ID {nonExistentId} not found.");
        }

        #endregion

        #region Regenerate screenshot

        private WebsiteService ServiceWithScreenshots(IWebsiteScreenshotService screenshots, IScreenshotQuota? quota = null) =>
            new(Context, _mockScraperService, _mockTypesenseService, _mockThumbnailStorage, _mockLogger, screenshots, quota);

        [Fact]
        public async Task RegenerateScreenshotAsync_ReturnsNull_WhenWebsiteDoesNotExist()
        {
            var service = ServiceWithScreenshots(Substitute.For<IWebsiteScreenshotService>());

            (await service.RegenerateScreenshotAsync(Guid.NewGuid(), force: false)).Should().BeNull();
        }

        [Fact]
        public async Task RegenerateScreenshotAsync_SkipsAWebsiteThatAlreadyHasAThumbnail_UnlessForced()
        {
            var website = TestDataFactory.CreateWebsite("Site", "https://site.com", "site.com");
            website.Thumbnail = "https://site.com/og.png";
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            var screenshots = Substitute.For<IWebsiteScreenshotService>();
            var service = ServiceWithScreenshots(screenshots);

            var result = await service.RegenerateScreenshotAsync(website.Id, force: false);

            result!.Skipped.Should().BeTrue();
            result.Rendered.Should().BeFalse();
            result.Thumbnail.Should().Be("https://site.com/og.png");
            await screenshots.DidNotReceiveWithAnyArgs().CaptureScreenshotAsync(default!, default);
        }

        [Fact]
        public async Task RegenerateScreenshotAsync_Forced_ReplacesTheThumbnailAndDeletesTheOldObject()
        {
            var website = TestDataFactory.CreateWebsite("Site", "https://site.com", "site.com");
            website.Thumbnail = "https://bucket.example/screenshots/old.gif";
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            var screenshots = Substitute.For<IWebsiteScreenshotService>();
            screenshots.CaptureScreenshotAsync("https://site.com", Arg.Any<CancellationToken>())
                .Returns("https://bucket.example/screenshots/new.png");
            var service = ServiceWithScreenshots(screenshots);

            var result = await service.RegenerateScreenshotAsync(website.Id, force: true);

            result!.Rendered.Should().BeTrue();
            result.Thumbnail.Should().Be("https://bucket.example/screenshots/new.png");
            Context.ChangeTracker.Clear();
            (await Context.Websites.SingleAsync(w => w.Id == website.Id)).Thumbnail.Should().Be("https://bucket.example/screenshots/new.png");
            await _mockThumbnailStorage.Received(1).DeleteAsync("https://bucket.example/screenshots/old.gif");
        }

        [Fact]
        public async Task RegenerateScreenshotAsync_ReportsAWarning_WhenNothingUsableIsRendered()
        {
            var website = TestDataFactory.CreateWebsite("Site", "https://site.com", "site.com");
            website.Thumbnail = null;
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            var screenshots = Substitute.For<IWebsiteScreenshotService>();
            screenshots.CaptureScreenshotAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
            var service = ServiceWithScreenshots(screenshots);

            var result = await service.RegenerateScreenshotAsync(website.Id, force: false);

            result!.Success.Should().BeTrue();
            result.Rendered.Should().BeFalse();
            result.WarningMessage.Should().NotBeNullOrEmpty();
            (await Context.Websites.SingleAsync(w => w.Id == website.Id)).Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task RegenerateScreenshotAsync_DoesNotRender_WhenTheQuotaIsExhausted()
        {
            var website = TestDataFactory.CreateWebsite("Site", "https://site.com", "site.com");
            website.Thumbnail = null;
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            var screenshots = Substitute.For<IWebsiteScreenshotService>();
            var quota = Substitute.For<IScreenshotQuota>();
            quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(0);
            var service = ServiceWithScreenshots(screenshots, quota);

            var result = await service.RegenerateScreenshotAsync(website.Id, force: true);

            result!.Rendered.Should().BeFalse();
            result.WarningMessage.Should().Contain("quota");
            await screenshots.DidNotReceiveWithAnyArgs().CaptureScreenshotAsync(default!, default);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task DeleteWebsiteAsync_ShouldDeleteWebsite_AndRemoveItFromTheSearchIndex()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite("To Delete", "https://delete.com", "delete.com");
            website.Thumbnail = null;
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            // Act
            var result = await _websiteService.DeleteWebsiteAsync(website.Id);

            // Assert
            result.Should().BeTrue();
            (await Context.Websites.FindAsync(website.Id)).Should().BeNull();
            await _mockTypesenseService.Received(1).DeleteMediaItemAsync(website.Id);
            await _mockThumbnailStorage.DidNotReceiveWithAnyArgs().DeleteAsync(default);
        }

        [Fact]
        public async Task DeleteWebsiteAsync_DeletesTheStoredThumbnail()
        {
            var website = TestDataFactory.CreateWebsite("To Delete", "https://delete.com", "delete.com");
            website.Thumbnail = "https://bucket.example/screenshots/abc.png";
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            await _websiteService.DeleteWebsiteAsync(website.Id);

            await _mockThumbnailStorage.Received(1).DeleteAsync("https://bucket.example/screenshots/abc.png");
        }

        [Fact]
        public async Task DeleteWebsiteAsync_SearchIndexFailure_DoesNotAbortTheDelete()
        {
            var website = TestDataFactory.CreateWebsite("To Delete", "https://delete.com", "delete.com");
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            _mockTypesenseService.DeleteMediaItemAsync(Arg.Any<Guid>())
                .Returns(Task.FromException(new HttpRequestException("Typesense down")));

            var result = await _websiteService.DeleteWebsiteAsync(website.Id);

            result.Should().BeTrue();
            (await Context.Websites.FindAsync(website.Id)).Should().BeNull();
        }

        [Fact]
        public async Task DeleteWebsiteAsync_ShouldReturnFalse_WhenWebsiteDoesNotExist()
        {
            var result = await _websiteService.DeleteWebsiteAsync(Guid.NewGuid());

            result.Should().BeFalse();
            await _mockTypesenseService.DidNotReceiveWithAnyArgs().DeleteMediaItemAsync(default);
        }

        #endregion
    }
}
