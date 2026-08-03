using System;
using System.Linq;
using System.Web;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Utility class for normalizing URLs to enable consistent comparison across different sources.
    /// Helps prevent duplicate articles from Instapaper, Readwise Reader, and other sources.
    /// </summary>
    public static class UrlNormalizer
    {
        /// <summary>
        /// Normalizes a URL for comparison and storage.
        /// Transformations applied:
        /// - Converts to lowercase
        /// - Removes trailing slash
        /// - Removes URL fragments (#section)
        /// - Removes common tracking parameters (utm_*, fbclid, gclid, etc.)
        /// - Removes www. prefix
        /// The scheme (http/https) is preserved because normalized URLs are stored
        /// and used as links; use <see cref="GetComparisonKey"/> or
        /// <see cref="AreEquivalent"/> for scheme-insensitive comparison.
        /// </summary>
        /// <param name="url">The URL to normalize</param>
        /// <returns>Normalized URL string, or empty string if invalid</returns>
        public static string Normalize(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url.Trim());

                // Build a new URI without fragment
                var builder = new UriBuilder(uri)
                {
                    Fragment = string.Empty
                };

                // Remove tracking parameters
                if (!string.IsNullOrEmpty(builder.Query))
                {
                    var query = HttpUtility.ParseQueryString(builder.Query);
                    var trackingParams = new[]
                    {
                        // Google Analytics & UTM parameters
                        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
                        // Facebook & Social Media
                        "fbclid", "fb_action_ids", "fb_action_types", "fb_source", "fb_ref",
                        // Google Ads
                        "gclid", "gclsrc",
                        // Other common tracking
                        "mc_cid", "mc_eid", // Mailchimp
                        "_ga", // Google Analytics
                        "ref", "referer", "referrer", // Generic referrer params
                        "source", // Generic source param
                        "icid", // Internal campaign ID
                        "s_kwcid", // Adobe/Omniture
                        "msclkid" // Microsoft/Bing
                    };

                    foreach (var param in trackingParams)
                    {
                        query.Remove(param);
                    }

                    builder.Query = query.Count > 0 ? query.ToString() : string.Empty;
                }

                // Convert to lowercase and remove trailing slash
                var normalized = builder.Uri.ToString().ToLowerInvariant();
                normalized = normalized.TrimEnd('/');

                // Remove www. prefix for consistent matching
                normalized = RemoveWwwPrefix(normalized);

                return normalized;
            }
            catch (UriFormatException)
            {
                // If URL parsing fails, just do basic normalization
                return url.Trim().ToLowerInvariant().TrimEnd('/');
            }
        }

        /// <summary>
        /// Removes www. prefix from a URL string for consistent matching.
        /// </summary>
        private static string RemoveWwwPrefix(string url)
        {
            // Handle both http:// and https://
            if (url.StartsWith("https://www."))
                return "https://" + url.Substring(12);
            if (url.StartsWith("http://www."))
                return "http://" + url.Substring(11);
            return url;
        }

        /// <summary>
        /// Produces a scheme-insensitive key for duplicate detection.
        /// Applies the same transformations as <see cref="Normalize"/>, then strips
        /// the http/https prefix so the same page reached over either protocol
        /// produces the same key. Not suitable for storage or display as a link.
        /// </summary>
        /// <param name="url">The URL to build a comparison key for</param>
        /// <returns>Comparison key, or empty string if invalid</returns>
        public static string GetComparisonKey(string? url)
        {
            var normalized = Normalize(url);

            if (normalized.StartsWith("https://"))
                return normalized.Substring(8);
            if (normalized.StartsWith("http://"))
                return normalized.Substring(7);

            return normalized;
        }

        /// <summary>
        /// Checks if two URLs are equivalent after normalization.
        /// Scheme-insensitive: http and https versions of the same URL are equivalent.
        /// Useful for deduplication logic.
        /// </summary>
        /// <param name="url1">First URL to compare</param>
        /// <param name="url2">Second URL to compare</param>
        /// <returns>True if URLs are equivalent, false otherwise</returns>
        public static bool AreEquivalent(string? url1, string? url2)
        {
            // Handle null/empty cases
            if (string.IsNullOrWhiteSpace(url1) && string.IsNullOrWhiteSpace(url2))
                return true;

            if (string.IsNullOrWhiteSpace(url1) || string.IsNullOrWhiteSpace(url2))
                return false;

            return GetComparisonKey(url1) == GetComparisonKey(url2);
        }

        /// <summary>
        /// Extracts the domain from a URL for grouping or display purposes.
        /// </summary>
        /// <param name="url">The URL to extract domain from</param>
        /// <returns>Domain name (e.g., "example.com") or empty string if invalid</returns>
        public static string ExtractDomain(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url.Trim());
                var host = uri.Host.ToLowerInvariant();

                // Remove www. prefix if present
                if (host.StartsWith("www."))
                    host = host.Substring(4);

                return host;
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if a URL is valid and can be normalized.
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if URL is valid, false otherwise</returns>
        public static bool IsValid(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                var uri = new Uri(url.Trim());
                return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }
}

