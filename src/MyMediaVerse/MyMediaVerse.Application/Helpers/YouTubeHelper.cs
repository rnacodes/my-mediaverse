namespace MyMediaVerse.Application.Helpers
{
    public static class YouTubeHelper
    {
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
