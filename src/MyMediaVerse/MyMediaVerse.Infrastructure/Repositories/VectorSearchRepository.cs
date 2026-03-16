using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Shared.Interfaces;
using System.Globalization;

namespace MyMediaVerse.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for vector similarity searches using PostgreSQL pgvector extension.
    /// Uses native pgvector operators for efficient similarity queries.
    /// </summary>
    public class VectorSearchRepository : IVectorSearchRepository
    {
        private readonly MediaLibraryDbContext _context;
        private readonly ILogger<VectorSearchRepository> _logger;

        public VectorSearchRepository(
            MediaLibraryDbContext context,
            ILogger<VectorSearchRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> IsPgVectorAvailableAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT EXISTS(SELECT 1 FROM pg_extension WHERE extname = 'vector')";

                var result = await command.ExecuteScalarAsync();
                return result is bool b && b;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pgvector availability");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> HasAnyMediaEmbeddingsAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT EXISTS(SELECT 1 FROM ""MediaItems"" WHERE ""Embedding"" IS NOT NULL LIMIT 1)";

                var result = await command.ExecuteScalarAsync();
                return result is bool b && b;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for media embeddings");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchResult>> FindSimilarMediaItemsAsync(
            float[] embedding,
            Guid? excludeId = null,
            string? mediaTypeFilter = null,
            int limit = 10)
        {
            var results = new List<VectorSearchResult>();

            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();

                // Build the query with pgvector cosine distance operator (<=>)
                // The <=> operator returns cosine distance (0 = identical, 2 = opposite)
                // We convert to similarity: 1 - (distance / 2) for 0-1 range, or just 1 - distance for approximate
                var sql = @"
                    SELECT
                        ""Id"",
                        ""Title"",
                        ""MediaType"",
                        ""Description"",
                        ""Thumbnail"",
                        ""Status"",
                        ""Rating"",
                        (1 - (""Embedding"" <=> @embedding::vector)) as similarity_score
                    FROM ""MediaItems""
                    WHERE ""Embedding"" IS NOT NULL";

                if (excludeId.HasValue)
                {
                    sql += " AND \"Id\" != @excludeId";
                }

                if (!string.IsNullOrEmpty(mediaTypeFilter))
                {
                    sql += " AND \"MediaType\" = @mediaType";
                }

                sql += @"
                    ORDER BY ""Embedding"" <=> @embedding::vector
                    LIMIT @limit";

                command.CommandText = sql;

                // Add parameters
                var embeddingParam = command.CreateParameter();
                embeddingParam.ParameterName = "@embedding";
                embeddingParam.Value = FormatEmbeddingForPgVector(embedding);
                command.Parameters.Add(embeddingParam);

                var limitParam = command.CreateParameter();
                limitParam.ParameterName = "@limit";
                limitParam.Value = limit;
                command.Parameters.Add(limitParam);

                if (excludeId.HasValue)
                {
                    var excludeParam = command.CreateParameter();
                    excludeParam.ParameterName = "@excludeId";
                    excludeParam.Value = excludeId.Value;
                    command.Parameters.Add(excludeParam);
                }

                if (!string.IsNullOrEmpty(mediaTypeFilter))
                {
                    // MediaType is stored as varchar, pass the string value
                    var mediaTypeParam = command.CreateParameter();
                    mediaTypeParam.ParameterName = "@mediaType";
                    mediaTypeParam.Value = mediaTypeFilter;
                    command.Parameters.Add(mediaTypeParam);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new VectorSearchResult
                    {
                        Id = reader.GetGuid(0),
                        Title = reader.GetString(1),
                        MediaType = reader.GetString(2),  // Stored as varchar
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Thumbnail = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Status = reader.GetString(5),  // Stored as varchar
                        Rating = reader.IsDBNull(6) ? null : reader.GetString(6),  // Stored as varchar
                        SimilarityScore = reader.GetDouble(7)
                    });
                }

                _logger.LogDebug("pgvector similarity search returned {Count} media items", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing pgvector similarity search for media items");
                throw;
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchNoteResult>> FindSimilarNotesAsync(
            float[] embedding,
            Guid? excludeId = null,
            string? vaultFilter = null,
            int limit = 10)
        {
            var results = new List<VectorSearchNoteResult>();

            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();

                var sql = @"
                    SELECT
                        ""Id"",
                        ""Title"",
                        ""VaultName"",
                        COALESCE(""AiDescription"", ""Description"") as ""Description"",
                        ""SourceUrl"",
                        ""Tags"",
                        (1 - (""Embedding"" <=> @embedding::vector)) as similarity_score
                    FROM ""Notes""
                    WHERE ""Embedding"" IS NOT NULL";

                if (excludeId.HasValue)
                {
                    sql += " AND \"Id\" != @excludeId";
                }

                if (!string.IsNullOrEmpty(vaultFilter))
                {
                    sql += " AND \"VaultName\" = @vaultFilter";
                }

                sql += @"
                    ORDER BY ""Embedding"" <=> @embedding::vector
                    LIMIT @limit";

                command.CommandText = sql;

                var embeddingParam = command.CreateParameter();
                embeddingParam.ParameterName = "@embedding";
                embeddingParam.Value = FormatEmbeddingForPgVector(embedding);
                command.Parameters.Add(embeddingParam);

                var limitParam = command.CreateParameter();
                limitParam.ParameterName = "@limit";
                limitParam.Value = limit;
                command.Parameters.Add(limitParam);

                if (excludeId.HasValue)
                {
                    var excludeParam = command.CreateParameter();
                    excludeParam.ParameterName = "@excludeId";
                    excludeParam.Value = excludeId.Value;
                    command.Parameters.Add(excludeParam);
                }

                if (!string.IsNullOrEmpty(vaultFilter))
                {
                    var vaultParam = command.CreateParameter();
                    vaultParam.ParameterName = "@vaultFilter";
                    vaultParam.Value = vaultFilter;
                    command.Parameters.Add(vaultParam);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Parse tags - stored as text[] in PostgreSQL
                    var tags = new List<string>();
                    if (!reader.IsDBNull(5))
                    {
                        var tagsValue = reader.GetValue(5);
                        if (tagsValue is string[] tagsArray)
                        {
                            tags = tagsArray.ToList();
                        }
                    }

                    results.Add(new VectorSearchNoteResult
                    {
                        Id = reader.GetGuid(0),
                        Title = reader.GetString(1),
                        VaultName = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                        SourceUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Tags = tags,
                        SimilarityScore = reader.GetDouble(6)
                    });
                }

                _logger.LogDebug("pgvector similarity search returned {Count} notes", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing pgvector similarity search for notes");
                throw;
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<float[]?> GetMediaItemEmbeddingAsync(Guid id)
        {
            try
            {
                // Use raw SQL because Embedding property is ignored in EF Core
                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT ""Embedding""::text FROM ""MediaItems"" WHERE ""Id"" = @id AND ""Embedding"" IS NOT NULL";

                var idParam = command.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                command.Parameters.Add(idParam);

                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                // Parse the vector string format: [0.1,0.2,0.3,...]
                var vectorString = result.ToString();
                return ParseVectorString(vectorString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding for media item {Id}", id);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<float[]?> GetNoteEmbeddingAsync(Guid id)
        {
            try
            {
                // Use raw SQL because Embedding property is ignored in EF Core
                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT ""Embedding""::text FROM ""Notes"" WHERE ""Id"" = @id AND ""Embedding"" IS NOT NULL";

                var idParam = command.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                command.Parameters.Add(idParam);

                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                // Parse the vector string format: [0.1,0.2,0.3,...]
                var vectorString = result.ToString();
                return ParseVectorString(vectorString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding for note {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Formats a float array as a pgvector literal string.
        /// Format: [0.1,0.2,0.3,...]
        /// </summary>
        private static string FormatEmbeddingForPgVector(float[] embedding)
        {
            return "[" + string.Join(",", embedding.Select(f => f.ToString("G9", CultureInfo.InvariantCulture))) + "]";
        }

        /// <summary>
        /// Parses a pgvector string format back to a float array.
        /// Format: [0.1,0.2,0.3,...]
        /// </summary>
        private static float[]? ParseVectorString(string? vectorString)
        {
            if (string.IsNullOrWhiteSpace(vectorString))
            {
                return null;
            }

            // Remove brackets and split by comma
            var trimmed = vectorString.Trim('[', ']');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return null;
            }

            var parts = trimmed.Split(',');
            var result = new float[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                {
                    return null;
                }
            }

            return result;
        }
    }
}
