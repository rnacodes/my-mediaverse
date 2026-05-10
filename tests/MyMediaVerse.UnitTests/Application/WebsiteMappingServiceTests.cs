using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class WebsiteMappingServiceTests
    {
        private readonly ILogger<WebsiteMappingService> _mockLogger;
        private readonly WebsiteMappingService _service;

        public WebsiteMappingServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<WebsiteMappingService>>();
            _service = new WebsiteMappingService(_mockLogger);
        }

        #region MapToResponseDtoAsync (single)

        [Fact]
        public async Task MapToResponseDtoAsync_ValidWebsite_MapsAllProperties()
        {
            var website = TestDataFactory.CreateWebsite("My Blog", "https://myblog.com", "myblog.com");
            website.Description = "A great blog";
            website.RssFeedUrl = "https://myblog.com/rss";
            website.Author = "John Doe";
            website.Publication = "Personal Blog";
            website.Rating = Rating.Like;
            website.Notes = "Good site";
            website.ArchiveUrl = "https://archive.org/myblog";
            website.ArchivedAt = new DateTime(2024, 1, 1);
            website.ArchiveStatus = "archived";
            website.WaybackUrl = "https://web.archive.org/web/myblog";
            website.Topics.Add(new Topic { Name = "tech" });
            website.Genres.Add(new Genre { Name = "blog" });

            var result = await _service.MapToResponseDtoAsync(website);

            result.Should().NotBeNull();
            result.Id.Should().Be(website.Id);
            result.Title.Should().Be("My Blog");
            result.Link.Should().Be("https://myblog.com");
            result.Domain.Should().Be("myblog.com");
            result.Description.Should().Be("A great blog");
            result.RssFeedUrl.Should().Be("https://myblog.com/rss");
            result.Author.Should().Be("John Doe");
            result.Publication.Should().Be("Personal Blog");
            result.Rating.Should().Be(Rating.Like.ToString());
            result.Notes.Should().Be("Good site");
            result.ArchiveUrl.Should().Be("https://archive.org/myblog");
            result.ArchiveStatus.Should().Be("archived");
            result.WaybackUrl.Should().Be("https://web.archive.org/web/myblog");
            result.Topics.Should().Contain("tech");
            result.Genres.Should().Contain("blog");
            result.MediaType.Should().Be(MediaType.Website.ToString());
            result.Status.Should().Be(Status.Uncharted.ToString());
        }

        [Fact]
        public async Task MapToResponseDtoAsync_NullTopicsAndGenres_DefaultsToEmptyLists()
        {
            var website = TestDataFactory.CreateWebsite();
            website.Topics = null;
            website.Genres = null;

            var result = await _service.MapToResponseDtoAsync(website);

            result.Topics.Should().NotBeNull();
            result.Topics.Should().BeEmpty();
            result.Genres.Should().NotBeNull();
            result.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task MapToResponseDtoAsync_NullRating_RatingIsNull()
        {
            var website = TestDataFactory.CreateWebsite();
            website.Rating = null;

            var result = await _service.MapToResponseDtoAsync(website);

            result.Rating.Should().BeNull();
        }

        #endregion

        #region MapToResponseDtoAsync (collection)

        [Fact]
        public async Task MapToResponseDtoAsync_MultipleWebsites_MapsAll()
        {
            var websites = TestDataFactory.CreateWebsites(3);

            var result = await _service.MapToResponseDtoAsync(websites);

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task MapToResponseDtoAsync_EmptyCollection_ReturnsEmpty()
        {
            var websites = new List<Website>();

            var result = await _service.MapToResponseDtoAsync(websites);

            result.Should().BeEmpty();
        }

        #endregion
    }
}
