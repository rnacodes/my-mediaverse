using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using MyMediaVerse.Infrastructure.Services.Search;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Pins the Typesense v8 client's <see cref="VectorQuery"/> emit behavior that the recommendation
    /// vector queries depend on. Notably, in 8.4.0 the strongly-typed distance_threshold constructor
    /// argument is dropped by <c>ToQuery()</c>, so TypesenseService passes it via ExtraParams instead.
    /// If a client upgrade changes any of this, these tests should fail and prompt a review of
    /// TypesenseService.BuildVectorQuery.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseVectorQueryContractTests
    {
        private static VectorQuery BuildLikeService(float[]? vector, Guid? id, int limit, double? distanceThreshold)
        {
            // Mirrors TypesenseService.BuildVectorQuery.
            Dictionary<string, string>? extraParams = distanceThreshold.HasValue
                ? new Dictionary<string, string>
                {
                    ["distance_threshold"] = distanceThreshold.Value.ToString(CultureInfo.InvariantCulture)
                }
                : null;

            return new VectorQuery(
                vector ?? Array.Empty<float>(),
                "embedding",
                id?.ToString(),
                limit,
                flatSearchCutoff: null,
                extraParams,
                distanceThreshold: null);
        }

        [Fact]
        public void IdForm_EmitsIdAndK_AndAutoExcludesSource()
        {
            var id = Guid.NewGuid();

            var query = BuildLikeService(vector: null, id: id, limit: 10, distanceThreshold: null).ToQuery();

            // id-form: empty vector, queried by stored doc id (Typesense excludes that doc from results).
            query.Should().Be($"embedding:([],id:{id},k:10)");
        }

        [Fact]
        public void VectorForm_EmitsVectorAndK()
        {
            var query = BuildLikeService(new[] { 0.1f, 0.2f, 0.3f }, id: null, limit: 25, distanceThreshold: null).ToQuery();

            query.Should().Be("embedding:([0.1,0.2,0.3],k:25)");
        }

        [Fact]
        public void DistanceThreshold_IsEmittedViaExtraParams()
        {
            var query = BuildLikeService(new[] { 0.1f, 0.2f }, id: null, limit: 25, distanceThreshold: 0.3).ToQuery();

            query.Should().Contain("distance_threshold:0.3");
        }

        [Fact]
        public void HybridForm_EmitsEmptyVectorWithKCoveringRequestedPage_AndThreshold()
        {
            // Hybrid search: the empty vector makes Typesense embed the query text itself; k must
            // reach the last requested result (page 2 of 20 = 40) and the threshold drops
            // dissimilar documents so a nonsense query no longer returns the whole collection.
            var query = TypesenseService.BuildHybridVectorQuery(perPage: 20, page: 2, distanceThreshold: 0.65).ToQuery();

            query.Should().Be("embedding:([],k:40,distance_threshold:0.65)");
        }

        [Fact]
        public void HybridForm_ClampsNonPositivePagingToOne()
        {
            var query = TypesenseService.BuildHybridVectorQuery(perPage: 0, page: -1, distanceThreshold: TypesenseService.DefaultHybridDistanceThreshold).ToQuery();

            query.Should().Be("embedding:([],k:1,distance_threshold:0.65)");
        }

        [Fact]
        public void HybridForm_SerializesThroughClientConverter_WithoutPlaceholderId()
        {
            // The multi_search request body is what actually reaches Typesense; the client's
            // VectorQueryJsonConverter must pick up the overridden ToQuery() rather than the
            // placeholder id used to satisfy the base constructor.
            var parameters = new MultiSearchParameters("obsidian_notes", "smart notes", "title,embedding")
            {
                VectorQuery = TypesenseService.BuildHybridVectorQuery(perPage: 20, page: 1, distanceThreshold: 0.65)
            };

            var json = JsonSerializer.Serialize(parameters);

            json.Should().Contain("\"vector_query\":\"embedding:([],k:20,distance_threshold:0.65)\"");
            json.Should().NotContain("id:");
        }
    }
}
