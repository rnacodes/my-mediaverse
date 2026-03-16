using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Service for capturing screenshots of websites using thum.io (free tier: 500/month).
    /// Screenshots are downloaded and stored in DigitalOcean Spaces for persistence.
    /// </summary>
    public class WebsiteScreenshotService : IWebsiteScreenshotService
    {
        private readonly ILogger<WebsiteScreenshotService> _logger;
        private readonly IAmazonS3? _s3Client;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // thum.io URL format - free tier, no API key required
        private const string ThumIoBaseUrl = "https://image.thum.io/get/width/1280";

        public WebsiteScreenshotService(
            ILogger<WebsiteScreenshotService> logger,
            IAmazonS3? s3Client,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _s3Client = s3Client;
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <inheritdoc />
        public string GetScreenshotPreviewUrl(string websiteUrl)
        {
            if (string.IsNullOrEmpty(websiteUrl))
            {
                throw new ArgumentNullException(nameof(websiteUrl));
            }

            // thum.io expects the URL to be passed directly after the base
            return $"{ThumIoBaseUrl}/{websiteUrl}";
        }

        /// <inheritdoc />
        public async Task<string?> CaptureScreenshotAsync(string websiteUrl)
        {
            if (string.IsNullOrEmpty(websiteUrl))
            {
                _logger.LogWarning("Cannot capture screenshot: URL is empty");
                return null;
            }

            // Check if S3 client is configured
            if (_s3Client == null)
            {
                _logger.LogWarning("S3 client not configured, returning thum.io preview URL instead");
                return GetScreenshotPreviewUrl(websiteUrl);
            }

            try
            {
                var thumUrl = GetScreenshotPreviewUrl(websiteUrl);
                _logger.LogInformation("Fetching screenshot from thum.io: {ThumUrl}", thumUrl);

                // Download the screenshot from thum.io
                using var response = await _httpClient.GetAsync(thumUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("thum.io returned non-success status: {StatusCode}. Returning preview URL.", response.StatusCode);
                    // Return the thum.io URL directly as a fallback - it can still be used as an image source
                    return thumUrl;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

                // Get file extension from content type
                var extension = contentType.ToLower() switch
                {
                    "image/jpeg" => ".jpg",
                    "image/jpg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".png"
                };

                // Get DigitalOcean Spaces configuration
                var spacesConfig = _configuration.GetSection("DigitalOceanSpaces");
                var bucketName = spacesConfig["BucketName"];
                var endpoint = spacesConfig["Endpoint"];

                if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(endpoint))
                {
                    _logger.LogWarning("DigitalOcean Spaces configuration incomplete, returning thum.io preview URL");
                    return thumUrl;
                }

                // Generate a unique file name
                var uniqueFileName = $"screenshots/{Guid.NewGuid()}{extension}";

                // Upload to DigitalOcean Spaces
                using var imageStream = await response.Content.ReadAsStreamAsync();

                var uploadRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = uniqueFileName,
                    InputStream = imageStream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                await _s3Client.PutObjectAsync(uploadRequest);

                // Construct the public URL
                var publicUrl = $"https://{bucketName}.{endpoint}/{uniqueFileName}";

                _logger.LogInformation("Successfully uploaded website screenshot to DigitalOcean Spaces: {PublicUrl}", publicUrl);

                return publicUrl;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Screenshot capture timed out for URL: {Url}. Returning preview URL.", websiteUrl);
                return GetScreenshotPreviewUrl(websiteUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Error downloading screenshot from thum.io for URL: {Url}. Returning preview URL.", websiteUrl);
                return GetScreenshotPreviewUrl(websiteUrl);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error uploading screenshot to DigitalOcean Spaces. Returning preview URL.");
                return GetScreenshotPreviewUrl(websiteUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during screenshot capture for URL: {Url}", websiteUrl);
                return null;
            }
        }
    }
}
