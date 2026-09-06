using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITypesenseService _typesenseService;
        private readonly ILogger<BookService> _logger;

        public BookService(
            IApplicationDbContext context,
            ITypesenseService typesenseService,
            ILogger<BookService> logger)
        {
            _context = context;
            _typesenseService = typesenseService;
            _logger = logger;
        }

        public async Task<IEnumerable<Book>> GetAllBooksAsync()
        {
            try
            {
                return await _context.Books
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(b => b.Topics)
                    .Include(b => b.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all books");
                throw;
            }
        }

        public async Task<Book?> GetBookByIdAsync(Guid id)
        {
            try
            {
                return await _context.Books
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(b => b.Topics)
                    .Include(b => b.Genres)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving book with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Book>> GetBooksByAuthorAsync(string author)
        {
            try
            {
                return await _context.Books
                    .Where(b => b.Author.ToLower().Contains(author.ToLower()))
                    .Include(b => b.Topics)
                    .Include(b => b.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving books by author: {Author}", author);
                throw;
            }
        }

        public async Task<IEnumerable<Book>> GetBookSeriesAsync()
        {
            try
            {
                return await _context.Books
                    .Where(b => b.PartOfSeries == true)
                    .Include(b => b.Topics)
                    .Include(b => b.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving book series");
                throw;
            }
        }

        public async Task<BookCreationResult> CreateBookAsync(CreateBookDto dto, BookIdentity? identity = null)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "Book data is required");
                }

                // Merge any import-supplied external ids with the dto's own identity fields
                // so every create path probes (and stores) the strongest keys available.
                var effectiveIdentity = new BookIdentity
                {
                    ReadwiseBookId = identity?.ReadwiseBookId,
                    GoodreadsBookId = identity?.GoodreadsBookId,
                    GoogleVolumeId = identity?.GoogleVolumeId,
                    OpenLibraryKey = identity?.OpenLibraryKey,
                    Isbn = identity?.Isbn ?? dto.ISBN,
                    Asin = identity?.Asin ?? dto.ASIN,
                    Title = dto.Title,
                    Author = dto.Author
                };

                var existingBook = await BookDuplicateFinder.FindExistingAsync(
                    _context.Books
                        .Include(b => b.Topics)
                        .Include(b => b.Genres),
                    effectiveIdentity);

                if (existingBook != null)
                {
                    _logger.LogInformation("Book already exists: {Title} by {Author}; absorbing external ids",
                        existingBook.Title, existingBook.Author);
                    if (BookDuplicateFinder.AbsorbIdentity(existingBook, effectiveIdentity))
                    {
                        await _context.SaveChangesAsync();
                    }
                    return new BookCreationResult(existingBook, Created: false);
                }

                var book = new Book
                {
                    Title = dto.Title,
                    MediaType = MediaType.Book,
                    Link = dto.Link,
                    Notes = dto.Notes,
                    Status = dto.Status,
                    DateAdded = DateTime.UtcNow,
                    DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                    Rating = dto.Rating,
                    OwnershipStatus = dto.OwnershipStatus,
                    Description = dto.Description,
                    RelatedNotes = dto.RelatedNotes,
                    Thumbnail = dto.Thumbnail,
                    Author = dto.Author,
                    ISBN = IsbnNormalizer.Normalize(effectiveIdentity.Isbn) ?? effectiveIdentity.Isbn,
                    ASIN = effectiveIdentity.Asin,
                    ReadwiseBookId = effectiveIdentity.ReadwiseBookId,
                    GoodreadsBookId = effectiveIdentity.GoodreadsBookId,
                    GoogleVolumeId = effectiveIdentity.GoogleVolumeId,
                    OpenLibraryKey = effectiveIdentity.OpenLibraryKey,
                    Format = dto.Format,
                    PartOfSeries = dto.PartOfSeries,
                    GoodreadsRating = dto.GoodreadsRating,
                    AverageRating = dto.AverageRating,
                    Publisher = dto.Publisher,
                    YearPublished = dto.YearPublished,
                    OriginalPublicationYear = dto.OriginalPublicationYear,
                    DateRead = DateTimeNormalizer.ToUtc(dto.DateRead),
                    MyReview = dto.MyReview,
                    GoodreadsTags = dto.GoodreadsTags ?? new List<string>()
                };

                // If GoodreadsRating is provided but Rating is not, auto-convert
                if (dto.GoodreadsRating.HasValue && !dto.Rating.HasValue)
                {
                    book.Rating = RatingConverter.ConvertGoodreadsRatingToPLBRating(dto.GoodreadsRating);
                }

                // Handle Topics array conversion
                await HandleTopicsAsync(book, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(book, dto.Genres);

                _context.Add(book);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created book: {Title} by {Author}", book.Title, book.Author);
                return new BookCreationResult(book, Created: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating book");
                throw;
            }
        }

        public async Task<Book?> UpdateBookAsync(Guid id, CreateBookDto dto)
        {
            try
            {
                // Load tracked (with topics/genres) so EF can persist removed relationships
                // when the collections are replaced below.
                var book = await _context.Books
                    .Include(b => b.Topics)
                    .Include(b => b.Genres)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (book == null)
                {
                    _logger.LogInformation("Update requested for book {Id}, which does not exist", id);
                    return null;
                }

                // Update book properties
                book.Title = dto.Title;
                book.Link = dto.Link;
                book.Notes = dto.Notes;
                book.Status = dto.Status;
                book.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
                book.Rating = dto.Rating;
                book.OwnershipStatus = dto.OwnershipStatus;
                book.Description = dto.Description;
                book.RelatedNotes = dto.RelatedNotes;
                book.Thumbnail = dto.Thumbnail;
                book.Author = dto.Author;
                book.ISBN = IsbnNormalizer.Normalize(dto.ISBN) ?? dto.ISBN;
                book.ASIN = dto.ASIN;
                book.Format = dto.Format;
                book.PartOfSeries = dto.PartOfSeries;
                book.GoodreadsRating = dto.GoodreadsRating;
                book.AverageRating = dto.AverageRating;
                book.Publisher = dto.Publisher;
                book.YearPublished = dto.YearPublished;
                book.OriginalPublicationYear = dto.OriginalPublicationYear;
                book.DateRead = DateTimeNormalizer.ToUtc(dto.DateRead);
                book.MyReview = dto.MyReview;
                book.GoodreadsTags = dto.GoodreadsTags ?? new List<string>();

                // If GoodreadsRating is provided but Rating is not, auto-convert
                if (dto.GoodreadsRating.HasValue && !dto.Rating.HasValue)
                {
                    book.Rating = RatingConverter.ConvertGoodreadsRatingToPLBRating(dto.GoodreadsRating);
                }

                // Replace topic/genre links in the same unit of work as the field updates;
                // a single SaveChanges below persists everything atomically.
                book.Topics.Clear();
                book.Genres.Clear();

                // Handle Topics array conversion
                await HandleTopicsAsync(book, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(book, dto.Genres);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated book: {Title} by {Author}", book.Title, book.Author);
                return book;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating book with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteBookAsync(Guid id)
        {
            try
            {
                var book = await _context.FindAsync<Book>(id);
                if (book == null)
                {
                    return false;
                }

                var bookId = book.Id;
                var bookTitle = book.Title;
                var bookAuthor = book.Author;

                _context.Remove(book);
                await _context.SaveChangesAsync();

                // Eager search-index cleanup so the deleted book stops appearing in search immediately.
                // Best effort: the next bulk reindex reconciles anything this misses.
                await SearchIndexCleanup.TryDeleteAsync(
                    () => _typesenseService.DeleteMediaItemAsync(bookId), _logger, "book", bookId);

                _logger.LogInformation("Successfully deleted book: {Title} by {Author}", bookTitle, bookAuthor);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting book with ID {Id}", id);
                throw;
            }
        }

        private async Task HandleTopicsAsync(Book book, string[]? topics)
        {
            if (topics?.Length > 0)
            {
                foreach (var topicName in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                    var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                    if (existingTopic != null)
                    {
                        book.Topics.Add(existingTopic);
                    }
                    else
                    {
                        var newTopic = new Topic { Name = normalizedTopicName };
                        _context.Add(newTopic);
                        book.Topics.Add(newTopic);
                    }
                }
            }
        }

        private async Task HandleGenresAsync(Book book, string[]? genres)
        {
            if (genres?.Length > 0)
            {
                foreach (var genreName in genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                    var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                    if (existingGenre != null)
                    {
                        book.Genres.Add(existingGenre);
                    }
                    else
                    {
                        // Register the new genre explicitly (see HandleTopicsAsync) so EF inserts
                        // it instead of assuming the client-set key already exists.
                        var newGenre = new Genre { Name = normalizedGenreName };
                        _context.Add(newGenre);
                        book.Genres.Add(newGenre);
                    }
                }
            }
        }
    }
}
