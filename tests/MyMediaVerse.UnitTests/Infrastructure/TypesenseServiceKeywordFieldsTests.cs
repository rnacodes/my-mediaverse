using AwesomeAssertions;
using MyMediaVerse.Infrastructure.Services.Search;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Pins the fields a media keyword search reads. An episode's own title rarely names its show,
    /// so the parent-title fields are what let "chernobyl" find the episodes of Chernobyl.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseServiceKeywordFieldsTests
    {
        private static readonly string[] KeywordFields = TypesenseService.MediaKeywordFields.Split(',');

        [Theory]
        [InlineData("show_title")]   // TV episodes: the parent show
        [InlineData("series_title")] // Podcast episodes: the parent series
        public void MediaKeywordFields_IncludeTheParentTitleOfAnEpisode(string field)
        {
            KeywordFields.Should().Contain(field);
        }

        [Fact]
        public void MediaKeywordFields_RankAnItemsOwnTitleAboveItsParentsTitle()
        {
            var ownTitle = Array.IndexOf(KeywordFields, "title");

            ownTitle.Should().Be(0);
            Array.IndexOf(KeywordFields, "show_title").Should().BeGreaterThan(ownTitle);
            Array.IndexOf(KeywordFields, "series_title").Should().BeGreaterThan(ownTitle);
        }

        [Fact]
        public void MediaKeywordFields_AllExistInTheCollectionSchemaAsText()
        {
            // Typesense answers a search that names an unknown field with an error, so a field
            // added here without a schema entry would break every media search.
            var schema = TypesenseService.MediaBaseFields().ToDictionary(f => f.Name);

            foreach (var field in KeywordFields)
            {
                schema.Should().ContainKey(field);
                schema[field].Type.Should().Be(Typesense.FieldType.String, $"'{field}' is searched as text");
            }
        }

        [Fact]
        public void MediaKeywordFields_HaveNoBlanksOrDuplicates()
        {
            KeywordFields.Should().OnlyContain(f => f.Length > 0 && f == f.Trim());
            KeywordFields.Should().OnlyHaveUniqueItems();
        }
    }
}
