using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for generating media recommendations using vector similarity.
    /// Uses PostgreSQL pgvector extension for efficient similarity queries.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IApplicationDbContext _context;
        private readonly IVectorSearchRepository _vectorSearch;
        private readonly IGradientAIClient _gradientClient;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IApplicationDbContext context,
            IVectorSearchRepository vectorSearch,
            IGradientAIClient gradientClient,
            ILogger<RecommendationService> logger)
        {
            _context = context;
            _vectorSearch = vectorSearch;
            _gradientClient = gradientClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Check if AI service is available for embedding generation
                if (!await _gradientClient.IsAvailableAsync())
                {
                    _logger.LogDebug("Recommendation service unavailable: AI client not configured");
                    return false;
                }

                // Check if pgvector is available
                var pgvectorAvailable = await _vectorSearch.IsPgVectorAvailableAsync();
                if (!pgvectorAvailable)
                {
                    _logger.LogWarning("Recommendation service: pgvector extension not available in database");
                }

                // Check if we have any items with embeddings (use raw SQL via repository)
                var hasEmbeddings = await _vectorSearch.HasAnyMediaEmbeddingsAsync();

                if (!hasEmbeddings)
                {
                    _logger.LogDebug("Recommendation service has limited functionality: no embeddings found in database");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking recommendation service availability");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<SimilarItemResult>> GetSimilarMediaItemsAsync(
            Guid mediaItemId,
            int count = 10,
            string? mediaTypeFilter = null)
        {
            try
            {
                // Get the source item's embedding
                var embedding = await _vectorSearch.GetMediaItemEmbeddingAsync(mediaItemId);

                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogWarning("Media item {Id} not found or has no embedding", mediaItemId);
                    return new List<SimilarItemResult>();
                }

                // Use pgvector for similarity search
                var results = await _vectorSearch.FindSimilarMediaItemsAsync(
                    embedding,
                    excludeId: mediaItemId,
                    mediaTypeFilter: mediaTypeFilter,
                    limit: count);

                _logger.LogDebug("Found {Count} similar items for media item {Id} using pgvector", results.Count, mediaItemId);

                return results.Select(r => new SimilarItemResult
                {
                    Id = r.Id,
                    Title = r.Title,
                    MediaType = r.MediaType,
                    Description = r.Description,
                    Thumbnail = r.Thumbnail,
                    Status = r.Status,
                    Rating = r.Rating,
                    SimilarityScore = r.SimilarityScore
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar media items for {MediaItemId}", mediaItemId);
                return new List<SimilarItemResult>();
            }
        }

        /// <inheritdoc />
        public async Task<List<SimilarNoteResult>> GetSimilarNotesAsync(
            Guid noteId,
            int count = 10,
            string? vaultFilter = null)
        {
            try
            {
                var embedding = await _vectorSearch.GetNoteEmbeddingAsync(noteId);

                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogWarning("Note {Id} not found or has no embedding", noteId);
                    return new List<SimilarNoteResult>();
                }

                var results = await _vectorSearch.FindSimilarNotesAsync(
                    embedding,
                    excludeId: noteId,
                    vaultFilter: vaultFilter,
                    limit: count);

                _logger.LogDebug("Found {Count} similar notes for note {Id} using pgvector", results.Count, noteId);

                return results.Select(r => new SimilarNoteResult
                {
                    Id = r.Id,
                    Title = r.Title,
                    VaultName = r.VaultName,
                    Description = r.Description,
                    SourceUrl = r.SourceUrl,
                    Tags = r.Tags,
                    SimilarityScore = r.SimilarityScore
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar notes for {NoteId}", noteId);
                return new List<SimilarNoteResult>();
            }
        }

        /// <inheritdoc />
        public async Task<List<SimilarItemResult>> SearchByVibeAsync(
            string vibeDescription,
            int count = 20,
            string? mediaTypeFilter = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vibeDescription))
                {
                    return new List<SimilarItemResult>();
                }

                // Generate embedding for the vibe description
                if (!await _gradientClient.IsAvailableAsync())
                {
                    _logger.LogWarning("Cannot perform vibe search: AI service unavailable");
                    return new List<SimilarItemResult>();
                }

                var queryEmbedding = await _gradientClient.GenerateEmbeddingAsync(vibeDescription);

                // Use pgvector for similarity search
                var results = await _vectorSearch.FindSimilarMediaItemsAsync(
                    queryEmbedding,
                    excludeId: null,
                    mediaTypeFilter: mediaTypeFilter,
                    limit: count);

                _logger.LogInformation("Vibe search for '{Vibe}' returned {Count} results using pgvector", vibeDescription, results.Count);

                return results.Select(r => new SimilarItemResult
                {
                    Id = r.Id,
                    Title = r.Title,
                    MediaType = r.MediaType,
                    Description = r.Description,
                    Thumbnail = r.Thumbnail,
                    Status = r.Status,
                    Rating = r.Rating,
                    SimilarityScore = r.SimilarityScore
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in vibe search for '{VibeDescription}'", vibeDescription);
                return new List<SimilarItemResult>();
            }
        }

        /// <inheritdoc />
        public async Task<List<SimilarItemResult>> GetPersonalizedRecommendationsAsync(
            int count = 20,
            bool excludeExplored = true)
        {
            try
            {
                // Get liked items (SuperLike and Like ratings)
                var likedItems = await _context.MediaItems
                    .AsNoTracking()
                    .Where(m => m.Rating == Rating.SuperLike || m.Rating == Rating.Like)
                    .Where(m => m.Embedding != null)
                    .ToListAsync();

                if (!likedItems.Any())
                {
                    _logger.LogDebug("No liked items with embeddings found for personalized recommendations");
                    return new List<SimilarItemResult>();
                }

                // Calculate average embedding of liked items (convert Vector to float[])
                var averageEmbedding = CalculateAverageEmbedding(likedItems.Select(i => i.Embedding!.ToArray()).ToList());

                // Use pgvector to find similar items
                var results = await _vectorSearch.FindSimilarMediaItemsAsync(
                    averageEmbedding,
                    excludeId: null,
                    mediaTypeFilter: null,
                    limit: count + likedItems.Count); // Get extra to filter out liked items

                // Filter out liked items and optionally explored items
                var likedIds = likedItems.Select(i => i.Id).ToHashSet();
                var filteredResults = results
                    .Where(r => !likedIds.Contains(r.Id))
                    .ToList();

                if (excludeExplored)
                {
                    filteredResults = filteredResults
                        .Where(r => r.Status == Status.Uncharted.ToString())
                        .ToList();
                }

                var finalResults = filteredResults.Take(count).ToList();

                _logger.LogInformation("Generated {Count} personalized recommendations based on {LikedCount} liked items using pgvector",
                    finalResults.Count, likedItems.Count);

                return finalResults.Select(r => new SimilarItemResult
                {
                    Id = r.Id,
                    Title = r.Title,
                    MediaType = r.MediaType,
                    Description = r.Description,
                    Thumbnail = r.Thumbnail,
                    Status = r.Status,
                    Rating = r.Rating,
                    SimilarityScore = r.SimilarityScore
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating personalized recommendations");
                return new List<SimilarItemResult>();
            }
        }

        /// <inheritdoc />
        public async Task<List<SimilarItemResult>> GetMediaRelatedToNoteAsync(
            Guid noteId,
            int count = 10)
        {
            try
            {
                var embedding = await _vectorSearch.GetNoteEmbeddingAsync(noteId);

                if (embedding == null)
                {
                    _logger.LogDebug("Note {Id} not found or has no embedding", noteId);
                    return new List<SimilarItemResult>();
                }

                // Find media items similar to this note's embedding
                var results = await _vectorSearch.FindSimilarMediaItemsAsync(
                    embedding,
                    excludeId: null,
                    mediaTypeFilter: null,
                    limit: count);

                _logger.LogDebug("Found {Count} media items related to note {Id} using pgvector", results.Count, noteId);

                return results.Select(r => new SimilarItemResult
                {
                    Id = r.Id,
                    Title = r.Title,
                    MediaType = r.MediaType,
                    Description = r.Description,
                    Thumbnail = r.Thumbnail,
                    Status = r.Status,
                    Rating = r.Rating,
                    SimilarityScore = r.SimilarityScore
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding media related to note {NoteId}", noteId);
                return new List<SimilarItemResult>();
            }
        }

        /// <inheritdoc />
        public async Task<List<SimilarNoteResult>> GetNotesRelatedToMediaAsync(
            Guid mediaItemId,
            int count = 10)
        {
            try
            {
                var embedding = await _vectorSearch.GetMediaItemEmbeddingAsync(mediaItemId);

                if (embedding == null)
                {
                    _logger.LogDebug("Media item {Id} not found or has no embedding", mediaItemId);
                    return new List<SimilarNoteResult>();
                }

                // Find notes similar to this media item's embedding
                var results = await _vectorSearch.FindSimilarNotesAsync(
                    embedding,
                    excludeId: null,
                    vaultFilter: null,
                    limit: count);

                _logger.LogDebug("Found {Count} notes related to media item {Id} using pgvector", results.Count, mediaItemId);

                return results.Select(r => new SimilarNoteResult
                {
                    Id = r.Id,
                    Title = r.Title,
                    VaultName = r.VaultName,
                    Description = r.Description,
                    SourceUrl = r.SourceUrl,
                    Tags = r.Tags,
                    SimilarityScore = r.SimilarityScore
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding notes related to media item {MediaItemId}", mediaItemId);
                return new List<SimilarNoteResult>();
            }
        }

        /// <summary>
        /// Calculates the average embedding from a list of embeddings.
        /// Used for personalized recommendations based on multiple liked items.
        /// </summary>
        private static float[] CalculateAverageEmbedding(List<float[]> embeddings)
        {
            if (embeddings.Count == 0)
                return Array.Empty<float>();

            var dimensions = embeddings[0].Length;
            var result = new float[dimensions];

            for (int i = 0; i < dimensions; i++)
            {
                result[i] = (float)embeddings.Average(e => e[i]);
            }

            return result;
        }
    }
}
