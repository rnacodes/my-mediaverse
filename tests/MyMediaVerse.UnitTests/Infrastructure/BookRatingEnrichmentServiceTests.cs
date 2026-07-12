using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class BookRatingEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<BookRatingEnrichmentService> _mockLogger;
        private readonly BookRatingEnrichmentService _service;

        public BookRatingEnrichmentServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<BookRatingEnrichmentService>>();
            _service = new BookRatingEnrichmentService(Context, _mockLogger);
        }

        private async Task<Book> SeedBook(string title, decimal? goodreadsRating, Rating? rating)
        {
            var book = TestDataFactory.CreateBook(title);
            book.GoodreadsRating = goodreadsRating;
            book.Rating = rating;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();
            return book;
        }

        [Fact]
        public async Task ConvertGoodreadsRatingsAsync_NullRating_DerivesFromGoodreadsRating()
        {
            var book = await SeedBook("Dune", goodreadsRating: 5m, rating: null);

            var result = await _service.ConvertGoodreadsRatingsAsync();

            book.Rating.Should().Be(Rating.SuperLike);
            result.TotalCandidates.Should().Be(1);
            result.Converted.Should().Be(1);
        }

        [Fact]
        public async Task ConvertGoodreadsRatingsAsync_ChangedGoodreadsRating_OverwritesStaleRating()
        {
            // Goodreads-primary: a book whose raw rating maps to a different value is overwritten.
            var book = await SeedBook("Neuromancer", goodreadsRating: 3m, rating: Rating.SuperLike);

            var result = await _service.ConvertGoodreadsRatingsAsync();

            book.Rating.Should().Be(Rating.Neutral);
            result.Converted.Should().Be(1);
        }

        [Fact]
        public async Task ConvertGoodreadsRatingsAsync_AlreadyMatches_LeavesUnchanged()
        {
            var book = await SeedBook("Snow Crash", goodreadsRating: 4m, rating: Rating.Like);

            var result = await _service.ConvertGoodreadsRatingsAsync();

            book.Rating.Should().Be(Rating.Like);
            result.Converted.Should().Be(0);
            result.Unchanged.Should().Be(1);
        }

        [Fact]
        public async Task ConvertGoodreadsRatingsAsync_NoRealGoodreadsRating_NotACandidate()
        {
            // Null or 0 (unrated) Goodreads ratings are not candidates and never touch an existing Rating.
            var unrated = await SeedBook("Unrated", goodreadsRating: null, rating: Rating.Like);
            var zero = await SeedBook("Zero", goodreadsRating: 0m, rating: Rating.Dislike);

            var result = await _service.ConvertGoodreadsRatingsAsync();

            unrated.Rating.Should().Be(Rating.Like);
            zero.Rating.Should().Be(Rating.Dislike);
            result.TotalCandidates.Should().Be(0);
            result.Converted.Should().Be(0);
        }

        [Fact]
        public async Task GetBooksNeedingRatingConversionCountAsync_CountsOnlyRealRatingWithNoMmvRating()
        {
            await SeedBook("Needs", goodreadsRating: 5m, rating: null);                // counted
            await SeedBook("AlreadyRated", goodreadsRating: 4m, rating: Rating.Like);  // has Rating → not counted
            await SeedBook("Unrated", goodreadsRating: null, rating: null);            // no GR rating → not counted
            await SeedBook("ZeroRated", goodreadsRating: 0m, rating: null);            // 0 → not counted

            var count = await _service.GetBooksNeedingRatingConversionCountAsync();

            count.Should().Be(1);
        }
    }
}
