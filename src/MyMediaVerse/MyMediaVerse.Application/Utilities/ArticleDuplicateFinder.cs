using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// The single lookup every article-creating path uses to decide whether an
    /// incoming article already exists. Matches on the Readwise Reader document id
    /// first, then on the normalized URL regardless of http/https scheme.
    /// </summary>
    public static class ArticleDuplicateFinder
    {
        /// <summary>
        /// Finds an existing article by Reader document id or URL.
        /// Pass a query with any Includes the caller needs on the returned entity.
        /// </summary>
        public static async Task<Article?> FindExistingAsync(
            IQueryable<Article> articles,
            string? readwiseDocumentId,
            string? url)
        {
            var hasDocumentId = !string.IsNullOrWhiteSpace(readwiseDocumentId);
            var comparisonKey = UrlNormalizer.GetComparisonKey(url);
            var hasUrl = !string.IsNullOrEmpty(comparisonKey);

            if (!hasDocumentId && !hasUrl)
                return null;

            // Stored links keep their scheme, so probe both scheme variants plus the bare key
            // (the fallback shape produced when a URL cannot be parsed). Normalized links are
            // already lowercase; the ToLower() guards legacy rows that were stored raw.
            var httpsVariant = "https://" + comparisonKey;
            var httpVariant = "http://" + comparisonKey;

            return await articles.FirstOrDefaultAsync(a =>
                (hasDocumentId && a.ReadwiseDocumentId == readwiseDocumentId) ||
                (hasUrl && a.Link != null &&
                    (a.Link.ToLower() == httpsVariant ||
                     a.Link.ToLower() == httpVariant ||
                     a.Link.ToLower() == comparisonKey)));
        }
    }
}
