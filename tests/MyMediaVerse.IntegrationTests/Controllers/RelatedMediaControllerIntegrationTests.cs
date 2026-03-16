using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class RelatedMediaControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public RelatedMediaControllerIntegrationTests(WebApplicationFactory factory)
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

        private async Task<BookResponseDto> CreateTestBookAsync(string? suffix = null)
        {
            suffix ??= Guid.NewGuid().ToString()[..8];
            var dto = TestDataFactory.CreateBookDto($"Related Book {suffix}", $"Author {suffix}");
            var response = await _client.PostAsJsonAsync("/api/book", dto);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<BookResponseDto>(_jsonOptions))!;
        }

        #region GetRelatedMedia

        [Fact]
        public async Task GetRelatedMedia_ShouldReturnNotFound_WhenMediaItemDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/relatedmedia/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetRelatedMedia_ShouldReturnEmptyList_WhenNoRelationsExist()
        {
            var book = await CreateTestBookAsync();

            var response = await _client.GetAsync($"/api/relatedmedia/{book.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var related = await response.Content.ReadFromJsonAsync<IEnumerable<RelatedMediaResponseDto>>(_jsonOptions);
            related.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region SaveRelatedMedia

        [Fact]
        public async Task SaveRelatedMedia_ShouldCreateRelation_WhenBothItemsExist()
        {
            var book1 = await CreateTestBookAsync("source");
            var book2 = await CreateTestBookAsync("related");

            var dto = new SaveRelatedMediaDto
            {
                RelatedMediaItemId = book2.Id,
                Source = "ManuallyAdded",
                Note = "Test relation"
            };

            var response = await _client.PostAsJsonAsync($"/api/relatedmedia/{book1.Id}", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var relation = await response.Content.ReadFromJsonAsync<RelatedMediaResponseDto>(_jsonOptions);
            relation.Should().NotBeNull();
            relation!.SourceMediaItemId.Should().Be(book1.Id);
            relation.RelatedMediaItemId.Should().Be(book2.Id);
        }

        #endregion

        #region RemoveRelatedMedia

        [Fact]
        public async Task RemoveRelatedMedia_ShouldRemoveRelation_WhenRelationExists()
        {
            var book1 = await CreateTestBookAsync("remove-src");
            var book2 = await CreateTestBookAsync("remove-rel");

            var saveDto = new SaveRelatedMediaDto
            {
                RelatedMediaItemId = book2.Id,
                Source = "ManuallyAdded"
            };
            await _client.PostAsJsonAsync($"/api/relatedmedia/{book1.Id}", saveDto);

            var response = await _client.DeleteAsync($"/api/relatedmedia/{book1.Id}/{book2.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/relatedmedia/{book1.Id}");
            var related = await getResponse.Content.ReadFromJsonAsync<IEnumerable<RelatedMediaResponseDto>>(_jsonOptions);
            related.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region SaveRelatedMediaBatch

        [Fact]
        public async Task SaveRelatedMediaBatch_ShouldCreateMultipleRelations()
        {
            var source = await CreateTestBookAsync("batch-src");
            var related1 = await CreateTestBookAsync("batch-rel1");
            var related2 = await CreateTestBookAsync("batch-rel2");

            var dtos = new List<SaveRelatedMediaDto>
            {
                new SaveRelatedMediaDto { RelatedMediaItemId = related1.Id, Source = "ManuallyAdded" },
                new SaveRelatedMediaDto { RelatedMediaItemId = related2.Id, Source = "ManuallyAdded" }
            };

            var response = await _client.PostAsJsonAsync($"/api/relatedmedia/{source.Id}/batch", dtos);

            response.IsSuccessStatusCode.Should().BeTrue();

            var getResponse = await _client.GetAsync($"/api/relatedmedia/{source.Id}");
            var related = await getResponse.Content.ReadFromJsonAsync<IEnumerable<RelatedMediaResponseDto>>(_jsonOptions);
            related.Should().NotBeNull();
            related!.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        #endregion
    }
}
