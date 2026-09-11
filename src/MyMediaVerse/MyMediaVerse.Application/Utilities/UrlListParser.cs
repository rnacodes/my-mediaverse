using System.Text.RegularExpressions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Turns pasted text into bookmarks: one URL per line, but tolerant of the ways people
    /// paste lists (bullets, commas, quotes, several per line). Bare domains such as
    /// <c>example.com/page</c> are assumed to be https.
    /// </summary>
    public static class UrlListParser
    {
        private static readonly Regex SeparatorPattern = new(@"[\s,;]+", RegexOptions.Compiled);
        private static readonly char[] Wrapping = { '<', '>', '"', '\'', '(', ')', '[', ']', '-', '*', '•', '–', '—' };

        public static BookmarkParseResult Parse(string? text)
        {
            var bookmarks = new List<ParsedBookmark>();
            var rejected = 0;

            if (string.IsNullOrWhiteSpace(text))
                return new BookmarkParseResult(bookmarks, 0, Array.Empty<string>());

            foreach (var rawToken in SeparatorPattern.Split(text))
            {
                var token = rawToken.Trim(Wrapping).Trim();
                if (token.Length == 0)
                    continue;

                var candidate = token;
                if (!candidate.Contains("://", StringComparison.Ordinal) && LooksLikeHost(candidate))
                {
                    candidate = "https://" + candidate;
                }

                if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    && uri.Host.Contains('.'))
                {
                    bookmarks.Add(new ParsedBookmark(candidate, null, null, Array.Empty<string>(), Array.Empty<string>()));
                }
                else
                {
                    rejected++;
                }
            }

            return new BookmarkParseResult(bookmarks, rejected, Array.Empty<string>());
        }

        // "example.com", "www.example.com/path" — a dotted host with no scheme, port, or user info
        // in front of it (so "mailto:x@example.com" is not mistaken for a site).
        private static bool LooksLikeHost(string token)
        {
            var firstSegment = token.Split('/', 2)[0];
            return firstSegment.Contains('.')
                && !firstSegment.StartsWith('.')
                && !firstSegment.EndsWith('.')
                && !firstSegment.Contains(':')
                && !firstSegment.Contains('@');
        }
    }
}
