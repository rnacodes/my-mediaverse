using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    public class GoodreadsImportService : IGoodreadsImportService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GoodreadsImportService> _logger;

        // Persist in batches so the unit of work (and the DB round-trips) stay bounded on a large
        // import instead of one giant SaveChanges at the very end.
        private const int BatchSize = 50;

        public GoodreadsImportService(
            IApplicationDbContext context,
            ILogger<GoodreadsImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GoodreadsImportResultDto> ImportFromCsvAsync(Stream csvStream, bool updateExisting = true)
        {
            var result = new GoodreadsImportResultDto();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };

            try
            {
                using var reader = new StreamReader(csvStream);
                using var csv = new CsvReader(reader, config);

                var records = csv.GetRecords<GoodreadsCsvImportDto>().ToList();
                result.TotalProcessed = records.Count;

                _logger.LogInformation("Processing {Count} books from Goodreads CSV", records.Count);

                // Preload existing books once into in-memory dedup dictionaries. The previous
                // implementation ran an ISBN and/or title+author query per record, which is an N+1
                // that times out on a 4000-book export. The books are tracked so a matched row can be
                // updated in place without another round-trip.
                var dedup = await BuildDedupIndexAsync();

                var processedSinceSave = 0;
                foreach (var record in records)
                {
                    try
                    {
                        ProcessBookRecord(record, result, updateExisting, dedup);

                        // Flush every BatchSize records so neither the change tracker nor a single
                        // SaveChanges grows unbounded over a large import.
                        if (++processedSinceSave % BatchSize == 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing '{record.Title}' by {record.Author}: {ex.Message}");
                        _logger.LogError(ex, "Error processing book: {Title} by {Author}", record.Title, record.Author);
                    }
                }

                // Persist the final partial batch.
                await _context.SaveChangesAsync();
                _logger.LogInformation("Goodreads import complete: {Created} created, {Updated} updated, {Errors} errors",
                    result.CreatedCount, result.UpdatedCount, result.ErrorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Goodreads CSV");
                result.Errors.Add($"CSV parsing error: {ex.Message}");
                result.ErrorCount++;
            }

            result.SuccessCount = result.CreatedCount + result.UpdatedCount;
            return result;
        }

        /// <summary>
        /// Loads existing books once and indexes them by cleaned ISBN and normalized title+author so
        /// each CSV record can be deduplicated against an in-memory lookup rather than a DB query.
        /// Books are tracked so matched rows update in place.
        /// </summary>
        private async Task<DedupIndex> BuildDedupIndexAsync()
        {
            var index = new DedupIndex();
            var existing = await _context.Books.ToListAsync();
            foreach (var book in existing)
            {
                index.Add(book);
            }
            return index;
        }

        private void ProcessBookRecord(GoodreadsCsvImportDto record, GoodreadsImportResultDto result, bool updateExisting, DedupIndex dedup)
        {
            if (string.IsNullOrWhiteSpace(record.Title) || string.IsNullOrWhiteSpace(record.Author))
            {
                result.SkippedCount++;
                result.Errors.Add($"Skipped book with missing title or author");
                return;
            }

            // Dedup on ISBN or ISBN13 (a book often carries only the latter), falling back to title+author.
            var cleanIsbn = CleanIsbn(record.ISBN) ?? CleanIsbn(record.ISBN13);
            var existingBook = dedup.Find(cleanIsbn, record.Title, record.Author);

            if (existingBook != null)
            {
                if (updateExisting)
                {
                    UpdateBookFromRecord(existingBook, record);
                    result.UpdatedCount++;
                    result.ImportedBooks.Add(new GoodreadsImportedBookDto
                    {
                        Id = existingBook.Id,
                        Title = existingBook.Title,
                        Author = existingBook.Author,
                        WasUpdated = true,
                        Thumbnail = existingBook.Thumbnail
                    });
                }
                else
                {
                    result.SkippedCount++;
                }
            }
            else
            {
                var newBook = CreateBookFromRecord(record);
                _context.Add(newBook);
                result.CreatedCount++;
                result.ImportedBooks.Add(new GoodreadsImportedBookDto
                {
                    Id = newBook.Id,
                    Title = newBook.Title,
                    Author = newBook.Author,
                    WasUpdated = false,
                    Thumbnail = newBook.Thumbnail
                });
                // Index the new book so a later duplicate in the same file updates it instead of
                // inserting a second copy.
                dedup.Add(newBook);
            }
        }

        public async Task<Book?> FindExistingBookAsync(string? isbn, string title, string author)
        {
            var cleanIsbn = CleanIsbn(isbn);

            // Try ISBN match first (most reliable)
            if (!string.IsNullOrWhiteSpace(cleanIsbn))
            {
                var byIsbn = await _context.Books
                    .FirstOrDefaultAsync(b => b.ISBN != null &&
                        b.ISBN.Replace("-", "").Replace(" ", "") == cleanIsbn);
                if (byIsbn != null)
                {
                    return byIsbn;
                }
            }

            // Fallback to Title+Author match (case-insensitive)
            var normalizedTitle = title.Trim().ToLowerInvariant();
            var normalizedAuthor = author.Trim().ToLowerInvariant();

            return await _context.Books
                .FirstOrDefaultAsync(b =>
                    b.Title.ToLower() == normalizedTitle &&
                    b.Author.ToLower() == normalizedAuthor);
        }

        private Book CreateBookFromRecord(GoodreadsCsvImportDto record)
        {
            var book = new Book
            {
                Title = record.Title.Trim(),
                Author = record.Author.Trim(),
                MediaType = MediaType.Book,
                ISBN = CleanIsbn(record.ISBN) ?? CleanIsbn(record.ISBN13),
                Status = MapShelfToStatus(record.Shelves),
                Format = MapBindingToFormat(record.Binding),
                // Store the raw Goodreads rating only; deriving the MMV Rating enum from it is deferred
                // to the book rating enrichment stage so import stays a dumb/fast raw capture.
                GoodreadsRating = record.MyRating,
                AverageRating = record.AverageRating,
                Publisher = record.Publisher?.Trim(),
                YearPublished = record.YearPublished,
                OriginalPublicationYear = record.OriginalPublicationYear,
                DateRead = ParseDate(record.DateRead),
                DateAdded = ParseDate(record.DateAdded) ?? DateTime.UtcNow,
                MyReview = record.MyReview?.Trim(),
                GoodreadsTags = ParseBookshelves(record.Bookshelves)
            };

            // Set DateCompleted if status is Completed and DateRead is available
            if (book.Status == Status.Completed && book.DateRead.HasValue)
            {
                book.DateCompleted = book.DateRead;
            }

            return book;
        }

        private void UpdateBookFromRecord(Book book, GoodreadsCsvImportDto record)
        {
            // Re-import policy: Goodreads is the primary tracker for reading STATUS and RATING, so
            // those always take the export's current value (reflecting any change made in Goodreads).
            // Every other field is fill-only — a re-import backfills a gap but never overwrites a
            // populated value, so anything edited in the app after the first import is preserved.

            // Status + rating: Goodreads always wins. Status maps inline; the raw Goodreads rating is
            // stored as-is and the MMV Rating enum is derived later in the rating enrichment stage. A
            // "My Rating" of 0 means unrated, so only a real 1-5 rating overwrites the stored value —
            // an unrated export never clears an existing rating.
            book.Status = MapShelfToStatus(record.Shelves);
            if (record.MyRating is > 0)
            {
                book.GoodreadsRating = record.MyRating;
            }

            // Everything else: fill-only (set only when the existing value is null/empty). Format is
            // a non-nullable enum with no gap sentinel, so it is intentionally left untouched here.
            if (string.IsNullOrWhiteSpace(book.ISBN))
            {
                book.ISBN = CleanIsbn(record.ISBN) ?? CleanIsbn(record.ISBN13);
            }
            book.AverageRating ??= record.AverageRating;
            if (string.IsNullOrWhiteSpace(book.Publisher))
            {
                book.Publisher = record.Publisher?.Trim();
            }
            book.YearPublished ??= record.YearPublished;
            book.OriginalPublicationYear ??= record.OriginalPublicationYear;
            book.DateRead ??= ParseDate(record.DateRead);
            if (string.IsNullOrWhiteSpace(book.MyReview))
            {
                book.MyReview = record.MyReview?.Trim();
            }
            if (book.GoodreadsTags == null || book.GoodreadsTags.Count == 0)
            {
                book.GoodreadsTags = ParseBookshelves(record.Bookshelves);
            }

            // Derive DateCompleted only when it is still unset (fill-only), from the status Goodreads
            // just supplied and whatever DateRead we now hold.
            if (book.DateCompleted == null && book.Status == Status.Completed && book.DateRead.HasValue)
            {
                book.DateCompleted = book.DateRead;
            }
        }

        public Status MapShelfToStatus(string? shelf)
        {
            if (string.IsNullOrWhiteSpace(shelf))
            {
                return Status.Uncharted;
            }

            return shelf.Trim().ToLowerInvariant() switch
            {
                "to-read" => Status.Uncharted,
                "currently-reading" => Status.ActivelyExploring,
                "read" => Status.Completed,
                "to-be-continued" => Status.Abandoned,
                _ => Status.Uncharted
            };
        }

        public Rating? MapMyRatingToPlbRating(int? myRating)
        {
            if (!myRating.HasValue || myRating.Value == 0)
            {
                return null;
            }

            return myRating.Value switch
            {
                5 => Rating.SuperLike,
                4 => Rating.Like,
                3 => Rating.Neutral,
                1 or 2 => Rating.Dislike,
                _ => null
            };
        }

        public BookFormat MapBindingToFormat(string? binding)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                return BookFormat.Digital;
            }

            var lower = binding.Trim().ToLowerInvariant();
            return lower switch
            {
                "paperback" or "hardcover" or "hardback" or "mass market paperback" => BookFormat.Physical,
                _ => BookFormat.Digital
            };
        }

        public List<string> ParseBookshelves(string? bookshelves)
        {
            if (string.IsNullOrWhiteSpace(bookshelves))
            {
                return new List<string>();
            }

            return bookshelves
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();
        }

        private static string? CleanIsbn(string? isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return null;
            }

            return isbn.Replace("-", "").Replace(" ", "").Trim();
        }

        private static string BuildTitleAuthorKey(string title, string author) =>
            $"{title.Trim().ToLowerInvariant()}|{author.Trim().ToLowerInvariant()}";

        /// <summary>
        /// In-memory dedup lookup for a single import run: existing (and newly created) books keyed
        /// by cleaned ISBN and by normalized title+author, so each record is matched without a query.
        /// </summary>
        private sealed class DedupIndex
        {
            private readonly Dictionary<string, Book> _byIsbn = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, Book> _byTitleAuthor = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>ISBN match first (most reliable), then title+author. Returns null when neither matches.</summary>
            public Book? Find(string? cleanIsbn, string title, string author)
            {
                if (!string.IsNullOrWhiteSpace(cleanIsbn) && _byIsbn.TryGetValue(cleanIsbn, out var byIsbn))
                {
                    return byIsbn;
                }

                return _byTitleAuthor.TryGetValue(BuildTitleAuthorKey(title, author), out var byTitleAuthor)
                    ? byTitleAuthor
                    : null;
            }

            /// <summary>
            /// Registers a book under both keys. First write wins (mirrors the prior FirstOrDefault
            /// behavior) so duplicate keys already present are left pointing at the original.
            /// </summary>
            public void Add(Book book)
            {
                var isbn = CleanIsbn(book.ISBN);
                if (!string.IsNullOrWhiteSpace(isbn))
                {
                    _byIsbn.TryAdd(isbn, book);
                }

                _byTitleAuthor.TryAdd(BuildTitleAuthorKey(book.Title, book.Author), book);
            }
        }

        private static DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // Try various date formats Goodreads might use
            string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "dd/MM/yyyy" };

            if (DateTime.TryParseExact(dateString.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }

            if (DateTime.TryParse(dateString.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }

            return null;
        }
    }
}
