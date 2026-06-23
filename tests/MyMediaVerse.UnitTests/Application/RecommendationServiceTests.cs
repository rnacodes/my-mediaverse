using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Search;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class RecommendationServiceTests : InMemoryDbTestBase
    {
        private readonly ITypesenseService _mockTypesense;
        private readonly ILogger<RecommendationService> _mockLogger;
        private readonly RecommendationService _service;

        public RecommendationServiceTests()
        {
            _mockTypesense = Substitute.For<ITypesenseService>();
            _mockLogger = Substitute.For<ILogger<RecommendationService>>();
            // Recommendations require auto-embedding; default the mock to enabled.
            _mockTypesense.IsAutoEmbeddingEnabled.Returns(true);
            _service = new RecommendationService(Context, _mockTypesense, _mockLogger);
        }

        #region IsAvailableAsync

        [Fact]
        public async Task IsAvailableAsync_AutoEmbeddingEnabled_ReturnsTrue()
        {
            _mockTypesense.IsAutoEmbeddingEnabled.Returns(true);

            var result = await _service.IsAvailableAsync();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAvailableAsync_AutoEmbeddingDisabled_ReturnsFalse()
        {
            _mockTypesense.IsAutoEmbeddingEnabled.Returns(false);

            var result = await _service.IsAvailableAsync();

            result.Should().BeFalse();
        }

        #endregion

        #region GetSimilarMediaItemsAsync

        [Fact]
        public async Task GetSimilarMediaItemsAsync_ReturnsMappedHits()
        {
            var mediaItemId = Guid.NewGuid();
            _mockTypesense.FindSimilarMediaByIdAsync(mediaItemId, 10, null, null)
                .Returns(new List<MediaVectorHit>
                {
                    new() { Id = Guid.NewGuid(), Title = "Similar Item", MediaType = "Book", SimilarityScore = 0.95 }
                });

            var result = await _service.GetSimilarMediaItemsAsync(mediaItemId);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Similar Item");
            result[0].SimilarityScore.Should().Be(0.95);
        }

        [Fact]
        public async Task GetSimilarMediaItemsAsync_WithMediaTypeFilter_PassesTypesenseFilter()
        {
            var mediaItemId = Guid.NewGuid();
            _mockTypesense.FindSimilarMediaByIdAsync(
                mediaItemId, 5, "media_type:=Book", null)
                .Returns(new List<MediaVectorHit>());

            await _service.GetSimilarMediaItemsAsync(mediaItemId, count: 5, mediaTypeFilter: "Book");

            await _mockTypesense.Received(1).FindSimilarMediaByIdAsync(
                mediaItemId, 5, "media_type:=Book", null);
        }

        [Fact]
        public async Task GetSimilarMediaItemsAsync_OnError_ReturnsEmptyList()
        {
            _mockTypesense.FindSimilarMediaByIdAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<double?>())
                .Returns<List<MediaVectorHit>>(_ => throw new InvalidOperationException("Typesense down"));

            var result = await _service.GetSimilarMediaItemsAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        #endregion

        #region GetSimilarNotesAsync

        [Fact]
        public async Task GetSimilarNotesAsync_ReturnsMappedHits()
        {
            var noteId = Guid.NewGuid();
            _mockTypesense.FindSimilarNotesByIdAsync(noteId, 10, null, null)
                .Returns(new List<NoteVectorHit>
                {
                    new() { Id = Guid.NewGuid(), Title = "Similar Note", VaultName = "general", SimilarityScore = 0.85 }
                });

            var result = await _service.GetSimilarNotesAsync(noteId);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Similar Note");
        }

        [Fact]
        public async Task GetSimilarNotesAsync_WithVaultFilter_PassesTypesenseFilter()
        {
            var noteId = Guid.NewGuid();
            _mockTypesense.FindSimilarNotesByIdAsync(noteId, 10, "vault_name:=general", null)
                .Returns(new List<NoteVectorHit>());

            await _service.GetSimilarNotesAsync(noteId, vaultFilter: "general");

            await _mockTypesense.Received(1).FindSimilarNotesByIdAsync(
                noteId, 10, "vault_name:=general", null);
        }

        #endregion

        #region SearchByVibeAsync

        [Fact]
        public async Task SearchByVibeAsync_EmptyDescription_ReturnsEmptyList()
        {
            var result = await _service.SearchByVibeAsync("");

            result.Should().BeEmpty();
            await _mockTypesense.DidNotReceive().SemanticSearchMediaAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>());
        }

        [Fact]
        public async Task SearchByVibeAsync_ValidDescription_RunsSemanticSearch()
        {
            _mockTypesense.SemanticSearchMediaAsync("cozy mystery", null, 20)
                .Returns(new List<MediaVectorHit>
                {
                    new() { Id = Guid.NewGuid(), Title = "Cozy Mystery Book", SimilarityScore = 0.88 }
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
            await _mockTypesense.DidNotReceive().GetMediaEmbeddingsAsync(Arg.Any<IReadOnlyCollection<Guid>>());
        }

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_NoEmbeddingsInTypesense_ReturnsEmptyList()
        {
            var likedBook = TestDataFactory.CreateBook("Liked Book");
            likedBook.Rating = Rating.SuperLike;
            Context.Books.Add(likedBook);
            await Context.SaveChangesAsync();

            _mockTypesense.GetMediaEmbeddingsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
                .Returns(new List<float[]>());

            var result = await _service.GetPersonalizedRecommendationsAsync();

            result.Should().BeEmpty();
            await _mockTypesense.DidNotReceive().VectorSearchMediaAsync(
                Arg.Any<float[]>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<double?>());
        }

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_WithLikedItems_AveragesAndSearches()
        {
            var likedBook = TestDataFactory.CreateBook("Liked Book");
            likedBook.Rating = Rating.SuperLike;
            Context.Books.Add(likedBook);
            await Context.SaveChangesAsync();

            _mockTypesense.GetMediaEmbeddingsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
                .Returns(new List<float[]> { new[] { 0.2f, 0.4f }, new[] { 0.4f, 0.6f } });

            _mockTypesense.VectorSearchMediaAsync(Arg.Any<float[]>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<double?>())
                .Returns(new List<MediaVectorHit>
                {
                    new() { Id = Guid.NewGuid(), Title = "Recommended Book", SimilarityScore = 0.9 }
                });

            var result = await _service.GetPersonalizedRecommendationsAsync();

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Recommended Book");

            // The averaged vector is the element-wise mean of the liked items' embeddings.
            await _mockTypesense.Received(1).VectorSearchMediaAsync(
                Arg.Is<float[]>(v => v.Length == 2 && v[0] == 0.3f && v[1] == 0.5f),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<double?>());
        }

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_ExcludeExplored_FiltersLikedAndUnexplored()
        {
            var likedBook = TestDataFactory.CreateBook("Liked Book");
            likedBook.Rating = Rating.SuperLike;
            Context.Books.Add(likedBook);
            await Context.SaveChangesAsync();

            _mockTypesense.GetMediaEmbeddingsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
                .Returns(new List<float[]> { new[] { 0.1f, 0.2f } });
            _mockTypesense.VectorSearchMediaAsync(Arg.Any<float[]>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<double?>())
                .Returns(new List<MediaVectorHit>());

            await _service.GetPersonalizedRecommendationsAsync(excludeExplored: true);

            await _mockTypesense.Received(1).VectorSearchMediaAsync(
                Arg.Any<float[]>(),
                Arg.Is<string?>(f => f != null
                    && f.Contains($"id:!=[{likedBook.Id}]")
                    && f.Contains("status:=Uncharted")),
                Arg.Any<int>(),
                Arg.Any<double?>());
        }

        #endregion

        #region GetMediaRelatedToNoteAsync

        [Fact]
        public async Task GetMediaRelatedToNoteAsync_NoEmbedding_ReturnsEmptyList()
        {
            _mockTypesense.GetNoteEmbeddingAsync(Arg.Any<Guid>())
                .Returns((float[]?)null);

            var result = await _service.GetMediaRelatedToNoteAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMediaRelatedToNoteAsync_WithEmbedding_ReturnsRelatedMedia()
        {
            var noteId = Guid.NewGuid();
            var embedding = new float[] { 0.1f, 0.2f };
            _mockTypesense.GetNoteEmbeddingAsync(noteId).Returns(embedding);
            _mockTypesense.VectorSearchMediaAsync(embedding, null, 10, null)
                .Returns(new List<MediaVectorHit>
                {
                    new() { Id = Guid.NewGuid(), Title = "Related Media", SimilarityScore = 0.75 }
                });

            var result = await _service.GetMediaRelatedToNoteAsync(noteId);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Related Media");
        }

        #endregion

        #region GetNotesRelatedToMediaAsync

        [Fact]
        public async Task GetNotesRelatedToMediaAsync_NoEmbedding_ReturnsEmptyList()
        {
            _mockTypesense.GetMediaEmbeddingAsync(Arg.Any<Guid>())
                .Returns((float[]?)null);

            var result = await _service.GetNotesRelatedToMediaAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetNotesRelatedToMediaAsync_WithEmbedding_ReturnsRelatedNotes()
        {
            var mediaItemId = Guid.NewGuid();
            var embedding = new float[] { 0.3f, 0.4f };
            _mockTypesense.GetMediaEmbeddingAsync(mediaItemId).Returns(embedding);
            _mockTypesense.VectorSearchNotesAsync(embedding, null, 10, null)
                .Returns(new List<NoteVectorHit>
                {
                    new() { Id = Guid.NewGuid(), Title = "Related Note", VaultName = "general", SimilarityScore = 0.80 }
                });

            var result = await _service.GetNotesRelatedToMediaAsync(mediaItemId);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Related Note");
        }

        #endregion
    }
}
