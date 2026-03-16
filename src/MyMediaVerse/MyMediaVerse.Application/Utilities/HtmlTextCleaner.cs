using System.Text.RegularExpressions;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Utility class for cleaning HTML, CSS, and other formatting from text content.
    /// Used to clean highlight text from Readwise that may contain embedded styles.
    /// </summary>
    public static class HtmlTextCleaner
    {
        /// <summary>
        /// Cleans HTML and CSS from text, preserving only the readable content.
        /// </summary>
        /// <param name="text">The text to clean</param>
        /// <returns>Cleaned text with HTML/CSS removed</returns>
        public static string Clean(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var result = text;

            // First, handle the specific MUI/styled-components CSS contamination pattern
            // This pattern appears at the start: ";color:#FFD700;margin-right:0.5rem;}&::after{@media...content:"
            // We need to remove everything up to and including the last "content:" if it looks like CSS
            var muiCssPattern = @"^[;\s]*(?:[a-z\-]+:[^;]+;)*\s*\}?\s*&?::(?:before|after)\s*\{.*?content\s*:\s*";
            result = Regex.Replace(result, muiCssPattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove @media blocks with nested content (iterative to handle nesting)
            // Match @media queries - handle nested braces by matching balanced pairs
            var prevResult = "";
            var iterations = 0;
            while (prevResult != result && iterations < 10)
            {
                prevResult = result;
                // Remove @media blocks (matching innermost braces first, then outer)
                result = Regex.Replace(result, @"@media\s*\([^)]*\)\s*\{[^{}]*\}", " ", RegexOptions.IgnoreCase);
                iterations++;
            }

            // Remove pseudo-elements (e.g., "&::after{...}" or "::before{...}")
            result = Regex.Replace(result, @"&?::(?:before|after|first-child|last-child)\s*\{[^{}]*\}", " ", RegexOptions.IgnoreCase);

            // Remove remaining CSS blocks with properties inside
            result = Regex.Replace(result, @"\{[^{}]*:[^{}]*\}", " ", RegexOptions.IgnoreCase);

            // Remove inline CSS property patterns that are clearly CSS (require colon and semicolon or close brace)
            // Be more specific to avoid matching actual content
            result = Regex.Replace(result, @"(?:^|;)\s*(?:color|font-size|margin-right|margin-left|margin|padding|background|border|display|width|height|content)\s*:\s*[^;{}]+[;}]", " ", RegexOptions.IgnoreCase);

            // Remove any remaining "content:" at the start or after whitespace
            result = Regex.Replace(result, @"(?:^|\s)content\s*:\s*", " ", RegexOptions.IgnoreCase);

            // Remove orphan braces and semicolons at the start
            result = Regex.Replace(result, @"^[\s;{}&]+", "");

            // Remove HTML tags
            result = Regex.Replace(result, @"<[^>]+>", " ", RegexOptions.IgnoreCase);

            // Remove HTML entities
            result = Regex.Replace(result, @"&[a-zA-Z]+;", " ");
            result = Regex.Replace(result, @"&#\d+;", " ");

            // Clean up multiple spaces and trim
            result = Regex.Replace(result, @"\s+", " ");
            result = result.Trim();

            return result;
        }

        /// <summary>
        /// Checks if text appears to contain HTML or CSS that should be cleaned.
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if text contains HTML/CSS patterns</returns>
        public static bool ContainsHtmlOrCss(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Check for common HTML/CSS patterns including MUI CSS contamination
            // Look for: HTML tags, @media queries, pseudo-elements, or common CSS properties with values
            return Regex.IsMatch(text, @"<[a-z]+[^>]*>|</[a-z]+>|@media|&?::(?:after|before)|(?:color|font-size|margin|padding|content)\s*:\s*[^;]+;", RegexOptions.IgnoreCase);
        }
    }
}
