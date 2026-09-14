using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Services.Podcasts;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class RssPodcastFeedReaderTests
    {
        private const string FeedUrl = "https://feeds.example.com/show.xml";

        private readonly TestHttpMessageHandler _handler = new();
        private readonly HttpClient _httpClient;
        private readonly RssPodcastFeedReader _reader;

        public RssPodcastFeedReaderTests()
        {
            _httpClient = new HttpClient(_handler) { Timeout = TimeSpan.FromSeconds(15) };
            _reader = new RssPodcastFeedReader(_httpClient, Substitute.For<ILogger<RssPodcastFeedReader>>());
        }

        private static string FixturePath(string name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "Feeds", name);

        private static PodcastFeed ParseFixture(string name, int maxItems = int.MaxValue)
        {
            using var stream = File.OpenRead(FixturePath(name));
            return RssPodcastFeedReader.Parse(stream, maxItems);
        }

        private static PodcastFeed ParseXml(string xml)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return RssPodcastFeedReader.Parse(stream);
        }

        #region Real feeds

        [Fact]
        public void Parse_DarknetDiaries_ReadsChannelAndEpisodes()
        {
            var feed = ParseFixture("darknet-diaries.xml");

            feed.Series.Title.Should().Be("Darknet Diaries");
            feed.Series.Publisher.Should().Be("Jack Rhysider");
            feed.Series.Language.Should().Be("en-us");
            feed.Series.Link.Should().Be("https://darknetdiaries.com/");
            feed.Series.PodcastGuid.Should().Be("ac631a54-a629-5367-a1a5-c2e2f7c25054");
            feed.Series.Categories.Should().Equal("Technology");
            feed.Series.Explicit.Should().BeFalse();
            feed.Series.ImageUrl.Should().StartWith("https://f.prxu.org/7057/images/68ff605a");
            feed.Series.Description.Should().StartWith("Explore true stories of the dark side of the Internet")
                .And.NotContain("<p>");
            feed.TotalItemCount.Should().Be(5);
            feed.Episodes.Should().HaveCount(5);

            var latest = feed.Episodes[0];
            latest.Title.Should().Be("179: The Courthouse - Revisited");
            latest.Guid.Should().Be("prx_7057_82aaba05-80c9-4d2e-8069-3c20f2b7dbc8");
            latest.PublishedAt.Should().Be(new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc));
            latest.PublishedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
            latest.DurationSeconds.Should().Be(5702);
            latest.EpisodeNumber.Should().Be(179);
            latest.SeasonNumber.Should().Be(1);
            latest.EnclosureType.Should().Be("audio/mpeg");
            latest.EnclosureUrl.Should().EndWith("darknet-diaries-ep179-the-courthouse-revisited.mp3");
            latest.Link.Should().Be("https://darknetdiaries.com/episode/179/");
            latest.ImageUrl.Should().EndWith("Ep179_Square.jpg");
            latest.Description.Should().NotContain("<");
        }

        [Fact]
        public void Parse_Unresolved_ReadsSpreakerFeed()
        {
            var feed = ParseFixture("unresolved.xml");

            feed.Series.Title.Should().Be("Unresolved: A True Crime & Mystery Podcast");
            feed.Series.Publisher.Should().Be("Unresolved Productions");
            feed.Series.PodcastGuid.Should().Be("7a7c358e-b26a-5a89-86c5-cd5c69ebfabb");
            feed.Series.Categories.Should().Equal("True Crime", "Society & Culture", "History");
            feed.Series.Explicit.Should().BeTrue();

            var latest = feed.Episodes[0];
            latest.Guid.Should().Be("https://api.spreaker.com/episode/74927920");
            latest.PublishedAt.Should().Be(new DateTime(2026, 9, 5, 19, 44, 25, DateTimeKind.Utc));
            latest.DurationSeconds.Should().Be(2341);
            latest.SeasonNumber.Should().Be(11);
            latest.EpisodeNumber.Should().Be(29);
        }

        [Fact]
        public void Parse_SelfPublishingSchool_ConvertsOffsetsToUtcAndFlattensNestedCategories()
        {
            var feed = ParseFixture("self-publishing-school.xml");

            feed.Series.PodcastGuid.Should().Be("65c8c26d-57bd-5939-b739-4a23549b113c");
            feed.Series.Publisher.Should().Be("Chandler Bolt, Founder of selfpublishing.com");
            feed.Series.Categories.Should().Equal("Business", "Arts", "Books", "Education", "Courses");

            var latest = feed.Episodes[0];
            latest.Guid.Should().Be("Buzzsprout-19751204");
            // Thu, 10 Sep 2026 16:00:00 -0500
            latest.PublishedAt.Should().Be(new DateTime(2026, 9, 10, 21, 0, 0, DateTimeKind.Utc));
            latest.DurationSeconds.Should().Be(682);
        }

        [Fact]
        public void Parse_BuildingASecondBrain_HandlesCdataGuidsAndMinuteDurations()
        {
            var feed = ParseFixture("building-a-second-brain.xml");

            feed.Series.Title.Should().Be("The Building a Second Brain Podcast");
            feed.Series.Publisher.Should().Be("Tiago Forte");
            feed.Series.PodcastGuid.Should().BeNull();
            feed.Series.Categories.Should().Equal("Education", "Self-Improvement", "Courses");

            var first = feed.Episodes[0];
            first.Title.Should().Be("The 9 Biggest Myths about Building a Second Brain");
            first.Guid.Should().Be("c84134cc-0a37-484a-acfa-306041a69ddf");
            first.Link.Should().Be("https://secondbrain.libsyn.com/the-9-biggest-myths-about-building-a-second-brain");
            first.DurationSeconds.Should().Be(343);
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(0, 0)]
        public void Parse_WithMaxItems_LimitsEpisodesButCountsEveryItem(int maxItems, int expectedEpisodes)
        {
            var feed = ParseFixture("darknet-diaries.xml", maxItems);

            feed.Episodes.Should().HaveCount(expectedEpisodes);
            feed.TotalItemCount.Should().Be(5);
            feed.Series.Title.Should().Be("Darknet Diaries");
        }

        #endregion

        #region Edge cases

        [Fact]
        public void Parse_EdgeCases_ChannelFallbacks()
        {
            var feed = ParseFixture("edge-cases.xml");

            feed.Series.Title.Should().Be("Edge Case Radio");
            feed.Series.Description.Should().Be("Stories about parsing & feeds.");
            feed.Series.Publisher.Should().Be("Edge Owner");
            feed.Series.ImageUrl.Should().Be("https://edge.example.com/channel.jpg");
            feed.Series.PodcastGuid.Should().Be("11111111-2222-3333-4444-555555555555");
            feed.Series.Categories.Should().Equal("Arts", "Books");
            feed.Series.Explicit.Should().BeTrue();
            feed.Series.Language.Should().Be("en-gb");
        }

        [Fact]
        public void Parse_EdgeCases_ItemQuirks()
        {
            var feed = ParseFixture("edge-cases.xml");
            feed.Episodes.Should().HaveCount(4);

            var badDate = feed.Episodes[0];
            badDate.PublishedAt.Should().BeNull();
            badDate.Guid.Should().BeNull();
            badDate.DurationSeconds.Should().Be(754);
            badDate.Description.Should().Be("First line. Second & last line.");
            badDate.EpisodeNumber.Should().BeNull();
            badDate.SeasonNumber.Should().BeNull();

            var video = feed.Episodes[1];
            video.EnclosureType.Should().Be("video/mp4");
            video.EnclosureUrl.Should().Be("https://edge.example.com/video/2.mp4");
            video.PublishedAt.Should().Be(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
            video.DurationSeconds.Should().Be(3600);
            video.ImageUrl.Should().Be("https://edge.example.com/2.jpg");

            var notesFirst = feed.Episodes[2];
            notesFirst.EnclosureUrl.Should().Be("https://edge.example.com/audio/3.m4a");
            notesFirst.PublishedAt.Should().Be(new DateTime(2026, 8, 15, 10, 30, 0, DateTimeKind.Utc));
            notesFirst.DurationSeconds.Should().Be(3723);
            notesFirst.Description.Should().Be("Notes Only in content:encoded.");

            var imageOnly = feed.Episodes[3];
            imageOnly.EnclosureUrl.Should().BeNull();
            imageOnly.PublishedAt.Should().Be(new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc));
            imageOnly.DurationSeconds.Should().BeNull();
        }

        [Fact]
        public void Parse_FeedWithDtd_IsRejected()
        {
            var act = () => ParseFixture("edge-cases-dtd.xml");

            act.Should().Throw<PodcastFeedException>()
                .Which.Reason.Should().Be(PodcastFeedFailureReason.InvalidXml);
        }

        [Fact]
        public void Parse_MalformedXml_IsInvalidXml()
        {
            var act = () => ParseXml("<rss><channel><title>Unclosed</channel>");

            act.Should().Throw<PodcastFeedException>()
                .Which.Reason.Should().Be(PodcastFeedFailureReason.InvalidXml);
        }

        [Theory]
        [InlineData("<html><body>Not a feed</body></html>")]
        [InlineData("<feed xmlns=\"http://www.w3.org/2005/Atom\"><title>Atom</title></feed>")]
        [InlineData("<rss version=\"2.0\"></rss>")]
        public void Parse_NonRssDocument_IsNotAFeed(string xml)
        {
            var act = () => ParseXml(xml);

            act.Should().Throw<PodcastFeedException>()
                .Which.Reason.Should().Be(PodcastFeedFailureReason.NotAFeed);
        }

        [Theory]
        [InlineData("Tue, 01 Sep 2026 07:00:00 GMT", "2026-09-01T07:00:00")]
        [InlineData("Tue, 01 Sep 2026 07:00:00 -0000", "2026-09-01T07:00:00")]
        [InlineData("Thu, 10 Sep 2026 16:00:00 -0500", "2026-09-10T21:00:00")]
        [InlineData("Thu, 10 Sep 2026 16:00:00 +05:30", "2026-09-10T10:30:00")]
        [InlineData("Wed, 01 Sep 2026 07:00:00 PDT", "2026-09-01T14:00:00")]
        [InlineData("1 Sep 2026 07:00 +0000", "2026-09-01T07:00:00")]
        [InlineData("2026-09-01T07:00:00Z", "2026-09-01T07:00:00")]
        [InlineData("Tue,  01   Sep 2026 07:00:00 GMT", "2026-09-01T07:00:00")]
        [InlineData("01 Sep 2026", "2026-09-01T00:00:00")]
        public void ParseDate_ReadsCommonFeedFormatsAsUtc(string value, string expectedUtc)
        {
            var parsed = RssPodcastFeedReader.ParseDate(value);

            parsed.Should().Be(DateTime.SpecifyKind(DateTime.Parse(expectedUtc), DateTimeKind.Utc));
            parsed!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("yesterday")]
        [InlineData("32 Foo 2026")]
        public void ParseDate_ReturnsNullForUnreadableValues(string? value)
        {
            RssPodcastFeedReader.ParseDate(value).Should().BeNull();
        }

        [Theory]
        [InlineData("2341", 2341)]
        [InlineData("05:43", 343)]
        [InlineData("1:35:02", 5702)]
        [InlineData(" 36:12 ", 2172)]
        [InlineData("90.6", 91)]
        [InlineData("bogus", null)]
        [InlineData("1:2:3:4", null)]
        [InlineData("-5", null)]
        [InlineData("", null)]
        public void ParseDuration_ReadsSecondsAndClockFormats(string value, int? expected)
        {
            RssPodcastFeedReader.ParseDuration(value).Should().Be(expected);
        }

        #endregion

        #region HTTP rules

        [Fact]
        public async Task ReadAsync_FetchesAndParsesTheFeed()
        {
            _handler.RespondWith(HttpStatusCode.OK, File.ReadAllText(FixturePath("darknet-diaries.xml")), "application/rss+xml");

            var feed = await _reader.ReadAsync(FeedUrl, maxItems: 1);

            feed.Series.Title.Should().Be("Darknet Diaries");
            feed.Episodes.Should().HaveCount(1);
            var request = _handler.Requests.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri.Should().Be(new Uri(FeedUrl));
        }

        [Theory]
        [InlineData("ftp://feeds.example.com/show.xml")]
        [InlineData("file:///etc/passwd")]
        [InlineData("not a url")]
        [InlineData("")]
        public async Task ReadAsync_RejectsNonHttpUrlsWithoutARequest(string url)
        {
            var act = () => _reader.ReadAsync(url);

            (await act.Should().ThrowAsync<PodcastFeedException>())
                .Which.Reason.Should().Be(PodcastFeedFailureReason.InvalidUrl);
            _handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task ReadAsync_NotFound_ThrowsHttpErrorWithStatus()
        {
            _handler.RespondWith(HttpStatusCode.NotFound, "gone", "text/plain");

            var act = () => _reader.ReadAsync(FeedUrl);

            var exception = (await act.Should().ThrowAsync<PodcastFeedException>()).Which;
            exception.Reason.Should().Be(PodcastFeedFailureReason.HttpError);
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("The feed returned HTTP 404.");
        }

        [Fact]
        public async Task ReadAsync_DeclaredLengthOverCap_ThrowsTooLarge()
        {
            _handler.RespondWith(HttpStatusCode.OK, new byte[RssPodcastFeedReader.MaxFeedBytes + 1], "application/rss+xml");

            var act = () => _reader.ReadAsync(FeedUrl);

            (await act.Should().ThrowAsync<PodcastFeedException>())
                .Which.Reason.Should().Be(PodcastFeedFailureReason.TooLarge);
        }

        [Fact]
        public async Task ReadAsync_UndeclaredLengthOverCap_StopsReadingAndThrowsTooLarge()
        {
            var body = new UnseekableStream(new MemoryStream(new byte[RssPodcastFeedReader.MaxFeedBytes + 100]));
            _handler.RespondWith(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) });

            var act = () => _reader.ReadAsync(FeedUrl);

            (await act.Should().ThrowAsync<PodcastFeedException>())
                .Which.Reason.Should().Be(PodcastFeedFailureReason.TooLarge);
            body.BytesRead.Should().BeLessThanOrEqualTo(RssPodcastFeedReader.MaxFeedBytes + 81920);
        }

        [Fact]
        public async Task ReadAsync_SlowServer_ThrowsTimeout()
        {
            _httpClient.Timeout = TimeSpan.FromMilliseconds(100);
            _handler.OnSend = async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            var act = () => _reader.ReadAsync(FeedUrl);

            (await act.Should().ThrowAsync<PodcastFeedException>())
                .Which.Reason.Should().Be(PodcastFeedFailureReason.Timeout);
        }

        [Fact]
        public async Task ReadAsync_ConnectionFailure_ThrowsUnreachable()
        {
            _handler.OnSend = (_, _) => throw new HttpRequestException("No such host is known.");

            var act = () => _reader.ReadAsync(FeedUrl);

            var exception = (await act.Should().ThrowAsync<PodcastFeedException>()).Which;
            exception.Reason.Should().Be(PodcastFeedFailureReason.Unreachable);
            exception.Message.Should().NotContain("No such host");
        }

        [Fact]
        public async Task ReadAsync_CallerCancellation_IsNotReportedAsAFeedFailure()
        {
            using var cancellation = new CancellationTokenSource();
            _handler.OnSend = async (_, ct) =>
            {
                cancellation.Cancel();
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            var act = () => _reader.ReadAsync(FeedUrl, cancellationToken: cancellation.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        /// <summary>A stream with no length, so the response carries no Content-Length.</summary>
        private sealed class UnseekableStream : Stream
        {
            private readonly Stream _inner;
            public UnseekableStream(Stream inner) => _inner = inner;
            public long BytesRead { get; private set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var read = _inner.Read(buffer, offset, count);
                BytesRead += read;
                return read;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
