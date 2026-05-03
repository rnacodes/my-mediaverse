using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class ArticleControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public ArticleControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private CreateArticleDto CreateValidArticleDto(string? suffix = null)
        {
            suffix ??= Guid.NewGuid().ToString()[..8];
            return new CreateArticleDto
            {
                Title = $"Test Article {suffix}",
                Link = $"https://example.com/article-{suffix}",
                Author = $"Author {suffix}",
                Status = Status.Uncharted,
                Description = "A test article description"
            };
        }

        private async Task<ArticleResponseDto> CreateArticleAsync(CreateArticleDto? dto = null)
        {
            dto ??= CreateValidArticleDto();
            // Clear Link to avoid EF.Functions.ILike duplicate check which InMemory DB doesn't support
            dto.Link = null;
            var response = await _client.PostAsJsonAsync("/api/article", dto, _jsonOptions);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ArticleResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.Id.Should().NotBe(Guid.Empty);
            return created;
        }

        #region GetAllArticles

        [Fact]
        public async Task GetAllArticles_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/article");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var articles = await response.Content.ReadFromJsonAsync<IEnumerable<ArticleResponseDto>>(_jsonOptions);
            articles.Should().NotBeNull();
        }

        #endregion

        #region CreateArticle

        [Fact]
        public async Task CreateArticle_ShouldReturnCreated_WhenValidDataProvided()
        {
            var dto = CreateValidArticleDto();

            var response = await _client.PostAsJsonAsync("/api/article", dto, _jsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<ArticleResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.Title.Should().Be(dto.Title);
            created.Author.Should().Be(dto.Author);
            created.Description.Should().Be(dto.Description);
        }

        [Fact]
        public async Task CreateArticle_ShouldReturnBadRequest_WhenTitleIsEmpty()
        {
            var dto = new CreateArticleDto
            {
                Title = "",
                Author = "Test Author"
            };

            var response = await _client.PostAsJsonAsync("/api/article", dto, _jsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetArticle

        [Fact]
        public async Task GetArticle_ShouldReturnOk_WhenArticleExists()
        {
            var created = await CreateArticleAsync();

            var response = await _client.GetAsync($"/api/article/{created.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var article = await response.Content.ReadFromJsonAsync<ArticleResponseDto>(_jsonOptions);
            article.Should().NotBeNull();
            article!.Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task GetArticle_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/article/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region UpdateArticle

        [Fact]
        public async Task UpdateArticle_ShouldReturnOk_WhenArticleExists()
        {
            var created = await CreateArticleAsync();

            var updateDto = CreateValidArticleDto("updated");
            updateDto.Description = "Updated description";

            var response = await _client.PutAsJsonAsync($"/api/article/{created.Id}", updateDto, _jsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<ArticleResponseDto>(_jsonOptions);
            updated.Should().NotBeNull();
            updated!.Title.Should().Be(updateDto.Title);
            updated.Description.Should().Be("Updated description");
        }

        [Fact]
        public async Task UpdateArticle_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var dto = CreateValidArticleDto();

            var response = await _client.PutAsJsonAsync($"/api/article/{Guid.NewGuid()}", dto, _jsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region DeleteArticle

        [Fact]
        public async Task DeleteArticle_ShouldDeleteArticle_WhenArticleExists()
        {
            var created = await CreateArticleAsync();

            var response = await _client.DeleteAsync($"/api/article/{created.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/article/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteArticle_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var response = await _client.DeleteAsync($"/api/article/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Filtering Endpoints

        [Fact]
        public async Task GetArchivedArticles_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/article/archived");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var articles = await response.Content.ReadFromJsonAsync<IEnumerable<ArticleResponseDto>>(_jsonOptions);
            articles.Should().NotBeNull();
        }

        [Fact]
        public async Task GetStarredArticles_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/article/starred");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var articles = await response.Content.ReadFromJsonAsync<IEnumerable<ArticleResponseDto>>(_jsonOptions);
            articles.Should().NotBeNull();
        }

        [Fact]
        public async Task GetArticlesByAuthor_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/article/by-author/TestAuthor");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion
    }
}
