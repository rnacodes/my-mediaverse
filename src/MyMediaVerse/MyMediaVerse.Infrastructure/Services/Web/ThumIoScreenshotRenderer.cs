using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Web
{
    /// <summary>
    /// Renders screenshots through thum.io. Asks for the final still frame only (thum.io streams
    /// an animated "loading" placeholder by default) and refuses anything that is not a real
    /// PNG or JPEG of plausible size, so a placeholder can never be stored as a thumbnail.
    /// </summary>
    public class ThumIoScreenshotRenderer : IScreenshotRenderer
    {
        private const string BaseUrl = "https://image.thum.io/get/";
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47 };
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] GifSignature = { 0x47, 0x49, 0x46, 0x38 }; // "GIF8"

        private readonly HttpClient _httpClient;
        private readonly WebsiteScreenshotOptions _options;
        private readonly ILogger<ThumIoScreenshotRenderer> _logger;

        public ThumIoScreenshotRenderer(
            HttpClient httpClient,
            IOptions<WebsiteScreenshotOptions> options,
            ILogger<ThumIoScreenshotRenderer> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// The thum.io request URL for a page: optional auth segment, final-frame-only, PNG,
        /// requested width, then the page URL verbatim (thum.io expects it unencoded).
        /// </summary>
        public string BuildRequestUrl(string url)
        {
            var auth = string.IsNullOrWhiteSpace(_options.ThumIoAuthKey)
                ? string.Empty
                : $"auth/{_options.ThumIoAuthKey}/";

            return $"{BaseUrl}{auth}noanimate/png/width/{_options.Width}/{url}";
        }

        public async Task<RenderedScreenshot?> RenderAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var requestUrl = BuildRequestUrl(url);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

                    if (IsRetryable(response.StatusCode) && attempt == 1)
                    {
                        _logger.LogWarning("thum.io returned {StatusCode} for {Url}; retrying once", response.StatusCode, url);
                        await Task.Delay(RetryDelay, cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("thum.io returned {StatusCode} for {Url}; no screenshot", response.StatusCode, url);
                        return null;
                    }

                    var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    return Validate(url, contentType, bytes);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    if (attempt == 1)
                    {
                        _logger.LogWarning(ex, "thum.io request failed for {Url}; retrying once", url);
                        await Task.Delay(RetryDelay, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning(ex, "thum.io request failed for {Url}; no screenshot", url);
                    return null;
                }
            }

            return null;
        }

        private RenderedScreenshot? Validate(string url, string? contentType, byte[] bytes)
        {
            if (StartsWith(bytes, GifSignature))
            {
                _logger.LogWarning("thum.io returned its animated loading placeholder for {Url}; discarded", url);
                return null;
            }

            var isPng = StartsWith(bytes, PngSignature);
            var isJpeg = StartsWith(bytes, JpegSignature);
            if (!isPng && !isJpeg)
            {
                _logger.LogWarning("thum.io response for {Url} is not a PNG or JPEG (content-type {ContentType}); discarded", url, contentType);
                return null;
            }

            if (contentType != "image/png" && contentType != "image/jpeg")
            {
                _logger.LogWarning("thum.io response for {Url} carried content-type {ContentType}; discarded", url, contentType);
                return null;
            }

            if (bytes.Length < _options.MinBytes)
            {
                _logger.LogWarning("thum.io response for {Url} is only {Bytes} bytes; discarded as a placeholder", url, bytes.Length);
                return null;
            }

            return new RenderedScreenshot(bytes, contentType);
        }

        private static bool IsRetryable(HttpStatusCode status) =>
            (int)status >= 500 || status == HttpStatusCode.TooManyRequests;

        private static bool StartsWith(byte[] bytes, byte[] signature) =>
            bytes.Length >= signature.Length && bytes.AsSpan(0, signature.Length).SequenceEqual(signature);
    }
}
