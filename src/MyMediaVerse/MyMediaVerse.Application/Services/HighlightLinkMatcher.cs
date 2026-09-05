using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// The single shared strategy for matching a highlight to its source media.
    /// Order: source URL(s) — anchored equality against every stored form of the
    /// normalized comparison key (scheme/www/trailing-slash variants) — then title
    /// for article highlights, then the Readwise book id, then title+author for
    /// book highlights. All comparisons are case-insensitive. Returns at most one
    /// match (article wins over book, mirroring the two FK columns).
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
            string? category,
            int? readwiseBookId = null)
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

            if (readwiseBookId.HasValue)
            {
                var bookById = await context.Books
                    .FirstOrDefaultAsync(b => b.ReadwiseBookId == readwiseBookId.Value);
                if (bookById != null)
                {
                    return new Match { Book = bookById };
                }
            }

            // Book highlights fall back to exact title + author
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
                // The comparison key is scheme-less, www-less, lowercase, no trailing slash.
                var key = UrlNormalizer.GetComparisonKey(url);
                if (key.Length == 0)
                {
                    continue;
                }

                var storedForms = new List<string>();
                foreach (var prefix in new[] { "", "http://", "https://", "http://www.", "https://www." })
                {
                    storedForms.Add(prefix + key);
                    storedForms.Add(prefix + key + "/");
                }

                var article = await context.Articles
                    .FirstOrDefaultAsync(a => a.Link != null && storedForms.Contains(a.Link.ToLower()));

                if (article != null)
                {
                    return article;
                }
            }

            return null;
        }
    }
}
