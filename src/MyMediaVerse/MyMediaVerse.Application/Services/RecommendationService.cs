using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Search;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for generating media recommendations using vector similarity.
    /// Reads embeddings and runs nearest-neighbour queries through Typesense (auto-embedding);
    /// PostgreSQL is only consulted to identify which items the user has liked.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITypesenseService _typesense;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IApplicationDbContext context,
            ITypesenseService typesense,
            ILogger<RecommendationService> logger)
        {
            _context = context;
            _typesense = typesense;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<bool> IsAvailableAsync()
        {
            try
            {
                // Recommendations are powered by Typesense vector search, which requires auto-embedding
                // (an OpenAI key). Without it the embedding field does not exist and queries cannot run.
                var available = _typesense.IsAutoEmbeddingEnabled;
                if (!available)
                {
                    _logger.LogDebug("Recommendation service unavailable: Typesense auto-embedding is not configured.");
                }

                return Task.FromResult(available);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking recommendation service availability");
                return Task.FromResult(false);
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
                var hits = await _typesense.FindSimilarMediaByIdAsync(
                    mediaItemId,
                    limit: count,
                    filters: BuildMediaTypeFilter(mediaTypeFilter));

                _logger.LogDebug("Found {Count} similar items for media item {Id} via Typesense.", hits.Count, mediaItemId);

                return hits.Select(MapToSimilarItem).ToList();
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
                var hits = await _typesense.FindSimilarNotesByIdAsync(
                    noteId,
                    limit: count,
                    filters: BuildVaultFilter(vaultFilter));

                _logger.LogDebug("Found {Count} similar notes for note {Id} via Typesense.", hits.Count, noteId);

                return hits.Select(MapToSimilarNote).ToList();
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

                // Typesense embeds the vibe text itself via the collection's remote embedder, so this is
                // a hybrid (keyword + semantic) search on the description - no in-app embedding step.
                var hits = await _typesense.SemanticSearchMediaAsync(
                    vibeDescription,
                    BuildMediaTypeFilter(mediaTypeFilter),
                    count);

                _logger.LogInformation("Vibe search for '{Vibe}' returned {Count} results via Typesense.", vibeDescription, hits.Count);

                return hits.Select(MapToSimilarItem).ToList();
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
                // Identify liked items in PostgreSQL, then average their Typesense-stored vectors.
                var likedIds = await _context.MediaItems
                    .AsNoTracking()
                    .Where(m => m.Rating == Rating.SuperLike || m.Rating == Rating.Like)
                    .Select(m => m.Id)
                    .ToListAsync();

                if (likedIds.Count == 0)
                {
                    _logger.LogDebug("No liked items found for personalized recommendations");
                    return new List<SimilarItemResult>();
                }

                var embeddings = await _typesense.GetMediaEmbeddingsAsync(likedIds);

                if (embeddings.Count == 0)
                {
                    _logger.LogDebug("No embeddings found in Typesense for {Count} liked items", likedIds.Count);
                    return new List<SimilarItemResult>();
                }

                var averageEmbedding = CalculateAverageEmbedding(embeddings);

                // Exclude the liked items themselves and, optionally, anything already explored.
                var filterClauses = new List<string> { $"id:!=[{string.Join(",", likedIds)}]" };
                if (excludeExplored)
                {
                    filterClauses.Add($"status:={Status.Uncharted}");
                }

                var hits = await _typesense.VectorSearchMediaAsync(
                    averageEmbedding,
                    filters: string.Join(" && ", filterClauses),
                    limit: count);

                _logger.LogInformation("Generated {Count} personalized recommendations from {LikedCount} liked items via Typesense.",
                    hits.Count, likedIds.Count);

                return hits.Select(MapToSimilarItem).ToList();
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
                // Cross-collection: fetch the note's vector, then find nearest media items.
                var embedding = await _typesense.GetNoteEmbeddingAsync(noteId);

                if (embedding == null)
                {
                    _logger.LogDebug("Note {Id} not found in Typesense or has no embedding", noteId);
                    return new List<SimilarItemResult>();
                }

                var hits = await _typesense.VectorSearchMediaAsync(embedding, limit: count);

                _logger.LogDebug("Found {Count} media items related to note {Id} via Typesense.", hits.Count, noteId);

                return hits.Select(MapToSimilarItem).ToList();
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
                // Cross-collection: fetch the media item's vector, then find nearest notes.
                var embedding = await _typesense.GetMediaEmbeddingAsync(mediaItemId);

                if (embedding == null)
                {
                    _logger.LogDebug("Media item {Id} not found in Typesense or has no embedding", mediaItemId);
                    return new List<SimilarNoteResult>();
                }

                var hits = await _typesense.VectorSearchNotesAsync(embedding, limit: count);

                _logger.LogDebug("Found {Count} notes related to media item {Id} via Typesense.", hits.Count, mediaItemId);

                return hits.Select(MapToSimilarNote).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding notes related to media item {MediaItemId}", mediaItemId);
                return new List<SimilarNoteResult>();
            }
        }

        private static string? BuildMediaTypeFilter(string? mediaType) =>
            string.IsNullOrWhiteSpace(mediaType) ? null : $"media_type:={mediaType}";

        private static string? BuildVaultFilter(string? vaultName) =>
            string.IsNullOrWhiteSpace(vaultName) ? null : $"vault_name:={vaultName}";

        private static SimilarItemResult MapToSimilarItem(MediaVectorHit hit) => new()
        {
            Id = hit.Id,
            Title = hit.Title,
            MediaType = hit.MediaType,
            Description = hit.Description,
            Thumbnail = hit.Thumbnail,
            Status = hit.Status,
            Rating = hit.Rating,
            SimilarityScore = hit.SimilarityScore
        };

        private static SimilarNoteResult MapToSimilarNote(NoteVectorHit hit) => new()
        {
            Id = hit.Id,
            Title = hit.Title,
            VaultName = hit.VaultName,
            Description = hit.Description,
            SourceUrl = hit.SourceUrl,
            Tags = hit.Tags,
            SimilarityScore = hit.SimilarityScore
        };

        /// <summary>
        /// Calculates the average embedding from a list of embeddings.
        /// Used for personalized recommendations based on multiple liked items.
        /// </summary>
        private static float[] CalculateAverageEmbedding(IReadOnlyList<float[]> embeddings)
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
