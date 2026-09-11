using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// The single lookup every website-creating path uses to decide whether an incoming
    /// URL is already in the library. A website's identity is its normalized URL: the
    /// stored <see cref="Website.UrlKey"/> is probed first, then (for rows saved before the
    /// key column existed) the stored Link in each scheme variant.
    /// </summary>
    public static class WebsiteDuplicateFinder
    {
        /// <summary>
        /// Finds an existing website for the URL, or null. Pass a query with any Includes
        /// the caller needs on the returned entity.
        /// </summary>
        public static async Task<Website?> FindExistingAsync(IQueryable<Website> websites, string? url)
        {
            var key = UrlNormalizer.GetComparisonKey(url);
            if (string.IsNullOrEmpty(key))
                return null;

            var byKey = await websites.FirstOrDefaultAsync(w => w.UrlKey == key);
            if (byKey != null)
                return byKey;

            // Legacy rows: stored links keep their scheme, so probe both scheme variants plus the
            // bare key (the fallback shape produced when a URL cannot be parsed). The ToLower()
            // guards rows that were stored raw before links were normalized.
            var httpsVariant = "https://" + key;
            var httpVariant = "http://" + key;

            return await websites.FirstOrDefaultAsync(w =>
                w.UrlKey == null && w.Link != null &&
                (w.Link.ToLower() == httpsVariant ||
                 w.Link.ToLower() == httpVariant ||
                 w.Link.ToLower() == key));
        }

        /// <summary>
        /// Sets <see cref="Website.UrlKey"/> and <see cref="Website.Domain"/> from the stored
        /// link when they are missing. Returns true when anything changed.
        /// </summary>
        public static bool FillIdentity(Website website)
        {
            var changed = false;

            if (string.IsNullOrEmpty(website.UrlKey))
            {
                var key = UrlNormalizer.GetComparisonKey(website.Link);
                if (!string.IsNullOrEmpty(key))
                {
                    website.UrlKey = key;
                    changed = true;
                }
            }

            if (string.IsNullOrEmpty(website.Domain))
            {
                var domain = UrlNormalizer.ExtractDomain(website.Link);
                if (!string.IsNullOrEmpty(domain))
                {
                    website.Domain = domain;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Fill-only merge of an incoming website's metadata onto the existing row, so a
        /// second save of the same URL enriches rather than duplicates. Values already present
        /// are never overwritten and the title is never touched; topics and genres are added
        /// when the existing row lacks them. Returns true when anything changed.
        /// </summary>
        public static bool AbsorbMetadata(Website existing, Website incoming)
        {
            var changed = FillIdentity(existing);

            if (string.IsNullOrWhiteSpace(existing.Description) && !string.IsNullOrWhiteSpace(incoming.Description))
            {
                existing.Description = incoming.Description;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Thumbnail) && !string.IsNullOrWhiteSpace(incoming.Thumbnail))
            {
                existing.Thumbnail = incoming.Thumbnail;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.RssFeedUrl) && !string.IsNullOrWhiteSpace(incoming.RssFeedUrl))
            {
                existing.RssFeedUrl = incoming.RssFeedUrl;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Author) && !string.IsNullOrWhiteSpace(incoming.Author))
            {
                existing.Author = incoming.Author;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Publication) && !string.IsNullOrWhiteSpace(incoming.Publication))
            {
                existing.Publication = incoming.Publication;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Notes) && !string.IsNullOrWhiteSpace(incoming.Notes))
            {
                existing.Notes = incoming.Notes;
                changed = true;
            }

            foreach (var topic in incoming.Topics)
            {
                if (existing.Topics.All(t => t.Name != topic.Name))
                {
                    existing.Topics.Add(topic);
                    changed = true;
                }
            }

            foreach (var genre in incoming.Genres)
            {
                if (existing.Genres.All(g => g.Name != genre.Name))
                {
                    existing.Genres.Add(genre);
                    changed = true;
                }
            }

            return changed;
        }
    }
}
