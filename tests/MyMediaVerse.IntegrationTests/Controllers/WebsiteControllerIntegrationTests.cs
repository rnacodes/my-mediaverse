using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    public class WebsiteControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;

        public WebsiteControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetAllWebsites_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/website");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var websites = await response.Content.ReadFromJsonAsync<IEnumerable<WebsiteResponseDto>>();
            websites.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateWebsite_ShouldCreateWebsite_WhenValidDataProvided()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("Test Website", "https://test.com");
            dto.Description = "A test website";
            dto.Notes = "Test notes";
            dto.Topics = new List<string> { "technology", "web" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/website", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdWebsite = await response.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            createdWebsite.Should().NotBeNull();
            createdWebsite!.Title.Should().Be(dto.Title);
            createdWebsite.Link.Should().Be(dto.Url);
            createdWebsite.Domain.Should().Be("test.com");
            createdWebsite.Description.Should().Be(dto.Description);
            createdWebsite.Notes.Should().Be(dto.Notes);
            createdWebsite.Topics.Should().Contain("technology");
            createdWebsite.Topics.Should().Contain("web");
        }

        [Fact]
        public async Task CreateWebsite_ShouldReturnBadRequest_WhenInvalidDataProvided()
        {
            // Arrange
            var dto = new CreateWebsiteDto
            {
                Title = "Test",
                Url = "not-a-valid-url" // Invalid URL
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/website", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetWebsiteById_ShouldReturnWebsite_WhenWebsiteExists()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("Test Website", "https://gettest.com");
            var createResponse = await _client.PostAsJsonAsync("/api/website", dto);
            var createdWebsite = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            // Act
            var response = await _client.GetAsync($"/api/website/{createdWebsite!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var website = await response.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            website.Should().NotBeNull();
            website!.Id.Should().Be(createdWebsite.Id);
            website.Title.Should().Be(createdWebsite.Title);
            website.Domain.Should().Be("gettest.com");
        }

        [Fact]
        public async Task GetWebsiteById_ShouldReturnNotFound_WhenWebsiteDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/website/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateWebsite_ShouldUpdateWebsite_WhenWebsiteExists()
        {
            // Arrange
            var createDto = TestDataFactory.CreateWebsiteDto("Original Title", "https://original.com");
            var createResponse = await _client.PostAsJsonAsync("/api/website", createDto);
            var createdWebsite = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var updateDto = TestDataFactory.CreateWebsiteDto("Updated Title", "https://updated.com");
            updateDto.Description = "Updated description";
            updateDto.RssFeedUrl = "https://updated.com/feed";

            // Act
            var response = await _client.PutAsJsonAsync($"/api/website/{createdWebsite!.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedWebsite = await response.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            updatedWebsite.Should().NotBeNull();
            updatedWebsite!.Title.Should().Be("Updated Title");
            updatedWebsite.Link.Should().Be("https://updated.com");
            updatedWebsite.Domain.Should().Be("updated.com");
            updatedWebsite.Description.Should().Be("Updated description");
            updatedWebsite.RssFeedUrl.Should().Be("https://updated.com/feed");
        }

        [Fact]
        public async Task DeleteWebsite_ShouldDeleteWebsite_WhenWebsiteExists()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("To Delete", "https://delete.com");
            var createResponse = await _client.PostAsJsonAsync("/api/website", dto);
            var createdWebsite = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/website/{createdWebsite!.Id}");

            // Assert
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify website is deleted
            var getResponse = await _client.GetAsync($"/api/website/{createdWebsite.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteWebsite_ShouldReturnNotFound_WhenWebsiteDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/website/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetWebsitesByDomain_ShouldReturnWebsitesFromSameDomain()
        {
            // Arrange
            var uniqueDomain = $"testdomain{Guid.NewGuid():N}.com";
            var dto1 = TestDataFactory.CreateWebsiteDto("Site 1", $"https://{uniqueDomain}/page1");
            var dto2 = TestDataFactory.CreateWebsiteDto("Site 2", $"https://{uniqueDomain}/page2");
            var dto3 = TestDataFactory.CreateWebsiteDto("Site 3", "https://other.com");

            await _client.PostAsJsonAsync("/api/website", dto1);
            await _client.PostAsJsonAsync("/api/website", dto2);
            await _client.PostAsJsonAsync("/api/website", dto3);

            // Act
            var response = await _client.GetAsync($"/api/website/by-domain/{uniqueDomain}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var websites = await response.Content.ReadFromJsonAsync<IEnumerable<WebsiteResponseDto>>();
            websites.Should().NotBeNull();
            websites!.Should().HaveCount(2);
            websites.Should().OnlyContain(w => w.Domain == uniqueDomain);
        }

        [Fact]
        public async Task GetWebsitesWithRss_ShouldReturnOnlyWebsitesWithRssFeeds()
        {
            // Arrange - Create unique URLs to avoid conflicts with other tests
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var dtoWithRss1 = TestDataFactory.CreateWebsiteDto("With RSS 1", $"https://rss1{uniqueId}.com");
            dtoWithRss1.RssFeedUrl = $"https://rss1{uniqueId}.com/feed";

            var dtoWithRss2 = TestDataFactory.CreateWebsiteDto("With RSS 2", $"https://rss2{uniqueId}.com");
            dtoWithRss2.RssFeedUrl = $"https://rss2{uniqueId}.com/feed";

            var dtoWithoutRss = TestDataFactory.CreateWebsiteDto("No RSS", $"https://norss{uniqueId}.com");

            await _client.PostAsJsonAsync("/api/website", dtoWithRss1);
            await _client.PostAsJsonAsync("/api/website", dtoWithRss2);
            await _client.PostAsJsonAsync("/api/website", dtoWithoutRss);

            // Act
            var response = await _client.GetAsync("/api/website/with-rss");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var websites = await response.Content.ReadFromJsonAsync<IEnumerable<WebsiteResponseDto>>();
            websites.Should().NotBeNull();
            // At least 2 websites with RSS should be present (the ones we just created)
            var websitesWithRss = websites!.Where(w => !string.IsNullOrEmpty(w.RssFeedUrl)).ToList();
            websitesWithRss.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task CreateWebsite_ShouldNormalizeTopicsAndGenresToLowercase()
        {
            // Arrange
            var dto = TestDataFactory.CreateWebsiteDto("Test", "https://test.com");
            dto.Topics = new List<string> { "TECHNOLOGY", "Programming" };
            dto.Genres = new List<string> { "NEWS", "Tutorial" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/website", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdWebsite = await response.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            createdWebsite.Should().NotBeNull();
            createdWebsite!.Topics.Should().Contain("technology");
            createdWebsite.Topics.Should().Contain("programming");
            createdWebsite.Genres.Should().Contain("news");
            createdWebsite.Genres.Should().Contain("tutorial");
        }
    }
}

