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

                foreach (var record in records)
                {
                    try
                    {
                        await ProcessBookRecordAsync(record, result, updateExisting);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing '{record.Title}' by {record.Author}: {ex.Message}");
                        _logger.LogError(ex, "Error processing book: {Title} by {Author}", record.Title, record.Author);
                    }
                }

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

        private async Task ProcessBookRecordAsync(GoodreadsCsvImportDto record, GoodreadsImportResultDto result, bool updateExisting)
        {
            if (string.IsNullOrWhiteSpace(record.Title) || string.IsNullOrWhiteSpace(record.Author))
            {
                result.SkippedCount++;
                result.Errors.Add($"Skipped book with missing title or author");
                return;
            }

            var cleanIsbn = CleanIsbn(record.ISBN);
            var existingBook = await FindExistingBookAsync(cleanIsbn, record.Title, record.Author);

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
                GoodreadsRating = record.MyRating,
                AverageRating = record.AverageRating,
                Rating = MapMyRatingToPlbRating(record.MyRating),
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
            // Update fields from Goodreads (preserve local-only fields)
            book.ISBN = CleanIsbn(record.ISBN) ?? CleanIsbn(record.ISBN13) ?? book.ISBN;
            book.Status = MapShelfToStatus(record.Shelves);
            book.Format = MapBindingToFormat(record.Binding);
            book.GoodreadsRating = record.MyRating ?? book.GoodreadsRating;
            book.AverageRating = record.AverageRating ?? book.AverageRating;
            book.Publisher = record.Publisher?.Trim() ?? book.Publisher;
            book.YearPublished = record.YearPublished ?? book.YearPublished;
            book.OriginalPublicationYear = record.OriginalPublicationYear ?? book.OriginalPublicationYear;
            book.DateRead = ParseDate(record.DateRead) ?? book.DateRead;
            book.MyReview = !string.IsNullOrWhiteSpace(record.MyReview) ? record.MyReview.Trim() : book.MyReview;

            // Merge GoodreadsTags (combine existing with new, dedupe)
            var existingTags = book.GoodreadsTags ?? new List<string>();
            var newTags = ParseBookshelves(record.Bookshelves);
            book.GoodreadsTags = existingTags.Union(newTags, StringComparer.OrdinalIgnoreCase).ToList();

            // Update Rating if GoodreadsRating changed
            if (record.MyRating.HasValue)
            {
                book.Rating = MapMyRatingToPlbRating(record.MyRating);
            }

            // Set DateCompleted if status is Completed and DateRead is available
            if (book.Status == Status.Completed && book.DateRead.HasValue)
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
                "to be continued" => Status.Abandoned,
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
