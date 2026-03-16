namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service for capturing screenshots of websites.
    /// Used to generate thumbnail images when no og:image is available.
    /// </summary>
    public interface IWebsiteScreenshotService
    {
        /// <summary>
        /// Captures a screenshot of the given URL and returns the URL where the screenshot is stored.
        /// </summary>
        /// <param name="websiteUrl">The URL of the website to screenshot.</param>
        /// <returns>The URL of the stored screenshot, or null if screenshot capture failed.</returns>
        Task<string?> CaptureScreenshotAsync(string websiteUrl);

        /// <summary>
        /// Generates a thum.io URL for the given website URL without storing it.
        /// Useful for preview purposes.
        /// </summary>
        /// <param name="websiteUrl">The URL of the website.</param>
        /// <returns>The thum.io URL that can be used directly as an image source.</returns>
        string GetScreenshotPreviewUrl(string websiteUrl);
    }
}
