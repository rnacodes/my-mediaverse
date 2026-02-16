using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectLoopbreaker.Application.Services;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.UnitTests.TestData;
using ProjectLoopbreaker.UnitTests.TestHelpers;

namespace ProjectLoopbreaker.UnitTests.Application
{
    public class GoodreadsImportServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ILogger<GoodreadsImportService>> _mockLogger;
        private readonly GoodreadsImportService _service;

        public GoodreadsImportServiceTests()
        {
            _mockLogger = new Mock<ILogger<GoodreadsImportService>>();
            _service = new GoodreadsImportService(Context, _mockLogger.Object, null);
        }

        #region MapShelfToStatus

        [Theory]
        [InlineData("to-read", Status.Uncharted)]
        [InlineData("currently-reading", Status.ActivelyExploring)]
        [InlineData("read", Status.Completed)]
        [InlineData("to be continued", Status.Abandoned)]
        [InlineData("TO-READ", Status.Uncharted)]
        [InlineData("Read", Status.Completed)]
        [InlineData("unknown-shelf", Status.Uncharted)]
        public void MapShelfToStatus_VariousShelves_MapsCorrectly(string shelf, Status expected)
        {
            var result = _service.MapShelfToStatus(shelf);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void MapShelfToStatus_NullOrEmpty_ReturnsUncharted(string? shelf)
        {
            var result = _service.MapShelfToStatus(shelf);
            result.Should().Be(Status.Uncharted);
        }

        #endregion

        #region MapMyRatingToPlbRating

        [Theory]
        [InlineData(5, Rating.SuperLike)]
        [InlineData(4, Rating.Like)]
        [InlineData(3, Rating.Neutral)]
        [InlineData(2, Rating.Dislike)]
        [InlineData(1, Rating.Dislike)]
        public void MapMyRatingToPlbRating_ValidRatings_MapsCorrectly(int rating, Rating expected)
        {
            var result = _service.MapMyRatingToPlbRating(rating);
            result.Should().Be(expected);
        }

        [Fact]
        public void MapMyRatingToPlbRating_Zero_ReturnsNull()
        {
            var result = _service.MapMyRatingToPlbRating(0);
            result.Should().BeNull();
        }

        [Fact]
        public void MapMyRatingToPlbRating_Null_ReturnsNull()
        {
            var result = _service.MapMyRatingToPlbRating(null);
            result.Should().BeNull();
        }

        #endregion

        #region MapBindingToFormat

        [Theory]
        [InlineData("Paperback", BookFormat.Physical)]
        [InlineData("Hardcover", BookFormat.Physical)]
        [InlineData("hardback", BookFormat.Physical)]
        [InlineData("Mass Market Paperback", BookFormat.Physical)]
        [InlineData("Kindle Edition", BookFormat.Digital)]
        [InlineData("ebook", BookFormat.Digital)]
        [InlineData("Audiobook", BookFormat.Digital)]
        public void MapBindingToFormat_VariousBindings_MapsCorrectly(string binding, BookFormat expected)
        {
            var result = _service.MapBindingToFormat(binding);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void MapBindingToFormat_NullOrEmpty_DefaultsToDigital(string? binding)
        {
            var result = _service.MapBindingToFormat(binding);
            result.Should().Be(BookFormat.Digital);
        }

        #endregion

        #region ParseBookshelves

        [Fact]
        public void ParseBookshelves_ValidBookshelves_ParsesAndLowercases()
        {
            var result = _service.ParseBookshelves("Fantasy Sci-Fi Classics");

            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo("fantasy", "sci-fi", "classics");
        }

        [Fact]
        public void ParseBookshelves_Duplicates_Deduplicates()
        {
            var result = _service.ParseBookshelves("fantasy FANTASY Fantasy");

            result.Should().HaveCount(1);
            result.Should().Contain("fantasy");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseBookshelves_NullOrEmpty_ReturnsEmptyList(string? bookshelves)
        {
            var result = _service.ParseBookshelves(bookshelves);
            result.Should().BeEmpty();
        }

        #endregion

        #region FindExistingBookAsync

        [Fact]
        public async Task FindExistingBookAsync_MatchByIsbn_ReturnsBook()
        {
            var book = TestDataFactory.CreateBook("Existing Book");
            book.ISBN = "9780123456789";
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.FindExistingBookAsync("978-012-345-6789", "Different Title", "Different Author");

            result.Should().NotBeNull();
            result!.Id.Should().Be(book.Id);
        }

        [Fact]
        public async Task FindExistingBookAsync_MatchByTitleAndAuthor_CaseInsensitive()
        {
            var book = TestDataFactory.CreateBook("The Great Gatsby", "F. Scott Fitzgerald");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.FindExistingBookAsync(null, "the great gatsby", "f. scott fitzgerald");

            result.Should().NotBeNull();
            result!.Id.Should().Be(book.Id);
        }

        [Fact]
        public async Task FindExistingBookAsync_NoMatch_ReturnsNull()
        {
            var result = await _service.FindExistingBookAsync(null, "Nonexistent Book", "Unknown Author");
            result.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingBookAsync_NullIsbn_FallsBackToTitleAuthor()
        {
            var book = TestDataFactory.CreateBook("Test Book", "Test Author");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.FindExistingBookAsync(null, "Test Book", "Test Author");

            result.Should().NotBeNull();
            result!.Id.Should().Be(book.Id);
        }

        #endregion

        #region ImportFromCsvAsync

        [Fact]
        public async Task ImportFromCsvAsync_ValidCsv_ImportsBooks()
        {
            var csvContent = "Title,Author,ISBN,ISBN13,My Rating,Average Rating,Publisher,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Exclusive Shelf,My Review,Binding\n" +
                             "\"The Great Gatsby\",\"F. Scott Fitzgerald\",\"=\"\"074327356X\"\"\",\"=\"\"9780743273565\"\"\",5,3.93,\"Scribner\",2004,1925,2024/01/15,2023/12/01,\"classics fiction\",\"read\",\"Amazing book\",\"Paperback\"";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ImportFromCsvAsync(stream);

            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(1);
            result.CreatedCount.Should().Be(1);
            result.ErrorCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportFromCsvAsync_EmptyCsv_ReturnsZero()
        {
            var csvContent = "Title,Author,ISBN,ISBN13,My Rating,Average Rating,Publisher,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Exclusive Shelf,My Review,Binding\n";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ImportFromCsvAsync(stream);

            result.TotalProcessed.Should().Be(0);
            result.CreatedCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ExistingBook_UpdatesWhenEnabled()
        {
            var existingBook = TestDataFactory.CreateBook("The Great Gatsby", "F. Scott Fitzgerald");
            existingBook.ISBN = "9780743273565";
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var csvContent = "Title,Author,ISBN,ISBN13,My Rating,Average Rating,Publisher,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Exclusive Shelf,My Review,Binding\n" +
                             "\"The Great Gatsby\",\"F. Scott Fitzgerald\",\"\",\"=\"\"9780743273565\"\"\",5,3.93,\"Scribner\",2004,1925,,,,\"read\",,\"Paperback\"";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ImportFromCsvAsync(stream, updateExisting: true);

            result.UpdatedCount.Should().Be(1);
            result.CreatedCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ExistingBook_SkipsWhenUpdateDisabled()
        {
            var existingBook = TestDataFactory.CreateBook("The Great Gatsby", "F. Scott Fitzgerald");
            existingBook.ISBN = "9780743273565";
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var csvContent = "Title,Author,ISBN,ISBN13,My Rating,Average Rating,Publisher,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Exclusive Shelf,My Review,Binding\n" +
                             "\"The Great Gatsby\",\"F. Scott Fitzgerald\",\"\",\"=\"\"9780743273565\"\"\",5,3.93,\"Scribner\",2004,1925,,,,\"read\",,\"Paperback\"";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ImportFromCsvAsync(stream, updateExisting: false);

            result.SkippedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(0);
        }

        #endregion
    }
}
