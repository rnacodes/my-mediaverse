using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class BookServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<BookService> _mockLogger;
        private readonly BookService _bookService;

        public BookServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<BookService>>();
            _bookService = new BookService(Context, _mockLogger);
        }

        [Fact]
        public async Task GetAllBooksAsync_ShouldReturnAllBooks()
        {
            // Arrange
            var books = TestDataFactory.CreateBooks(3);
            Context.Books.AddRange(books);
            await Context.SaveChangesAsync();

            // Act
            var result = await _bookService.GetAllBooksAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(books);
        }

        [Fact]
        public async Task GetBookByIdAsync_ShouldReturnBook_WhenBookExists()
        {
            // Arrange
            var book = TestDataFactory.CreateBook("Test Book", "Test Author");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            // Act
            var result = await _bookService.GetBookByIdAsync(book.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(book);
        }

        [Fact]
        public async Task GetBookByIdAsync_ShouldReturnNull_WhenBookDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _bookService.GetBookByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetBooksByAuthorAsync_ShouldReturnBooksByAuthor()
        {
            // Arrange
            var author = "Test Author";
            var books = new[]
            {
                TestDataFactory.CreateBook("Book 1", author),
                TestDataFactory.CreateBook("Book 2", author),
                TestDataFactory.CreateBook("Book 3", "Other Author")
            };
            Context.Books.AddRange(books);
            await Context.SaveChangesAsync();

            // Act
            var result = await _bookService.GetBooksByAuthorAsync(author);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(b => b.Author.ToLower().Contains(author.ToLower()));
        }

        [Fact]
        public async Task GetBookSeriesAsync_ShouldReturnOnlyBooksInSeries()
        {
            // Arrange
            var books = new[]
            {
                TestDataFactory.CreateBook("Series Book 1", "Author 1"),
                TestDataFactory.CreateBook("Series Book 2", "Author 2"),
                TestDataFactory.CreateBook("Standalone Book", "Author 3")
            };
            books[0].PartOfSeries = true;
            books[1].PartOfSeries = true;
            books[2].PartOfSeries = false;

            Context.Books.AddRange(books);
            await Context.SaveChangesAsync();

            // Act
            var result = await _bookService.GetBookSeriesAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(b => b.PartOfSeries == true);
        }

        [Fact]
        public async Task CreateBookAsync_ShouldCreateNewBook_WhenBookDoesNotExist()
        {
            // Arrange
            var dto = TestDataFactory.CreateBookDto("New Book", "New Author");

            // Act
            var result = await _bookService.CreateBookAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(dto.Title);
            result.Author.Should().Be(dto.Author);
            result.MediaType.Should().Be(MediaType.Book);
            
            // Verify the book was saved to the database
            var savedBook = await Context.Books.FindAsync(result.Id);
            savedBook.Should().NotBeNull();
            savedBook!.Title.Should().Be(dto.Title);
        }

        [Fact]
        public async Task CreateBookAsync_ShouldThrowArgumentNullException_WhenDtoIsNull()
        {
            // Arrange
            CreateBookDto? dto = null;

            // Act & Assert
            await _bookService.Invoking(s => s.CreateBookAsync(dto!))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithMessage("Book data is required (Parameter 'dto')");
        }

        [Fact]
        public async Task CreateBookAsync_ShouldReturnExistingBook_WhenBookAlreadyExists()
        {
            // Arrange
            var existingBook = TestDataFactory.CreateBook("Existing Book", "Existing Author");
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto("Existing Book", "Existing Author");

            // Act
            var result = await _bookService.CreateBookAsync(dto);

            // Assert
            result.Should().BeEquivalentTo(existingBook);
            
            // Verify no duplicate was created
            var allBooks = Context.Books.ToList();
            allBooks.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldUpdateExistingBook()
        {
            // Arrange
            var existingBook = TestDataFactory.CreateBook("Original Title", "Original Author");
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto("Updated Title", "Updated Author");

            // Act
            var result = await _bookService.UpdateBookAsync(existingBook.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Author.Should().Be("Updated Author");
            
            // Clear tracker and reload from database to verify persistence
            Context.ChangeTracker.Clear();
            var updatedBook = await Context.Books.FindAsync(existingBook.Id);
            updatedBook.Should().NotBeNull();
            updatedBook!.Title.Should().Be("Updated Title");
            updatedBook.Author.Should().Be("Updated Author");
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldThrowInvalidOperationException_WhenBookDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = TestDataFactory.CreateBookDto("Updated Title", "Updated Author");

            // Act & Assert
            await _bookService.Invoking(s => s.UpdateBookAsync(nonExistentId, dto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Book with ID {nonExistentId} not found.");
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldReturnTrue_WhenBookExists()
        {
            // Arrange
            var book = TestDataFactory.CreateBook("Book to Delete", "Author");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            // Act
            var result = await _bookService.DeleteBookAsync(book.Id);

            // Assert
            result.Should().BeTrue();
            
            // Verify the book was removed from the database
            var deletedBook = await Context.Books.FindAsync(book.Id);
            deletedBook.Should().BeNull();
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldReturnFalse_WhenBookDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _bookService.DeleteBookAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task BookExistsAsync_ShouldReturnTrue_WhenBookExists()
        {
            // Arrange
            var book = TestDataFactory.CreateBook("Existing Book", "Existing Author");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            // Act
            var result = await _bookService.BookExistsAsync("Existing Book", "Existing Author");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task BookExistsAsync_ShouldReturnFalse_WhenBookDoesNotExist()
        {
            // Arrange
            // No books in database

            // Act
            var result = await _bookService.BookExistsAsync("Non-existent Book", "Non-existent Author");

            // Assert
            result.Should().BeFalse();
        }
    }
}
