using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class BookDuplicateFinderTests : InMemoryDbTestBase
    {
        private Book AddBook(string title = "Some Book", string author = "Some Author")
        {
            var book = TestDataFactory.CreateBook(title, author);
            Context.Books.Add(book);
            return book;
        }

        [Fact]
        public async Task FindExistingAsync_MatchesOnReadwiseBookId_BeforeAnythingElse()
        {
            // Arrange: decoy matches title+author; target matches only the Readwise id
            var decoy = AddBook("Shared Title", "Shared Author");
            var target = AddBook("Different Title", "Different Author");
            target.ReadwiseBookId = 42;
            await Context.SaveChangesAsync();

            // Act
            var match = await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity
            {
                ReadwiseBookId = 42,
                Title = "Shared Title",
                Author = "Shared Author"
            });

            // Assert: the id match wins over the title+author decoy
            match!.Id.Should().Be(target.Id);
            match.Id.Should().NotBe(decoy.Id);
        }

        [Fact]
        public async Task FindExistingAsync_MatchesEachExternalId()
        {
            var byGoodreads = AddBook("A", "AA");
            byGoodreads.GoodreadsBookId = 7;
            var byGoogle = AddBook("B", "BB");
            byGoogle.GoogleVolumeId = "vol-1";
            var byOpenLibrary = AddBook("C", "CC");
            byOpenLibrary.OpenLibraryKey = "/works/OL1W";
            await Context.SaveChangesAsync();

            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity { GoodreadsBookId = 7 }))!
                .Id.Should().Be(byGoodreads.Id);
            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity { GoogleVolumeId = "vol-1" }))!
                .Id.Should().Be(byGoogle.Id);
            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity { OpenLibraryKey = "/works/OL1W" }))!
                .Id.Should().Be(byOpenLibrary.Id);
        }

        [Fact]
        public async Task FindExistingAsync_MatchesIsbn_AcrossStoredIsbn10AndIncoming13()
        {
            var legacy = AddBook("Legacy Row", "Legacy Author");
            legacy.ISBN = "0596007124"; // stored as ISBN-10
            await Context.SaveChangesAsync();

            var match = await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity
            {
                Isbn = "978-0-596-00712-6"
            });

            match!.Id.Should().Be(legacy.Id);
        }

        [Fact]
        public async Task FindExistingAsync_MatchesAsin_ThenTitleAuthorCaseInsensitive()
        {
            var byAsin = AddBook("Asin Book", "Asin Author");
            byAsin.ASIN = "B00X4WHP5E";
            var byTitle = AddBook("The Fallback", "Jane Doe");
            await Context.SaveChangesAsync();

            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity { Asin = "B00X4WHP5E" }))!
                .Id.Should().Be(byAsin.Id);
            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity
            {
                Title = "  THE FALLBACK ",
                Author = "jane doe"
            }))!.Id.Should().Be(byTitle.Id);
        }

        [Fact]
        public async Task FindExistingAsync_ReturnsNull_WhenNothingMatches_OrIdentityEmpty()
        {
            AddBook();
            await Context.SaveChangesAsync();

            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity
            {
                ReadwiseBookId = 1,
                Isbn = "9780596007126",
                Title = "No Such",
                Author = "Nobody"
            })).Should().BeNull();

            (await BookDuplicateFinder.FindExistingAsync(Context.Books, new BookIdentity()))
                .Should().BeNull();
        }

        [Fact]
        public void AbsorbIdentity_FillsOnlyMissingValues_AndReportsChange()
        {
            var book = TestDataFactory.CreateBook("Kept", "Author");
            book.GoodreadsBookId = 99;          // already set — must not be overwritten
            book.ISBN = "9780306406157";        // already set — must not be overwritten

            var changed = BookDuplicateFinder.AbsorbIdentity(book, new BookIdentity
            {
                ReadwiseBookId = 5,
                GoodreadsBookId = 100,
                GoogleVolumeId = "vol-x",
                OpenLibraryKey = "/works/OLXW",
                Isbn = "9780596007126",
                Asin = "B000000000"
            });

            changed.Should().BeTrue();
            book.ReadwiseBookId.Should().Be(5);
            book.GoodreadsBookId.Should().Be(99);           // kept
            book.ISBN.Should().Be("9780306406157");         // kept
            book.GoogleVolumeId.Should().Be("vol-x");
            book.OpenLibraryKey.Should().Be("/works/OLXW");
            book.ASIN.Should().Be("B000000000");
        }

        [Fact]
        public void AbsorbIdentity_NormalizesIsbn_WhenFilling()
        {
            var book = TestDataFactory.CreateBook("No Isbn Yet", "Author");
            book.ISBN = null;

            BookDuplicateFinder.AbsorbIdentity(book, new BookIdentity { Isbn = "0-596-00712-4" });

            book.ISBN.Should().Be("9780596007126");
        }

        [Fact]
        public void AbsorbIdentity_ReturnsFalse_WhenNothingToFill()
        {
            var book = TestDataFactory.CreateBook("Full", "Author");
            book.ReadwiseBookId = 1;

            var changed = BookDuplicateFinder.AbsorbIdentity(book, new BookIdentity { ReadwiseBookId = 2 });

            changed.Should().BeFalse();
            book.ReadwiseBookId.Should().Be(1);
        }
    }
}
