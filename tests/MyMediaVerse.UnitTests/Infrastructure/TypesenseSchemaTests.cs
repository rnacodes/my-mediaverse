using AwesomeAssertions;
using MyMediaVerse.Infrastructure.Services.Search;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Unit tests for the add-only schema diff the media collection runs on startup and before each
    /// bulk reindex, so a field added in code reaches an existing Typesense collection without the
    /// destructive reset. Pure function; no Typesense client involved.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseSchemaTests
    {
        private static readonly List<Field> Desired = new()
        {
            new Field("id", FieldType.String, false),
            new Field("title", FieldType.String, false),
            new Field("author", FieldType.String, true, optional: true),
            new Field("isbn", FieldType.String, false, optional: true),
            new Field("goodreads_rating", FieldType.Float, true, optional: true),
        };

        [Fact]
        public void ComputeMissingFields_ReturnsOnlyFieldsTheLiveCollectionLacks()
        {
            var missing = TypesenseService.ComputeMissingFields(Desired, new[] { "title", "author" });

            missing.Select(f => f.Name).Should().BeEquivalentTo(new[] { "isbn", "goodreads_rating" });
        }

        [Fact]
        public void ComputeMissingFields_ReturnsEmpty_WhenTheLiveCollectionIsComplete()
        {
            var missing = TypesenseService.ComputeMissingFields(Desired, Desired.Select(f => f.Name));

            missing.Should().BeEmpty();
        }

        [Fact]
        public void ComputeMissingFields_NeverProposesTheIdField()
        {
            var liveWithoutId = Desired.Select(f => f.Name).Where(n => n != "id");

            var missing = TypesenseService.ComputeMissingFields(Desired, liveWithoutId);

            missing.Should().BeEmpty();
        }

        [Fact]
        public void ComputeMissingFields_NeverProposesTheEmbeddingPair()
        {
            // Adding the auto-embedding fields by alter would re-embed every document at a cost; that
            // path stays behind the explicit reset endpoint.
            var withEmbedding = Desired.Concat(new[]
            {
                new Field("embedding_source", FieldType.String, false, optional: true),
                new Field("embedding", FieldType.FloatArray, false, optional: true),
            });

            var missing = TypesenseService.ComputeMissingFields(withEmbedding, new[] { "id", "title" });

            missing.Select(f => f.Name).Should().NotContain(new[] { "embedding_source", "embedding" });
            missing.Select(f => f.Name).Should().Contain("isbn");
        }

        [Fact]
        public void ComputeMissingFields_IgnoresEmptyLiveNames()
        {
            var missing = TypesenseService.ComputeMissingFields(Desired, new[] { "id", "", "title", "author", "isbn", "goodreads_rating" });

            missing.Should().BeEmpty();
        }
    }
}
