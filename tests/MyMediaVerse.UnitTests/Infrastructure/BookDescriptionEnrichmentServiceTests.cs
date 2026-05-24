using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
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

        #region GetBooksNeedingEnrichmentCountAsync

        [Fact]
        public async Task GetBooksNeedingEnrichmentCountAsync_NoBooks_ReturnsZero()
        {
            var result = await _service.GetBooksNeedingEnrichmentCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetBooksNeedingEnrichmentCountAsync_BooksWithIsbnAndNoDescription_ReturnsCount()
        {
            var bookNeedsEnrichment = TestDataFactory.CreateBook("Book 1");
            bookNeedsEnrichment.ISBN = "9780123456789";
            bookNeedsEnrichment.Description = null;

            var bookAlreadyHasDescription = TestDataFactory.CreateBook("Book 2");
            bookAlreadyHasDescription.ISBN = "9780987654321";
            bookAlreadyHasDescription.Description = "Already enriched";

            var bookNoIsbn = TestDataFactory.CreateBook("Book 3");
            bookNoIsbn.ISBN = null;
            bookNoIsbn.Description = null;

            Context.Books.AddRange(bookNeedsEnrichment, bookAlreadyHasDescription, bookNoIsbn);
            await Context.SaveChangesAsync();

            var result = await _service.GetBooksNeedingEnrichmentCountAsync();

            result.Should().Be(1);
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
            var book = TestDataFactory.CreateBook("Test Book");
            book.ISBN = "9780123456789";
            book.Description = "Existing description";
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.AlreadyHasDescription.Should().BeTrue();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_NoIsbn_ReturnsNoIsbn()
        {
            var book = TestDataFactory.CreateBook("Test Book");
            book.ISBN = null;
            book.Description = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.NoIsbn.Should().BeTrue();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task EnrichBookByIdAsync_ValidBook_EnrichesDescription()
        {
            var book = TestDataFactory.CreateBook("Test Book");
            book.ISBN = "9780123456789";
            book.Description = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync("9780123456789")
                .Returns("A fascinating book about technology.");

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeTrue();
            result.Description.Should().Be("A fascinating book about technology.");

            var updatedBook = Context.Books.First(b => b.Id == book.Id);
            updatedBook.Description.Should().Be("A fascinating book about technology.");
        }

        [Fact]
        public async Task EnrichBookByIdAsync_ApiReturnsNull_ReturnsFailure()
        {
            var book = TestDataFactory.CreateBook("Test Book");
            book.ISBN = "9780123456789";
            book.Description = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync("9780123456789")
                .Returns((string?)null);

            var result = await _service.EnrichBookByIdAsync(book.Id);

            result.Success.Should().BeFalse();
        }

        #endregion

        #region EnrichBooksWithoutDescriptionsAsync

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_NoBooksNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.EnrichBooksWithoutDescriptionsAsync();

            result.TotalProcessed.Should().Be(0);
            result.EnrichedCount.Should().Be(0);
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_WithBooks_EnrichesAndReturnsResult()
        {
            var book = TestDataFactory.CreateBook("Test Book");
            book.ISBN = "9780123456789";
            book.Description = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync(Arg.Any<string>())
                .Returns("Enriched description");

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_ApiFailsForSome_ContinuesProcessing()
        {
            var book1 = TestDataFactory.CreateBook("Book 1");
            book1.ISBN = "9780000000001";
            book1.Description = null;

            var book2 = TestDataFactory.CreateBook("Book 2");
            book2.ISBN = "9780000000002";
            book2.Description = null;

            Context.Books.AddRange(book1, book2);
            await Context.SaveChangesAsync();

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync("9780000000001")
                .Throws(new Exception("API error"));

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync("9780000000002")
                .Returns("Description for book 2");

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(2);
            result.EnrichedCount.Should().BeGreaterThanOrEqualTo(1);
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_RespectsBatchSize()
        {
            for (int i = 0; i < 5; i++)
            {
                var book = TestDataFactory.CreateBook($"Book {i}");
                book.ISBN = $"978000000000{i}";
                book.Description = null;
                Context.Books.Add(book);
            }
            await Context.SaveChangesAsync();

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync(Arg.Any<string>())
                .Returns("Description");

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 2, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(2);
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_RespectsCancellation()
        {
            var book = TestDataFactory.CreateBook("Test");
            book.ISBN = "9780123456789";
            book.Description = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(cancellationToken: cts.Token);

            result.WasCancelled.Should().BeTrue();
        }

        [Fact]
        public async Task EnrichBooksWithoutDescriptionsAsync_CleansIsbn_RemovesHyphens()
        {
            var book = TestDataFactory.CreateBook("Test Book");
            book.ISBN = "978-0-12-345678-9";
            book.Description = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockGoogleBooksClient.GetBookDescriptionByISBNAsync("9780123456789")
                .Returns("Description");

            var result = await _service.EnrichBooksWithoutDescriptionsAsync(batchSize: 10, delayBetweenCallsMs: 0);

            _mockGoogleBooksClient.Received(1).GetBookDescriptionByISBNAsync("9780123456789");
        }

        #endregion
    }
}
