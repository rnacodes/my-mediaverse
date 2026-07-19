using AwesomeAssertions;
using MyMediaVerse.Infrastructure.Services.Search;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Unit tests for the sort_by allowlist that backs the search sort dropdown. Only expressions
    /// against sortable fields with a valid direction are honored; everything else is rejected so it
    /// degrades to the default sort instead of sending Typesense a 400 (or an injected expression).
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseServiceSortTests
    {
        [Theory]
        [InlineData("date_added:desc")]
        [InlineData("date_added:asc")]
        [InlineData("date_added:DESC")] // direction is case-insensitive
        [InlineData("date_added : desc")] // whitespace around the separator is trimmed
        public void IsAllowedSortExpression_AllowsSortableMediaField(string expression)
        {
            TypesenseService.IsAllowedSortExpression(expression, TypesenseService.MediaSortableFields)
                .Should().BeTrue();
        }

        [Theory]
        [InlineData("title:asc")]      // string field, not declared sortable in the schema
        [InlineData("rating:desc")]    // string enum, not sortable / meaningless order
        [InlineData("date_created:desc")] // a mixlist field, not valid for the media collection
        [InlineData("date_added:sideways")] // invalid direction
        [InlineData("date_added")]     // missing direction
        [InlineData("date_added:desc:extra")] // malformed
        [InlineData("")]
        [InlineData(null)]
        public void IsAllowedSortExpression_RejectsUnsupportedOrMalformed(string? expression)
        {
            TypesenseService.IsAllowedSortExpression(expression, TypesenseService.MediaSortableFields)
                .Should().BeFalse();
        }

        [Theory]
        [InlineData("date_created:desc")]
        [InlineData("media_item_count:asc")]
        public void IsAllowedSortExpression_AllowsSortableMixlistFields(string expression)
        {
            TypesenseService.IsAllowedSortExpression(expression, TypesenseService.MixlistSortableFields)
                .Should().BeTrue();
        }
    }
}
