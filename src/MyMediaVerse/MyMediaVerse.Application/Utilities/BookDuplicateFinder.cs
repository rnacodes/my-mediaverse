using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Everything known about an incoming book that can identify an existing row.
    /// Populate whatever the source provides; nulls are skipped during probing.
    /// </summary>
    public record BookIdentity
    {
        public int? ReadwiseBookId { get; init; }
        public long? GoodreadsBookId { get; init; }
        public string? GoogleVolumeId { get; init; }
        public string? OpenLibraryKey { get; init; }
        /// <summary>Raw ISBN as the source provided it; normalized internally.</summary>
        public string? Isbn { get; init; }
        public string? Asin { get; init; }
        public string? Title { get; init; }
        public string? Author { get; init; }
    }

    /// <summary>
    /// The single lookup every book-creating path uses to decide whether an
    /// incoming book already exists. Probes in identity-strength order:
    /// external ids (Readwise, Goodreads, Google Books, Open Library) first,
    /// then normalized ISBN (both 13 and legacy 10 forms), then ASIN, and only
    /// as a last resort case-insensitive title+author equality.
    /// </summary>
    public static class BookDuplicateFinder
    {
        /// <summary>
        /// Finds an existing book matching the identity, or null.
        /// Pass a query with any Includes the caller needs on the returned entity.
        /// Probes run in priority order so a strong-id match always wins over a
        /// weaker one, even when different rows would match different keys.
        /// </summary>
        public static async Task<Book?> FindExistingAsync(IQueryable<Book> books, BookIdentity identity)
        {
            if (identity.ReadwiseBookId.HasValue)
            {
                var match = await books.FirstOrDefaultAsync(b => b.ReadwiseBookId == identity.ReadwiseBookId);
                if (match != null) return match;
            }

            if (identity.GoodreadsBookId.HasValue)
            {
                var match = await books.FirstOrDefaultAsync(b => b.GoodreadsBookId == identity.GoodreadsBookId);
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.GoogleVolumeId))
            {
                var match = await books.FirstOrDefaultAsync(b => b.GoogleVolumeId == identity.GoogleVolumeId);
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.OpenLibraryKey))
            {
                var match = await books.FirstOrDefaultAsync(b => b.OpenLibraryKey == identity.OpenLibraryKey);
                if (match != null) return match;
            }

            var isbnVariants = IsbnNormalizer.GetSearchVariants(identity.Isbn);
            if (isbnVariants.Count > 0)
            {
                var match = await books.FirstOrDefaultAsync(b => b.ISBN != null && isbnVariants.Contains(b.ISBN));
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.Asin))
            {
                var match = await books.FirstOrDefaultAsync(b => b.ASIN == identity.Asin);
                if (match != null) return match;
            }

            if (!string.IsNullOrWhiteSpace(identity.Title) && !string.IsNullOrWhiteSpace(identity.Author))
            {
                var titleLower = identity.Title.Trim().ToLower();
                var authorLower = identity.Author.Trim().ToLower();
                var match = await books.FirstOrDefaultAsync(b =>
                    b.Title.ToLower() == titleLower && b.Author.ToLower() == authorLower);
                if (match != null) return match;
            }

            return null;
        }

        /// <summary>
        /// Fill-only copy of the identity's external ids onto an existing match, so
        /// books acquire ids over time as they're seen by more sources. Never
        /// overwrites a value already present. Returns true when anything changed.
        /// </summary>
        public static bool AbsorbIdentity(Book book, BookIdentity identity)
        {
            var changed = false;

            if (book.ReadwiseBookId == null && identity.ReadwiseBookId.HasValue)
            {
                book.ReadwiseBookId = identity.ReadwiseBookId;
                changed = true;
            }

            if (book.GoodreadsBookId == null && identity.GoodreadsBookId.HasValue)
            {
                book.GoodreadsBookId = identity.GoodreadsBookId;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(book.GoogleVolumeId) && !string.IsNullOrWhiteSpace(identity.GoogleVolumeId))
            {
                book.GoogleVolumeId = identity.GoogleVolumeId;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(book.OpenLibraryKey) && !string.IsNullOrWhiteSpace(identity.OpenLibraryKey))
            {
                book.OpenLibraryKey = identity.OpenLibraryKey;
                changed = true;
            }

            var normalizedIsbn = IsbnNormalizer.Normalize(identity.Isbn);
            if (string.IsNullOrWhiteSpace(book.ISBN) && normalizedIsbn != null)
            {
                book.ISBN = normalizedIsbn;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(book.ASIN) && !string.IsNullOrWhiteSpace(identity.Asin))
            {
                book.ASIN = identity.Asin;
                changed = true;
            }

            return changed;
        }
    }
}
