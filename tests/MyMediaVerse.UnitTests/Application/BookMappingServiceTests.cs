using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class BookMappingServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<BookMappingService> _mockLogger;
        private readonly BookMappingService _service;

        public BookMappingServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<BookMappingService>>();
            _service = new BookMappingService(Context, _mockLogger);
        }

        #region MapToResponseDtoAsync

        [Fact]
        public async Task MapToResponseDtoAsync_ValidBook_MapsAllProperties()
        {
            var book = TestDataFactory.CreateBook("Test Book", "Test Author");
            book.ISBN = "978-0123456789";
            book.Format = BookFormat.Physical;
            book.Publisher = "Test Publisher";
            book.YearPublished = 2020;
            book.Topics.Add(new Topic { Name = "test topic" });
            book.Genres.Add(new Genre { Name = "test genre" });

            var result = await _service.MapToResponseDtoAsync(book);

            result.Should().NotBeNull();
            result.Id.Should().Be(book.Id);
            result.Title.Should().Be("Test Book");
            result.Author.Should().Be("Test Author");
            result.ISBN.Should().Be("978-0123456789");
            result.Format.Should().Be(BookFormat.Physical);
            result.Publisher.Should().Be("Test Publisher");
            result.YearPublished.Should().Be(2020);
            result.Topics.Should().Contain("test topic");
            result.Genres.Should().Contain("test genre");
        }

        [Fact]
        public async Task MapToResponseDtoAsync_NullGoodreadsTags_DefaultsToEmptyList()
        {
            var book = TestDataFactory.CreateBook();
            book.GoodreadsTags = null;

            var result = await _service.MapToResponseDtoAsync(book);

            result.GoodreadsTags.Should().NotBeNull();
            result.GoodreadsTags.Should().BeEmpty();
        }

        [Fact]
        public async Task MapToResponseDtoAsync_ExposesExternalIdsAndEnrichmentStamp()
        {
            // The ids imports and enrichment fill in are read-only on the wire but must be visible,
            // so a client (or a smoke test) can see which sources a book has been matched to.
            var enrichedAt = new DateTime(2026, 9, 5, 19, 1, 54, DateTimeKind.Utc);
            var book = TestDataFactory.CreateBook("Dune", "Frank Herbert");
            book.ReadwiseBookId = 12345;
            book.GoodreadsBookId = 234225;
            book.GoogleVolumeId = "9ddRibJTyJAC";
            book.OpenLibraryKey = "/works/OL893415W";
            book.EnrichedAt = enrichedAt;

            var result = await _service.MapToResponseDtoAsync(book);

            result.ReadwiseBookId.Should().Be(12345);
            result.GoodreadsBookId.Should().Be(234225);
            result.GoogleVolumeId.Should().Be("9ddRibJTyJAC");
            result.OpenLibraryKey.Should().Be("/works/OL893415W");
            result.EnrichedAt.Should().Be(enrichedAt);
        }

        #endregion

        #region MapFromOpenLibraryAsync

        [Fact]
        public async Task MapFromOpenLibraryAsync_ValidDto_MapsCorrectly()
        {
            var olBook = new OpenLibraryBookDto
            {
                Title = "1984",
                AuthorName = new[] { "George Orwell" },
                Key = "/works/OL1168083W",
                Isbn = new[] { "978-0451524935" },
                CoverId = 12345
            };

            var result = await _service.MapFromOpenLibraryAsync(olBook);

            result.Title.Should().Be("1984");
            result.Author.Should().Be("George Orwell");
            result.ISBN.Should().Be("978-0451524935");
            result.MediaType.Should().Be(MediaType.Book);
            result.Status.Should().Be(Status.Uncharted);
            result.Format.Should().Be(BookFormat.Digital);
            result.Thumbnail.Should().Contain("openlibrary.org/b/id/12345");
            result.Link.Should().Contain("openlibrary.org/works/OL1168083W");
        }

        [Fact]
        public async Task MapFromOpenLibraryAsync_NullTitle_DefaultsToUnknownTitle()
        {
            var olBook = new OpenLibraryBookDto { Title = null };

            var result = await _service.MapFromOpenLibraryAsync(olBook);

            result.Title.Should().Be("Unknown Title");
        }

        [Fact]
        public async Task MapFromOpenLibraryAsync_NullAuthor_DefaultsToUnknownAuthor()
        {
            var olBook = new OpenLibraryBookDto { Title = "Test", AuthorName = null };

            var result = await _service.MapFromOpenLibraryAsync(olBook);

            result.Author.Should().Be("Unknown Author");
        }

        [Fact]
        public async Task MapFromOpenLibraryAsync_NoCoverId_ThumbnailIsNull()
        {
            var olBook = new OpenLibraryBookDto { Title = "Test", CoverId = null };

            var result = await _service.MapFromOpenLibraryAsync(olBook);

            result.Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task MapFromOpenLibraryAsync_WithSubjects_GeneratesDescription()
        {
            var olBook = new OpenLibraryBookDto
            {
                Title = "Test",
                Subject = new[] { "Science", "Technology", "History", "Philosophy" }
            };

            var result = await _service.MapFromOpenLibraryAsync(olBook);

            result.Description.Should().Contain("Science");
            result.Description.Should().Contain("Technology");
            result.Description.Should().Contain("History");
            // Only first 3 subjects should be included
            result.Description.Should().NotContain("Philosophy");
        }

        #endregion

        #region MapFromGoogleBooksAsync

        [Fact]
        public async Task MapFromGoogleBooksAsync_ValidVolume_MapsCorrectly()
        {
            var volume = new GoogleBooksVolumeDto
            {
                Id = "vol123",
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = "Clean Code",
                    Authors = new[] { "Robert C. Martin" },
                    Publisher = "Prentice Hall",
                    PublishedDate = "2008",
                    Description = "<b>A great book</b> about clean code.",
                    PageCount = 464,
                    AverageRating = 4.5,
                    IndustryIdentifiers = new[]
                    {
                        new GoogleBooksIndustryIdentifierDto { Type = "ISBN_13", Identifier = "9780132350884" }
                    },
                    ImageLinks = new GoogleBooksImageLinksDto
                    {
                        Thumbnail = "http://books.google.com/thumb.jpg"
                    },
                    CanonicalVolumeLink = "https://books.google.com/books?id=vol123"
                },
                SaleInfo = new GoogleBooksSaleInfoDto { IsEbook = false }
            };

            var result = await _service.MapFromGoogleBooksAsync(volume);

            result.Title.Should().Be("Clean Code");
            result.Author.Should().Be("Robert C. Martin");
            result.Publisher.Should().Be("Prentice Hall");
            result.ISBN.Should().Be("9780132350884");
            result.Format.Should().Be(BookFormat.Physical);
            result.MediaType.Should().Be(MediaType.Book);
            result.Status.Should().Be(Status.Uncharted);
            result.AverageRating.Should().Be(4.5m);
            result.Link.Should().Be("https://books.google.com/books?id=vol123");
            // HTML should be stripped
            result.Description.Should().NotContain("<b>");
            result.Description.Should().Contain("A great book");
        }

        [Fact]
        public async Task MapFromGoogleBooksAsync_EbookFormat_SetsDigital()
        {
            var volume = new GoogleBooksVolumeDto
            {
                VolumeInfo = new GoogleBooksVolumeInfoDto { Title = "Test" },
                SaleInfo = new GoogleBooksSaleInfoDto { IsEbook = true }
            };

            var result = await _service.MapFromGoogleBooksAsync(volume);

            result.Format.Should().Be(BookFormat.Digital);
        }

        [Fact]
        public async Task MapFromGoogleBooksAsync_NullVolumeInfo_DefaultsToUnknown()
        {
            var volume = new GoogleBooksVolumeDto { VolumeInfo = null };

            var result = await _service.MapFromGoogleBooksAsync(volume);

            result.Title.Should().Be("Unknown Title");
            result.Author.Should().Be("Unknown Author");
        }

        #endregion

        #region MapToSearchResultDtoAsync

        [Fact]
        public async Task MapToSearchResultDtoAsync_OpenLibraryBook_MapsCorrectly()
        {
            var olBook = new OpenLibraryBookDto
            {
                Key = "/works/OL123",
                Title = "Test Book",
                AuthorName = new[] { "Author 1", "Author 2" },
                FirstPublishYear = 2020,
                Isbn = new[] { "978-0123456789" },
                CoverId = 999,
                NumberOfPagesMedian = 300,
                RatingAverage = 4.2,
                RatingCount = 150,
                EditionCount = 5
            };

            var result = await _service.MapToSearchResultDtoAsync(olBook);

            result.Key.Should().Be("/works/OL123");
            result.Title.Should().Be("Test Book");
            result.Authors.Should().HaveCount(2);
            result.FirstPublishYear.Should().Be(2020);
            result.CoverUrl.Should().Contain("openlibrary.org/b/id/999");
            result.PageCount.Should().Be(300);
            result.AverageRating.Should().Be(4.2);
            result.EditionCount.Should().Be(5);
        }

        [Fact]
        public async Task MapGoogleBooksToSearchResultDtoAsync_ValidVolume_MapsCorrectly()
        {
            var volume = new GoogleBooksVolumeDto
            {
                Id = "vol456",
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = "Test Book",
                    Authors = new[] { "Author" },
                    Publisher = "Publisher",
                    PageCount = 200,
                    AverageRating = 3.5,
                    RatingsCount = 50,
                    Categories = new[] { "Fiction" },
                    Language = "en",
                    ImageLinks = new GoogleBooksImageLinksDto
                    {
                        Thumbnail = "http://thumb.jpg"
                    },
                    IndustryIdentifiers = new[]
                    {
                        new GoogleBooksIndustryIdentifierDto { Type = "ISBN_13", Identifier = "9780123" }
                    }
                }
            };

            var result = await _service.MapGoogleBooksToSearchResultDtoAsync(volume);

            result.Key.Should().Be("vol456");
            result.Title.Should().Be("Test Book");
            result.Publishers.Should().Contain("Publisher");
            result.PageCount.Should().Be(200);
            result.AverageRating.Should().Be(3.5);
            result.RatingCount.Should().Be(50);
            result.Isbn.Should().Contain("9780123");
        }

        #endregion
    }
}
