using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// The single shared strategy for matching a highlight to its source media.
    /// Order: source URL(s) — exact normalized match, then protocol-stripped
    /// suffix match — then title for article highlights, then title+author for
    /// book highlights. All comparisons are case-insensitive. Returns at most
    /// one match (article wins over book, mirroring the two FK columns).
    /// </summary>
    public static class HighlightLinkMatcher
    {
        public sealed class Match
        {
            public Article? Article { get; init; }
            public Book? Book { get; init; }
            public bool HasMatch => Article != null || Book != null;
        }

        public static async Task<Match> ResolveAsync(
            IApplicationDbContext context,
            IEnumerable<string?> candidateUrls,
            string? title,
            string? author,
            string? category)
        {
            var normalizedCategory = category?.ToLowerInvariant();

            var article = await FindArticleByUrlsAsync(context, candidateUrls);

            // Fallback: match articles by exact title when no URL matched
            if (article == null &&
                normalizedCategory == "articles" &&
                !string.IsNullOrEmpty(title))
            {
                var loweredTitle = title.ToLower();
                article = await context.Articles
                    .FirstOrDefaultAsync(a => a.Title.ToLower() == loweredTitle);
            }

            if (article != null)
            {
                return new Match { Article = article };
            }

            // Book highlights match on exact title + author
            if (normalizedCategory == "books" &&
                !string.IsNullOrEmpty(title) &&
                !string.IsNullOrEmpty(author))
            {
                var loweredTitle = title.ToLower();
                var loweredAuthor = author.ToLower();
                var book = await context.Books
                    .FirstOrDefaultAsync(b =>
                        b.Title.ToLower() == loweredTitle &&
                        b.Author != null && b.Author.ToLower() == loweredAuthor);
                if (book != null)
                {
                    return new Match { Book = book };
                }
            }

            return new Match();
        }

        private static async Task<Article?> FindArticleByUrlsAsync(
            IApplicationDbContext context,
            IEnumerable<string?> candidateUrls)
        {
            foreach (var url in candidateUrls.Where(u => !string.IsNullOrEmpty(u)).Distinct())
            {
                // Normalize already lowercases; lower again defensively so the
                // comparisons below stay case-insensitive regardless.
                var normalizedUrl = UrlNormalizer.Normalize(url).ToLower();
                if (normalizedUrl.Length == 0)
                {
                    continue;
                }

                var article = await context.Articles
                    .FirstOrDefaultAsync(a => a.Link != null && a.Link.ToLower() == normalizedUrl);

                // Fall back to a protocol-agnostic suffix match so http/https
                // (and stored-with-www) variants of the same page still hit
                if (article == null)
                {
                    var urlWithoutProtocol = normalizedUrl
                        .Replace("https://", "")
                        .Replace("http://", "");
                    article = await context.Articles
                        .FirstOrDefaultAsync(a => a.Link != null &&
                            (a.Link.ToLower().EndsWith(urlWithoutProtocol) ||
                             a.Link.ToLower().EndsWith(urlWithoutProtocol + "/")));
                }

                if (article != null)
                {
                    return article;
                }
            }

            return null;
        }
    }
}
