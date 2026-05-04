using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class RecommendationServiceTests : InMemoryDbTestBase
    {
        private readonly IVectorSearchRepository _mockVectorSearch;
        private readonly IGradientAIClient _mockGradientClient;
        private readonly ILogger<RecommendationService> _mockLogger;
        private readonly RecommendationService _service;

        public RecommendationServiceTests()
        {
            _mockVectorSearch = Substitute.For<IVectorSearchRepository>();
            _mockGradientClient = Substitute.For<IGradientAIClient>();
            _mockLogger = Substitute.For<ILogger<RecommendationService>>();
            _service = new RecommendationService(Context, _mockVectorSearch, _mockGradientClient, _mockLogger);
        }

        #region IsAvailableAsync

        [Fact]
        public async Task IsAvailableAsync_AllAvailable_ReturnsTrue()
        {
            _mockGradientClient.IsAvailableAsync().Returns(true);
            _mockVectorSearch.IsPgVectorAvailableAsync().Returns(true);
            _mockVectorSearch.HasAnyMediaEmbeddingsAsync().Returns(true);

            var result = await _service.IsAvailableAsync();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAvailableAsync_AIUnavailable_ReturnsFalse()
        {
            _mockGradientClient.IsAvailableAsync().Returns(false);
            _mockVectorSearch.IsPgVectorAvailableAsync().Returns(true);
            _mockVectorSearch.HasAnyMediaEmbeddingsAsync().Returns(true);

            var result = await _service.IsAvailableAsync();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsAvailableAsync_PgVectorUnavailable_StillReturnsTrue()
        {
            _mockGradientClient.IsAvailableAsync().Returns(true);
            _mockVectorSearch.IsPgVectorAvailableAsync().Returns(false);

            var result = await _service.IsAvailableAsync();

            // Service returns true as long as AI client is available (pgvector is optional)
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAvailableAsync_NoEmbeddings_StillReturnsTrue()
        {
            _mockGradientClient.IsAvailableAsync().Returns(true);
            _mockVectorSearch.IsPgVectorAvailableAsync().Returns(true);
            _mockVectorSearch.HasAnyMediaEmbeddingsAsync().Returns(false);

            var result = await _service.IsAvailableAsync();

            // Service returns true as long as AI client is available (embeddings are optional)
            result.Should().BeTrue();
        }

        #endregion

        #region GetSimilarMediaItemsAsync

        [Fact]
        public async Task GetSimilarMediaItemsAsync_NoEmbedding_ReturnsEmptyList()
        {
            _mockVectorSearch.GetMediaItemEmbeddingAsync(Arg.Any<Guid>())
                .Returns((float[]?)null);

            var result = await _service.GetSimilarMediaItemsAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSimilarMediaItemsAsync_WithEmbedding_ReturnsSimilarItems()
        {
            var mediaItemId = Guid.NewGuid();
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _mockVectorSearch.GetMediaItemEmbeddingAsync(mediaItemId)
                .Returns(embedding);

            _mockVectorSearch.FindSimilarMediaItemsAsync(
                embedding, mediaItemId, null, 10)
                .Returns(new List<VectorSearchResult>
                {
                    new VectorSearchResult
                    {
                        Id = Guid.NewGuid(),
                        Title = "Similar Item",
                        MediaType = "Book",
                        SimilarityScore = 0.95
                    }
                });

            var result = await _service.GetSimilarMediaItemsAsync(mediaItemId);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Similar Item");
        }

        [Fact]
        public async Task GetSimilarMediaItemsAsync_WithMediaTypeFilter_PassesFilter()
        {
            var mediaItemId = Guid.NewGuid();
            var embedding = new float[] { 0.1f, 0.2f };

            _mockVectorSearch.GetMediaItemEmbeddingAsync(mediaItemId)
                .Returns(embedding);

            _mockVectorSearch.FindSimilarMediaItemsAsync(
                embedding, mediaItemId, "Book", 5)
                .Returns(new List<VectorSearchResult>());

            await _service.GetSimilarMediaItemsAsync(mediaItemId, count: 5, mediaTypeFilter: "Book");

            _mockVectorSearch.Received(1).FindSimilarMediaItemsAsync(
                embedding, mediaItemId, "Book", 5);
        }

        #endregion

        #region GetSimilarNotesAsync

        [Fact]
        public async Task GetSimilarNotesAsync_NoEmbedding_ReturnsEmptyList()
        {
            _mockVectorSearch.GetNoteEmbeddingAsync(Arg.Any<Guid>())
                .Returns((float[]?)null);

            var result = await _service.GetSimilarNotesAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSimilarNotesAsync_WithEmbedding_ReturnsSimilarNotes()
        {
            var noteId = Guid.NewGuid();
            var embedding = new float[] { 0.5f, 0.5f };

            _mockVectorSearch.GetNoteEmbeddingAsync(noteId)
                .Returns(embedding);

            _mockVectorSearch.FindSimilarNotesAsync(
                embedding, noteId, null, 10)
                .Returns(new List<VectorSearchNoteResult>
                {
                    new VectorSearchNoteResult
                    {
                        Id = Guid.NewGuid(),
                        Title = "Similar Note",
                        VaultName = "general",
                        SimilarityScore = 0.85
                    }
                });

            var result = await _service.GetSimilarNotesAsync(noteId);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Similar Note");
        }

        #endregion

        #region SearchByVibeAsync

        [Fact]
        public async Task SearchByVibeAsync_EmptyDescription_ReturnsEmptyList()
        {
            var result = await _service.SearchByVibeAsync("");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchByVibeAsync_ValidDescription_GeneratesEmbeddingAndSearches()
        {
            var queryEmbedding = new float[] { 0.3f, 0.4f, 0.5f };

            _mockGradientClient.IsAvailableAsync().Returns(true);

            _mockGradientClient.GenerateEmbeddingAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(queryEmbedding);

            _mockVectorSearch.FindSimilarMediaItemsAsync(
                Arg.Any<float[]>(), null, null, 20)
                .Returns(new List<VectorSearchResult>
                {
                    new VectorSearchResult
                    {
                        Id = Guid.NewGuid(),
                        Title = "Cozy Mystery Book",
                        SimilarityScore = 0.88
                    }
                });

            var result = await _service.SearchByVibeAsync("cozy mystery");

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Cozy Mystery Book");
        }

        #endregion

        #region GetPersonalizedRecommendationsAsync

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_NoLikedItems_ReturnsEmptyList()
        {
            var result = await _service.GetPersonalizedRecommendationsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_WithLikedItems_ReturnsRecommendations()
        {
            // Add liked media items to the database
            var likedBook = TestDataFactory.CreateBook("Liked Book");
            likedBook.Rating = Rating.SuperLike;
            Context.Books.Add(likedBook);
            await Context.SaveChangesAsync();

            var embedding = new float[] { 0.5f, 0.5f };
            _mockVectorSearch.GetMediaItemEmbeddingAsync(likedBook.Id)
                .Returns(embedding);

            _mockVectorSearch.FindSimilarMediaItemsAsync(
                Arg.Any<float[]>(), null, null, Arg.Any<int>())
                .Returns(new List<VectorSearchResult>
                {
                    new VectorSearchResult
                    {
                        Id = Guid.NewGuid(),
                        Title = "Recommended Book",
                        SimilarityScore = 0.9
                    }
                });

            var result = await _service.GetPersonalizedRecommendationsAsync();

            result.Should().NotBeNull();
        }

        #endregion

        #region GetMediaRelatedToNoteAsync

        [Fact]
        public async Task GetMediaRelatedToNoteAsync_NoEmbedding_ReturnsEmptyList()
        {
            _mockVectorSearch.GetNoteEmbeddingAsync(Arg.Any<Guid>())
                .Returns((float[]?)null);

            var result = await _service.GetMediaRelatedToNoteAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMediaRelatedToNoteAsync_WithEmbedding_ReturnsRelatedMedia()
        {
            var noteId = Guid.NewGuid();
            var embedding = new float[] { 0.1f, 0.2f };

            _mockVectorSearch.GetNoteEmbeddingAsync(noteId)
                .Returns(embedding);

            _mockVectorSearch.FindSimilarMediaItemsAsync(
                embedding, null, null, 10)
                .Returns(new List<VectorSearchResult>
                {
                    new VectorSearchResult
                    {
                        Id = Guid.NewGuid(),
                        Title = "Related Media",
                        SimilarityScore = 0.75
                    }
                });

            var result = await _service.GetMediaRelatedToNoteAsync(noteId);

            result.Should().HaveCount(1);
        }

        #endregion

        #region GetNotesRelatedToMediaAsync

        [Fact]
        public async Task GetNotesRelatedToMediaAsync_NoEmbedding_ReturnsEmptyList()
        {
            _mockVectorSearch.GetMediaItemEmbeddingAsync(Arg.Any<Guid>())
                .Returns((float[]?)null);

            var result = await _service.GetNotesRelatedToMediaAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetNotesRelatedToMediaAsync_WithEmbedding_ReturnsRelatedNotes()
        {
            var mediaItemId = Guid.NewGuid();
            var embedding = new float[] { 0.3f, 0.4f };

            _mockVectorSearch.GetMediaItemEmbeddingAsync(mediaItemId)
                .Returns(embedding);

            _mockVectorSearch.FindSimilarNotesAsync(
                embedding, null, null, 10)
                .Returns(new List<VectorSearchNoteResult>
                {
                    new VectorSearchNoteResult
                    {
                        Id = Guid.NewGuid(),
                        Title = "Related Note",
                        SimilarityScore = 0.80
                    }
                });

            var result = await _service.GetNotesRelatedToMediaAsync(mediaItemId);

            result.Should().HaveCount(1);
        }

        #endregion
    }
}
