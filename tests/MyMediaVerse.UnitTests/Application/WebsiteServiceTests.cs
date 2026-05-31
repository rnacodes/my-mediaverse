using AwesomeAssertions;
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
        private readonly WebsiteService _websiteService;

        public WebsiteServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<WebsiteService>>();
            _mockScraperService = Substitute.For<IWebsiteScraperService>();
            _websiteService = new WebsiteService(Context, _mockScraperService, _mockLogger);
        }

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
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _websiteService.GetWebsiteByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateWebsiteAsync_ShouldCreateNewWebsite()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("New Website", "https://newsite.com");

            // Act
            var result = await _websiteService.CreateWebsiteAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Website");
            result.Link.Should().Be("https://newsite.com");
            result.Domain.Should().Be("newsite.com");
            result.MediaType.Should().Be(MediaType.Website);
            
            // Verify the website was saved to the database
            var savedWebsite = await Context.Websites.FindAsync(result.Id);
            savedWebsite.Should().NotBeNull();
            savedWebsite!.Title.Should().Be("New Website");
        }

        [Fact]
        public async Task CreateWebsiteAsync_ShouldCreateTopicsAndGenres_WhenProvided()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("Website with Tags", "https://tagged.com");
            dto.Topics = new List<string> { "Technology", "Programming" };
            dto.Genres = new List<string> { "News", "Tutorial" };

            // Act
            var result = await _websiteService.CreateWebsiteAsync(dto);

            // Assert
            result.Topics.Should().HaveCount(2);
            result.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "technology", "programming" }); // lowercase per standards
            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().BeEquivalentTo(new[] { "news", "tutorial" }); // lowercase per standards
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
            result.Should().NotBeNull();
            result.Title.Should().Be("Scraped Title");
            result.Description.Should().Be("Scraped Description");
            result.Thumbnail.Should().Be("https://test.com/image.jpg");
            result.RssFeedUrl.Should().Be("https://test.com/feed");
            result.Domain.Should().Be("test.com");
            result.Author.Should().Be("Test Author");
            result.Publication.Should().Be("Test Publication");
            result.Notes.Should().Be("Test notes");
            result.Topics.Should().HaveCount(1);
            result.Genres.Should().HaveCount(1);

            _mockScraperService.Received(1).ScrapeWebsiteAsync(importDto.Url);
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

            var scrapedData = new ScrapedWebsiteDataDto
            {
                Url = "https://test.com",
                Title = "Scraped Title",
                Domain = "test.com"
            };

            _mockScraperService
                .ScrapeWebsiteAsync(importDto.Url)
                .Returns(scrapedData);

            // Act
            var result = await _websiteService.ImportWebsiteFromUrlAsync(importDto);

            // Assert
            result.Title.Should().Be("My Custom Title");
        }

        [Fact]
        public async Task UpdateWebsiteAsync_ShouldUpdateExistingWebsite()
        {
            // Arrange
            var existingWebsite = TestDataFactory.CreateWebsite("Old Title", "https://old.com", "old.com");
            Context.Websites.Add(existingWebsite);
            await Context.SaveChangesAsync();
            
            // Clear change tracker to avoid entity tracking conflicts
            Context.ChangeTracker.Clear();

            var updateDto = TestDataFactory.CreateWebsiteDto("Updated Title", "https://updated.com");
            updateDto.Description = "Updated Description";

            // Act
            var result = await _websiteService.UpdateWebsiteAsync(existingWebsite.Id, updateDto);

            // Assert
            result.Title.Should().Be("Updated Title");
            result.Link.Should().Be("https://updated.com");
            result.Domain.Should().Be("updated.com");
            result.Description.Should().Be("Updated Description");
        }

        [Fact]
        public async Task UpdateWebsiteAsync_ShouldThrowKeyNotFoundException_WhenWebsiteDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = TestDataFactory.CreateWebsiteDto("Title", "https://test.com");

            // Act & Assert
            await _websiteService.Invoking(s => s.UpdateWebsiteAsync(nonExistentId, updateDto))
                .Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Website with ID {nonExistentId} not found.");
        }

        [Fact]
        public async Task DeleteWebsiteAsync_ShouldDeleteWebsite_WhenWebsiteExists()
        {
            // Arrange
            var website = TestDataFactory.CreateWebsite("To Delete", "https://delete.com", "delete.com");
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();
            
            // Clear change tracker to avoid entity tracking conflicts
            Context.ChangeTracker.Clear();

            // Act
            var result = await _websiteService.DeleteWebsiteAsync(website.Id);

            // Assert
            result.Should().BeTrue();
            var deletedWebsite = await Context.Websites.FindAsync(website.Id);
            deletedWebsite.Should().BeNull();
        }

        [Fact]
        public async Task DeleteWebsiteAsync_ShouldReturnFalse_WhenWebsiteDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _websiteService.DeleteWebsiteAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }
    }
}

