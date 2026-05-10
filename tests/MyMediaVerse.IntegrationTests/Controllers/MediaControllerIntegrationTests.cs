using Microsoft.AspNetCore.Mvc.Testing;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    public class MediaControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MediaControllerIntegrationTests(WebApplicationFactory factory)
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

        #region GET Tests

        [Fact]
        public async Task GetAllMedia_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/media");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mediaItems);
        }

        [Fact]
        public async Task GetMediaItem_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a media item
            var createDto = new CreateMediaItemDto
            {
                Title = "Test Article for Get",
                Description = "A test article description",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "test", "article" },
                Genres = new[] { "news", "technology" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.GetAsync($"/api/media/{createdMedia.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var mediaItem = JsonSerializer.Deserialize<MediaItemResponseDto>(content, _jsonOptions);
            Assert.NotNull(mediaItem);
            Assert.Equal(createdMedia.Id, mediaItem.Id);
            Assert.Equal("Test Article for Get", mediaItem.Title);
        }

        [Fact]
        public async Task GetMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/media/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region POST Tests - Article

        [Fact]
        public async Task CreateMediaItem_WithArticleType_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMediaItemDto
            {
                Title = "New Test Article",
                Description = "A comprehensive test article",
                MediaType = MediaType.Article,
                Link = "https://example.com/article",
                Status = Status.Uncharted,
                Rating = Rating.Like,
                Topics = new[] { "technology", "science" },
                Genres = new[] { "news", "research" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdMedia);
            Assert.Equal("New Test Article", createdMedia.Title);
            Assert.Equal(MediaType.Article, createdMedia.MediaType);
            Assert.Contains("technology", createdMedia.Topics);
            Assert.Contains("news", createdMedia.Genres);
        }

        #endregion

        #region POST Tests - Video

        [Fact]
        public async Task CreateMediaItem_WithVideoType_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMediaItemDto
            {
                Title = "New Test Video",
                Description = "A test video description",
                MediaType = MediaType.Video,
                Link = "https://youtube.com/watch?v=test",
                Status = Status.Uncharted,
                Topics = new[] { "tutorial", "programming" },
                Genres = new[] { "educational" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdMedia);
            Assert.Equal("New Test Video", createdMedia.Title);
            Assert.Equal(MediaType.Video, createdMedia.MediaType);
        }

        #endregion

        #region POST Tests - Podcast

        [Fact]
        public async Task CreateMediaItem_WithPodcastType_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMediaItemDto
            {
                Title = "New Test Podcast",
                Description = "A test podcast description",
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                Topics = new[] { "interview", "business" },
                Genres = new[] { "entrepreneurship" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdMedia);
            Assert.Equal("New Test Podcast", createdMedia.Title);
            Assert.Equal(MediaType.Podcast, createdMedia.MediaType);
        }

        #endregion

        #region POST Tests - Unsupported Types

        [Fact]
        public async Task CreateMediaItem_WithMovieType_ShouldReturnCreated()
        {
            // Arrange - Movies can be created through the Media controller
            var createDto = new CreateMediaItemDto
            {
                Title = "Test Movie via Media Controller",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(createdMedia);
            Assert.Equal(MediaType.Movie, createdMedia.MediaType);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdateMediaItem_WithValidData_ShouldReturnOk()
        {
            // Arrange - First create a media item
            var createDto = new CreateMediaItemDto
            {
                Title = "Original Article Title",
                Description = "Original description",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "original" },
                Genres = new[] { "tech" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Now update it
            var updateDto = new CreateMediaItemDto
            {
                Title = "Updated Article Title",
                Description = "Updated description",
                MediaType = MediaType.Article,
                Link = "https://example.com/updated",
                Status = Status.ActivelyExploring,
                Rating = Rating.SuperLike,
                Topics = new[] { "updated", "modified" },
                Genres = new[] { "news", "science" }
            };

            var updateContent = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/media/{createdMedia.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(updatedMedia);
            Assert.Equal("Updated Article Title", updatedMedia.Title);
            Assert.Equal("Updated description", updatedMedia.Description);
            Assert.Equal(Status.ActivelyExploring, updatedMedia.Status);
        }

        [Fact]
        public async Task UpdateMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new CreateMediaItemDto
            {
                Title = "Updated Article",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/media/{invalidId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteMediaItem_WithValidId_ShouldReturnNoContent()
        {
            // Arrange - First create a media item
            var createDto = new CreateMediaItemDto
            {
                Title = "Article to Delete",
                Description = "This article will be deleted",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "test" },
                Genres = new[] { "test" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.DeleteAsync($"/api/media/{createdMedia.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the media item is actually deleted
            var getResponse = await _client.GetAsync($"/api/media/{createdMedia.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/media/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchMedia_WithValidQuery_ShouldReturnOk()
        {
            // Arrange - First create a media item to search for
            var createDto = new CreateMediaItemDto
            {
                Title = "Searchable Unique Article Title",
                Description = "A searchable article",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", createContent);

            // Act
            var response = await _client.GetAsync("/api/media/search?query=Searchable");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(searchResults);
        }

        [Fact]
        public async Task SearchMedia_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/media/search?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Filter Tests

        [Fact]
        public async Task GetMediaByType_WithValidType_ShouldReturnOk()
        {
            // Arrange - First create an article
            var createDto = new CreateMediaItemDto
            {
                Title = "Test Article for Type Filter",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", createContent);

            // Act
            var response = await _client.GetAsync("/api/media/by-type/Article");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mediaItems);
            Assert.All(mediaItems, item => Assert.Equal(MediaType.Article, item.MediaType));
        }

        [Fact]
        public async Task GetMediaByType_WithInvalidType_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/media/by-type/InvalidType");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMediaByTopic_WithValidTopicId_ShouldReturnOk()
        {
            // Arrange - First create a media item with a specific topic
            var createDto = new CreateMediaItemDto
            {
                Title = "Article with Specific Topic",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "uniquetopicfortesting" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Get the topic ID (we need to fetch topics first)
            var topicsResponse = await _client.GetAsync("/api/topics");
            var topicsContent = await topicsResponse.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(topicsContent, _jsonOptions);
            var uniqueTopic = topics.FirstOrDefault(t => t.Name == "uniquetopicfortesting");

            if (uniqueTopic != null)
            {
                // Act
                var response = await _client.GetAsync($"/api/media/by-topic/{uniqueTopic.Id}");

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                var content = await response.Content.ReadAsStringAsync();
                var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
                Assert.NotNull(mediaItems);
                Assert.All(mediaItems, item => Assert.Contains("uniquetopicfortesting", item.Topics));
            }
        }

        [Fact]
        public async Task GetMediaByGenre_WithValidGenreId_ShouldReturnOk()
        {
            // Arrange - First create a media item with a specific genre
            var createDto = new CreateMediaItemDto
            {
                Title = "Article with Specific Genre",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Genres = new[] { "uniquegenrefortesting" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Get the genre ID
            var genresResponse = await _client.GetAsync("/api/genres");
            var genresContent = await genresResponse.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(genresContent, _jsonOptions);
            var uniqueGenre = genres.FirstOrDefault(g => g.Name == "uniquegenrefortesting");

            if (uniqueGenre != null)
            {
                // Act
                var response = await _client.GetAsync($"/api/media/by-genre/{uniqueGenre.Id}");

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                var content = await response.Content.ReadAsStringAsync();
                var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
                Assert.NotNull(mediaItems);
                Assert.All(mediaItems, item => Assert.Contains("uniquegenrefortesting", item.Genres));
            }
        }

        #endregion

        #region Export Tests

        [Fact]
        public async Task ExportMediaItem_WithValidId_ShouldReturnCsvFile()
        {
            // Arrange - First create a media item
            var createDto = new CreateMediaItemDto
            {
                Title = "Article to Export",
                Description = "Export test",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.GetAsync($"/api/media/{createdMedia.Id}/export");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.ToString());
        }

        [Fact]
        public async Task ExportMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/media/{invalidId}/export");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateMediaItem_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMediaItem_WithNullData_ShouldReturnBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Data Validation Tests

        [Fact]
        public async Task CreateMediaItem_WithEmptyTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateMediaItemDto
            {
                Title = "", // Empty title
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMediaItem_WithLongTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateMediaItemDto
            {
                Title = new string('A', 501), // Exceeds 500 character limit
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion
    }
}

