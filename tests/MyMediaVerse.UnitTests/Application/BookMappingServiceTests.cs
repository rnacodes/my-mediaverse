using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class BookMappingServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ILogger<BookMappingService>> _mockLogger;
        private readonly BookMappingService _service;

        public BookMappingServiceTests()
        {
            _mockLogger = new Mock<ILogger<BookMappingService>>();
            _service = new BookMappingService(Context, _mockLogger.Object);
        }

        #region MapFromDtoAsync

        [Fact]
        public async Task MapFromDtoAsync_ValidDto_MapsAllProperties()
        {
            var dto = new CreateBookDto
            {
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                MediaType = MediaType.Book,
                Status = Status.Completed,
                Format = BookFormat.Physical,
                PartOfSeries = false,
                ISBN = "978-0743273565",
                ASIN = "B000FC0PDA",
                Description = "A classic novel",
                Publisher = "Scribner",
                YearPublished = 1925,
                Rating = Rating.Like,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            var result = await _service.MapFromDtoAsync(dto);

            result.Should().NotBeNull();
            result.Title.Should().Be("The Great Gatsby");
            result.Author.Should().Be("F. Scott Fitzgerald");
            result.MediaType.Should().Be(MediaType.Book);
            result.Status.Should().Be(Status.Completed);
            result.Format.Should().Be(BookFormat.Physical);
            result.ISBN.Should().Be("978-0743273565");
            result.ASIN.Should().Be("B000FC0PDA");
            result.Publisher.Should().Be("Scribner");
            result.YearPublished.Should().Be(1925);
            result.Rating.Should().Be(Rating.Like);
            result.DateAdded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task MapFromDtoAsync_WithTopics_NormalizesToLowercase()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.Topics = new[] { "Science Fiction", "  ADVENTURE  ", "classic" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(3);
            result.Topics.Select(t => t.Name).Should().BeEquivalentTo("science fiction", "adventure", "classic");
        }

        [Fact]
        public async Task MapFromDtoAsync_WithExistingTopic_ReusesExistingTopic()
        {
            var existingTopic = new Topic { Name = "science fiction" };
            Context.Topics.Add(existingTopic);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto();
            dto.Topics = new[] { "Science Fiction" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(1);
            result.Topics.First().Id.Should().Be(existingTopic.Id);
        }

        [Fact]
        public async Task MapFromDtoAsync_WithGenres_NormalizesToLowercase()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.Genres = new[] { "Fiction", "  LITERARY  " };

            var result = await _service.MapFromDtoAsync(dto);

            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().BeEquivalentTo("fiction", "literary");
        }

        [Fact]
        public async Task MapFromDtoAsync_WithExistingGenre_ReusesExistingGenre()
        {
            var existingGenre = new Genre { Name = "fiction" };
            Context.Genres.Add(existingGenre);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateBookDto();
            dto.Genres = new[] { "Fiction" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Genres.Should().HaveCount(1);
            result.Genres.First().Id.Should().Be(existingGenre.Id);
        }

        [Fact]
        public async Task MapFromDtoAsync_SkipsWhitespaceTopicsAndGenres()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.Topics = new[] { "", "  ", "valid topic" };
            dto.Genres = new[] { "", "valid genre" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(1);
            result.Topics.First().Name.Should().Be("valid topic");
            result.Genres.Should().HaveCount(1);
            result.Genres.First().Name.Should().Be("valid genre");
        }

        [Fact]
        public async Task MapFromDtoAsync_GoodreadsRatingWithoutPlbRating_AutoConverts()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.GoodreadsRating = 5.0m;
            dto.Rating = null;

            var result = await _service.MapFromDtoAsync(dto);

            result.Rating.Should().Be(Rating.SuperLike);
        }

        [Fact]
        public async Task MapFromDtoAsync_GoodreadsRatingWithPlbRating_KeepsPlbRating()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.GoodreadsRating = 5.0m;
            dto.Rating = Rating.Dislike;

            var result = await _service.MapFromDtoAsync(dto);

            result.Rating.Should().Be(Rating.Dislike);
        }

        [Fact]
        public async Task MapFromDtoAsync_NullTopicsAndGenres_CreatesEmptyCollections()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.Topics = null;
            dto.Genres = null;

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().BeEmpty();
            result.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task MapFromDtoAsync_GoodreadsTags_DefaultsToEmptyList()
        {
            var dto = TestDataFactory.CreateBookDto();
            dto.GoodreadsTags = null;

            var result = await _service.MapFromDtoAsync(dto);

            result.GoodreadsTags.Should().NotBeNull();
            result.GoodreadsTags.Should().BeEmpty();
        }

        #endregion

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
