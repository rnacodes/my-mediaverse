using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Infrastructure.Services.Web;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class ThumIoScreenshotRendererTests
    {
        private const string PageUrl = "https://example.com/page";

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0 };
        private static readonly byte[] GifHeader = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };

        private readonly TestHttpMessageHandler _handler = new();

        private ThumIoScreenshotRenderer CreateRenderer(Action<WebsiteScreenshotOptions>? configure = null)
        {
            var options = new WebsiteScreenshotOptions { MinBytes = 64 };
            configure?.Invoke(options);
            return new ThumIoScreenshotRenderer(
                new HttpClient(_handler),
                Options.Create(options),
                Substitute.For<ILogger<ThumIoScreenshotRenderer>>());
        }

        private static byte[] Image(byte[] header, int totalLength)
        {
            var bytes = new byte[totalLength];
            Array.Copy(header, bytes, header.Length);
            return bytes;
        }

        [Fact]
        public void BuildRequestUrl_AsksForTheFinalPngFrameAtTheConfiguredWidth()
        {
            var renderer = CreateRenderer(o => o.Width = 1024);

            renderer.BuildRequestUrl(PageUrl)
                .Should().Be("https://image.thum.io/get/noanimate/png/width/1024/https://example.com/page");
        }

        [Fact]
        public void BuildRequestUrl_IncludesTheAuthSegment_WhenAKeyIsConfigured()
        {
            var renderer = CreateRenderer(o => o.ThumIoAuthKey = "abc123");

            renderer.BuildRequestUrl(PageUrl)
                .Should().Be("https://image.thum.io/get/auth/abc123/noanimate/png/width/1280/https://example.com/page");
        }

        [Fact]
        public async Task RenderAsync_ReturnsPngBytes_ForAValidPngResponse()
        {
            var png = Image(PngHeader, 500);
            _handler.RespondWith(HttpStatusCode.OK, png, "image/png");

            var result = await CreateRenderer().RenderAsync(PageUrl);

            result.Should().NotBeNull();
            result!.ContentType.Should().Be("image/png");
            result.Bytes.Should().Equal(png);
            _handler.Requests.Should().ContainSingle()
                .Which.RequestUri!.ToString().Should().StartWith("https://image.thum.io/get/noanimate/png/");
        }

        [Fact]
        public async Task RenderAsync_AcceptsJpeg()
        {
            _handler.RespondWith(HttpStatusCode.OK, Image(JpegHeader, 500), "image/jpeg");

            var result = await CreateRenderer().RenderAsync(PageUrl);

            result!.ContentType.Should().Be("image/jpeg");
        }

        [Fact]
        public async Task RenderAsync_RejectsTheAnimatedGifPlaceholder_EvenWhenLabeledPng()
        {
            // The RAS-153 failure: thum.io streamed its loading GIF and it was saved as the thumbnail.
            _handler.RespondWith(HttpStatusCode.OK, Image(GifHeader, 800_000), "image/png");

            (await CreateRenderer().RenderAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task RenderAsync_RejectsAGifContentType()
        {
            _handler.RespondWith(HttpStatusCode.OK, Image(GifHeader, 500), "image/gif");

            (await CreateRenderer().RenderAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task RenderAsync_RejectsPngBytesServedWithTheWrongContentType()
        {
            _handler.RespondWith(HttpStatusCode.OK, Image(PngHeader, 500), "text/html");

            (await CreateRenderer().RenderAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task RenderAsync_RejectsABodySmallerThanTheFloor()
        {
            _handler.RespondWith(HttpStatusCode.OK, Image(PngHeader, 32), "image/png");

            (await CreateRenderer().RenderAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task RenderAsync_RetriesOnceOnServerError_ThenSucceeds()
        {
            var png = Image(PngHeader, 500);
            _handler.RespondInSequence(
                () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(png) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") } }
                });

            var result = await CreateRenderer().RenderAsync(PageUrl);

            result.Should().NotBeNull();
            _handler.Requests.Should().HaveCount(2);
        }

        [Fact]
        public async Task RenderAsync_GivesUpAfterTheSecondServerError()
        {
            _handler.RespondInSequence(
                () => new HttpResponseMessage(HttpStatusCode.BadGateway),
                () => new HttpResponseMessage(HttpStatusCode.BadGateway));

            (await CreateRenderer().RenderAsync(PageUrl)).Should().BeNull();
            _handler.Requests.Should().HaveCount(2);
        }

        [Fact]
        public async Task RenderAsync_DoesNotRetryAClientError()
        {
            _handler.RespondWith(new HttpResponseMessage(HttpStatusCode.NotFound));

            (await CreateRenderer().RenderAsync(PageUrl)).Should().BeNull();
            _handler.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task RenderAsync_ReturnsNull_WhenTheRequestKeepsFailing()
        {
            _handler.OnSend = (_, _) => throw new HttpRequestException("connection reset");

            var act = () => CreateRenderer().RenderAsync(PageUrl);

            (await act()).Should().BeNull();
            _handler.Requests.Should().HaveCount(2, "one retry is allowed");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RenderAsync_ReturnsNull_ForABlankUrl(string? url)
        {
            (await CreateRenderer().RenderAsync(url!)).Should().BeNull();
            _handler.Requests.Should().BeEmpty();
        }
    }
}
