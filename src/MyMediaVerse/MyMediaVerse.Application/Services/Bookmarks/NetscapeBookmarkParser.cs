using System.Net;
using System.Text.RegularExpressions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services.Bookmarks
{
    /// <summary>
    /// Reads the Netscape bookmark file format that every browser and most bookmark managers
    /// export (Chrome, Firefox, Edge, Safari, Brave, Vivaldi, Raindrop, Pinboard, linkding).
    /// The format is HTML in name only: <c>&lt;DT&gt;</c> and <c>&lt;p&gt;</c> are never closed and
    /// nesting is expressed by <c>&lt;DL&gt;</c> blocks, so instead of building a tree this scans the
    /// text in order and keeps a folder stack: an <c>&lt;H3&gt;</c> names the next <c>&lt;DL&gt;</c>,
    /// <c>&lt;/DL&gt;</c> leaves it, and each <c>&lt;A&gt;</c> is a bookmark inside whatever folders
    /// are open.
    /// </summary>
    public sealed class NetscapeBookmarkParser : IBookmarkFileParser
    {
        private const string Signature = "NETSCAPE-Bookmark-file";

        // Browsers wrap everything in one or two container folders that carry no meaning as topics.
        private static readonly HashSet<string> RootFolderNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Bookmarks", "Bookmarks bar", "Bookmarks Bar", "Bookmarks Toolbar", "Bookmarks Menu",
            "Other bookmarks", "Other Bookmarks", "Mobile bookmarks", "Mobile Bookmarks", "Menu",
            "Favorites", "Favorites Bar", "Favourites", "Imported", "Unfiled"
        };

        private static readonly Regex TokenPattern = new(
            @"<DL\b[^>]*>|</DL\s*>|<H3\b([^>]*)>(.*?)</H3\s*>|<A\b([^>]*)>(.*?)</A\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex AttributePattern = new(
            @"([A-Za-z_][A-Za-z0-9_\-]*)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))",
            RegexOptions.Compiled);

        private static readonly Regex TagStripPattern = new("<[^>]+>", RegexOptions.Compiled);

        public bool CanParse(string fileName, string head)
        {
            if (!string.IsNullOrEmpty(head) && head.Contains(Signature, StringComparison.OrdinalIgnoreCase))
                return true;

            var extension = Path.GetExtension(fileName ?? string.Empty);
            return extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);
        }

        public BookmarkParseResult Parse(string content)
        {
            var bookmarks = new List<ParsedBookmark>();
            var folders = new List<string>();
            var seenFolders = new HashSet<string>(StringComparer.Ordinal);
            var nonWebLinks = 0;

            if (string.IsNullOrWhiteSpace(content))
                return new BookmarkParseResult(bookmarks, 0, folders);

            // One entry per open <DL>: the folder it belongs to, or null for a container we skip.
            var folderStack = new Stack<string?>();
            string? pendingFolder = null;
            var pendingFolderIsContainer = false;

            foreach (Match token in TokenPattern.Matches(content))
            {
                var text = token.Value;

                if (text.StartsWith("</", StringComparison.Ordinal))
                {
                    if (folderStack.Count > 0) folderStack.Pop();
                    continue;
                }

                if (text.StartsWith("<DL", StringComparison.OrdinalIgnoreCase))
                {
                    folderStack.Push(pendingFolderIsContainer ? null : pendingFolder);
                    pendingFolder = null;
                    pendingFolderIsContainer = false;
                    continue;
                }

                if (text.StartsWith("<H3", StringComparison.OrdinalIgnoreCase))
                {
                    var attributes = ParseAttributes(token.Groups[1].Value);
                    var name = CleanText(token.Groups[2].Value);
                    pendingFolder = name;
                    pendingFolderIsContainer = string.IsNullOrEmpty(name)
                        || IsTrue(attributes, "PERSONAL_TOOLBAR_FOLDER")
                        || (folderStack.Count <= 1 && RootFolderNames.Contains(name));
                    continue;
                }

                // <A ...>title</A>
                var linkAttributes = ParseAttributes(token.Groups[3].Value);
                if (!linkAttributes.TryGetValue("HREF", out var href) || !IsWebLink(href))
                {
                    nonWebLinks++;
                    continue;
                }

                var path = folderStack.Reverse().Where(f => !string.IsNullOrEmpty(f)).Select(f => f!).ToList();
                if (path.Count > 0)
                {
                    var joined = string.Join("/", path);
                    if (seenFolders.Add(joined)) folders.Add(joined);
                }

                var title = CleanText(token.Groups[4].Value);
                bookmarks.Add(new ParsedBookmark(
                    href.Trim(),
                    string.IsNullOrEmpty(title) ? null : title,
                    ParseAddDate(linkAttributes.GetValueOrDefault("ADD_DATE")),
                    path,
                    ParseTags(linkAttributes.GetValueOrDefault("TAGS"))));
            }

            return new BookmarkParseResult(bookmarks, nonWebLinks, folders);
        }

        /// <summary>
        /// Unix timestamp in seconds (the standard), tolerating exports that wrote milliseconds
        /// or microseconds, judged by magnitude. Anything implausible is dropped.
        /// </summary>
        public static DateTime? ParseAddDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !long.TryParse(raw.Trim(), out var value) || value <= 0)
                return null;

            var seconds = value switch
            {
                > 100_000_000_000_000 => value / 1_000_000, // microseconds
                > 100_000_000_000 => value / 1_000,         // milliseconds
                _ => value
            };

            try
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                if (date.Year < 1990 || date > DateTime.UtcNow.AddDays(1))
                    return null;
                return date;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static IReadOnlyList<string> ParseTags(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return WebUtility.HtmlDecode(raw)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Dictionary<string, string> ParseAttributes(string raw)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in AttributePattern.Matches(raw))
            {
                var value = match.Groups[2].Success ? match.Groups[2].Value
                    : match.Groups[3].Success ? match.Groups[3].Value
                    : match.Groups[4].Value;
                attributes[match.Groups[1].Value] = WebUtility.HtmlDecode(value);
            }
            return attributes;
        }

        private static bool IsTrue(Dictionary<string, string> attributes, string name) =>
            attributes.TryGetValue(name, out var value) && value.Equals("true", StringComparison.OrdinalIgnoreCase);

        private static bool IsWebLink(string href) =>
            Uri.TryCreate(href.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static string CleanText(string raw)
        {
            var stripped = TagStripPattern.Replace(raw, string.Empty);
            return WebUtility.HtmlDecode(stripped).Replace(' ', ' ').Trim();
        }
    }
}
