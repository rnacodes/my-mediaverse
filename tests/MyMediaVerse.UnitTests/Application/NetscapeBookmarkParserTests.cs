using AwesomeAssertions;
using MyMediaVerse.Application.Services.Bookmarks;

namespace MyMediaVerse.UnitTests.Application
{
    /// <summary>
    /// The Netscape bookmark format as browsers actually write it: container folders that carry no
    /// meaning, nested user folders, unclosed tags, timestamps in three units, bookmarklets mixed in
    /// with web links, and entities in titles.
    /// </summary>
    [Trait("Category", "Unit")]
    public class NetscapeBookmarkParserTests
    {
        private readonly NetscapeBookmarkParser _parser = new();

        private const string ChromeExport = """
            <!DOCTYPE NETSCAPE-Bookmark-file-1>
            <!-- This is an automatically generated file.
                 It will be read and overwritten.
                 DO NOT EDIT! -->
            <META HTTP-EQUIV="Content-Type" CONTENT="text/html; charset=UTF-8">
            <TITLE>Bookmarks</TITLE>
            <H1>Bookmarks</H1>
            <DL><p>
                <DT><H3 ADD_DATE="1700000000" LAST_MODIFIED="1700000000" PERSONAL_TOOLBAR_FOLDER="true">Bookmarks bar</H3>
                <DL><p>
                    <DT><H3 ADD_DATE="1700000000" LAST_MODIFIED="0">Dev</H3>
                    <DL><p>
                        <DT><A HREF="https://example.com/dev" ADD_DATE="1700000000" ICON="data:image/png;base64,iVBORw0KGgo=">Dev &amp; Tools</A>
                        <DT><H3 ADD_DATE="1700000000">Rust</H3>
                        <DL><p>
                            <DT><A HREF="https://www.rust-lang.org/" ADD_DATE="1700000001000">Rust</A>
                        </DL><p>
                    </DL><p>
                    <DT><A HREF="javascript:(function(){location.href='https://x'})()" ADD_DATE="1700000000">Save to MMV</A>
                    <DT><A HREF="https://example.com/toolbar" ADD_DATE="1700000002">Toolbar link</A>
                </DL><p>
                <DT><H3 ADD_DATE="1700000000" LAST_MODIFIED="0">Other bookmarks</H3>
                <DL><p>
                    <DT><A HREF="https://example.com/other" ADD_DATE="1700000003" TAGS="reading, later">Other</A>
                    <DT><A HREF="chrome://settings/">Settings</A>
                </DL><p>
            </DL><p>
            """;

        private const string FirefoxExport = """
            <!DOCTYPE NETSCAPE-Bookmark-file-1>
            <META HTTP-EQUIV="Content-Type" CONTENT="text/html; charset=UTF-8">
            <TITLE>Bookmarks</TITLE>
            <H1>Bookmarks Menu</H1>

            <DL><p>
                <DT><A HREF="https://example.com/menu" ADD_DATE="1700000000" LAST_MODIFIED="1700000000">Menu link</A>
                <DD>A description line that browsers add.</DD>
                <HR>
                <DT><H3 ADD_DATE="1700000000" LAST_MODIFIED="1700000000" PERSONAL_TOOLBAR_FOLDER="true">Bookmarks Toolbar</H3>
                <DL><p>
                    <DT><H3 ADD_DATE="1700000000">Recipes</H3>
                    <DL><p>
                        <DT><A HREF="https://example.com/soup" ADD_DATE="1700000000000000" TAGS="food">Soup</A>
                    </DL><p>
                </DL><p>
                <DT><H3 ADD_DATE="1700000000" UNFILED_BOOKMARKS_FOLDER="true">Other Bookmarks</H3>
                <DL><p>
                    <DT><A HREF="place:sort=8&maxResults=10">Recent tags</A>
                </DL><p>
            </DL>
            """;

        [Fact]
        public void Parse_ChromeExport_ReadsWebLinksWithFolderPaths_AndSkipsContainersAndNonWebLinks()
        {
            var result = _parser.Parse(ChromeExport);

            result.Bookmarks.Select(b => b.Url).Should().Equal(
                "https://example.com/dev",
                "https://www.rust-lang.org/",
                "https://example.com/toolbar",
                "https://example.com/other");
            result.NonWebLinkCount.Should().Be(2, "the bookmarklet and the chrome:// page are not web links");

            var dev = result.Bookmarks[0];
            dev.Title.Should().Be("Dev & Tools", "entities are decoded");
            dev.FolderPath.Should().Equal("Dev");
            dev.AddedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime);

