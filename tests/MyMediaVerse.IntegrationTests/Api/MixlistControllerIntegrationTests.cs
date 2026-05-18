using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class MixlistControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MixlistControllerIntegrationTests(ApiFactory factory)
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

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region GET Tests

        [Fact]
        public async Task GetAllMixlists_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/mixlist");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mixlists = JsonSerializer.Deserialize<List<MixlistResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mixlists);
        }

        [Fact]
        public async Task GetMixlist_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a mixlist
            var createDto = new CreateMixlistDto
            {
                Name = "Test Mixlist for Get",
                Description = "A test mixlist description"
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/mixlist", createContent);
            var createdMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdMixlist);

            // Act
            var response = await _client.GetAsync($"/api/mixlist/{createdMixlist.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mixlist = JsonSerializer.Deserialize<MixlistResponseDto>(content, _jsonOptions);
            Assert.NotNull(mixlist);
            Assert.Equal(createdMixlist.Id, mixlist.Id);
            Assert.Equal("Test Mixlist for Get", mixlist.Name);
        }

        [Fact]
        public async Task GetMixlist_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/mixlist/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreateMixlist_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMixlistDto
            {
                Name = "New Test Mixlist",
                Description = "A comprehensive test mixlist",
                Thumbnail = "https://example.com/thumb.jpg"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/mixlist", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdMixlist);
            Assert.Equal("New Test Mixlist", createdMixlist.Name);
            Assert.Equal("A comprehensive test mixlist", createdMixlist.Description);
            Assert.Equal("https://example.com/thumb.jpg", createdMixlist.Thumbnail);
            Assert.Empty(createdMixlist.MediaItemIds);
        }

        [Fact]
        public async Task CreateMixlist_WithMinimalData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMixlistDto
            {
                Name = "Minimal Mixlist"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/mixlist", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdMixlist);
            Assert.Equal("Minimal Mixlist", createdMixlist.Name);
        }

        [Fact]
        public async Task CreateMixlist_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateMixlistDto { Name = "" };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/mixlist", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdateMixlist_WithValidData_ShouldReturnOk()
        {
            // Arrange - First create a mixlist
            var createDto = new CreateMixlistDto
            {
                Name = "Original Mixlist Name",
                Description = "Original description"
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/mixlist", createContent);
            var createdMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdMixlist);

            // Now update it
            var updateDto = new CreateMixlistDto
            {
                Name = "Updated Mixlist Name",
                Description = "Updated description",
                Thumbnail = "https://example.com/new-thumb.jpg"
            };

            var updateContent = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/mixlist/{createdMixlist.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(updatedMixlist);
            Assert.Equal("Updated Mixlist Name", updatedMixlist.Name);
            Assert.Equal("Updated description", updatedMixlist.Description);
            Assert.Equal("https://example.com/new-thumb.jpg", updatedMixlist.Thumbnail);
        }

        [Fact]
        public async Task UpdateMixlist_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new CreateMixlistDto { Name = "Updated Name" };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/mixlist/{invalidId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteMixlist_WithValidId_ShouldReturnNoContent()
        {
            // Arrange - First create a mixlist
            var createDto = new CreateMixlistDto
            {
                Name = "Mixlist to Delete"
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/mixlist", createContent);
            var createdMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdMixlist);

            // Act
            var response = await _client.DeleteAsync($"/api/mixlist/{createdMixlist.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the mixlist is actually deleted
            var getResponse = await _client.GetAsync($"/api/mixlist/{createdMixlist.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMixlist_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/mixlist/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Media Item Management Tests

        [Fact]
        public async Task AddMediaItemToMixlist_WithValidIds_ShouldReturnOk()
        {
            // Arrange - Create a mixlist
            var mixlistDto = new CreateMixlistDto { Name = "Mixlist for Media" };
            var mixlistContent = new StringContent(JsonSerializer.Serialize(mixlistDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mixlistResponse = await _client.PostAsync("/api/mixlist", mixlistContent);
            var mixlist = JsonSerializer.Deserialize<MixlistResponseDto>(await mixlistResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(mixlist);

            // Create a media item
            var mediaDto = new CreateMediaItemDto
            {
                Title = "Media for Mixlist",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };
            var mediaContent = new StringContent(JsonSerializer.Serialize(mediaDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mediaResponse = await _client.PostAsync("/api/media", mediaContent);
            var media = JsonSerializer.Deserialize<MediaItemResponseDto>(await mediaResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(media);

            // Act
            var response = await _client.PostAsync($"/api/mixlist/{mixlist.Id}/items/{media.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify the media item was added
            var getMixlistResponse = await _client.GetAsync($"/api/mixlist/{mixlist.Id}");
            var updatedMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(await getMixlistResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(updatedMixlist);
            Assert.Contains(media.Id, updatedMixlist.MediaItemIds);
        }

        [Fact]
        public async Task AddMediaItemToMixlist_WithInvalidMixlistId_ShouldReturnNotFound()
        {
            // Arrange - Create a media item
            var mediaDto = new CreateMediaItemDto
            {
                Title = "Media for Invalid Mixlist",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };
            var mediaContent = new StringContent(JsonSerializer.Serialize(mediaDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mediaResponse = await _client.PostAsync("/api/media", mediaContent);
            var media = JsonSerializer.Deserialize<MediaItemResponseDto>(await mediaResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(media);

            var invalidMixlistId = Guid.NewGuid();

            // Act
            var response = await _client.PostAsync($"/api/mixlist/{invalidMixlistId}/items/{media.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMediaItemFromMixlist_WithValidIds_ShouldReturnOk()
        {
            // Arrange - Create mixlist and media, then add media to mixlist
            var mixlistDto = new CreateMixlistDto { Name = "Mixlist for Removal" };
            var mixlistContent = new StringContent(JsonSerializer.Serialize(mixlistDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mixlistResponse = await _client.PostAsync("/api/mixlist", mixlistContent);
            var mixlist = JsonSerializer.Deserialize<MixlistResponseDto>(await mixlistResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(mixlist);

            var mediaDto = new CreateMediaItemDto { Title = "Media to Remove", MediaType = MediaType.Article, Status = Status.Uncharted };
            var mediaContent = new StringContent(JsonSerializer.Serialize(mediaDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mediaResponse = await _client.PostAsync("/api/media", mediaContent);
            var media = JsonSerializer.Deserialize<MediaItemResponseDto>(await mediaResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(media);

            await _client.PostAsync($"/api/mixlist/{mixlist.Id}/items/{media.Id}", null);

            // Act
            var response = await _client.DeleteAsync($"/api/mixlist/{mixlist.Id}/items/{media.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify the media item was removed
            var getMixlistResponse = await _client.GetAsync($"/api/mixlist/{mixlist.Id}");
            var updatedMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(await getMixlistResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(updatedMixlist);
            Assert.DoesNotContain(media.Id, updatedMixlist.MediaItemIds);
        }

        [Fact]
        public async Task GetMixlist_ShouldReturnMediaItemDescriptions()
        {
            // Arrange - Create a mixlist
            var mixlistDto = new CreateMixlistDto { Name = "Mixlist for Description Test" };
            var mixlistContent = new StringContent(JsonSerializer.Serialize(mixlistDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mixlistResponse = await _client.PostAsync("/api/mixlist", mixlistContent);
            var mixlist = JsonSerializer.Deserialize<MixlistResponseDto>(await mixlistResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(mixlist);

            // Create a media item with a description
            var mediaDescription = "This is a detailed description for testing";
            var mediaDto = new CreateMediaItemDto
            {
                Title = "Media with Description",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Description = mediaDescription
            };
            var mediaContent = new StringContent(JsonSerializer.Serialize(mediaDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mediaResponse = await _client.PostAsync("/api/media", mediaContent);
            var media = JsonSerializer.Deserialize<MediaItemResponseDto>(await mediaResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(media);

            // Add media item to mixlist
            await _client.PostAsync($"/api/mixlist/{mixlist.Id}/items/{media.Id}", null);

            // Act - Get the mixlist
            var getMixlistResponse = await _client.GetAsync($"/api/mixlist/{mixlist.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, getMixlistResponse.StatusCode);

            var content = await getMixlistResponse.Content.ReadAsStringAsync();
            var retrievedMixlist = JsonSerializer.Deserialize<MixlistResponseDto>(content, _jsonOptions);

            Assert.NotNull(retrievedMixlist);
            Assert.NotNull(retrievedMixlist.MediaItems);
            Assert.Single(retrievedMixlist.MediaItems);

            var mediaItem = retrievedMixlist.MediaItems[0];
            Assert.Equal(media.Id, mediaItem.Id);
            Assert.Equal("Media with Description", mediaItem.Title);
            Assert.Equal(mediaDescription, mediaItem.Description);
        }

        [Fact]
        public async Task GetAllMixlists_ShouldReturnMediaItemDescriptions()
        {
            // Arrange - Create a mixlist
            var mixlistName = "Mixlist All Desc Test";
            var mixlistDto = new CreateMixlistDto { Name = mixlistName };
            var mixlistContent = new StringContent(JsonSerializer.Serialize(mixlistDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mixlistResponse = await _client.PostAsync("/api/mixlist", mixlistContent);
            var mixlist = JsonSerializer.Deserialize<MixlistResponseDto>(await mixlistResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(mixlist);

            // Create a media item with a description
            var mediaDescription = "Description for GetAll test";
            var mediaDto = new CreateMediaItemDto
            {
                Title = "Media for GetAll Test",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                Description = mediaDescription
            };
            var mediaContent = new StringContent(JsonSerializer.Serialize(mediaDto, _jsonOptions), Encoding.UTF8, "application/json");
            var mediaResponse = await _client.PostAsync("/api/media", mediaContent);
            var media = JsonSerializer.Deserialize<MediaItemResponseDto>(await mediaResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(media);

            // Add media item to mixlist
            await _client.PostAsync($"/api/mixlist/{mixlist.Id}/items/{media.Id}", null);

            // Act - Get all mixlists
            var getAllResponse = await _client.GetAsync("/api/mixlist");

            // Assert
            Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);

            var content = await getAllResponse.Content.ReadAsStringAsync();
            var mixlists = JsonSerializer.Deserialize<List<MixlistResponseDto>>(content, _jsonOptions);

            Assert.NotNull(mixlists);
            Assert.Single(mixlists);

            var retrievedMixlist = mixlists[0];
            Assert.Equal(mixlistName, retrievedMixlist.Name);
            Assert.NotNull(retrievedMixlist.MediaItems);
            Assert.Single(retrievedMixlist.MediaItems);

            var mediaItem = retrievedMixlist.MediaItems[0];
            Assert.Equal(media.Id, mediaItem.Id);
            Assert.Equal(mediaDescription, mediaItem.Description);
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchMixlists_WithValidQuery_ShouldReturnMatchingMixlists()
        {
            // Arrange - Create mixlists with searchable names
            var createDto1 = new CreateMixlistDto { Name = "SearchTest Mixlist One" };
            var createDto2 = new CreateMixlistDto { Name = "SearchTest Mixlist Two" };
            var createDto3 = new CreateMixlistDto { Name = "Different Mixlist" };

            var content1 = new StringContent(JsonSerializer.Serialize(createDto1, _jsonOptions), Encoding.UTF8, "application/json");
            var content2 = new StringContent(JsonSerializer.Serialize(createDto2, _jsonOptions), Encoding.UTF8, "application/json");
            var content3 = new StringContent(JsonSerializer.Serialize(createDto3, _jsonOptions), Encoding.UTF8, "application/json");

            await _client.PostAsync("/api/mixlist", content1);
            await _client.PostAsync("/api/mixlist", content2);
            await _client.PostAsync("/api/mixlist", content3);

            // Act
            var response = await _client.GetAsync("/api/mixlist/search?query=SearchTest");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mixlists = JsonSerializer.Deserialize<List<MixlistResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mixlists);
            Assert.Equal(2, mixlists.Count);
            Assert.All(mixlists, m => Assert.Contains("SearchTest", m.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchMixlists_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/mixlist/search?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchMixlists_ByDescription_ShouldReturnMatches()
        {
            // Arrange - Create a mixlist with a searchable description
            var uniqueDesc = "UniqueDescription";
            var createDto = new CreateMixlistDto
            {
                Name = "Test Mixlist",
                Description = $"This is a {uniqueDesc} for testing search"
            };

            var createContent = new StringContent(JsonSerializer.Serialize(createDto, _jsonOptions), Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/mixlist", createContent);

            // Act
            var response = await _client.GetAsync($"/api/mixlist/search?query={uniqueDesc}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mixlists = JsonSerializer.Deserialize<List<MixlistResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mixlists);
            Assert.Single(mixlists);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateMixlist_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/mixlist", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMixlist_WithNullData_ShouldReturnBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/mixlist", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion
    }
}
