namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Settings for rendering website screenshots when a page offers no og:image. Bound from
    /// the "WebsiteScreenshots" configuration section; every value has a code default so no
    /// settings file entry is required. Override with environment variables such as
    /// <c>WebsiteScreenshots__Provider=none</c>.
    /// </summary>
    public class WebsiteScreenshotOptions
    {
        public const string SectionName = "WebsiteScreenshots";

        public const string ThumIoProvider = "thumio";
        public const string NoneProvider = "none";

        /// <summary>Which renderer to use: "thumio" (default) or "none" (screenshots disabled).</summary>
        public string Provider { get; set; } = ThumIoProvider;

        /// <summary>Viewport width requested from the renderer, in pixels.</summary>
        public int Width { get; set; } = 1280;

        /// <summary>
        /// Renders allowed per calendar month before the service stops rendering. Sits under
        /// thum.io's free tier so a bulk import cannot exhaust it mid-run.
        /// </summary>
        public int MonthlyCap { get; set; } = 900;

        /// <summary>Optional thum.io auth key for a paid plan; omitted from the URL when empty.</summary>
        public string? ThumIoAuthKey { get; set; }

        /// <summary>
        /// Smallest body accepted as a real screenshot. Placeholder images and error pages come
        /// back far below this.
        /// </summary>
        public int MinBytes { get; set; } = 4096;
    }
}