            result.Bookmarks[1].FolderPath.Should().Equal("Dev", "Rust");
            result.Bookmarks[2].FolderPath.Should().BeEmpty("the toolbar container is not a folder");
            result.Bookmarks[3].FolderPath.Should().BeEmpty("'Other bookmarks' is a container");
            result.Bookmarks[3].Tags.Should().Equal("reading", "later");

            result.Folders.Should().Equal("Dev", "Dev/Rust");
        }

        [Fact]
        public void Parse_FirefoxExport_IgnoresDescriptionsAndRules_AndSkipsTheUnfiledContainer()
        {
            var result = _parser.Parse(FirefoxExport);

            result.Bookmarks.Select(b => b.Url).Should().Equal("https://example.com/menu", "https://example.com/soup");
            result.NonWebLinkCount.Should().Be(1, "place: is a Firefox-internal query");
            result.Bookmarks[1].FolderPath.Should().Equal("Recipes");
            result.Bookmarks[1].Tags.Should().Equal("food");
            result.Bookmarks[1].AddedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime, "microsecond timestamps are recognized by magnitude");
        }

        [Fact]
        public void Parse_ToleratesLowercaseTags_AndMissingTimestamps()
        {
            var result = _parser.Parse("<dl><p><dt><h3>work</h3><dl><p><dt><a href=\"https://example.com/a\">A</a></dl></dl>");

            result.Bookmarks.Should().ContainSingle();
            result.Bookmarks[0].FolderPath.Should().Equal("work");
            result.Bookmarks[0].AddedAt.Should().BeNull();
            result.Bookmarks[0].Tags.Should().BeEmpty();
        }

        [Fact]
        public void Parse_UsesNullTitle_WhenTheAnchorIsEmpty()
        {
            var result = _parser.Parse("<DL><p><DT><A HREF=\"https://example.com/untitled\"></A></DL>");

            result.Bookmarks.Single().Title.Should().BeNull();
        }

        [Fact]
        public void Parse_KeepsAUserFolderNamedLikeAContainer_WhenItIsNested()
        {
            const string html = """
                <DL><p>
                    <DT><H3>Projects</H3>
                    <DL><p>
                        <DT><H3>Menu</H3>
                        <DL><p>
                            <DT><A HREF="https://example.com/menu-design">Menu design</A>
                        </DL><p>
                    </DL><p>
                </DL><p>
                """;

            var result = _parser.Parse(html);

            result.Bookmarks.Single().FolderPath.Should().Equal("Projects", "Menu");
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("   ", 0)]
        [InlineData("<html><body>No bookmarks here</body></html>", 0)]
        public void Parse_EmptyOrUnrelatedContent_YieldsNothing(string content, int expected)
        {
            _parser.Parse(content).Bookmarks.Should().HaveCount(expected);
        }

        [Theory]
        [InlineData("1700000000", 2023, 11, 14)]
        [InlineData("1700000000000", 2023, 11, 14)]
        [InlineData("1700000000000000", 2023, 11, 14)]
        public void ParseAddDate_RecognizesSecondsMillisecondsAndMicroseconds(string raw, int year, int month, int day)
        {
            var date = NetscapeBookmarkParser.ParseAddDate(raw);

            date.Should().NotBeNull();
            date!.Value.Date.Should().Be(new DateTime(year, month, day));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-number")]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("99999999999999999")]
        public void ParseAddDate_DropsImplausibleValues(string? raw)
        {
            NetscapeBookmarkParser.ParseAddDate(raw).Should().BeNull();
        }

        [Theory]
        [InlineData("bookmarks.html", "<html>", true)]
        [InlineData("bookmarks_9_7_26.HTM", "", true)]
        [InlineData("export.txt", "<!DOCTYPE NETSCAPE-Bookmark-file-1>", true)]
        [InlineData("export.csv", "url,title", false)]
        [InlineData("notes.txt", "just text", false)]
        public void CanParse_JudgesByExtensionOrSignature(string fileName, string head, bool expected)
        {
            _parser.CanParse(fileName, head).Should().Be(expected);
        }
    }
}
