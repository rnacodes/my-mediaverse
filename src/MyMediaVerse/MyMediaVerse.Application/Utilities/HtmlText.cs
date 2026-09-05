using System.Text.RegularExpressions;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Turns HTML fragments from external APIs (Google Books descriptions, feed summaries)
    /// into plain text: tags removed, common entities decoded, whitespace collapsed.
    /// </summary>
    public static class HtmlText
    {
        private static readonly Regex TagPattern = new("<.*?>", RegexOptions.Compiled);
        private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);
        // Replacing an inline tag such as </b> with a space leaves "word ." behind; close that gap.
        private static readonly Regex SpaceBeforePunctuation = new(@"\s+([.,;:!?)\]])", RegexOptions.Compiled);

        /// <summary>
        /// Returns the plain-text form of <paramref name="html"/>, or null when the input is
        /// null, empty, or contains nothing but markup.
        /// </summary>
        public static string? Strip(string? html)
        {
            if (string.IsNullOrEmpty(html)) return null;

            var text = TagPattern.Replace(html, " ");

            text = text
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#39;", "'")
                .Replace("&apos;", "'");

            text = WhitespacePattern.Replace(text, " ");
            text = SpaceBeforePunctuation.Replace(text, "$1").Trim();

            return text.Length == 0 ? null : text;
        }
    }
}
