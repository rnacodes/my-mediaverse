using System.Globalization;
using AwesomeAssertions;
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
    }
}
