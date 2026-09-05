using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class BookServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<BookService> _mockLogger;
        private readonly ITypesenseService _mockTypesense;
        private readonly BookService _bookService;

        public BookServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<BookService>>();
            _mockTypesense = Substitute.For<ITypesenseService>();
            _bookService = new BookService(Context, _mockTypesense, _mockLogger);
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
            result.Created.Should().BeTrue();
            result.Book.Title.Should().Be(dto.Title);
            result.Book.Author.Should().Be(dto.Author);
            result.Book.MediaType.Should().Be(MediaType.Book);

            // Verify the book was saved to the database
            var savedBook = await Context.Books.FindAsync(result.Book.Id);
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
            result.Created.Should().BeFalse();
            result.Book.Id.Should().Be(existingBook.Id);

            // Verify no duplicate was created
            var allBooks = Context.Books.ToList();
            allBooks.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateBookAsync_ShouldMatchExistingBook_ByIsbnAcross10And13Forms()
        {
            // Arrange: existing row stores the ISBN-10 form (legacy data)
            var existingBook = TestDataFactory.CreateBook("Some Edition", "Some Author");
            existingBook.ISBN = "0596007124";
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            // Incoming book carries the equivalent ISBN-13 under a different title
            var dto = TestDataFactory.CreateBookDto("Some Edition (Anniversary)", "Some Author");
            dto.ISBN = "978-0-596-00712-6";

            // Act
            var result = await _bookService.CreateBookAsync(dto);

            // Assert
            result.Created.Should().BeFalse();
            result.Book.Id.Should().Be(existingBook.Id);
            Context.Books.ToList().Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateBookAsync_ShouldAbsorbExternalIds_OntoExistingMatch()
        {
            // Arrange
            var existingBook = TestDataFactory.CreateBook("Known Book", "Known Author");
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto("Known Book", "Known Author");
            var identity = new BookIdentity
            {
                GoogleVolumeId = "vol-123",
                OpenLibraryKey = "/works/OL1W",
                Isbn = "0596007124"
            };

            // Act
            var result = await _bookService.CreateBookAsync(dto, identity);

            // Assert: title+author matched, and the ids were filled onto the existing row
            result.Created.Should().BeFalse();
            Context.ChangeTracker.Clear();
            var saved = await Context.Books.FindAsync(existingBook.Id);
            saved!.GoogleVolumeId.Should().Be("vol-123");
            saved.OpenLibraryKey.Should().Be("/works/OL1W");
            saved.ISBN.Should().Be("9780596007126");
        }

        [Fact]
        public async Task CreateBookAsync_ShouldPersistExternalIds_AndNormalizedIsbn_OnCreate()
        {
            // Arrange
            var dto = TestDataFactory.CreateBookDto("Fresh Book", "Fresh Author");
            dto.ISBN = "0-596-00712-4";
            var identity = new BookIdentity
            {
                GoogleVolumeId = "vol-9",
                OpenLibraryKey = "/works/OL9W"
            };

            // Act
            var result = await _bookService.CreateBookAsync(dto, identity);

            // Assert
            result.Created.Should().BeTrue();
            Context.ChangeTracker.Clear();
            var saved = await Context.Books.FindAsync(result.Book.Id);
            saved!.ISBN.Should().Be("9780596007126");
            saved.GoogleVolumeId.Should().Be("vol-9");
            saved.OpenLibraryKey.Should().Be("/works/OL9W");
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
        public async Task CreateBookAsync_ShouldMapExtendedFields()
        {
            // Arrange
            var dto = TestDataFactory.CreateBookDto("Extended Book", "Extended Author");
            dto.Publisher = "Penguin";
            dto.YearPublished = 2014;
            dto.DateRead = new DateTime(2020, 1, 1);
            dto.MyReview = "A thorough review.";

            // Act
            var result = await _bookService.CreateBookAsync(dto);

            // Assert
            Context.ChangeTracker.Clear();
            var saved = await Context.Books.FindAsync(result.Book.Id);
            saved.Should().NotBeNull();
            saved!.Publisher.Should().Be("Penguin");
            saved.YearPublished.Should().Be(2014);
            saved.DateRead.Should().Be(new DateTime(2020, 1, 1));
            saved.MyReview.Should().Be("A thorough review.");
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldMapExtendedFields()
        {
            // Arrange
            var existingBook = TestDataFactory.CreateBook("Original", "Author");
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto("Original", "Author");
            dto.Publisher = "Tor";
            dto.YearPublished = 1999;
            dto.DateRead = new DateTime(2019, 6, 15);
            dto.MyReview = "Updated review.";

            // Act
            await _bookService.UpdateBookAsync(existingBook.Id, dto);

            // Assert
            Context.ChangeTracker.Clear();
            var updated = await Context.Books.FindAsync(existingBook.Id);
            updated.Should().NotBeNull();
            updated!.Publisher.Should().Be("Tor");
            updated.YearPublished.Should().Be(1999);
            updated.DateRead.Should().Be(new DateTime(2019, 6, 15));
            updated.MyReview.Should().Be("Updated review.");
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldReturnNull_WhenBookDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = TestDataFactory.CreateBookDto("Updated Title", "Updated Author");

            // Act
            var result = await _bookService.UpdateBookAsync(nonExistentId, dto);

            // Assert: a typed "not found" the controller turns into a 404, not an exception to parse.
            result.Should().BeNull();
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
            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task DeleteBookAsync_RemovesTheSearchDocument()
        {
            var book = TestDataFactory.CreateBook("Indexed Book", "Author");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _bookService.DeleteBookAsync(book.Id);

            await _mockTypesense.Received(1).DeleteMediaItemAsync(book.Id);
        }

        [Fact]
        public async Task DeleteBookAsync_SearchIndexFailure_DoesNotAbortTheDelete()
        {
            var book = TestDataFactory.CreateBook("Ghost Candidate", "Author");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockTypesense.DeleteMediaItemAsync(book.Id)
                .Returns<Task>(_ => throw new InvalidOperationException("Typesense down"));

            var result = await _bookService.DeleteBookAsync(book.Id);

            result.Should().BeTrue();
            (await Context.Books.FindAsync(book.Id)).Should().BeNull();
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldNormalizeIsbn()
        {
            // Arrange
            var existingBook = TestDataFactory.CreateBook("Original", "Author");
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto("Original", "Author");
            dto.ISBN = "0-596-00712-4";

            // Act
            await _bookService.UpdateBookAsync(existingBook.Id, dto);

            // Assert
            Context.ChangeTracker.Clear();
            var updated = await Context.Books.FindAsync(existingBook.Id);
            updated!.ISBN.Should().Be("9780596007126");
        }
    }
}
