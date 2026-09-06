using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The Google Books client is substituted; no real HTTP. Books are looked up by ISBN when they
    /// have one and by title plus author otherwise, and every write is fill-only.
    /// </summary>
    [Trait("Category", "Unit")]
    public class BookDescriptionEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly IGoogleBooksApiClient _mockGoogleBooksClient;
        private readonly ILogger<BookDescriptionEnrichmentService> _mockLogger;
        private readonly BookDescriptionEnrichmentService _service;

        public BookDescriptionEnrichmentServiceTests()
        {
            _mockGoogleBooksClient = Substitute.For<IGoogleBooksApiClient>();
            _mockLogger = Substitute.For<ILogger<BookDescriptionEnrichmentService>>();
            _service = new BookDescriptionEnrichmentService(Context, _mockGoogleBooksClient, _mockLogger);
        }

        #region Helpers

        private static GoogleBooksSearchResultDto Results(params GoogleBooksVolumeDto[] volumes) =>
            new() { TotalItems = volumes.Length, Items = volumes };

        private static GoogleBooksVolumeDto Volume(
            string id = "vol-1",
            string title = "Test Book",
            string[]? authors = null,
            string? description = "<p>A fascinating book about <b>technology</b>.</p>",
            string? isbn13 = null,
            string? isbn10 = null,
            string? publisher = null,
            string? publishedDate = null,
            string? thumbnail = null)
        {
            var identifiers = new List<GoogleBooksIndustryIdentifierDto>();
            if (isbn13 != null) identifiers.Add(new GoogleBooksIndustryIdentifierDto { Type = "ISBN_13", Identifier = isbn13 });
            if (isbn10 != null) identifiers.Add(new GoogleBooksIndustryIdentifierDto { Type = "ISBN_10", Identifier = isbn10 });

            return new GoogleBooksVolumeDto
            {
                Id = id,
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = title,
                    Authors = authors ?? new[] { "Test Author" },
                    Description = description,
                    IndustryIdentifiers = identifiers.Count > 0 ? identifiers.ToArray() : null,
                    Publisher = publisher,
                    PublishedDate = publishedDate,
                    ImageLinks = thumbnail != null ? new GoogleBooksImageLinksDto { Thumbnail = thumbnail } : null
                }
            };
        }

        private void IsbnLookupReturns(string isbn, params GoogleBooksVolumeDto[] volumes) =>
            _mockGoogleBooksClient.SearchBooksByISBNAsync(isbn).Returns(Results(volumes));

        private void TitleAuthorLookupReturns(string title, string author, params GoogleBooksVolumeDto[] volumes) =>
            _mockGoogleBooksClient.SearchBooksAsync($"intitle:{title} inauthor:{author}", Arg.Any<int?>(), Arg.Any<int?>())
                .Returns(Results(volumes));

        private async Task<Book> SeedBook(string title = "Test Book", string author = "Test Author", string? isbn = null, string? description = null)
        {
            var book = TestDataFactory.CreateBook(title, author);
            book.ISBN = isbn;
            book.Description = description;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();
            return book;
        }

        #endregion

        #region GetBooksNeedingEnrichmentCountAsync

        [Fact]
        public async Task GetBooksNeedingEnrichmentCountAsync_NoBooks_ReturnsZero()
        {
            var result = await _service.GetBooksNeedingEnrichmentCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetBooksNeedingEnrichmentCountAsync_CountsBooksWithALookupKeyAndNoDescription()
        {
            await SeedBook("Has ISBN", isbn: "9780123456789");                              // counted: ISBN
            await SeedBook("Title and author", author: "Frank Herbert");                    // counted: title+author
            await SeedBook("Already enriched", isbn: "9780987654321", description: "Done"); // not counted
            await SeedBook("Empty description", author: "Frank Herbert", description: ""); // counted: "" is missing
            await SeedBook("Placeholder author", author: "Unknown Author");                 // not counted: no guardable author

            var result = await _service.GetBooksNeedingEnrichmentCountAsync();

            result.Should().Be(3);
        }

        #endregion

        #region EnrichBookByIdAsync

        [Fact]
        public async Task EnrichBookByIdAsync_BookNotFound_ReturnsNotFound()
        {
            var result = await _service.EnrichBookByIdAsync(Guid.NewGuid());

            result.NotFound.Should().BeTrue();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_AlreadyHasDescription_ReturnsAlreadyHasDescription()
        {
            var book = await SeedBook(isbn: "9780123456789", description: "Existing description");

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.AlreadyHasDescription.Should().BeTrue();
            await _mockGoogleBooksClient.DidNotReceive().SearchBooksByISBNAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task EnrichBookByIdAsync_NoIsbnAndPlaceholderAuthor_ReturnsNoLookupKey()
        {
            var book = await SeedBook(author: "Unknown Author", isbn: null);

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.NoLookupKey.Should().BeTrue();
            result.Success.Should().BeFalse();
            await _mockGoogleBooksClient.DidNotReceive().SearchBooksAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>());
        }

        [Fact]
        public async Task EnrichBookByIdAsync_ByIsbn_FillsDescriptionAndExternalIdentity()
        {
            var book = await SeedBook(isbn: "9780123456789");
            IsbnLookupReturns("9780123456789", Volume(
                id: "vol-abc",
                isbn13: "9780123456789",
                publisher: "Chilton Books",
                publishedDate: "1965-08-01",
                thumbnail: "http://books.google.com/thumb.jpg"));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeTrue();
            result.Description.Should().Be("A fascinating book about technology.");
            result.FilledFields.Should().BeEquivalentTo(new[] { "description", "googleVolumeId", "publisher", "yearPublished", "thumbnail" });

            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.Description.Should().Be("A fascinating book about technology.");
            updated.GoogleVolumeId.Should().Be("vol-abc");
            updated.Publisher.Should().Be("Chilton Books");
            updated.YearPublished.Should().Be(1965);
            updated.Thumbnail.Should().Be("https://books.google.com/thumb.jpg");
            updated.EnrichedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_NoIsbn_FallsBackToTitleAndAuthorAndFillsIsbn13()
        {
            var book = await SeedBook("Dune", "Frank Herbert", isbn: null);
            TitleAuthorLookupReturns("Dune", "Frank Herbert", Volume(
                id: "vol-dune",
                title: "Dune",
                authors: new[] { "Frank Herbert" },
                isbn10: "0441013597"));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeTrue();
            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.ISBN.Should().Be("9780441013593"); // ISBN-10 normalized to ISBN-13
            updated.GoogleVolumeId.Should().Be("vol-dune");
            await _mockGoogleBooksClient.DidNotReceive().SearchBooksByISBNAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task EnrichBookByIdAsync_TitleAuthorFallback_RejectsVolumeByDifferentAuthor()
        {
            var book = await SeedBook("Dune", "Frank Herbert", isbn: null);
            TitleAuthorLookupReturns("Dune", "Frank Herbert",
                Volume(id: "wrong", title: "Dune", authors: new[] { "Brian Herbert", "Kevin J. Anderson" }),
                Volume(id: "right", title: "Dune", authors: new[] { "FRANK HERBERT " }, description: "The right one."));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeTrue();
            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.Description.Should().Be("The right one.");
            updated.GoogleVolumeId.Should().Be("right");
        }

        [Fact]
        public async Task EnrichBookByIdAsync_TitleAuthorFallback_NoAuthorMatch_WritesNothing()
        {
            var book = await SeedBook("Dune", "Frank Herbert", isbn: null);
            TitleAuthorLookupReturns("Dune", "Frank Herbert",
                Volume(id: "wrong", title: "Dune", authors: new[] { "Brian Herbert" }));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeFalse();
            result.NoLookupKey.Should().BeFalse();
            result.FilledFields.Should().BeEmpty();
            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.Description.Should().BeNull();
            updated.GoogleVolumeId.Should().BeNull();
            updated.EnrichedAt.Should().BeNull();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_IsbnMiss_FallsThroughToTitleAndAuthor()
        {
            var book = await SeedBook("Dune", "Frank Herbert", isbn: "9780123456789");
            IsbnLookupReturns("9780123456789"); // empty result
            TitleAuthorLookupReturns("Dune", "Frank Herbert", Volume(authors: new[] { "Frank Herbert" }));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_FillOnly_NeverOverwritesExistingValues()
        {
            var book = await SeedBook(isbn: "9780123456789");
            book.Publisher = "Original Publisher";
            book.Thumbnail = "https://cdn.example.com/original.jpg";
            book.YearPublished = 1999;
            book.GoogleVolumeId = "already-set";
            await Context.SaveChangesAsync();
            IsbnLookupReturns("9780123456789", Volume(
                id: "vol-new", publisher: "Other", publishedDate: "2001", thumbnail: "http://books.google.com/new.jpg"));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.FilledFields.Should().BeEquivalentTo(new[] { "description" });
            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.Publisher.Should().Be("Original Publisher");
            updated.Thumbnail.Should().Be("https://cdn.example.com/original.jpg");
            updated.YearPublished.Should().Be(1999);
            updated.GoogleVolumeId.Should().Be("already-set");
        }

        [Fact]
        public async Task EnrichBookByIdAsync_GoogleVolumeIdHeldByAnotherBook_IsNotCopied()
        {
            var other = await SeedBook("Other Book", isbn: "9780000000000", description: "Done");
            other.GoogleVolumeId = "shared-vol";
            await Context.SaveChangesAsync();
            var book = await SeedBook(isbn: "9780123456789");
            IsbnLookupReturns("9780123456789", Volume(id: "shared-vol"));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeTrue();
            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.Description.Should().NotBeNull();
            updated.GoogleVolumeId.Should().BeNull();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_VolumeWithoutDescription_StillFillsIdsButIsNotSuccess()
        {
            var book = await SeedBook(isbn: "9780123456789");
            IsbnLookupReturns("9780123456789", Volume(id: "vol-nodesc", description: null, publisher: "Pub"));

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeFalse();
            result.FilledFields.Should().BeEquivalentTo(new[] { "googleVolumeId", "publisher" });
            var updated = Context.Books.First(b => b.Id == book.Id);
            updated.GoogleVolumeId.Should().Be("vol-nodesc");
            updated.Description.Should().BeNull();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_ApiReturnsNothing_ReturnsFailure()
        {
            var book = await SeedBook(isbn: "9780123456789");
            IsbnLookupReturns("9780123456789");

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region EnrichBooksWithoutDescriptionsAsync

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_NoBooksNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.EnrichBooksWithoutDescriptionsAsync();

            result.TotalProcessed.Should().Be(0);
            result.EnrichedCount.Should().Be(0);
            result.Success.Should().BeTrue();
            result.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_WithBooks_EnrichesAndReturnsContractShapedResult()
        {
            await SeedBook(isbn: "9780123456789");
            _mockGoogleBooksClient.SearchBooksByISBNAsync(Arg.Any<string>()).Returns(Results(Volume()));

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("book-description-enrichment");
            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.WarningMessage.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.StartedAt.Should().NotBe(default);
            result.CompletedAt.Should().NotBeNull();
            result.Duration.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_PicksUpStubBooksWithoutIsbn()
        {
            // A book created from a Readwise highlight stub: title + author, no ISBN, no description.
            var stub = await SeedBook("Dune", "Frank Herbert", isbn: null);
            TitleAuthorLookupReturns("Dune", "Frank Herbert", Volume(id: "vol-dune", authors: new[] { "Frank Herbert" }, isbn13: "9780441013593"));

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.EnrichedCount.Should().Be(1);
            var updated = Context.Books.First(b => b.Id == stub.Id);
            updated.ISBN.Should().Be("9780441013593");
            updated.GoogleVolumeId.Should().Be("vol-dune");
            updated.Description.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_ApiFailsForSome_ContinuesAndWarns()
        {
            await SeedBook("Book 1", isbn: "9780000000001");
            await SeedBook("Book 2", isbn: "9780000000002");
            _mockGoogleBooksClient.SearchBooksByISBNAsync("9780000000001").Throws(new Exception("API error"));
            IsbnLookupReturns("9780000000002", Volume());

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.Success.Should().BeTrue("per-item failures never flip the fatal flag");
            result.TotalProcessed.Should().Be(2);
            result.EnrichedCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().Contain("Book 1");
            result.WarningMessage.Should().Contain("1 of 2");
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_NoMatch_CountsNotFoundNotFailed()
        {
            await SeedBook(isbn: "9780123456789");
            IsbnLookupReturns("9780123456789");

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.WarningMessage.Should().BeNull();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_RespectsBatchSize()
        {
            for (int i = 0; i < 5; i++)
            {
                await SeedBook($"Book {i}", isbn: $"978000000000{i}");
            }
            _mockGoogleBooksClient.SearchBooksByISBNAsync(Arg.Any<string>()).Returns(Results(Volume()));

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 2, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(2);
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_RespectsCancellation()
        {
            await SeedBook(isbn: "9780123456789");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(cancellationToken: cts.Token);

            result.WasCancelled.Should().BeTrue();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_NormalizesIsbnForLookup()
        {
            await SeedBook(isbn: "978-0-12-345678-9");
            IsbnLookupReturns("9780123456789", Volume());

            await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            await _mockGoogleBooksClient.Received(1).SearchBooksByISBNAsync("9780123456789");
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_WholeRunThrows_ReportsFatal()
        {
            // A failure before the loop starts (e.g. the database is unreachable) is fatal.
            var throwingContext = Substitute.For<MyMediaVerse.Application.Interfaces.IApplicationDbContext>();
            throwingContext.Books.Returns(_ => throw new InvalidOperationException("database unreachable"));
            var service = new BookDescriptionEnrichmentService(throwingContext, _mockGoogleBooksClient, _mockLogger);

            var result = await service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("database unreachable");
            result.CompletedAt.Should().BeNull();
        }

        #endregion

        #region EnrichedAt stamping

        [Fact]
        public async Task EnrichBookByIdAsync_ApiReturnsNothing_DoesNotStampEnrichedAt()
        {
            var book = await SeedBook(isbn: "9780123456789");
            book.EnrichedAt = null;
            await Context.SaveChangesAsync();
            IsbnLookupReturns("9780123456789");

            await _service.EnrichBookByIdAsync(book.Id);

            Context.Books.First(b => b.Id == book.Id).EnrichedAt.Should().BeNull();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_OnSuccess_StampsEnrichedAt()
        {
            var book = await SeedBook(isbn: "9780123456789");
            _mockGoogleBooksClient.SearchBooksByISBNAsync(Arg.Any<string>()).Returns(Results(Volume()));

            await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            Context.Books.First(b => b.Id == book.Id).EnrichedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_SecondRun_DoesNotRetouchAlreadyEnriched()
        {
            var book = await SeedBook(isbn: "9780123456789");
            _mockGoogleBooksClient.SearchBooksByISBNAsync(Arg.Any<string>()).Returns(Results(Volume()));

            await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);
            var firstStamp = Context.Books.First(b => b.Id == book.Id).EnrichedAt;

            // The book now has a description, so it's no longer a candidate — a second run must leave
            // both the description and the original EnrichedAt stamp untouched (fill-gaps-only).
            var secondResult = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            secondResult.TotalProcessed.Should().Be(0);
            Context.Books.First(b => b.Id == book.Id).EnrichedAt.Should().Be(firstStamp);
        }

        #endregion
    }
}
