using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Services.Web;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class WebsiteScreenshotServiceTests
    {
        private const string PageUrl = "https://example.com/page";
        private static readonly RenderedScreenshot Png = new(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 }, "image/png");

        private readonly IScreenshotRenderer _renderer = Substitute.For<IScreenshotRenderer>();
        private readonly IScreenshotQuota _quota = Substitute.For<IScreenshotQuota>();
        private readonly IThumbnailStorageService _storage = Substitute.For<IThumbnailStorageService>();
        private readonly WebsiteScreenshotService _service;

        public WebsiteScreenshotServiceTests()
        {
            _quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(10);
            _quota.TryReserveAsync(Arg.Any<CancellationToken>()).Returns(true);
            _service = new WebsiteScreenshotService(_renderer, _quota, _storage, Substitute.For<ILogger<WebsiteScreenshotService>>());
        }

        [Fact]
        public async Task CaptureScreenshotAsync_StoresTheRenderedImage_AndReturnsItsPublicUrl()
        {
            _renderer.RenderAsync(PageUrl, Arg.Any<CancellationToken>()).Returns(Png);
            _storage.UploadStreamAsync(Arg.Any<Stream>(), "image/png", "screenshots")
                .Returns(new ThumbnailUploadResult("https://bucket.example/screenshots/abc.png", "screenshots/abc.png"));

            var result = await _service.CaptureScreenshotAsync(PageUrl);

            result.Should().Be("https://bucket.example/screenshots/abc.png");
            await _quota.Received(1).TryReserveAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CaptureScreenshotAsync_ReturnsNull_WhenTheRendererProducesNothing()
        {
            _renderer.RenderAsync(PageUrl, Arg.Any<CancellationToken>()).Returns((RenderedScreenshot?)null);

            var result = await _service.CaptureScreenshotAsync(PageUrl);

            result.Should().BeNull();
            await _storage.DidNotReceiveWithAnyArgs().UploadStreamAsync(default!, default!, default!);
            await _quota.DidNotReceive().TryReserveAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CaptureScreenshotAsync_ReturnsNull_WhenStorageIsUnavailable()
        {
            // The old service handed back the render provider's own URL here; that must never happen.
            _renderer.RenderAsync(PageUrl, Arg.Any<CancellationToken>()).Returns(Png);
            _storage.UploadStreamAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((ThumbnailUploadResult?)null);

            var result = await _service.CaptureScreenshotAsync(PageUrl);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CaptureScreenshotAsync_SkipsRendering_WhenTheMonthlyQuotaIsExhausted()
        {
            _quota.RemainingAsync(Arg.Any<CancellationToken>()).Returns(0);

            var result = await _service.CaptureScreenshotAsync(PageUrl);

            result.Should().BeNull();
            await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        }

        [Fact]
        public async Task CaptureScreenshotAsync_ReservesQuotaOnlyAfterASuccessfulRender()
        {
            _renderer.RenderAsync(PageUrl, Arg.Any<CancellationToken>()).Returns(Png);
            _quota.TryReserveAsync(Arg.Any<CancellationToken>()).Returns(false);

            var result = await _service.CaptureScreenshotAsync(PageUrl);

            result.Should().BeNull("the cap was reached between the check and the reservation");
            await _storage.DidNotReceiveWithAnyArgs().UploadStreamAsync(default!, default!, default!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("  ")]
        public async Task CaptureScreenshotAsync_ReturnsNull_ForABlankUrl(string? url)
        {
            (await _service.CaptureScreenshotAsync(url!)).Should().BeNull();
            await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
        }
    }
}
