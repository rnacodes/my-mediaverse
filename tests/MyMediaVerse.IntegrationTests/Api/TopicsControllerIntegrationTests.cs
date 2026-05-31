using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class TopicsControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public TopicsControllerIntegrationTests(ApiFactory factory)
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
        public async Task GetAllTopics_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/topics");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(content, _jsonOptions);
            Assert.NotNull(topics);
        }

        [Fact]
        public async Task GetTopic_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a topic
            var createDto = new CreateTopicDto
            {
                Name = "Test Topic for Get"
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/topics", createContent);
            var createdTopic = JsonSerializer.Deserialize<TopicResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdTopic);

            // Act
            var response = await _client.GetAsync($"/api/topics/{createdTopic.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var topic = JsonSerializer.Deserialize<TopicResponseDto>(content, _jsonOptions);
            Assert.NotNull(topic);
            Assert.Equal(createdTopic.Id, topic.Id);
            Assert.Equal("test topic for get", topic.Name); // Normalized to lowercase
        }

        [Fact]
        public async Task GetTopic_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/topics/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreateTopic_WithValidName_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateTopicDto
            {
                Name = "New Unique Topic Name"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTopic = JsonSerializer.Deserialize<TopicResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTopic);
            Assert.NotEqual(Guid.Empty, createdTopic.Id);
            Assert.Contains("new unique topic name", createdTopic.Name);
        }

        [Fact]
        public async Task CreateTopic_WithDuplicateName_ShouldReturnExistingTopic()
        {
            // Arrange - Create a topic first
            var topicName = "Duplicate Topic Test";
            var createDto = new CreateTopicDto { Name = topicName };

            var content1 = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var firstResponse = await _client.PostAsync("/api/topics", content1);
            var firstTopic = JsonSerializer.Deserialize<TopicResponseDto>(
                await firstResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(firstTopic);

            // Try to create the same topic again
            var content2 = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content2);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Returns OK with existing topic

            var responseContent = await response.Content.ReadAsStringAsync();
            var returnedTopic = JsonSerializer.Deserialize<TopicResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(returnedTopic);
            Assert.Equal(firstTopic.Id, returnedTopic.Id); // Same ID as first topic
        }

        [Fact]
        public async Task CreateTopic_WithCaseInsensitiveDuplicate_ShouldReturnExistingTopic()
        {
            // Arrange - Create a topic first
            var topicName = "CaseInsensitive Test";
            var createDto1 = new CreateTopicDto { Name = topicName.ToLower() };

            var content1 = new StringContent(
                JsonSerializer.Serialize(createDto1, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var firstResponse = await _client.PostAsync("/api/topics", content1);
            var firstTopic = JsonSerializer.Deserialize<TopicResponseDto>(
                await firstResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(firstTopic);

            // Try to create with different case
            var createDto2 = new CreateTopicDto { Name = topicName.ToUpper() };
            var content2 = new StringContent(
                JsonSerializer.Serialize(createDto2, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content2);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var returnedTopic = JsonSerializer.Deserialize<TopicResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(returnedTopic);
            Assert.Equal(firstTopic.Id, returnedTopic.Id);
        }

        [Fact]
        public async Task CreateTopic_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateTopicDto { Name = "" };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTopic_WithWhitespaceName_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateTopicDto { Name = "   " };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTopic_WithTrimmableWhitespace_ShouldTrimAndCreate()
        {
            // Arrange
            var createDto = new CreateTopicDto { Name = "  Trimmed Topic  " };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTopic = JsonSerializer.Deserialize<TopicResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTopic);
            Assert.DoesNotContain("  ", createdTopic.Name); // Whitespace should be trimmed
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteTopic_WithValidIdAndNoMediaItems_ShouldReturnNoContent()
        {
            // Arrange - Create a topic
            var createDto = new CreateTopicDto { Name = "Topic to Delete" };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/topics", createContent);
            var createdTopic = JsonSerializer.Deserialize<TopicResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdTopic);

            // Act
            var response = await _client.DeleteAsync($"/api/topics/{createdTopic.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the topic is actually deleted
            var getResponse = await _client.GetAsync($"/api/topics/{createdTopic.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteTopic_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/topics/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchTopics_WithValidQuery_ShouldReturnMatchingTopics()
        {
            // Arrange - Create topics with searchable names
            var createDto1 = new CreateTopicDto { Name = "SearchTest Topic One" };
            var createDto2 = new CreateTopicDto { Name = "SearchTest Topic Two" };
            var createDto3 = new CreateTopicDto { Name = "Different Topic" };

            var content1 = new StringContent(JsonSerializer.Serialize(createDto1, _jsonOptions), Encoding.UTF8, "application/json");
            var content2 = new StringContent(JsonSerializer.Serialize(createDto2, _jsonOptions), Encoding.UTF8, "application/json");
            var content3 = new StringContent(JsonSerializer.Serialize(createDto3, _jsonOptions), Encoding.UTF8, "application/json");

            await _client.PostAsync("/api/topics", content1);
            await _client.PostAsync("/api/topics", content2);
            await _client.PostAsync("/api/topics", content3);

            // Act
            var response = await _client.GetAsync("/api/topics/search?query=searchtest");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(content, _jsonOptions);
            Assert.NotNull(topics);
            Assert.Equal(2, topics.Count);
            Assert.All(topics, t => Assert.Contains("searchtest", t.Name));
        }

        [Fact]
        public async Task SearchTopics_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act - Empty query should return BadRequest as it's not a valid search
            var response = await _client.GetAsync("/api/topics/search?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchTopics_WithNoMatches_ShouldReturnEmptyList()
        {
            // Act
            var response = await _client.GetAsync("/api/topics/search?query=NonExistentSearchTerm");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(content, _jsonOptions);
            Assert.NotNull(topics);
            Assert.Empty(topics);
        }

        [Fact]
        public async Task SearchTopics_IsCaseInsensitive_ShouldReturnMatches()
        {
            // Arrange - Create a topic
            var uniqueName = "CaseSearchTest";
            var createDto = new CreateTopicDto { Name = uniqueName };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/topics", createContent);

            // Act - Search with different case
            var response = await _client.GetAsync($"/api/topics/search?query={uniqueName.ToUpper()}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(content, _jsonOptions);
            Assert.NotNull(topics);
            Assert.Single(topics);
            Assert.Contains(topics, t => t.Name.Contains(uniqueName.ToLower()));
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateTopic_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTopic_WithNullData_ShouldReturnBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Integration with Media Items

        [Fact]
        public async Task CreateMediaWithTopic_ShouldAssociateTopicWithMedia()
        {
            // Arrange - Create a topic
            var topicName = "MediaAssocTest";

            // Create media item with the topic
            var mediaDto = new CreateMediaItemDto
            {
                Title = "Media with Topic",
                MediaType = Domain.Entities.MediaType.Article,
                Status = Domain.Entities.Status.Uncharted,
                Topics = new[] { topicName }
            };

            var mediaContent = new StringContent(
                JsonSerializer.Serialize(mediaDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", mediaContent);

            // Act - Search for the topic to get its ID
            var searchResponse = await _client.GetAsync($"/api/topics/search?query={topicName}");
            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(searchContent, _jsonOptions);
            Assert.NotNull(topics);
            Assert.Single(topics);

            var topicFromSearch = topics.First();
            Assert.Contains(topicName.ToLower(), topicFromSearch.Name);

            // Get topic by ID to verify media association (search endpoint omits MediaItemIds for performance)
            var response = await _client.GetAsync($"/api/topics/{topicFromSearch.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var topic = JsonSerializer.Deserialize<TopicResponseDto>(content, _jsonOptions);
            Assert.NotNull(topic);
            Assert.NotEmpty(topic.MediaItemIds); // Should have associated media
        }

        #endregion

        #region Normalization Tests

        [Fact]
        public async Task TopicName_ShouldBeNormalizedToLowerCase()
        {
            // Arrange
            var createDto = new CreateTopicDto { Name = "UPPERCASE TOPIC" };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/topics", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTopic = JsonSerializer.Deserialize<TopicResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTopic);
            Assert.Equal(createDto.Name.Trim().ToLowerInvariant(), createdTopic.Name);
        }

        #endregion
    }
}
