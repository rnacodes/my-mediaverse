using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class WebsiteDuplicateFinderTests : InMemoryDbTestBase
    {
        private Website AddWebsite(string link, string? urlKey, string title = "Some Site")
        {
            var website = TestDataFactory.CreateWebsite(title, link, UrlNormalizer.ExtractDomain(link));
            website.UrlKey = urlKey;
            Context.Websites.Add(website);
            return website;
        }

        [Fact]
        public async Task FindExistingAsync_MatchesOnUrlKey_IgnoringSchemeWwwTrackingAndFragment()
        {
            var target = AddWebsite("https://example.com/page", "example.com/page");
            AddWebsite("https://example.com/other", "example.com/other");
            await Context.SaveChangesAsync();

            var match = await WebsiteDuplicateFinder.FindExistingAsync(
                Context.Websites, "HTTP://WWW.Example.com/Page/?utm_campaign=x#top");

            match!.Id.Should().Be(target.Id);
        }

        [Theory]
        [InlineData("http://example.com/page")]
        [InlineData("https://www.example.com/page/")]
        [InlineData("https://example.com/page?utm_source=newsletter#frag")]
        public async Task FindExistingAsync_LegacyRowWithoutUrlKey_MatchesOnStoredLink(string probe)
        {
            // Rows saved before the key column existed keep a raw, scheme-bearing link.
            var legacy = AddWebsite("https://Example.com/Page", urlKey: null);
            await Context.SaveChangesAsync();

            var match = await WebsiteDuplicateFinder.FindExistingAsync(Context.Websites, probe);

            match!.Id.Should().Be(legacy.Id);
        }

        [Fact]
        public async Task FindExistingAsync_ReturnsNull_WhenNoRowMatches()
        {
            AddWebsite("https://example.com/page", "example.com/page");
            await Context.SaveChangesAsync();

            var match = await WebsiteDuplicateFinder.FindExistingAsync(Context.Websites, "https://example.com/elsewhere");

            match.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task FindExistingAsync_ReturnsNull_ForBlankUrl(string? url)
        {
            AddWebsite("https://example.com/page", "example.com/page");
            await Context.SaveChangesAsync();

            (await WebsiteDuplicateFinder.FindExistingAsync(Context.Websites, url)).Should().BeNull();
        }

        [Fact]
        public void FillIdentity_SetsUrlKeyAndDomainFromLink_OnlyWhenMissing()
        {
            var website = TestDataFactory.CreateWebsite("Site", "https://www.Example.com/Path/", domain: "");
            website.UrlKey = null;
            website.Domain = null;

            WebsiteDuplicateFinder.FillIdentity(website).Should().BeTrue();

            website.UrlKey.Should().Be("example.com/path");
            website.Domain.Should().Be("example.com");
            WebsiteDuplicateFinder.FillIdentity(website).Should().BeFalse("nothing is missing any more");
        }

        [Fact]
        public void AbsorbMetadata_FillsOnlyEmptyFields_AndNeverTouchesTitle()
        {
            var existing = TestDataFactory.CreateWebsite("Kept Title", "https://example.com", "example.com");
            existing.UrlKey = "example.com";
            existing.Description = "Existing description";
            existing.Thumbnail = null;
            existing.RssFeedUrl = null;
            existing.Author = "  ";
            existing.Publication = null;
            existing.Notes = null;

            var incoming = TestDataFactory.CreateWebsite("Incoming Title", "https://example.com", "example.com");
            incoming.Description = "Incoming description";
            incoming.Thumbnail = "https://example.com/og.png";
            incoming.RssFeedUrl = "https://example.com/feed";
            incoming.Author = "Author";
            incoming.Publication = "Publication";
            incoming.Notes = "Notes";

            WebsiteDuplicateFinder.AbsorbMetadata(existing, incoming).Should().BeTrue();

            existing.Title.Should().Be("Kept Title");
            existing.Description.Should().Be("Existing description");
            existing.Thumbnail.Should().Be("https://example.com/og.png");
            existing.RssFeedUrl.Should().Be("https://example.com/feed");
            existing.Author.Should().Be("Author", "whitespace counts as empty");
            existing.Publication.Should().Be("Publication");
            existing.Notes.Should().Be("Notes");
        }

        [Fact]
        public void AbsorbMetadata_AddsMissingTopicsAndGenres_WithoutDuplicating()
        {
            var shared = new Topic { Name = "tech" };
            var existing = TestDataFactory.CreateWebsite("Site", "https://example.com", "example.com");
            existing.UrlKey = "example.com";
            existing.Topics.Add(shared);
            existing.Genres.Add(new Genre { Name = "blog" });

            var incoming = TestDataFactory.CreateWebsite("Site", "https://example.com", "example.com");
            incoming.Topics.Add(new Topic { Name = "tech" });
            incoming.Topics.Add(new Topic { Name = "news" });
            incoming.Genres.Add(new Genre { Name = "blog" });
            incoming.Genres.Add(new Genre { Name = "tutorial" });

            WebsiteDuplicateFinder.AbsorbMetadata(existing, incoming).Should().BeTrue();

            existing.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "tech", "news" });
            existing.Topics.Should().Contain(shared, "the existing Topic instance is kept");
            existing.Genres.Select(g => g.Name).Should().BeEquivalentTo(new[] { "blog", "tutorial" });
        }

        [Fact]
        public void AbsorbMetadata_ReturnsFalse_WhenNothingChanges()
        {
            var existing = TestDataFactory.CreateWebsite("Site", "https://example.com", "example.com");
            existing.UrlKey = "example.com";
            existing.Description = "Full";
            existing.Thumbnail = "https://example.com/og.png";
            existing.Topics.Add(new Topic { Name = "tech" });

            var incoming = TestDataFactory.CreateWebsite("Other", "https://example.com", "example.com");
            incoming.Description = "Different";
            incoming.Thumbnail = "https://example.com/other.png";
            incoming.Topics.Add(new Topic { Name = "tech" });

            WebsiteDuplicateFinder.AbsorbMetadata(existing, incoming).Should().BeFalse();

            existing.Description.Should().Be("Full");
            existing.Thumbnail.Should().Be("https://example.com/og.png");
        }
    }
}
