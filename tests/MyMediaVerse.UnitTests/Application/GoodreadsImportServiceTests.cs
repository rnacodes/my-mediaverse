using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class GoodreadsImportServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<GoodreadsImportService> _mockLogger;
        private readonly GoodreadsImportService _service;

        public GoodreadsImportServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<GoodreadsImportService>>();
            _service = new GoodreadsImportService(Context, _mockLogger);
        }

        #region MapShelfToStatus

        [Theory]
        [InlineData("to-read", Status.Uncharted)]
        [InlineData("currently-reading", Status.ActivelyExploring)]
        [InlineData("read", Status.Completed)]
        [InlineData("to-be-continued", Status.Abandoned)]
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
        [InlineData("Audiobook", BookFormat.Audiobook)]
        [InlineData("Audible Audio", BookFormat.Audiobook)]
        [InlineData("Audio CD", BookFormat.Audiobook)]
        [InlineData("Audio Cassette", BookFormat.Audiobook)]
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

        [Fact]
        public async Task ImportFromCsvAsync_DuplicateRowInSameFile_CreatesOnlyOne()
        {
            // The same book appearing twice in one CSV must not produce two rows: the second
            // occurrence should dedup against the one created earlier in the same run.
            var csv = CsvStream(
                Row("Dune", "Frank Herbert") + "\n" +
                Row("Dune", "Frank Herbert"));

            var result = await _service.ImportFromCsvAsync(csv);

            result.CreatedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(1);
            Context.Books.Count(b => b.Title == "Dune").Should().Be(1);
        }

        [Fact]
        public async Task ImportFromCsvAsync_DedupsByBookId_WhenIsbnBlank()
        {
            // Kindle editions frequently ship with a blank ISBN in the export; the stable
            // Goodreads Book Id must still match the existing row even when the title differs
            // (e.g. Goodreads renamed the edition).
            var existingBook = TestDataFactory.CreateBook("Old Edition Title", "Frank Herbert");
            existingBook.GoodreadsBookId = 44444;
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var result = await _service.ImportFromCsvAsync(
                CsvStream(Row("Dune (Kindle Edition)", "Frank Herbert", bookId: "44444")));

            result.UpdatedCount.Should().Be(1);
            result.CreatedCount.Should().Be(0);
            Context.Books.Count().Should().Be(1);
        }

        [Fact]
        public async Task ImportFromCsvAsync_Isbn10And13_CollapseToOneBook()
        {
            // A legacy row storing the ISBN-10 must match an export row carrying the ISBN-13.
            var existingBook = TestDataFactory.CreateBook("The Great Gatsby", "Fitzgerald, F. Scott");
            existingBook.ISBN = "074327356X";
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            var result = await _service.ImportFromCsvAsync(
                CsvStream(Row("The Great Gatsby (2004 ed.)", "F. Scott Fitzgerald", isbn13: "9780743273565")));

            result.UpdatedCount.Should().Be(1);
            result.CreatedCount.Should().Be(0);
            Context.Books.Count().Should().Be(1);
        }

        [Fact]
        public async Task ImportFromCsvAsync_NewBook_StoresBookIdAndNormalizedIsbn()
        {
            await _service.ImportFromCsvAsync(
                CsvStream(Row("New Book", "New Author", isbn: "=\"\"074327356X\"\"", bookId: "98765")));

            var book = Context.Books.Single(b => b.Title == "New Book");
            book.GoodreadsBookId.Should().Be(98765);
            book.ISBN.Should().Be("9780743273565"); // ISBN-10 stored in canonical 13 form
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_BackfillsGoodreadsBookId()
        {
            // A book matched by ISBN that predates the Book Id column acquires the id (fill-only).
            var existingBook = TestDataFactory.CreateBook("The Great Gatsby", "F. Scott Fitzgerald");
            existingBook.ISBN = "9780743273565";
            Context.Books.Add(existingBook);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(Row("The Great Gatsby", "F. Scott Fitzgerald", isbn13: "9780743273565", bookId: "31337")));

            existingBook.GoodreadsBookId.Should().Be(31337);
        }

        [Fact]
        public async Task ImportFromCsvAsync_MoreThanOneBatch_ImportsEveryRecord()
        {
            // 120 distinct books spans multiple 50-record save batches; nothing should be dropped
            // or duplicated across batch boundaries.
            const int count = 120;
            var body = string.Join("\n",
                Enumerable.Range(0, count).Select(i => Row($"Book {i}", "Batch Author")));

            var result = await _service.ImportFromCsvAsync(CsvStream(body));

            result.TotalProcessed.Should().Be(count);
            result.CreatedCount.Should().Be(count);
            result.ErrorCount.Should().Be(0);
            Context.Books.Count().Should().Be(count);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ReimportSameCsv_UpdatesNotDuplicates()
        {
            // Idempotency: re-running the same export updates the existing rows and creates nothing,
            // so a retried/partial import is safe.
            const int count = 60;
            var body = string.Join("\n",
                Enumerable.Range(0, count).Select(i => Row($"Book {i}", "Idem Author")));

            var first = await _service.ImportFromCsvAsync(CsvStream(body));
            first.CreatedCount.Should().Be(count);

            var second = await _service.ImportFromCsvAsync(CsvStream(body));

            second.CreatedCount.Should().Be(0);
            second.UpdatedCount.Should().Be(count);
            Context.Books.Count().Should().Be(count);
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_ChangedShelf_UpdatesStatus()
        {
            // Goodreads is the primary tracker for status: moving a book to "read" in Goodreads and
            // re-importing must update MMV's status.
            var book = TestDataFactory.CreateBook("Project Hail Mary", "Andy Weir");
            book.Status = Status.ActivelyExploring;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("Project Hail Mary", "Andy Weir", exclusiveShelf: "read")));

            book.Status.Should().Be(Status.Completed);
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_ChangedRating_UpdatesRawGoodreadsRatingOnly()
        {
            // Goodreads is the primary tracker for rating: a new star rating flows into the raw
            // GoodreadsRating on re-import. Deriving the MMV Rating enum is deferred to the rating
            // enrichment stage, so import itself leaves Rating untouched.
            var book = TestDataFactory.CreateBook("Dune", "Frank Herbert");
            book.GoodreadsRating = 3m;
            book.Rating = Rating.Neutral;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("Dune", "Frank Herbert", myRating: 5)));

            book.GoodreadsRating.Should().Be(5m);
            book.Rating.Should().Be(Rating.Neutral); // unchanged by import; enrichment does the conversion
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_UnratedExport_PreservesExistingRating()
        {
            // A "My Rating" of 0 means unrated in Goodreads and must not clear an existing rating.
            var book = TestDataFactory.CreateBook("Neuromancer", "William Gibson");
            book.GoodreadsRating = 4m;
            book.Rating = Rating.Like;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("Neuromancer", "William Gibson", myRating: 0)));

            book.GoodreadsRating.Should().Be(4m);
            book.Rating.Should().Be(Rating.Like);
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_PreservesAppEditedFields()
        {
            // Everything except status/rating is fill-only: a re-import must not overwrite values the
            // user edited in the app after the first import (Format, Publisher, review).
            var book = TestDataFactory.CreateBook("The Hobbit", "J.R.R. Tolkien");
            book.Format = BookFormat.Physical;
            book.Publisher = "Hand-Edited Publisher";
            book.MyReview = "Hand-edited review";
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("The Hobbit", "J.R.R. Tolkien",
                    publisher: "Goodreads Publisher", myReview: "Goodreads review",
                    binding: "Kindle Edition")));

            book.Format.Should().Be(BookFormat.Physical);
            book.Publisher.Should().Be("Hand-Edited Publisher");
            book.MyReview.Should().Be("Hand-edited review");
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_BackfillsEmptyFields()
        {
            // Fill-only still backfills a genuinely empty field from Goodreads.
            var book = TestDataFactory.CreateBook("Snow Crash", "Neal Stephenson");
            book.Publisher = null;
            book.ISBN = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("Snow Crash", "Neal Stephenson",
                    isbn13: "9780553380958", publisher: "Bantam")));

            book.Publisher.Should().Be("Bantam");
            book.ISBN.Should().Be("9780553380958");
        }

        [Fact]
        public async Task ImportFromCsvAsync_NewBook_SeedsStatusFormatAndRawRating()
        {
            // Create-path seeds status, format and the RAW Goodreads rating. The MMV Rating enum is
            // derived later in the rating enrichment stage, so import leaves it null.
            var result = await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("A Brand New Book", "New Author",
                    myRating: 4, exclusiveShelf: "read", binding: "Paperback")));

            result.CreatedCount.Should().Be(1);
            var book = Context.Books.Single(b => b.Title == "A Brand New Book");
            book.Status.Should().Be(Status.Completed);
            book.Format.Should().Be(BookFormat.Physical);
            book.GoodreadsRating.Should().Be(4m);
            book.Rating.Should().BeNull();
        }

        [Fact]
        public async Task ImportFromCsvAsync_NewBook_UnratedExport_StoresNoGoodreadsRating()
        {
            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("Unrated Book", "New Author", myRating: 0)));

            Context.Books.Single(b => b.Title == "Unrated Book").GoodreadsRating.Should().BeNull();
        }

        [Fact]
        public async Task ImportFromCsvAsync_NewBook_AudioBinding_SeedsAudiobookFormat()
        {
            await _service.ImportFromCsvAsync(
                CsvStream(DetailedRow("Project Hail Mary", "Andy Weir", binding: "Audible Audio")));

            Context.Books.Single(b => b.Title == "Project Hail Mary").Format.Should().Be(BookFormat.Audiobook);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ReportsContractFields()
        {
            var result = await _service.ImportFromCsvAsync(CsvStream(Row("Dune", "Frank Herbert")));

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("goodreads-import");
            result.ErrorMessage.Should().BeNull();
            result.WarningMessage.Should().BeNull();
            result.StartedAt.Should().NotBe(default);
            result.CompletedAt.Should().NotBeNull();
            result.Duration.Should().NotBeNull();
        }

        [Fact]
        public async Task ImportFromCsvAsync_UnreadableStream_IsFatal()
        {
            // A stream that cannot be read means nothing was imported: success flips, and the reason is
            // in errorMessage rather than buried in the per-row errors list.
            var closed = new MemoryStream();
            closed.Dispose();

            var result = await _service.ImportFromCsvAsync(closed);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().StartWith("CSV parsing error:");
            result.CompletedAt.Should().BeNull();
            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task ImportFromCsvAsync_RowWithoutTitle_IsSkippedAndWarned_NotFatal()
        {
            // A single bad row is a per-item problem: the import completes with success true and a
            // warning summarizing the failures.
            var result = await _service.ImportFromCsvAsync(CsvStream(
                Row("", "Ghost Author") + "\n" +
                Row("Dune", "Frank Herbert")));

            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            result.SkippedCount.Should().Be(1);
        }

        #endregion

        #region Shelves → Topics

        [Fact]
        public async Task ImportFromCsvAsync_NewBook_SubjectShelvesBecomeTopics_StatusShelvesDoNot()
        {
            await _service.ImportFromCsvAsync(CsvStream(
                ShelvedRow("Dune", "Frank Herbert", bookshelves: "read fantasy classics currently-reading", exclusiveShelf: "read")));

            var book = Context.Books.Include(b => b.Topics).Single(b => b.Title == "Dune");
            book.Topics.Select(t => t.Name).Should().BeEquivalentTo("fantasy", "classics");
            book.GoodreadsTags.Should().BeEquivalentTo("read", "fantasy", "classics", "currently-reading"); // raw snapshot untouched
            Context.Topics.Select(t => t.Name).Should().BeEquivalentTo("fantasy", "classics");
        }

        [Fact]
        public async Task ImportFromCsvAsync_Reimport_AddsNewShelfTopic_KeepsAppAddedTopic()
        {
            var book = TestDataFactory.CreateBook("Dune", "Frank Herbert");
            book.Topics.Add(new Topic { Name = "desert-planets" });
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(CsvStream(
                ShelvedRow("Dune", "Frank Herbert", bookshelves: "fantasy")));

            var updated = Context.Books.Include(b => b.Topics).Single(b => b.Id == book.Id);
            updated.Topics.Select(t => t.Name).Should().BeEquivalentTo("desert-planets", "fantasy");
        }

        [Fact]
        public async Task ImportFromCsvAsync_TwoRowsSharingANewShelf_CreateOneTopic()
        {
            await _service.ImportFromCsvAsync(CsvStream(
                ShelvedRow("Dune", "Frank Herbert", bookshelves: "fantasy") + "\n" +
                ShelvedRow("Hyperion", "Dan Simmons", bookshelves: "fantasy")));

            Context.Topics.Count(t => t.Name == "fantasy").Should().Be(1);
            Context.Books.Include(b => b.Topics).All(b => b.Topics.Any(t => t.Name == "fantasy")).Should().BeTrue();
        }

        [Fact]
        public async Task ImportFromCsvAsync_ExistingTopic_IsReusedNotDuplicated()
        {
            Context.Topics.Add(new Topic { Name = "fantasy" });
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(CsvStream(
                ShelvedRow("Dune", "Frank Herbert", bookshelves: "fantasy")));

            Context.Topics.Count(t => t.Name == "fantasy").Should().Be(1);
        }

        [Fact]
        public async Task ImportFromCsvAsync_SkipWhenUpdateDisabled_DoesNotTouchTopics()
        {
            var book = TestDataFactory.CreateBook("Dune", "Frank Herbert");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.ImportFromCsvAsync(
                CsvStream(ShelvedRow("Dune", "Frank Herbert", bookshelves: "fantasy")), updateExisting: false);

            Context.Books.Include(b => b.Topics).Single(b => b.Id == book.Id).Topics.Should().BeEmpty();
        }

        #endregion

        #region CSV helpers

        // Row with control over the Bookshelves and Exclusive Shelf columns for the shelves → topics tests.
        private static string ShelvedRow(string title, string author, string bookshelves, string exclusiveShelf = "read") =>
            $"\"\",\"{title}\",\"{author}\",\"\",\"\",0,3.50,\"Pub\",2000,2000,,,\"{bookshelves}\",\"{exclusiveShelf}\",\"\",\"Paperback\"";

        private const string GoodreadsHeader =
            "Book Id,Title,Author,ISBN,ISBN13,My Rating,Average Rating,Publisher,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Exclusive Shelf,My Review,Binding";

        private static MemoryStream CsvStream(string body) =>
            new(System.Text.Encoding.UTF8.GetBytes(GoodreadsHeader + "\n" + body));

        private static string Row(string title, string author, string isbn = "", string isbn13 = "", string bookId = "") =>
            $"\"{bookId}\",\"{title}\",\"{author}\",\"{isbn}\",\"{isbn13}\",0,3.50,\"Pub\",2000,2000,,,\"shelf\",\"read\",\"\",\"Paperback\"";

        // Row with per-field control for the re-import policy tests. Column order matches GoodreadsHeader.
        private static string DetailedRow(
            string title, string author, string isbn = "", string isbn13 = "",
            int myRating = 0, string publisher = "Pub", string dateRead = "",
            string exclusiveShelf = "read", string myReview = "", string binding = "Paperback",
            string bookId = "") =>
            $"\"{bookId}\",\"{title}\",\"{author}\",\"{isbn}\",\"{isbn13}\",{myRating},3.50,\"{publisher}\",2000,2000,\"{dateRead}\",,\"shelf\",\"{exclusiveShelf}\",\"{myReview}\",\"{binding}\"";

        #endregion
    }
}
