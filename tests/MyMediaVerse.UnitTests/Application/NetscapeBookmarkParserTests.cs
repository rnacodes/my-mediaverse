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

        private const string SafariExport = """
            <!DOCTYPE NETSCAPE-Bookmark-file-1>
            <HTML>
            <META HTTP-EQUIV="Content-Type" CONTENT="text/html; charset=UTF-8">
            <Title>Bookmarks</Title>
            <H1>Bookmarks</H1>
            <DT><H3 FOLDED>Favorites</H3>
            <DL><p>
                <DT><A HREF="https://www.apple.com/">Apple</A>
                <DT><H3 FOLDED>Reading</H3>
                <DL><p>
                    <DT><A HREF="https://example.com/article" ICON="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAOklEQVQ4T2NkoBAwUqifYdQAhtEwYBgNA4bRMGAYDQOG0TBgGA0DhtEwYBgNA4bRMGAYDQOG0TBgGA0DAAAdAAGaG8rIAAAAAElFTkSuQmCC">Long read &mdash; part 1</A>
                </DL><p>
            </DL><p>
            <DT><H3 FOLDED>Bookmarks Menu</H3>
            <DL><p>
                <DT><A HREF="file:///Users/me/Desktop/Recipe.webloc">Recipe.webloc</A>
                <DT><A HREF="https://example.com/menu">Menu link</A>
            </DL><p>
            </HTML>
            """;

        private const string RaindropExport = """
            <!DOCTYPE NETSCAPE-Bookmark-file-1>
            <!-- This is an automatically generated file. It will be read and overwritten. DO NOT EDIT! -->
            <META HTTP-EQUIV="Content-Type" CONTENT="text/html; charset=UTF-8">
            <TITLE>Bookmarks</TITLE>
            <H1>Bookmarks</H1>
            <DL><p>
            <DT><H3 ADD_DATE="1725000000" LAST_MODIFIED="1725000100">Design</H3>
            <DL><p>
            <DT><A HREF="https://example.com/palette" ADD_DATE="1725000000" LAST_MODIFIED="1725000100" TAGS="color,ui">Palette tool</A>
            <DD>Pick a palette from an image.
            <DT><A HREF="https://example.com/type" ADD_DATE="1725000001" LAST_MODIFIED="1725000001" TAGS="typography">Type scale</A>
            </DL><p>
            <DT><H3 ADD_DATE="1725000000" LAST_MODIFIED="1725000100">Unsorted</H3>
            <DL><p>
            <DT><A HREF="https://example.com/later" ADD_DATE="1725000002" LAST_MODIFIED="1725000002" TAGS="">Later</A>
            </DL><p>
            </DL><p>
            """;

        [Fact]
        public void Parse_SafariExport_HandlesBareTopLevelFolders_IconsAndWeblocFiles()
        {
            var result = _parser.Parse(SafariExport);

            result.Bookmarks.Select(b => b.Url).Should().Equal(
                "https://www.apple.com/",
                "https://example.com/article",
                "https://example.com/menu");
            result.NonWebLinkCount.Should().Be(1, "the .webloc file is a file:// link");

            result.Bookmarks[0].FolderPath.Should().BeEmpty("'Favorites' is Safari's toolbar container");
            result.Bookmarks[1].FolderPath.Should().Equal("Reading");
            result.Bookmarks[1].Title.Should().Be("Long read — part 1", "the ICON data URI does not leak into the title");
            result.Bookmarks[2].FolderPath.Should().BeEmpty("'Bookmarks Menu' is a container");
            result.Bookmarks.Should().OnlyContain(b => b.AddedAt == null, "Safari writes no timestamps");
            result.Bookmarks.Should().OnlyContain(b => b.Tags.Count == 0);

            result.Folders.Should().Equal("Reading");
        }

        [Fact]
        public void Parse_RaindropExport_ReadsCollectionsAsFolders_TagsAndSecondTimestamps()
        {
            var result = _parser.Parse(RaindropExport);

            result.Bookmarks.Select(b => b.Url).Should().Equal(
                "https://example.com/palette",
                "https://example.com/type",
                "https://example.com/later");
            result.NonWebLinkCount.Should().Be(0);

            result.Bookmarks[0].FolderPath.Should().Equal("Design");
            result.Bookmarks[0].Tags.Should().Equal("color", "ui");
            result.Bookmarks[0].AddedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1725000000).UtcDateTime);
            result.Bookmarks[0].Title.Should().Be("Palette tool", "the <DD> excerpt is not part of the title");
            result.Bookmarks[1].Tags.Should().Equal("typography");
            result.Bookmarks[2].FolderPath.Should().Equal(new[] { "Unsorted" }, "Raindrop's Unsorted is a user-visible collection, not a browser container");
            result.Bookmarks[2].Tags.Should().BeEmpty("an empty TAGS attribute yields no tags");

            result.Folders.Should().Equal("Design", "Unsorted");
        }

        [Fact]
        public void Parse_TwoThousandEntries_CompletesQuickly_AndDropsNothing()
        {
            const int count = 2000;
            var builder = new System.Text.StringBuilder("<!DOCTYPE NETSCAPE-Bookmark-file-1>\n<DL><p>\n");
            var icon = "ICON=\"data:image/png;base64," + new string('A', 400) + "\"";
            for (var i = 0; i < count; i++)
            {
                if (i % 100 == 0) builder.Append($"<DT><H3 ADD_DATE=\"1700000000\">Folder {i / 100}</H3>\n<DL><p>\n");
                builder.Append($"<DT><A HREF=\"https://example.com/page-{i}?ref=x&amp;n={i}\" ADD_DATE=\"{1700000000 + i}\" {icon} TAGS=\"t{i % 7},bulk\">Page {i} &amp; friends <b>bold</b></A>\n");
                if (i % 100 == 99) builder.Append("</DL><p>\n");
            }
            builder.Append("</DL><p>\n");
            var content = builder.ToString();

            // Warm up so the compiled regexes' first-use JIT cost is not what gets timed.
            _parser.Parse(ChromeExport);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = _parser.Parse(content);
            stopwatch.Stop();

            result.Bookmarks.Should().HaveCount(count);
            result.NonWebLinkCount.Should().Be(0);
            result.Folders.Should().HaveCount(count / 100);
            result.Bookmarks[1999].Url.Should().Be("https://example.com/page-1999?ref=x&n=1999");
            result.Bookmarks[1999].Title.Should().Be("Page 1999 & friends bold");
            result.Bookmarks[1999].FolderPath.Should().Equal("Folder 19");
            result.Bookmarks[1999].Tags.Should().Equal("t4", "bulk");
            // Catastrophic backtracking would take minutes here; the generous bound keeps a busy
            // machine from turning this into a flaky test while still catching a regressed regex.
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3), "the token scanner must stay linear in the file size");
        }

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
