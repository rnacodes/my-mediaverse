namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Captures a screenshot of a website and stores it as the site's thumbnail image.
    /// Used when a page offers no og:image of its own.
    /// </summary>
    public interface IWebsiteScreenshotService
    {
        /// <summary>
        /// Renders a screenshot of the URL and uploads it to thumbnail storage.
        /// </summary>
        /// <param name="websiteUrl">The URL of the website to screenshot.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The public URL of the stored screenshot, or null when no usable image could be
        /// produced or stored.
        /// </returns>
        Task<string?> CaptureScreenshotAsync(string websiteUrl, CancellationToken cancellationToken = default);
    }
}
