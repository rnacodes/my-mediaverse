using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<BookService> _logger;

        public BookService(
            IApplicationDbContext context,
            ILogger<BookService> logger)
        {
            _context = context;
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

        public async Task<Book> CreateBookAsync(CreateBookDto dto)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "Book data is required");
                }

                // Check if book already exists
                if (await BookExistsAsync(dto.Title, dto.Author))
                {
                    _logger.LogWarning("Book already exists: {Title} by {Author}", dto.Title, dto.Author);
                    var existingBook = await GetBookByTitleAndAuthorAsync(dto.Title, dto.Author);
                    if (existingBook != null)
                    {
                        return existingBook;
                    }
                }

                var book = new Book
                {
                    Title = dto.Title,
                    MediaType = MediaType.Book,
                    Link = dto.Link,
                    Notes = dto.Notes,
                    Status = dto.Status,
                    DateAdded = DateTime.UtcNow,
                    DateCompleted = dto.DateCompleted,
                    Rating = dto.Rating,
                    OwnershipStatus = dto.OwnershipStatus,
                    Description = dto.Description,
                    RelatedNotes = dto.RelatedNotes,
                    Thumbnail = dto.Thumbnail,
                    Author = dto.Author,
                    ISBN = dto.ISBN,
                    ASIN = dto.ASIN,
                    Format = dto.Format,
                    PartOfSeries = dto.PartOfSeries,
                    GoodreadsRating = dto.GoodreadsRating
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
                return book;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating book");
                throw;
            }
        }

        public async Task<Book> UpdateBookAsync(Guid id, CreateBookDto dto)
        {
            try
            {
                var book = await GetBookByIdAsync(id);
                if (book == null)
                {
                    throw new InvalidOperationException($"Book with ID {id} not found.");
                }

                // Update book properties
                book.Title = dto.Title;
                book.Link = dto.Link;
                book.Notes = dto.Notes;
                book.Status = dto.Status;
                book.DateCompleted = dto.DateCompleted;
                book.Rating = dto.Rating;
                book.OwnershipStatus = dto.OwnershipStatus;
                book.Description = dto.Description;
                book.RelatedNotes = dto.RelatedNotes;
                book.Thumbnail = dto.Thumbnail;
                book.Author = dto.Author;
                book.ISBN = dto.ISBN;
                book.ASIN = dto.ASIN;
                book.Format = dto.Format;
                book.PartOfSeries = dto.PartOfSeries;
                book.GoodreadsRating = dto.GoodreadsRating;
                
                // If GoodreadsRating is provided but Rating is not, auto-convert
                if (dto.GoodreadsRating.HasValue && !dto.Rating.HasValue)
                {
                    book.Rating = RatingConverter.ConvertGoodreadsRatingToPLBRating(dto.GoodreadsRating);
                }

                // Clear existing topics and genres
                book.Topics.Clear();
                book.Genres.Clear();

                // Handle Topics array conversion
                await HandleTopicsAsync(book, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(book, dto.Genres);

                // Clear change tracker and explicitly update the entity since it was retrieved with AsNoTracking
                _context.ClearChangeTracker();
                _context.Update(book);
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

                _logger.LogInformation("Successfully deleted book: {Title} by {Author}", bookTitle, bookAuthor);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting book with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> BookExistsAsync(string title, string author)
        {
            try
            {
                return await _context.Books
                    .AnyAsync(b => b.Title.ToLower() == title.ToLower() && 
                                   b.Author.ToLower() == author.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if book exists: {Title} by {Author}", title, author);
                throw;
            }
        }

        public async Task<Book?> GetBookByTitleAndAuthorAsync(string title, string author)
        {
            try
            {
                return await _context.Books
                    .Include(b => b.Topics)
                    .Include(b => b.Genres)
                    .FirstOrDefaultAsync(b => b.Title.ToLower() == title.ToLower() && 
                                             b.Author.ToLower() == author.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving book by title and author: {Title} by {Author}", title, author);
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
                        book.Topics.Add(new Topic { Name = normalizedTopicName });
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
                        book.Genres.Add(new Genre { Name = normalizedGenreName });
                    }
                }
            }
        }
    }
}
