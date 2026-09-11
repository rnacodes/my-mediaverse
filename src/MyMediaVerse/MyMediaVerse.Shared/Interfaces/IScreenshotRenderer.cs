namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// A rendered screenshot: raw image bytes plus the content type they were served with.
    /// </summary>
    public record RenderedScreenshot(byte[] Bytes, string ContentType);

    /// <summary>
    /// Produces a screenshot image for a URL. One implementation per provider; the provider in
    /// use is chosen by configuration. Returns null for anything that is not a usable still image
    /// (placeholder frames, error responses, timeouts) and never throws for a bad page.
    /// </summary>
    public interface IScreenshotRenderer
    {
        Task<RenderedScreenshot?> RenderAsync(string url, CancellationToken cancellationToken = default);
    }
}
