using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class UrlListParserTests
    {
        [Fact]
        public void Parse_ReadsOneUrlPerLine_ToleratingBulletsCommasAndQuotes()
        {
            const string text = """
                https://example.com/one
                - https://example.com/two
                * "https://example.com/three", <https://example.com/four>
                • https://example.com/five;https://example.com/six
                """;

            var result = UrlListParser.Parse(text);

            result.Bookmarks.Select(b => b.Url).Should().Equal(
                "https://example.com/one",
                "https://example.com/two",
                "https://example.com/three",
                "https://example.com/four",
                "https://example.com/five",
                "https://example.com/six");
            result.NonWebLinkCount.Should().Be(0);
        }

        [Fact]
        public void Parse_AssumesHttps_ForBareDomains()
        {
            var result = UrlListParser.Parse("example.com\nwww.example.org/path?x=1");

            result.Bookmarks.Select(b => b.Url).Should().Equal("https://example.com", "https://www.example.org/path?x=1");
        }

        [Fact]
        public void Parse_CountsTokensThatAreNotWebLinks()
        {
            var result = UrlListParser.Parse("https://example.com/ok hello ftp://example.com/file mailto:someone@example.com");

            result.Bookmarks.Should().ContainSingle().Which.Url.Should().Be("https://example.com/ok");
            result.NonWebLinkCount.Should().Be(3);
        }

        [Fact]
        public void Parse_ProducesBareBookmarks_WithNoTitleFoldersOrTags()
        {
            var bookmark = UrlListParser.Parse("https://example.com/plain").Bookmarks.Single();

            bookmark.Title.Should().BeNull();
            bookmark.AddedAt.Should().BeNull();
            bookmark.FolderPath.Should().BeEmpty();
            bookmark.Tags.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  \n\t ")]
        [InlineData("- \n * ")]
        public void Parse_EmptyInput_YieldsNothing(string? text)
        {
            var result = UrlListParser.Parse(text);

            result.Bookmarks.Should().BeEmpty();
            result.NonWebLinkCount.Should().Be(0);
        }
    }
}
