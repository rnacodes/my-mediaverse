namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Outcome of POST api/website/{id}/screenshot. Follows the sync/import reporting contract:
    /// <see cref="Success"/> is false only when the operation itself failed; "no usable image"
    /// and "already has a thumbnail" are reported through <see cref="Rendered"/>,
    /// <see cref="Skipped"/> and <see cref="WarningMessage"/>.
    /// </summary>
    public class WebsiteScreenshotResultDto
    {
        public bool Success { get; set; } = true;
        public string Operation { get; set; } = "website-screenshot";

        /// <summary>True when the website already had a thumbnail and force was not requested.</summary>
        public bool Skipped { get; set; }

        /// <summary>True when a new screenshot was stored as the thumbnail.</summary>
        public bool Rendered { get; set; }

        /// <summary>The website's thumbnail after the call (new or unchanged).</summary>
        public string? Thumbnail { get; set; }

        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public bool ReindexTriggered { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }
}
