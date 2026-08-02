using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.DTOs;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly IBookMappingService _bookMappingService;
        private readonly ILogger<BookController> _logger;
        private readonly IOpenLibraryService _openLibraryService;
        private readonly IGoogleBooksService _googleBooksService;

        public BookController(
            IBookService bookService,
            IBookMappingService bookMappingService,
            ILogger<BookController> logger,
            IOpenLibraryService openLibraryService,
            IGoogleBooksService googleBooksService)
        {
            _bookService = bookService;
            _bookMappingService = bookMappingService;
            _logger = logger;
            _openLibraryService = openLibraryService;
            _googleBooksService = googleBooksService;
        }

        // GET: api/book
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetAllBooks()
        {
            try
            {
                var books = await _bookService.GetAllBooksAsync();
                var response = await Task.WhenAll(books.Select(b => _bookMappingService.MapToResponseDtoAsync(b)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all books");
                return StatusCode(500, new { error = "Failed to retrieve books", details = ex.Message });
            }
        }

        // GET: api/book/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BookResponseDto>> GetBook(Guid id)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id);
                if (book == null)
                {
                    return NotFound($"Book with ID {id} not found.");
                }

                var response = await _bookMappingService.MapToResponseDtoAsync(book);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving book with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve book", details = ex.Message });
            }
        }

        // GET: api/book/by-author/{author}
        [HttpGet("by-author/{author}")]
        public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetBooksByAuthor(string author)
        {
            try
            {
                var books = await _bookService.GetBooksByAuthorAsync(author);
                var response = await Task.WhenAll(books.Select(b => _bookMappingService.MapToResponseDtoAsync(b)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving books by author: {Author}", author);
                return StatusCode(500, new { error = "Failed to retrieve books by author", details = ex.Message });
            }
        }

        // GET: api/book/series
        [HttpGet("series")]
        public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetBookSeries()
        {
            try
            {
                var seriesBooks = await _bookService.GetBookSeriesAsync();
                var response = await Task.WhenAll(seriesBooks.Select(b => _bookMappingService.MapToResponseDtoAsync(b)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving book series");
                return StatusCode(500, new { error = "Failed to retrieve book series", details = ex.Message });
            }
        }

        // POST: api/book
        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Book data is required");
                }

                var book = await _bookService.CreateBookAsync(dto);
                var response = await _bookMappingService.MapToResponseDtoAsync(book);

                return CreatedAtAction(nameof(GetBook), new { id = book.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating book");
                return StatusCode(500, new { error = "Failed to create book", details = ex.Message });
            }
        }

        // PUT: api/book/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(Guid id, [FromBody] CreateBookDto dto)
        {
            try
            {
                var book = await _bookService.UpdateBookAsync(id, dto);
                var response = await _bookMappingService.MapToResponseDtoAsync(book);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound($"Book with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating book with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update book", details = ex.Message });
            }
        }

        // DELETE: api/book/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(Guid id)
        {
            try
            {
                var deleted = await _bookService.DeleteBookAsync(id);
                if (!deleted)
                {
                    return NotFound($"Book with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting book with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete book", details = ex.Message });
            }
        }

        // GET: api/book/search-openlibrary
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpGet("search-openlibrary")]
        public async Task<ActionResult<IEnumerable<BookSearchResultDto>>> SearchOpenLibrary([FromQuery] SearchBooksDto searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.Query))
                {
                    return BadRequest("Search query is required");
                }

                OpenLibrarySearchResultDto searchResult;

                switch (searchDto.SearchType)
                {
                    case BookSearchType.Title:
                        searchResult = await _openLibraryService.SearchBooksByTitleAsync(searchDto.Query, searchDto.Offset, searchDto.Limit);
                        break;
                    case BookSearchType.Author:
                        searchResult = await _openLibraryService.SearchBooksByAuthorAsync(searchDto.Query, searchDto.Offset, searchDto.Limit);
                        break;
                    case BookSearchType.ISBN:
                        searchResult = await _openLibraryService.SearchBooksByISBNAsync(searchDto.Query);
                        break;
                    default:
                        searchResult = await _openLibraryService.SearchBooksAsync(searchDto.Query, searchDto.Offset, searchDto.Limit);
                        break;
                }
                
                if (searchResult?.Docs == null)
                {
                    return Ok(new List<BookSearchResultDto>());
                }

                var results = await Task.WhenAll(searchResult.Docs.Select(book => _bookMappingService.MapToSearchResultDtoAsync(book)));

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching Open Library for query: {Query}", searchDto.Query);
                return StatusCode(500, new { error = "Failed to search Open Library", details = ex.Message });
            }
        }

        // POST: api/book/import-from-openlibrary
        [HttpPost("import-from-openlibrary")]
        public async Task<IActionResult> ImportFromOpenLibrary([FromBody] ImportBookFromOpenLibraryDto importDto)
        {
            try
            {
                if (importDto == null)
                {
                    return BadRequest("Import data is required");
                }

                Domain.Entities.Book createdBook;

                // Use the OpenLibrary service to import the book based on available data
                if (!string.IsNullOrWhiteSpace(importDto.OpenLibraryKey))
                {
                    createdBook = await _openLibraryService.ImportBookFromOpenLibraryKeyAsync(importDto.OpenLibraryKey);
                }
                else if (!string.IsNullOrWhiteSpace(importDto.Isbn))
                {
                    createdBook = await _openLibraryService.ImportBookFromISBNAsync(importDto.Isbn);
                }
                else if (!string.IsNullOrWhiteSpace(importDto.Title))
                {
                    createdBook = await _openLibraryService.ImportBookFromTitleAndAuthorAsync(importDto.Title, importDto.Author);
                }
                else
                {
                    return BadRequest("At least one of OpenLibraryKey, ISBN, or Title must be provided");
                }

                // Note: Thumbnail uploading to DigitalOcean Spaces is disabled for book imports
                // to avoid Entity Framework tracking conflicts. The OpenLibrary CDN URLs are
                // reliable and work well. If custom thumbnail upload is needed, it can be done
                // separately after import via the update book endpoint.

                _logger.LogInformation("Successfully imported book from Open Library: {Title} by {Author}", createdBook.Title, createdBook.Author);

                var responseDto = await _bookMappingService.MapToResponseDtoAsync(createdBook);
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, responseDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Book not found in Open Library");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing book from Open Library");
                return StatusCode(500, new { error = "Failed to import book from Open Library", details = ex.Message });
            }
        }

        // ============================================
        // Google Books Endpoints
        // ============================================

        // GET: api/book/search-googlebooks
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpGet("search-googlebooks")]
        public async Task<ActionResult<IEnumerable<BookSearchResultDto>>> SearchGoogleBooks([FromQuery] SearchBooksDto searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.Query))
                {
                    return BadRequest("Search query is required");
                }

                GoogleBooksSearchResultDto searchResult;

                switch (searchDto.SearchType)
                {
                    case BookSearchType.Title:
                        searchResult = await _googleBooksService.SearchBooksByTitleAsync(searchDto.Query, searchDto.Offset, searchDto.Limit);
                        break;
                    case BookSearchType.Author:
                        searchResult = await _googleBooksService.SearchBooksByAuthorAsync(searchDto.Query, searchDto.Offset, searchDto.Limit);
                        break;
                    case BookSearchType.ISBN:
                        searchResult = await _googleBooksService.SearchBooksByISBNAsync(searchDto.Query);
                        break;
                    default:
                        searchResult = await _googleBooksService.SearchBooksAsync(searchDto.Query, searchDto.Offset, searchDto.Limit);
                        break;
                }

                if (searchResult?.Items == null)
                {
                    return Ok(new List<BookSearchResultDto>());
                }

                var results = await Task.WhenAll(searchResult.Items.Select(volume => _bookMappingService.MapGoogleBooksToSearchResultDtoAsync(volume)));

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching Google Books for query: {Query}", searchDto.Query);
                return StatusCode(500, new { error = "Failed to search Google Books", details = ex.Message });
            }
        }

        // POST: api/book/import-from-googlebooks
        [HttpPost("import-from-googlebooks")]
        public async Task<IActionResult> ImportFromGoogleBooks([FromBody] ImportBookFromGoogleBooksDto importDto)
        {
            try
            {
                if (importDto == null)
                {
                    return BadRequest("Import data is required");
                }

                Domain.Entities.Book createdBook;

                // Use the Google Books service to import the book based on available data
                if (!string.IsNullOrWhiteSpace(importDto.VolumeId))
                {
                    createdBook = await _googleBooksService.ImportBookFromVolumeIdAsync(importDto.VolumeId);
                }
                else if (!string.IsNullOrWhiteSpace(importDto.Isbn))
                {
                    createdBook = await _googleBooksService.ImportBookFromISBNAsync(importDto.Isbn);
                }
                else if (!string.IsNullOrWhiteSpace(importDto.Title))
                {
                    createdBook = await _googleBooksService.ImportBookFromTitleAndAuthorAsync(importDto.Title, importDto.Author);
                }
                else
                {
                    return BadRequest("At least one of VolumeId, ISBN, or Title must be provided");
                }

                _logger.LogInformation("Successfully imported book from Google Books: {Title} by {Author}", createdBook.Title, createdBook.Author);

                var responseDto = await _bookMappingService.MapToResponseDtoAsync(createdBook);
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, responseDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Book not found in Google Books");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing book from Google Books");
                return StatusCode(500, new { error = "Failed to import book from Google Books", details = ex.Message });
            }
        }
    }
}
