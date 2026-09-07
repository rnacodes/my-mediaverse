using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Web
{
    /// <summary>
    /// Turns a website URL into a stored thumbnail: checks the monthly budget, asks the
    /// configured renderer for an image, and uploads it to thumbnail storage.
    /// </summary>
    public class WebsiteScreenshotService : IWebsiteScreenshotService
    {
        private const string StorageKeyPrefix = "screenshots";

        private readonly IScreenshotRenderer _renderer;
        private readonly IScreenshotQuota _quota;
        private readonly IThumbnailStorageService _storage;
        private readonly ILogger<WebsiteScreenshotService> _logger;

        public WebsiteScreenshotService(
            IScreenshotRenderer renderer,
            IScreenshotQuota quota,
            IThumbnailStorageService storage,
            ILogger<WebsiteScreenshotService> logger)
        {
            _renderer = renderer;
            _quota = quota;
            _storage = storage;
            _logger = logger;
        }

        public async Task<string?> CaptureScreenshotAsync(string websiteUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(websiteUrl))
            {
                _logger.LogWarning("Cannot capture screenshot: URL is empty");
                return null;
            }

            if (await _quota.RemainingAsync(cancellationToken) <= 0)
            {
                _logger.LogWarning("Screenshot quota for this month is exhausted; skipping {Url}", websiteUrl);
                return null;
            }

            var rendered = await _renderer.RenderAsync(websiteUrl, cancellationToken);
            if (rendered == null)
            {
                return null;
            }

            // A render happened, so it counts — even if storage turns it away below.
            if (!await _quota.TryReserveAsync(cancellationToken))
            {
                _logger.LogWarning("Screenshot quota reached while rendering {Url}; result discarded", websiteUrl);
                return null;
            }

            using var stream = new MemoryStream(rendered.Bytes);
            var upload = await _storage.UploadStreamAsync(stream, rendered.ContentType, StorageKeyPrefix);
            if (upload == null)
            {
                _logger.LogWarning("Screenshot for {Url} was rendered but thumbnail storage is unavailable; no thumbnail stored", websiteUrl);
                return null;
            }

            _logger.LogInformation("Stored website screenshot for {Url} at {PublicUrl}", websiteUrl, upload.PublicUrl);
            return upload.PublicUrl;
        }
    }
}
