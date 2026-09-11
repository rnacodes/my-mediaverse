using AwesomeAssertions;
using MyMediaVerse.Application.Services.Bookmarks;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class NetscapeBookmarkWriterTests
    {
        private static Website Site(string title, string url, DateTime added, params string[] topics)
        {
            var website = TestDataFactory.CreateWebsite(title, url);
            website.DateAdded = added;
            foreach (var topic in topics) website.Topics.Add(new Topic { Name = topic });
            return website;
        }

        [Fact]
        public void Write_ProducesTheNetscapeHeader_AndOneEntryPerWebsite()
        {
            var added = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            var html = NetscapeBookmarkWriter.Write(new[]
            {
                Site("Tools & Tips", "https://example.com/a?q=1&r=2", added, "dev", "tips"),
                Site("Plain", "https://example.com/b", added)
            });

            html.Should().StartWith("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            html.Should().Contain("<DL><p>").And.Contain("</DL><p>");
            html.Should().Contain("<DT><A HREF=\"https://example.com/a?q=1&amp;r=2\" ADD_DATE=\"1710504000\" TAGS=\"dev,tips\">Tools &amp; Tips</A>");
            html.Should().Contain("<DT><A HREF=\"https://example.com/b\" ADD_DATE=\"1710504000\">Plain</A>");
        }

        [Fact]
        public void Write_SkipsWebsitesWithoutALink()
        {
            var website = Site("No link", "https://example.com/x", DateTime.UtcNow);
            website.Link = null;

            var html = NetscapeBookmarkWriter.Write(new[] { website });

            html.Should().NotContain("<DT><A");
        }

        [Fact]
        public void Write_RoundTripsThroughTheParser()
        {
            var added = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            var html = NetscapeBookmarkWriter.Write(new[]
            {
                Site("Tools & Tips", "https://example.com/a", added, "dev", "tips"),
                Site("Plain", "https://example.com/b", added)
            });

            var parsed = new NetscapeBookmarkParser().Parse(html);

            parsed.NonWebLinkCount.Should().Be(0);
            parsed.Bookmarks.Should().HaveCount(2);
            parsed.Bookmarks[0].Url.Should().Be("https://example.com/a");
            parsed.Bookmarks[0].Title.Should().Be("Tools & Tips");
            parsed.Bookmarks[0].Tags.Should().Equal("dev", "tips");
            parsed.Bookmarks[0].AddedAt.Should().Be(added);
            parsed.Bookmarks[0].FolderPath.Should().BeEmpty("the export is flat");
            parsed.Bookmarks[1].Tags.Should().BeEmpty();
        }

        [Fact]
        public void FileName_IsDateStamped()
        {
            NetscapeBookmarkWriter.FileName(new DateTime(2026, 9, 7)).Should().Be("mymediaverse-bookmarks-20260907.html");
        }
    }
}
