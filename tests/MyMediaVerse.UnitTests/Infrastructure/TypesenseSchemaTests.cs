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
        public void MediaBaseFields_CarriesTheWebsiteFields_WithTheirFacetAndTypeSettings()
        {
            var fields = TypesenseService.MediaBaseFields().ToDictionary(f => f.Name);

            fields.Should().ContainKey("domain").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("has_rss").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.Bool && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("link_status").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.Int32 && f.Facet == false && f.Optional == true);
        }

        [Fact]
        public void MediaBaseFields_CarriesThePodcastFields_WithTheirFacetAndTypeSettings()
        {
            var fields = TypesenseService.MediaBaseFields().ToDictionary(f => f.Name);

            fields.Should().ContainKey("podcast_type").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("series_title").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("is_subscribed").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.Bool && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("metadata_source").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == true && f.Optional == true);
        }

        [Fact]
        public void MediaBaseFields_CarriesTheTvFields_WithTheirFacetAndTypeSettings()
        {
            var fields = TypesenseService.MediaBaseFields().ToDictionary(f => f.Name);

            fields.Should().ContainKey("tv_type").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("show_id").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == false && f.Optional == true && f.Index == false);
            fields.Should().ContainKey("show_title").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.String && f.Facet == true && f.Optional == true);
            fields.Should().ContainKey("season_number").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.Int32 && f.Facet == false && f.Optional == true);
            fields.Should().ContainKey("episode_number").WhoseValue.Should().Match<Field>(f =>
                f.Type == FieldType.Int32 && f.Facet == false && f.Optional == true);
        }

        [Fact]
        public void ComputeMissingFields_ProposesTheTvFields_ForACollectionThatPredatesThem()
        {
            // A deployed collection without them must pick them up by alter, not by a destructive reset.
            var live = TypesenseService.MediaBaseFields()
                .Select(f => f.Name)
                .Where(n => n is not ("tv_type" or "show_id" or "show_title" or "season_number" or "episode_number"));

            var missing = TypesenseService.ComputeMissingFields(TypesenseService.MediaBaseFields(), live);

            missing.Select(f => f.Name).Should().BeEquivalentTo(
                "tv_type", "show_id", "show_title", "season_number", "episode_number");
        }

        [Fact]
        public void MediaItemDocument_SerializesTheTvFields_UnderTheSchemaNames()
        {
            var document = new MyMediaVerse.Infrastructure.Models.MediaItemDocument
            {
                Id = "1", Title = "Winter Is Coming", MediaType = "TVShow", Status = "Uncharted",
                TvType = "Episode", ShowId = "abc", ShowTitle = "Game of Thrones", SeasonNumber = 1, EpisodeNumber = 1
            };

            var json = System.Text.Json.JsonSerializer.Serialize(document);

            json.Should().Contain("\"tv_type\":\"Episode\"")
                .And.Contain("\"show_id\":\"abc\"")
                .And.Contain("\"show_title\":\"Game of Thrones\"")
                .And.Contain("\"season_number\":1")
                .And.Contain("\"episode_number\":1");
        }

        [Fact]
        public void ComputeMissingFields_ProposesThePodcastFields_ForACollectionThatPredatesThem()
        {
            // A deployed collection without them must pick them up by alter, not by a destructive reset.
            var live = TypesenseService.MediaBaseFields()
                .Select(f => f.Name)
                .Where(n => n is not ("podcast_type" or "series_title" or "is_subscribed" or "metadata_source"));

            var missing = TypesenseService.ComputeMissingFields(TypesenseService.MediaBaseFields(), live);

            missing.Select(f => f.Name).Should().BeEquivalentTo(
                "podcast_type", "series_title", "is_subscribed", "metadata_source");
        }

        [Fact]
        public void MediaItemDocument_SerializesThePodcastFields_UnderTheSchemaNames()
        {
            var document = new MyMediaVerse.Infrastructure.Models.MediaItemDocument
            {
                Id = "1", Title = "Episode 1", MediaType = "Podcast", Status = "Uncharted",
                PodcastType = "Episode", SeriesTitle = "Darknet Diaries", IsSubscribed = true, MetadataSource = "rss"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(document);

            json.Should().Contain("\"podcast_type\":\"Episode\"")
                .And.Contain("\"series_title\":\"Darknet Diaries\"")
                .And.Contain("\"is_subscribed\":true")
                .And.Contain("\"metadata_source\":\"rss\"");
        }

        [Fact]
        public void MediaBaseFields_LeavesTheEmbeddingPairToTheRuntimeConfig()
        {
            TypesenseService.MediaBaseFields().Select(f => f.Name).Should().NotContain(new[] { "embedding", "embedding_source" });
        }

        [Fact]
        public void MediaItemDocument_SerializesTheWebsiteFields_UnderTheSchemaNames()
        {
            var document = new MyMediaVerse.Infrastructure.Models.MediaItemDocument
            {
                Id = "1", Title = "Site", MediaType = "Website", Status = "Uncharted",
                Domain = "example.com", HasRss = true, LinkStatus = 404
            };

            var json = System.Text.Json.JsonSerializer.Serialize(document);

            json.Should().Contain("\"domain\":\"example.com\"")
                .And.Contain("\"has_rss\":true")
                .And.Contain("\"link_status\":404");
        }

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
