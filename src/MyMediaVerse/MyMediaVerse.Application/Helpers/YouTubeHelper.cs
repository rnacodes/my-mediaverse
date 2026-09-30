using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Helpers
{
    public static class YouTubeHelper
    {
        // Hosts YouTube serves video, channel and playlist images from.
        private static readonly string[] YouTubeImageHosts = { "ytimg.com", "ggpht.com", "googleusercontent.com" };

        /// <summary>
        /// The largest thumbnail YouTube offers, or null when it offers none
        /// </summary>
        public static string? GetBestThumbnailUrl(YouTubeThumbnailsDto? thumbnails)
        {
            if (thumbnails == null)
                return null;

            // Prefer higher quality thumbnails
            return thumbnails.Maxres?.Url ??
                   thumbnails.Standard?.Url ??
                   thumbnails.High?.Url ??
                   thumbnails.Medium?.Url ??
                   thumbnails.Default?.Url;
        }

        /// <summary>
        /// Whether an image URL points at one of YouTube's image hosts. Any other URL was
        /// chosen by hand or uploaded.
        /// </summary>
        public static bool IsYouTubeImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                return false;

            return YouTubeImageHosts.Any(host =>
                uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Whether a value has the shape of a YouTube video id: exactly 11 characters drawn
        /// from letters, digits, '-' and '_'
        /// </summary>
        public static bool IsValidVideoId(string? videoId)
        {
            return !string.IsNullOrEmpty(videoId)
                && System.Text.RegularExpressions.Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$");
        }

        /// <summary>
        /// Extract video ID from various YouTube URL formats
        /// </summary>
        public static string? ExtractVideoIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Handle different YouTube URL formats
            var patterns = new[]
            {
                @"(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/(?:shorts|live)\/([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/v\/([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/watch\?.*v=([a-zA-Z0-9_-]{11})"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            // If it's already just a video ID
            if (System.Text.RegularExpressions.Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{11}$"))
                return url;

            return null;
        }

        /// <summary>
        /// Extract playlist ID from YouTube URL
        /// </summary>
        public static string? ExtractPlaylistIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(url, @"[?&]list=([a-zA-Z0-9_-]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Extract channel ID from YouTube URL
        /// </summary>
        public static string? ExtractChannelIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var patterns = new[]
            {
                @"youtube\.com\/channel\/([a-zA-Z0-9_-]+)",
                @"youtube\.com\/c\/([a-zA-Z0-9_-]+)",
                @"youtube\.com\/user\/([a-zA-Z0-9_-]+)",
                @"youtube\.com\/@([a-zA-Z0-9_.-]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// Parse ISO 8601 duration format (PT4M13S) to seconds
        /// </summary>
        public static int ParseDurationToSeconds(string? duration)
        {
            if (string.IsNullOrEmpty(duration))
                return 0;

            try
            {
                var timeSpan = System.Xml.XmlConvert.ToTimeSpan(duration);
                return (int)timeSpan.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
