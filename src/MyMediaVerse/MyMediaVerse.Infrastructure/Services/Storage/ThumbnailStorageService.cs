using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Storage
{
    // S3-backed implementation of IThumbnailStorageService. Reads bucket/endpoint
    // from the "DigitalOceanSpaces" configuration section. When the S3 client is
    // unregistered (config missing), upload falls back to the original URL and
    // delete is a no-op, matching the pre-extraction behavior of the duplicated
    // helpers this service replaced.
    public class ThumbnailStorageService : IThumbnailStorageService
    {
        private readonly IAmazonS3? _s3Client;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ThumbnailStorageService> _logger;

        public ThumbnailStorageService(
            IAmazonS3? s3Client,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<ThumbnailStorageService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string?> UploadFromUrlAsync(string? imageUrl, string keyPrefix)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return imageUrl;
            }

            if (_s3Client == null)
            {
                _logger.LogWarning("S3 client is not configured; returning original image URL {ImageUrl}", imageUrl);
                return imageUrl;
            }

            try
            {
                var (bucketName, endpoint) = GetSpacesConfig();
                if (bucketName == null || endpoint == null)
                {
                    _logger.LogWarning("DigitalOcean Spaces configuration incomplete; keeping original image URL {ImageUrl}", imageUrl);
                    return imageUrl;
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyMediaVerse/1.0");

                using var response = await httpClient.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image from {ImageUrl}: {StatusCode}", imageUrl, response.StatusCode);
                    return imageUrl;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                using var imageStream = await response.Content.ReadAsStreamAsync();

                var result = await PutObjectAsync(_s3Client, bucketName, endpoint, imageStream, contentType, keyPrefix);
                _logger.LogInformation("Uploaded thumbnail to DigitalOcean Spaces: {OriginalUrl} -> {PublicUrl}", imageUrl, result.PublicUrl);
                return result.PublicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading thumbnail from {ImageUrl}; keeping original URL", imageUrl);
                return imageUrl;
            }
        }

        public async Task<ThumbnailUploadResult?> UploadStreamAsync(Stream content, string contentType, string keyPrefix)
        {
            if (_s3Client == null)
            {
                _logger.LogWarning("S3 client is not configured; cannot upload stream under {KeyPrefix}", keyPrefix);
                return null;
            }

            var (bucketName, endpoint) = GetSpacesConfig();
            if (bucketName == null || endpoint == null)
            {
                _logger.LogWarning("DigitalOcean Spaces configuration incomplete; cannot upload stream under {KeyPrefix}", keyPrefix);
                return null;
            }

            var result = await PutObjectAsync(_s3Client, bucketName, endpoint, content, contentType, keyPrefix);
            _logger.LogInformation("Uploaded thumbnail stream to DigitalOcean Spaces: {PublicUrl}", result.PublicUrl);
            return result;
        }

        public async Task DeleteAsync(string? publicUrl)
        {
            if (string.IsNullOrEmpty(publicUrl))
            {
                return;
            }

            if (_s3Client == null)
            {
                _logger.LogWarning("S3 client is not configured; skipping thumbnail deletion for {Url}", publicUrl);
                return;
            }

            try
            {
                var (bucketName, endpoint) = GetSpacesConfig();
                if (bucketName == null || endpoint == null)
                {
                    _logger.LogWarning("DigitalOcean Spaces configuration incomplete; skipping thumbnail deletion for {Url}", publicUrl);
                    return;
                }

                var expectedPrefix = $"https://{bucketName}.{endpoint}/";
                if (!publicUrl.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Thumbnail URL {Url} isn't in this bucket; skipping deletion", publicUrl);
                    return;
                }

                var key = publicUrl.Substring(expectedPrefix.Length);

                await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                });

                _logger.LogInformation("Deleted thumbnail from DigitalOcean Spaces: {Url}", publicUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting thumbnail {Url}; continuing", publicUrl);
            }
        }

        private (string? BucketName, string? Endpoint) GetSpacesConfig()
        {
            var spacesConfig = _configuration.GetSection("DigitalOceanSpaces");
            var bucketName = spacesConfig["BucketName"];
            var endpoint = spacesConfig["Endpoint"];
            return (string.IsNullOrEmpty(bucketName) ? null : bucketName,
                    string.IsNullOrEmpty(endpoint) ? null : endpoint);
        }

        private static async Task<ThumbnailUploadResult> PutObjectAsync(
            IAmazonS3 s3Client,
            string bucketName,
            string endpoint,
            Stream content,
            string contentType,
            string keyPrefix)
        {
            var extension = ExtensionFor(contentType);
            var key = $"{keyPrefix}{Guid.NewGuid()}{extension}";

            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            });

            var publicUrl = $"https://{bucketName}.{endpoint}/{key}";
            return new ThumbnailUploadResult(publicUrl, key);
        }

        private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
    }
}
