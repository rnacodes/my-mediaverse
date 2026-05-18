using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.Interfaces;
using Npgsql;

namespace MyMediaVerse.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Exercises <see cref="IVectorSearchRepository"/> against a real Postgres + pgvector
    /// container. Validates the cosine-distance operator (<c>&lt;=&gt;</c>) ordering, the
    /// similarity score conversion (<c>1 - distance</c>), filter parameters, and vector
    /// roundtripping. The <c>Embedding</c> column is <c>Ignore()</c>d in EF Core, so seeding
    /// goes through raw SQL — that path is itself part of what these tests cover.
    /// </summary>
    [Trait("Category", "Database")]
    [Collection("Database")]
    public class VectorSearchRepositoryTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;

        public VectorSearchRepositoryTests(ApiFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        // ----- Connection-state probes -----

        [Fact]
        public async Task IsPgVectorAvailable_ReturnsTrue_OnPgvectorImage()
        {
            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var available = await repo.IsPgVectorAvailableAsync();
                available.Should().BeTrue("the ApiFactory container is pgvector/pgvector:pg16");
            }
        }

        [Fact]
        public async Task HasAnyMediaEmbeddings_ReturnsFalse_WhenTableIsEmpty()
        {
            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var hasAny = await repo.HasAnyMediaEmbeddingsAsync();
                hasAny.Should().BeFalse();
            }
        }

        [Fact]
        public async Task HasAnyMediaEmbeddings_ReturnsTrue_AfterEmbeddingSeeded()
        {
            await InsertMediaItemWithEmbeddingAsync(Guid.NewGuid(), "Seeded", "Article", BuildUnitVector(0));

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var hasAny = await repo.HasAnyMediaEmbeddingsAsync();
                hasAny.Should().BeTrue();
            }
        }

        // ----- FindSimilarMediaItemsAsync -----

        [Fact]
        public async Task FindSimilarMediaItems_OrdersByAscendingCosineDistance()
        {
            // Query vector is unit basis e0. Compare three rows of decreasing similarity.
            var queryVector = BuildUnitVector(0);
            var closeId = Guid.NewGuid();
            var mediumId = Guid.NewGuid();
            var farId = Guid.NewGuid();

            await InsertMediaItemWithEmbeddingAsync(closeId, "Close", "Article", MakeVector(new (int Index, float Value)[] { (0, 0.9f), (1, 0.1f) }));
            await InsertMediaItemWithEmbeddingAsync(mediumId, "Medium", "Article", MakeVector(new (int Index, float Value)[] { (0, 0.5f), (1, 0.5f) }));
            await InsertMediaItemWithEmbeddingAsync(farId, "Far", "Article", BuildUnitVector(1));

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var results = await repo.FindSimilarMediaItemsAsync(queryVector, limit: 10);

                results.Should().HaveCount(3);
                results.Select(r => r.Id).Should().ContainInOrder(closeId, mediumId, farId);

                // Similarity scores decrease monotonically and match the analytical cosine.
                results[0].SimilarityScore.Should().BeApproximately(0.9938837, 1e-4);
                results[1].SimilarityScore.Should().BeApproximately(0.70710677, 1e-4);
                results[2].SimilarityScore.Should().BeApproximately(0.0, 1e-4);
            }
        }

        [Fact]
        public async Task FindSimilarMediaItems_RespectsLimit()
        {
            for (int i = 0; i < 5; i++)
            {
                await InsertMediaItemWithEmbeddingAsync(
                    Guid.NewGuid(),
                    $"Item-{i}",
                    "Article",
                    MakeVector(new (int Index, float Value)[] { (0, 1f - i * 0.1f), (1, i * 0.1f) }));
            }

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var results = await repo.FindSimilarMediaItemsAsync(BuildUnitVector(0), limit: 2);
                results.Should().HaveCount(2);
            }
        }

        [Fact]
        public async Task FindSimilarMediaItems_ExcludesGivenId()
        {
            var sourceId = Guid.NewGuid();
            var otherId = Guid.NewGuid();

            await InsertMediaItemWithEmbeddingAsync(sourceId, "Source", "Article", BuildUnitVector(0));
            await InsertMediaItemWithEmbeddingAsync(otherId, "Other", "Article", BuildUnitVector(0));

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var results = await repo.FindSimilarMediaItemsAsync(BuildUnitVector(0), excludeId: sourceId);

                results.Should().HaveCount(1);
                results.Single().Id.Should().Be(otherId);
            }
        }

        [Fact]
        public async Task FindSimilarMediaItems_FiltersByMediaType()
        {
            var articleId = Guid.NewGuid();
            var movieId = Guid.NewGuid();

            await InsertMediaItemWithEmbeddingAsync(articleId, "Article-A", "Article", BuildUnitVector(0));
            await InsertMediaItemWithEmbeddingAsync(movieId, "Movie-A", "Movie", BuildUnitVector(0));

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var results = await repo.FindSimilarMediaItemsAsync(BuildUnitVector(0), mediaTypeFilter: "Movie");

                results.Should().HaveCount(1);
                results.Single().Id.Should().Be(movieId);
                results.Single().MediaType.Should().Be("Movie");
            }
        }

        [Fact]
        public async Task FindSimilarMediaItems_SkipsRowsWithoutEmbedding()
        {
            var withEmbeddingId = Guid.NewGuid();
            var withoutEmbeddingId = Guid.NewGuid();

            await InsertMediaItemWithEmbeddingAsync(withEmbeddingId, "Embedded", "Article", BuildUnitVector(0));
            await InsertMediaItemAsync(withoutEmbeddingId, "Bare", "Article");

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var results = await repo.FindSimilarMediaItemsAsync(BuildUnitVector(0));

                results.Should().HaveCount(1);
                results.Single().Id.Should().Be(withEmbeddingId);
            }
        }

        [Fact]
        public async Task GetMediaItemEmbedding_RoundtripsVector()
        {
            var id = Guid.NewGuid();
            var original = MakeVector(new (int Index, float Value)[] { (0, 0.25f), (1, 0.5f), (2, 0.75f) });

            await InsertMediaItemWithEmbeddingAsync(id, "Roundtrip", "Article", original);

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var fetched = await repo.GetMediaItemEmbeddingAsync(id);

                fetched.Should().NotBeNull();
                fetched!.Length.Should().Be(1024);
                fetched[0].Should().BeApproximately(0.25f, 1e-6f);
                fetched[1].Should().BeApproximately(0.5f, 1e-6f);
                fetched[2].Should().BeApproximately(0.75f, 1e-6f);
                fetched[3].Should().Be(0f);
            }
        }

        // ----- FindSimilarNotesAsync -----

        [Fact]
        public async Task FindSimilarNotes_OrdersByAscendingCosineDistance_AndFiltersByVault()
        {
            var queryVector = BuildUnitVector(0);
            var generalCloseId = Guid.NewGuid();
            var generalFarId = Guid.NewGuid();
            var programmingId = Guid.NewGuid();

            await InsertNoteWithEmbeddingAsync(generalCloseId, "Close-General", "general", MakeVector(new (int Index, float Value)[] { (0, 0.9f), (1, 0.1f) }));
            await InsertNoteWithEmbeddingAsync(generalFarId, "Far-General", "general", BuildUnitVector(1));
            await InsertNoteWithEmbeddingAsync(programmingId, "Programming", "programming", BuildUnitVector(0));

            var repo = ResolveRepository(out var scope);
            using (scope)
            {
                var allVaults = await repo.FindSimilarNotesAsync(queryVector);
                allVaults.Select(r => r.Id).Should().ContainInOrder(programmingId, generalCloseId, generalFarId);

                var generalOnly = await repo.FindSimilarNotesAsync(queryVector, vaultFilter: "general");
                generalOnly.Should().HaveCount(2);
                generalOnly.Select(r => r.Id).Should().ContainInOrder(generalCloseId, generalFarId);
                generalOnly.Should().OnlyContain(r => r.VaultName == "general");
            }
        }

        // ----- Helpers -----

        private IVectorSearchRepository ResolveRepository(out IServiceScope scope)
        {
            scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IVectorSearchRepository>();
        }

        /// <summary>1024-dim unit basis vector with 1.0 at <paramref name="hotIndex"/>.</summary>
        private static float[] BuildUnitVector(int hotIndex)
        {
            var v = new float[1024];
            v[hotIndex] = 1f;
            return v;
        }

        /// <summary>1024-dim vector with values set at the given sparse indices, zeros elsewhere.</summary>
        private static float[] MakeVector(IEnumerable<(int Index, float Value)> values)
        {
            var v = new float[1024];
            foreach (var (i, val) in values)
            {
                v[i] = val;
            }
            return v;
        }

        private static string FormatPgVector(float[] embedding)
        {
            return "[" + string.Join(",", embedding.Select(f => f.ToString("G9", CultureInfo.InvariantCulture))) + "]";
        }

        private async Task InsertMediaItemAsync(Guid id, string title, string mediaType)
        {
            await using var conn = new NpgsqlConnection(_factory.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ""MediaItems"" (""Id"", ""Title"", ""MediaType"", ""Status"", ""DateAdded"")
                VALUES (@id, @title, @mediaType, 'Uncharted', @now)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@mediaType", mediaType);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertMediaItemWithEmbeddingAsync(Guid id, string title, string mediaType, float[] embedding)
        {
            await using var conn = new NpgsqlConnection(_factory.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ""MediaItems"" (""Id"", ""Title"", ""MediaType"", ""Status"", ""DateAdded"", ""Embedding"")
                VALUES (@id, @title, @mediaType, 'Uncharted', @now, @embedding::vector)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@mediaType", mediaType);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@embedding", FormatPgVector(embedding));
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertNoteWithEmbeddingAsync(Guid id, string title, string vaultName, float[] embedding)
        {
            await using var conn = new NpgsqlConnection(_factory.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ""Notes"" (""Id"", ""Slug"", ""Title"", ""VaultName"", ""Tags"", ""DateImported"", ""IsDescriptionManual"", ""Embedding"")
                VALUES (@id, @slug, @title, @vault, '[]'::jsonb, @now, false, @embedding::vector)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@slug", $"slug-{id:N}");
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@vault", vaultName);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@embedding", FormatPgVector(embedding));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
