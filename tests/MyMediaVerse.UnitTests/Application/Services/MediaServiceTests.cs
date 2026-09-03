using System.Text;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;
using MyMediaVerse.UnitTests.TestHelpers.Builders;

namespace MyMediaVerse.UnitTests.Application.Services
{
    /// <summary>
    /// Unit tests for <see cref="MediaService"/>. Uses the real EF Core context on the
    /// InMemory provider (so the service's own LINQ is exercised) and substitutes only the
    /// I/O boundary, <see cref="IThumbnailStorageService"/>.
    ///
    /// Note: Website and PodcastEpisode are intentionally NOT creatable through
    /// <see cref="MediaService.CreateMediaItemAsync"/> — websites have a dedicated
    /// WebsiteService, and podcast-episode creation is tracked under a separate review.
    /// The dispatch tests assert the current NotSupportedException behavior for those.
    /// </summary>
    [Trait("Category", "Unit")]
    public class MediaServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<MediaService> _mockLogger;
        private readonly IThumbnailStorageService _mockThumbnailStorage;
        private readonly MediaService _service;

        public MediaServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<MediaService>>();
            _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
            _service = new MediaService(Context, _mockLogger, _mockThumbnailStorage);
        }

        private static CreateMediaItemDto MakeDto(
            string title,
            MediaType mediaType,
            string[]? topics = null,
            string[]? genres = null) => new()
            {
                Title = title,
                MediaType = mediaType,
                Topics = topics ?? Array.Empty<string>(),
                Genres = genres ?? Array.Empty<string>()
            };

        #region GetAllMediaAsync / GetMediaItemAsync

        [Fact]
        public async Task GetAllMediaAsync_ShouldReturnAllItemsAsMappedDtos()
        {
            Context.Books.AddRange(TestDataFactory.CreateBooks(2));
            Context.Articles.Add(TestDataFactory.CreateArticle());
            await Context.SaveChangesAsync();

            var result = await _service.GetAllMediaAsync();

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllMediaAsync_ShouldReturnEmpty_WhenNoMedia()
        {
            var result = await _service.GetAllMediaAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllMediaAsync_ShouldMapTopicsGenresAndMixlists()
        {
            var book = TestDataFactory.CreateBook();
            book.Topics = new List<Topic> { new() { Name = "philosophy" } };
            book.Genres = new List<Genre> { new() { Name = "nonfiction" } };
            book.Mixlists = new List<Mixlist> { TestDataFactory.CreateMixlist() };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = (await _service.GetAllMediaAsync()).Single();

            result.Topics.Should().ContainSingle().Which.Should().Be("philosophy");
            result.Genres.Should().ContainSingle().Which.Should().Be("nonfiction");
            result.MixlistIds.Should().ContainSingle();
        }

        [Fact]
        public async Task GetMediaItemAsync_ShouldReturnMappedDto_WhenExists()
        {
            var movie = TestDataFactory.CreateMovie("The Matrix");
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaItemAsync(movie.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(movie.Id);
            result.Title.Should().Be("The Matrix");
            result.MediaType.Should().Be(MediaType.Movie);
        }

        [Fact]
        public async Task GetMediaItemAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _service.GetMediaItemAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        #endregion

        #region CreateMediaItemAsync — dispatch by media type

        [Theory]
        [InlineData(MediaType.Article, typeof(Article))]
        [InlineData(MediaType.Podcast, typeof(PodcastSeries))]
        [InlineData(MediaType.Video, typeof(Video))]
        [InlineData(MediaType.Movie, typeof(Movie))]
        [InlineData(MediaType.TVShow, typeof(TvShow))]
        [InlineData(MediaType.Channel, typeof(YouTubeChannel))]
        public async Task CreateMediaItemAsync_ShouldDispatchToConcreteEntityType(
            MediaType mediaType, Type expectedEntityType)
        {
            var dto = MakeDto("Dispatch Test", mediaType);

            var result = await _service.CreateMediaItemAsync(dto);

            var stored = await Context.MediaItems
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == result.Id);
            stored.Should().NotBeNull();
            stored!.GetType().Should().Be(expectedEntityType);
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldForcePodcastMediaType_OnPodcastSeries()
        {
            var result = await _service.CreateMediaItemAsync(MakeDto("My Show", MediaType.Podcast));

            result.MediaType.Should().Be(MediaType.Podcast);
        }

        [Theory]
        [InlineData(MediaType.Website)]
        [InlineData(MediaType.Document)]
        [InlineData(MediaType.Playlist)]
        [InlineData(MediaType.Music)]
        public async Task CreateMediaItemAsync_ShouldThrowNotSupported_ForUnsupportedTypes(MediaType mediaType)
        {
            var act = () => _service.CreateMediaItemAsync(MakeDto("x", mediaType));

            await act.Should().ThrowAsync<NotSupportedException>();
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldRejectBooks_WithGuidanceToBookEndpoint()
        {
            // The generic DTO has no author field, so a book created here would be
            // permanently authorless; MediaController returns 400 and the service throws.
            var act = () => _service.CreateMediaItemAsync(MakeDto("A Book", MediaType.Book));

            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*POST /api/book*");
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldPersistEntity_AndReturnGeneratedId()
        {
            var result = await _service.CreateMediaItemAsync(MakeDto("Persisted", MediaType.Movie));

            result.Id.Should().NotBeEmpty();
            (await Context.MediaItems.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldMapBaseProperties()
        {
            var dto = MakeDto("Mapped", MediaType.Article);
            dto.Link = "https://example.com/a";
            dto.Notes = "some notes";
            dto.Description = "a description";
            dto.Status = Status.ActivelyExploring;
            dto.Rating = Rating.Like;

            var result = await _service.CreateMediaItemAsync(dto);

            result.Title.Should().Be("Mapped");
            result.Link.Should().Be("https://example.com/a");
            result.Notes.Should().Be("some notes");
            result.Description.Should().Be("a description");
            result.Status.Should().Be(Status.ActivelyExploring);
            result.Rating.Should().Be(Rating.Like);
        }

        #endregion

        #region CreateMediaItemAsync — topics & genres

        [Fact]
        public async Task CreateMediaItemAsync_ShouldCreateAndAssociateNewTopics()
        {
            var dto = MakeDto("Tagged", MediaType.Movie, topics: new[] { "science", "history" });

            var result = await _service.CreateMediaItemAsync(dto);

            result.Topics.Should().BeEquivalentTo(new[] { "science", "history" });
            (await Context.Topics.CountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldNormalizeTopicsToLowercase()
        {
            var dto = MakeDto("Cased", MediaType.Movie, topics: new[] { "SCIENCE", "History" });

            var result = await _service.CreateMediaItemAsync(dto);

            result.Topics.Should().BeEquivalentTo(new[] { "science", "history" });
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldDeduplicateTopics_AfterNormalization()
        {
            var dto = MakeDto("Dupes", MediaType.Movie, topics: new[] { "Tech", "tech", " tech " });

            var result = await _service.CreateMediaItemAsync(dto);

            result.Topics.Should().ContainSingle().Which.Should().Be("tech");
            (await Context.Topics.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldSkipBlankTopics()
        {
            var dto = MakeDto("Blanks", MediaType.Movie, topics: new[] { "valid", "", "   " });

            var result = await _service.CreateMediaItemAsync(dto);

            result.Topics.Should().ContainSingle().Which.Should().Be("valid");
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldReuseExistingTopic_InsteadOfCreatingDuplicate()
        {
            Context.Topics.Add(new Topic { Name = "tech" });
            await Context.SaveChangesAsync();

            var result = await _service.CreateMediaItemAsync(
                MakeDto("Reuse", MediaType.Movie, topics: new[] { "Tech" }));

            result.Topics.Should().ContainSingle().Which.Should().Be("tech");
            (await Context.Topics.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task CreateMediaItemAsync_ShouldCreateAndAssociateGenres()
        {
            var dto = MakeDto("Genred", MediaType.Movie, genres: new[] { "thriller" });

            var result = await _service.CreateMediaItemAsync(dto);

            result.Genres.Should().ContainSingle().Which.Should().Be("thriller");
            (await Context.Genres.CountAsync()).Should().Be(1);
        }

        #endregion

        #region UpdateMediaItemAsync

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldUpdateBasicProperties()
        {
            var book = TestDataFactory.CreateBook("Old Title");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var dto = MakeDto("New Title", MediaType.Book);
            dto.Notes = "updated notes";
            dto.Status = Status.Completed;
            dto.Rating = Rating.SuperLike;

            var result = await _service.UpdateMediaItemAsync(book.Id, dto);

            result.Title.Should().Be("New Title");
            result.Notes.Should().Be("updated notes");
            result.Status.Should().Be(Status.Completed);
            result.Rating.Should().Be(Rating.SuperLike);
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldThrowKeyNotFound_WhenItemMissing()
        {
            var act = () => _service.UpdateMediaItemAsync(Guid.NewGuid(), MakeDto("x", MediaType.Book));

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldClearAndReassociateTopics()
        {
            var book = TestDataFactory.CreateBook();
            book.Topics = new List<Topic> { new() { Name = "old-topic" } };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var dto = MakeDto(book.Title, MediaType.Book, topics: new[] { "new-topic" });

            var result = await _service.UpdateMediaItemAsync(book.Id, dto);

            result.Topics.Should().ContainSingle().Which.Should().Be("new-topic");
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldClearTopics_WhenNoneProvided()
        {
            var book = TestDataFactory.CreateBook();
            book.Topics = new List<Topic> { new() { Name = "old-topic" } };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.UpdateMediaItemAsync(
                book.Id, MakeDto(book.Title, MediaType.Book));

            result.Topics.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldClearAndReassociateGenres()
        {
            var movie = TestDataFactory.CreateMovie();
            movie.Genres = new List<Genre> { new() { Name = "drama" } };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var dto = MakeDto(movie.Title, MediaType.Movie, genres: new[] { "comedy" });

            var result = await _service.UpdateMediaItemAsync(movie.Id, dto);

            result.Genres.Should().ContainSingle().Which.Should().Be("comedy");
        }

        #endregion

        #region DeleteMediaItemAsync / BulkDeleteMediaItemsAsync

        [Fact]
        public async Task DeleteMediaItemAsync_ShouldRemoveItem_AndReturnTrue()
        {
            var book = TestDataFactory.CreateBook();
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.DeleteMediaItemAsync(book.Id);

            result.Should().BeTrue();
            (await Context.MediaItems.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task DeleteMediaItemAsync_ShouldReturnFalse_WhenItemMissing()
        {
            var result = await _service.DeleteMediaItemAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteMediaItemAsync_ShouldDeleteThumbnail_WhenPresent()
        {
            var book = TestDataFactory.CreateBook();
            book.Thumbnail = "https://cdn.example.com/thumbs/x.jpg";
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.DeleteMediaItemAsync(book.Id);

            await _mockThumbnailStorage.Received(1).DeleteAsync(book.Thumbnail);
        }

        [Fact]
        public async Task DeleteMediaItemAsync_ShouldNotCallThumbnailDelete_WhenThumbnailEmpty()
        {
            var book = TestDataFactory.CreateBook();
            book.Thumbnail = null;
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.DeleteMediaItemAsync(book.Id);

            await _mockThumbnailStorage.DidNotReceive().DeleteAsync(Arg.Any<string?>());
        }

        [Fact]
        public async Task BulkDeleteMediaItemsAsync_ShouldDeleteAll_AndReturnCount()
        {
            var books = TestDataFactory.CreateBooks(3);
            Context.Books.AddRange(books);
            await Context.SaveChangesAsync();
            var ids = books.Select(b => b.Id).ToList();

            var (deletedCount, thumbnailErrors) = await _service.BulkDeleteMediaItemsAsync(ids);

            deletedCount.Should().Be(3);
            thumbnailErrors.Should().BeEmpty();
            (await Context.MediaItems.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task BulkDeleteMediaItemsAsync_ShouldReturnZero_WhenNoMatches()
        {
            var (deletedCount, _) = await _service.BulkDeleteMediaItemsAsync(
                new List<Guid> { Guid.NewGuid() });

            deletedCount.Should().Be(0);
        }

        [Fact]
        public async Task BulkDeleteMediaItemsAsync_ShouldCollectThumbnailErrors_WhenDeleteThrows()
        {
            var book = TestDataFactory.CreateBook("Has Bad Thumb");
            book.Thumbnail = "https://cdn.example.com/bad.jpg";
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockThumbnailStorage
                .When(s => s.DeleteAsync(book.Thumbnail))
                .Do(_ => throw new InvalidOperationException("storage down"));

            var (deletedCount, thumbnailErrors) =
                await _service.BulkDeleteMediaItemsAsync(new List<Guid> { book.Id });

            deletedCount.Should().Be(1);
            thumbnailErrors.Should().ContainSingle().Which.Should().Contain("Has Bad Thumb");
            (await Context.MediaItems.CountAsync()).Should().Be(0);
        }

        #endregion

        #region SearchMediaAsync / GetMediaByType / GetMediaByTopic / GetMediaByGenre

        [Fact]
        public async Task SearchMediaAsync_ShouldMatchByTitle()
        {
            Context.Books.Add(TestDataFactory.CreateBook("Unique Whale Title"));
            Context.Books.Add(TestDataFactory.CreateBook("Something Else"));
            await Context.SaveChangesAsync();

            var result = await _service.SearchMediaAsync("whale");

            result.Should().ContainSingle().Which.Title.Should().Be("Unique Whale Title");
        }

        [Fact]
        public async Task SearchMediaAsync_ShouldMatchByDescription()
        {
            var book = TestDataFactory.CreateBook("No Match Title");
            book.Description = "a story about submarines";
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.SearchMediaAsync("submarine");

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task SearchMediaAsync_ShouldMatchByTopic()
        {
            var book = TestDataFactory.CreateBook("Topic Match");
            book.Topics = new List<Topic> { new() { Name = "astronomy" } };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.SearchMediaAsync("astronomy");

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task SearchMediaAsync_ShouldMatchByGenre()
        {
            var movie = TestDataFactory.CreateMovie("Genre Match");
            movie.Genres = new List<Genre> { new() { Name = "horror" } };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.SearchMediaAsync("horror");

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task SearchMediaAsync_ShouldBeCaseInsensitive()
        {
            Context.Books.Add(TestDataFactory.CreateBook("MixedCase Title"));
            await Context.SaveChangesAsync();

            var result = await _service.SearchMediaAsync("MIXEDCASE");

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task SearchMediaAsync_ShouldReturnEmpty_WhenNoMatch()
        {
            Context.Books.Add(TestDataFactory.CreateBook("Nothing Relevant"));
            await Context.SaveChangesAsync();

            var result = await _service.SearchMediaAsync("zzzznomatch");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMediaByTypeAsync_ShouldReturnOnlyMatchingType()
        {
            Context.Books.AddRange(TestDataFactory.CreateBooks(2));
            Context.Movies.Add(TestDataFactory.CreateMovie());
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaByTypeAsync("Book");

            result.Should().HaveCount(2);
            result.Should().OnlyContain(m => m.MediaType == MediaType.Book);
        }

        [Fact]
        public async Task GetMediaByTypeAsync_ShouldBeCaseInsensitive()
        {
            Context.Books.Add(TestDataFactory.CreateBook());
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaByTypeAsync("book");

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task GetMediaByTypeAsync_ShouldThrowArgumentException_WhenTypeInvalid()
        {
            var act = () => _service.GetMediaByTypeAsync("NotARealType");

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetMediaByTopicAsync_ShouldReturnItemsWithTopic()
        {
            var topic = new Topic { Name = "shared-topic" };
            var book = TestDataFactory.CreateBook();
            book.Topics = new List<Topic> { topic };
            Context.Books.Add(book);
            Context.Books.Add(TestDataFactory.CreateBook("No Topic"));
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaByTopicAsync(topic.Id);

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task GetMediaByGenreAsync_ShouldReturnItemsWithGenre()
        {
            var genre = new Genre { Name = "shared-genre" };
            var movie = TestDataFactory.CreateMovie();
            movie.Genres = new List<Genre> { genre };
            Context.Movies.Add(movie);
            Context.Movies.Add(TestDataFactory.CreateMovie("No Genre"));
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaByGenreAsync(genre.Id);

            result.Should().ContainSingle();
        }

        #endregion

        #region MapToResponseDto — Website-specific properties

        [Fact]
        public async Task GetMediaItemAsync_ShouldMapWebsiteSpecificProperties()
        {
            var checkedDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            Website website = new WebsiteBuilder()
                .WithDomain("mysite.com")
                .WithRssFeedUrl("https://mysite.com/rss")
                .WithAuthor("Jane Doe")
                .WithPublication("My Publication")
                .WithLastCheckedDate(checkedDate)
                .WithTitle("My Website");
            Context.Websites.Add(website);
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaItemAsync(website.Id);

            result.Should().NotBeNull();
            result!.RssFeedUrl.Should().Be("https://mysite.com/rss");
            result.Domain.Should().Be("mysite.com");
            result.Author.Should().Be("Jane Doe");
            result.Publication.Should().Be("My Publication");
            result.LastCheckedDate.Should().Be(checkedDate);
        }

        [Fact]
        public async Task GetMediaItemAsync_ShouldNotPopulateWebsiteProperties_ForNonWebsite()
        {
            var book = TestDataFactory.CreateBook();
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.GetMediaItemAsync(book.Id);

            result.Should().NotBeNull();
            result!.RssFeedUrl.Should().BeNull();
            result.Domain.Should().BeNull();
            result.Author.Should().BeNull();
            result.Publication.Should().BeNull();
            result.LastCheckedDate.Should().BeNull();
        }

        #endregion

        #region ExportMediaItemAsync / ExportAllMediaAsync

        [Fact]
        public async Task ExportMediaItemAsync_ShouldReturnCsvWithData_WhenExists()
        {
            var book = TestDataFactory.CreateBook("Export Me");
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.ExportMediaItemAsync(book.Id);

            result.Should().NotBeNull();
            var csv = Encoding.UTF8.GetString(result!.Value.content);
            csv.Should().Contain("Title");
            csv.Should().Contain("Export Me");
            result.Value.fileName.Should().StartWith("media-item-").And.EndWith(".csv");
        }

        [Fact]
        public async Task ExportMediaItemAsync_ShouldReturnNull_WhenItemMissing()
        {
            var result = await _service.ExportMediaItemAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task ExportMediaItemAsync_ShouldJoinTopicsAndGenresWithSemicolons()
        {
            var book = TestDataFactory.CreateBook("Joined");
            book.Topics = new List<Topic> { new() { Name = "alpha" }, new() { Name = "beta" } };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var result = await _service.ExportMediaItemAsync(book.Id);

            var csv = Encoding.UTF8.GetString(result!.Value.content);
            csv.Should().Contain("alpha;beta");
        }

        [Fact]
        public async Task ExportAllMediaAsync_ShouldReturnCsvWithAllItems()
        {
            Context.Books.AddRange(TestDataFactory.CreateBooks(2));
            await Context.SaveChangesAsync();

            var result = await _service.ExportAllMediaAsync();

            var csv = Encoding.UTF8.GetString(result.content);
            csv.Should().Contain("Test Book 1");
            csv.Should().Contain("Test Book 2");
            result.fileName.Should().StartWith("all-media-").And.EndWith(".csv");
        }

        [Fact]
        public async Task ExportAllMediaAsync_ShouldReturnValidFile_WhenNoMedia()
        {
            var result = await _service.ExportAllMediaAsync();

            result.content.Should().NotBeNull();
            result.fileName.Should().StartWith("all-media-").And.EndWith(".csv");
        }

        #endregion
    }
}
