using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for Google Books operations.
    /// Provides search and import functionality for books from Google Books API.
    /// </summary>
    public class GoogleBooksService : IGoogleBooksService
    {
        private readonly IGoogleBooksApiClient _googleBooksApiClient;
        private readonly IBookService _bookService;
        private readonly IBookMappingService _bookMappingService;
        private readonly ILogger<GoogleBooksService> _logger;

        public GoogleBooksService(
            IGoogleBooksApiClient googleBooksApiClient,
            IBookService bookService,
            IBookMappingService bookMappingService,
            ILogger<GoogleBooksService> logger)
        {
            _googleBooksApiClient = googleBooksApiClient;
            _bookService = bookService;
            _bookMappingService = bookMappingService;
            _logger = logger;
        }

        // Search operations (return DTOs for API consumption)

        public async Task<GoogleBooksSearchResultDto> SearchBooksAsync(string query, int? startIndex = null, int? maxResults = null)
        {
            _logger.LogInformation("Searching Google Books with query: {Query}, startIndex: {StartIndex}, maxResults: {MaxResults}",
                query, startIndex, maxResults);
            return await _googleBooksApiClient.SearchBooksAsync(query, startIndex, maxResults);
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksByTitleAsync(string title, int? startIndex = null, int? maxResults = null)
        {
            _logger.LogInformation("Searching Google Books by title: {Title}, startIndex: {StartIndex}, maxResults: {MaxResults}",
                title, startIndex, maxResults);
            return await _googleBooksApiClient.SearchBooksByTitleAsync(title, startIndex, maxResults);
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksByAuthorAsync(string author, int? startIndex = null, int? maxResults = null)
        {
            _logger.LogInformation("Searching Google Books by author: {Author}, startIndex: {StartIndex}, maxResults: {MaxResults}",
                author, startIndex, maxResults);
            return await _googleBooksApiClient.SearchBooksByAuthorAsync(author, startIndex, maxResults);
        }

        public async Task<GoogleBooksSearchResultDto> SearchBooksByISBNAsync(string isbn)
        {
            _logger.LogInformation("Searching Google Books by ISBN: {ISBN}", isbn);
            return await _googleBooksApiClient.SearchBooksByISBNAsync(isbn);
        }

        // Detail operations

        public async Task<GoogleBooksVolumeDto?> GetVolumeByIdAsync(string volumeId)
        {
            _logger.LogInformation("Getting Google Books volume details for ID: {VolumeId}", volumeId);
            return await _googleBooksApiClient.GetVolumeByIdAsync(volumeId);
        }

        // Import operations (business logic - convert DTOs to Domain Entities)

        public async Task<Book> ImportBookFromVolumeIdAsync(string volumeId)
        {
            try
            {
                _logger.LogInformation("Importing book from Google Books volume ID: {VolumeId}", volumeId);

                var volume = await _googleBooksApiClient.GetVolumeByIdAsync(volumeId);

                if (volume == null || volume.VolumeInfo == null)
                {
                    throw new InvalidOperationException($"Volume with ID {volumeId} not found in Google Books");
                }

                // Check if book already exists by title and author
                var title = volume.VolumeInfo.Title ?? "Unknown Title";
                var author = volume.VolumeInfo.Authors?.FirstOrDefault() ?? "Unknown Author";

                var existingBook = await _bookService.GetBookByTitleAndAuthorAsync(title, author);
                if (existingBook != null)
                {
                    _logger.LogInformation("Book {Title} by {Author} already exists", title, author);
                    return existingBook;
                }

                // Map to Book entity
                var book = await _bookMappingService.MapFromGoogleBooksAsync(volume);

                // Create DTO for the service
                var createBookDto = new CreateBookDto
                {
                    Title = book.Title,
                    Author = book.Author,
                    Description = book.Description,
                    Link = book.Link,
                    Thumbnail = book.Thumbnail,
                    ISBN = book.ISBN,
                    Format = book.Format,
                    PartOfSeries = book.PartOfSeries,
                    Status = book.Status,
                    Rating = book.Rating,
                    OwnershipStatus = book.OwnershipStatus,
                    Notes = book.Notes,
                    RelatedNotes = book.RelatedNotes,
                    Publisher = book.Publisher,
                    AverageRating = book.AverageRating,
                    YearPublished = book.YearPublished
                };

                // Save to database through domain service
                var savedBook = await _bookService.CreateBookAsync(createBookDto);

                _logger.LogInformation("Successfully imported book from Google Books: {Title} (Volume ID: {VolumeId})",
                    title, volumeId);

                return savedBook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing book from Google Books volume ID: {VolumeId}", volumeId);
                throw;
            }
        }

        public async Task<Book> ImportBookFromISBNAsync(string isbn)
        {
            try
            {
                _logger.LogInformation("Importing book from ISBN: {ISBN}", isbn);

                // Search by ISBN to get book data
                var searchResult = await _googleBooksApiClient.SearchBooksByISBNAsync(isbn);
                var volume = searchResult.Items?.FirstOrDefault();

                if (volume == null || volume.VolumeInfo == null)
                {
                    throw new InvalidOperationException($"Book with ISBN {isbn} not found in Google Books");
                }

                // Check if book already exists by title and author
                var title = volume.VolumeInfo.Title ?? "Unknown Title";
                var author = volume.VolumeInfo.Authors?.FirstOrDefault() ?? "Unknown Author";

                var existingBook = await _bookService.GetBookByTitleAndAuthorAsync(title, author);
                if (existingBook != null)
                {
                    _logger.LogInformation("Book {Title} by {Author} already exists", title, author);
                    return existingBook;
                }

                // Map to Book entity
                var book = await _bookMappingService.MapFromGoogleBooksAsync(volume);

                // Create DTO for the service
                var createBookDto = new CreateBookDto
                {
                    Title = book.Title,
                    Author = book.Author,
                    Description = book.Description,
                    Link = book.Link,
                    Thumbnail = book.Thumbnail,
                    ISBN = book.ISBN,
                    Format = book.Format,
                    PartOfSeries = book.PartOfSeries,
                    Status = book.Status,
                    Rating = book.Rating,
                    OwnershipStatus = book.OwnershipStatus,
                    Notes = book.Notes,
                    RelatedNotes = book.RelatedNotes,
                    Publisher = book.Publisher,
                    AverageRating = book.AverageRating,
                    YearPublished = book.YearPublished
                };

                // Save to database through domain service
                var savedBook = await _bookService.CreateBookAsync(createBookDto);

                _logger.LogInformation("Successfully imported book from ISBN: {Title} (ISBN: {ISBN})",
                    title, isbn);

                return savedBook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing book from ISBN: {ISBN}", isbn);
                throw;
            }
        }

        public async Task<Book> ImportBookFromTitleAndAuthorAsync(string title, string? author = null)
        {
            try
            {
                _logger.LogInformation("Importing book from title: {Title}, author: {Author}", title, author);

                GoogleBooksSearchResultDto searchResult;

                if (!string.IsNullOrWhiteSpace(author))
                {
                    // Search by title and author
                    var query = $"intitle:{title} inauthor:{author}";
                    searchResult = await _googleBooksApiClient.SearchBooksAsync(query, maxResults: 1);
                }
                else
                {
                    // Search by title only
                    searchResult = await _googleBooksApiClient.SearchBooksByTitleAsync(title, maxResults: 1);
                }

                var volume = searchResult.Items?.FirstOrDefault();
                if (volume == null || volume.VolumeInfo == null)
                {
                    throw new InvalidOperationException($"Book with title '{title}' and author '{author}' not found in Google Books");
                }

                // Check if book already exists by title and author
                var bookTitle = volume.VolumeInfo.Title ?? "Unknown Title";
                var bookAuthor = volume.VolumeInfo.Authors?.FirstOrDefault() ?? "Unknown Author";

                var existingBook = await _bookService.GetBookByTitleAndAuthorAsync(bookTitle, bookAuthor);
                if (existingBook != null)
                {
                    _logger.LogInformation("Book {Title} by {Author} already exists", bookTitle, bookAuthor);
                    return existingBook;
                }

                // Map to Book entity
                var book = await _bookMappingService.MapFromGoogleBooksAsync(volume);

                // Create DTO for the service
                var createBookDto = new CreateBookDto
                {
                    Title = book.Title,
                    Author = book.Author,
                    Description = book.Description,
                    Link = book.Link,
                    Thumbnail = book.Thumbnail,
                    ISBN = book.ISBN,
                    Format = book.Format,
                    PartOfSeries = book.PartOfSeries,
                    Status = book.Status,
                    Rating = book.Rating,
                    OwnershipStatus = book.OwnershipStatus,
                    Notes = book.Notes,
                    RelatedNotes = book.RelatedNotes,
                    Publisher = book.Publisher,
                    AverageRating = book.AverageRating,
                    YearPublished = book.YearPublished
                };

                // Save to database through domain service
                var savedBook = await _bookService.CreateBookAsync(createBookDto);

                _logger.LogInformation("Successfully imported book from title and author: {Title} by {Author}",
                    bookTitle, bookAuthor);

                return savedBook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing book from title: {Title}, author: {Author}", title, author);
                throw;
            }
        }
    }
}
