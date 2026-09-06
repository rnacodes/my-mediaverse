using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.Application.Services
{
    public class BookMappingService : IBookMappingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<BookMappingService> _logger;

        public BookMappingService(IApplicationDbContext context, ILogger<BookMappingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<BookResponseDto> MapToResponseDtoAsync(Book book)
        {
            return Task.FromResult(new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                Description = book.Description,
                MediaType = book.MediaType,
                Status = book.Status,
                DateAdded = book.DateAdded,
                Link = book.Link,
                Thumbnail = book.Thumbnail,
                Author = book.Author,
                ISBN = book.ISBN,
                ASIN = book.ASIN,
                Format = book.Format,
                PartOfSeries = book.PartOfSeries,
                Rating = book.Rating,
                OwnershipStatus = book.OwnershipStatus,
                DateCompleted = book.DateCompleted,
                Notes = book.Notes,
                RelatedNotes = book.RelatedNotes,
                Topics = book.Topics.Select(t => t.Name).ToArray(),
                Genres = book.Genres.Select(g => g.Name).ToArray(),
                GoodreadsRating = book.GoodreadsRating,
                AverageRating = book.AverageRating,
                YearPublished = book.YearPublished,
                OriginalPublicationYear = book.OriginalPublicationYear,
                DateRead = book.DateRead,
                MyReview = book.MyReview,
                Publisher = book.Publisher,
                GoodreadsTags = book.GoodreadsTags ?? new List<string>(),
                ReadwiseBookId = book.ReadwiseBookId,
                GoodreadsBookId = book.GoodreadsBookId,
                GoogleVolumeId = book.GoogleVolumeId,
                OpenLibraryKey = book.OpenLibraryKey,
                EnrichedAt = book.EnrichedAt
            });
        }

        public Task<Book> MapFromOpenLibraryAsync(OpenLibraryBookDto openLibraryBook)
        {
            var book = new Book
            {
                Title = openLibraryBook.Title ?? "Unknown Title",
                Author = openLibraryBook.AuthorName?.FirstOrDefault() ?? "Unknown Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                ISBN = openLibraryBook.Isbn?.FirstOrDefault(),
                Format = BookFormat.Digital, // Default to digital since it's from Open Library
                PartOfSeries = false,
                Thumbnail = openLibraryBook.CoverId.HasValue
                    ? $"https://covers.openlibrary.org/b/id/{openLibraryBook.CoverId}-L.jpg"
                    : null,
                Description = ExtractDescription(openLibraryBook),
                Link = !string.IsNullOrWhiteSpace(openLibraryBook.Key)
                    ? $"https://openlibrary.org{openLibraryBook.Key}"
                    : null
            };

            // Note: Topics and genres are NOT auto-imported from Open Library subjects
            // Users can add them manually after import if desired

            return Task.FromResult(book);
        }

        public Task<BookSearchResultDto> MapToSearchResultDtoAsync(OpenLibraryBookDto openLibraryBook)
        {
            return Task.FromResult(new BookSearchResultDto
            {
                Key = openLibraryBook.Key,
                Title = openLibraryBook.Title,
                Authors = openLibraryBook.AuthorName,
                FirstPublishYear = openLibraryBook.FirstPublishYear,
                Isbn = openLibraryBook.Isbn,
                Subjects = openLibraryBook.Subject,
                CoverUrl = openLibraryBook.CoverId.HasValue 
                    ? $"https://covers.openlibrary.org/b/id/{openLibraryBook.CoverId}-L.jpg" 
                    : null,
                Publishers = openLibraryBook.Publisher,
                Languages = openLibraryBook.Language,
                PageCount = openLibraryBook.NumberOfPagesMedian,
                AverageRating = openLibraryBook.RatingAverage,
                RatingCount = openLibraryBook.RatingCount,
                HasFulltext = openLibraryBook.HasFulltext,
                EditionCount = openLibraryBook.EditionCount
            });
        }

        private static string? ExtractDescription(OpenLibraryBookDto bookData)
        {
            // Open Library search results don't typically include descriptions
            // This is a placeholder for future enhancement
            if (bookData.Subject?.Length > 0)
            {
                return $"Subjects: {string.Join(", ", bookData.Subject.Take(3))}";
            }
            return null;
        }

        // ============================================
        // Google Books Mapping Methods
        // ============================================

        public Task<Book> MapFromGoogleBooksAsync(GoogleBooksVolumeDto volume)
        {
            var volumeInfo = volume.VolumeInfo;

            var book = new Book
            {
                Title = volumeInfo?.Title ?? "Unknown Title",
                Author = volumeInfo?.Authors?.FirstOrDefault() ?? "Unknown Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                ISBN = volumeInfo?.GetBestIsbn(),
                Format = volume.SaleInfo?.IsEbook == true ? BookFormat.Digital : BookFormat.Physical,
                PartOfSeries = false,
                Thumbnail = volumeInfo?.ImageLinks?.GetBestThumbnail(),
                Description = HtmlText.Strip(volumeInfo?.Description),
                Link = volumeInfo?.CanonicalVolumeLink ?? volumeInfo?.InfoLink,
                Publisher = volumeInfo?.Publisher,
                AverageRating = (decimal?)volumeInfo?.AverageRating,
                YearPublished = volumeInfo?.GetPublishedYear()
            };

            // Note: Topics and genres are NOT auto-imported from Google Books categories
            // Users can add them manually after import if desired

            return Task.FromResult(book);
        }

        public Task<BookSearchResultDto> MapGoogleBooksToSearchResultDtoAsync(GoogleBooksVolumeDto volume)
        {
            var volumeInfo = volume.VolumeInfo;

            return Task.FromResult(new BookSearchResultDto
            {
                Key = volume.Id, // Google Books Volume ID
                Title = volumeInfo?.Title,
                Authors = volumeInfo?.Authors,
                FirstPublishYear = volumeInfo?.GetPublishedYear(),
                Isbn = volumeInfo?.IndustryIdentifiers?
                    .Where(i => i.Type == "ISBN_13" || i.Type == "ISBN_10")
                    .Select(i => i.Identifier)
                    .Where(i => i != null)
                    .ToArray() as string[],
                Subjects = volumeInfo?.Categories,
                CoverUrl = volumeInfo?.ImageLinks?.GetBestThumbnail(),
                Publishers = volumeInfo?.Publisher != null ? new[] { volumeInfo.Publisher } : null,
                Languages = volumeInfo?.Language != null ? new[] { volumeInfo.Language } : null,
                PageCount = volumeInfo?.PageCount,
                AverageRating = volumeInfo?.AverageRating,
                RatingCount = volumeInfo?.RatingsCount,
                HasFulltext = null, // Not available in Google Books
                EditionCount = null // Not available in Google Books
            });
        }
    }
}
