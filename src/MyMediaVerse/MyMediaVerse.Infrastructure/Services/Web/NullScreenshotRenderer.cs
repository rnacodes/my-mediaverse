using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Web
{
    /// <summary>
    /// The renderer used when screenshots are switched off (provider "none"): never renders,
    /// so websites without an og:image simply keep no thumbnail.
    /// </summary>
    public class NullScreenshotRenderer : IScreenshotRenderer
    {
        public Task<RenderedScreenshot?> RenderAsync(string url, CancellationToken cancellationToken = default) =>
            Task.FromResult<RenderedScreenshot?>(null);
    }
}
