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
    /// Note: media items are created through each type's own service, never here, so this
    /// class has no create tests. The topic and genre rules shared by every write are
    /// covered through <see cref="MediaService.UpdateMediaItemAsync"/>.
    /// </summary>
    [Trait("Category", "Unit")]
    public class MediaServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<MediaService> _mockLogger;
        private readonly IThumbnailStorageService _mockThumbnailStorage;
        private readonly ITypesenseService _mockTypesense;
        private readonly MediaService _service;

        public MediaServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<MediaService>>();
            _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
            _mockTypesense = Substitute.For<ITypesenseService>();
            _service = new MediaService(Context, _mockLogger, _mockThumbnailStorage, _mockTypesense);
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

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldNormalizeTopicsToLowercase()
        {
            var movie = TestDataFactory.CreateMovie();
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.UpdateMediaItemAsync(
                movie.Id, MakeDto(movie.Title, MediaType.Movie, topics: new[] { "SCIENCE", "History" }));

            result.Topics.Should().BeEquivalentTo(new[] { "science", "history" });
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldDeduplicateTopics_AfterNormalization()
        {
            var movie = TestDataFactory.CreateMovie();
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.UpdateMediaItemAsync(
                movie.Id, MakeDto(movie.Title, MediaType.Movie, topics: new[] { "Tech", "tech", " tech " }));

            result.Topics.Should().ContainSingle().Which.Should().Be("tech");
            (await Context.Topics.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldSkipBlankTopics()
        {
            var movie = TestDataFactory.CreateMovie();
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.UpdateMediaItemAsync(
                movie.Id, MakeDto(movie.Title, MediaType.Movie, topics: new[] { "valid", "", "   " }));

            result.Topics.Should().ContainSingle().Which.Should().Be("valid");
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldReuseExistingTopic_InsteadOfCreatingDuplicate()
        {
            Context.Topics.Add(new Topic { Name = "tech" });
            var movie = TestDataFactory.CreateMovie();
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.UpdateMediaItemAsync(
                movie.Id, MakeDto(movie.Title, MediaType.Movie, topics: new[] { "Tech" }));

            result.Topics.Should().ContainSingle().Which.Should().Be("tech");
            (await Context.Topics.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task UpdateMediaItemAsync_ShouldNormalizeAndDeduplicateGenres()
        {
            var movie = TestDataFactory.CreateMovie();
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var result = await _service.UpdateMediaItemAsync(
                movie.Id, MakeDto(movie.Title, MediaType.Movie, genres: new[] { "Thriller", "thriller", " THRILLER " }));

            result.Genres.Should().ContainSingle().Which.Should().Be("thriller");
            (await Context.Genres.CountAsync()).Should().Be(1);
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
        public async Task DeleteMediaItemAsync_PodcastSeries_RemovesItsEpisodesToo()
        {
            var series = new PodcastSeries { Title = "Show", MediaType = MediaType.Podcast };
            var episode = new PodcastEpisode { Title = "Ep", MediaType = MediaType.Podcast, SeriesId = series.Id };
            Context.PodcastSeries.Add(series);
            Context.PodcastEpisodes.Add(episode);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            (await _service.DeleteMediaItemAsync(series.Id)).Should().BeTrue();

            (await Context.MediaItems.CountAsync()).Should().Be(0);
            await _mockTypesense.Received(1).DeleteMediaItemAsync(episode.Id);
        }

        [Fact]
        public async Task DeleteMediaItemAsync_TvShow_RemovesItsEpisodesToo()
        {
            var show = new TvShow { Title = "Show", MediaType = MediaType.TVShow };
            var episode = new TvShowEpisode { Title = "Pilot", MediaType = MediaType.TVShow, ShowId = show.Id };
            Context.TvShows.Add(show);
            Context.TvShowEpisodes.Add(episode);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            (await _service.DeleteMediaItemAsync(show.Id)).Should().BeTrue();

            (await Context.MediaItems.CountAsync()).Should().Be(0);
            await _mockTypesense.Received(1).DeleteMediaItemAsync(episode.Id);
        }

        [Fact]
        public async Task BulkDeleteMediaItemsAsync_SeriesAndOneOfItsEpisodes_RemovesEverythingOnce()
        {
            var series = new PodcastSeries { Title = "Show", MediaType = MediaType.Podcast };
            var ep1 = new PodcastEpisode { Title = "One", MediaType = MediaType.Podcast, SeriesId = series.Id };
            var ep2 = new PodcastEpisode { Title = "Two", MediaType = MediaType.Podcast, SeriesId = series.Id };
            Context.PodcastSeries.Add(series);
            Context.PodcastEpisodes.AddRange(ep1, ep2);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            var (deletedCount, _) = await _service.BulkDeleteMediaItemsAsync(new List<Guid> { series.Id, ep1.Id });

            deletedCount.Should().Be(2);
            (await Context.MediaItems.CountAsync()).Should().Be(0);
            await _mockTypesense.Received(1).DeleteMediaItemAsync(ep1.Id);
            await _mockTypesense.Received(1).DeleteMediaItemAsync(ep2.Id);
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

        [Fact]
        public async Task DeleteMediaItemAsync_RemovesTheSearchDocument()
        {
            var book = TestDataFactory.CreateBook();
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            await _service.DeleteMediaItemAsync(book.Id);

            await _mockTypesense.Received(1).DeleteMediaItemAsync(book.Id);
        }

        [Fact]
        public async Task DeleteMediaItemAsync_DoesNotTouchSearchIndex_WhenItemMissing()
        {
            await _service.DeleteMediaItemAsync(Guid.NewGuid());

            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task DeleteMediaItemAsync_SearchIndexFailure_DoesNotAbortTheDelete()
        {
            var book = TestDataFactory.CreateBook();
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            _mockTypesense.DeleteMediaItemAsync(book.Id)
                .Returns<Task>(_ => throw new InvalidOperationException("Typesense down"));

            var result = await _service.DeleteMediaItemAsync(book.Id);

            result.Should().BeTrue();
            (await Context.MediaItems.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task BulkDeleteMediaItemsAsync_RemovesEverySearchDocument()
        {
            var books = TestDataFactory.CreateBooks(3);
            Context.Books.AddRange(books);
            await Context.SaveChangesAsync();

            await _service.BulkDeleteMediaItemsAsync(books.Select(b => b.Id).ToList());

            foreach (var book in books)
            {
                await _mockTypesense.Received(1).DeleteMediaItemAsync(book.Id);
            }
        }

        [Fact]
        public async Task BulkDeleteMediaItemsAsync_SearchIndexFailure_DoesNotAbortTheDelete()
        {
            var books = TestDataFactory.CreateBooks(2);
            Context.Books.AddRange(books);
            await Context.SaveChangesAsync();

            _mockTypesense.DeleteMediaItemAsync(Arg.Any<Guid>())
                .Returns<Task>(_ => throw new InvalidOperationException("Typesense down"));

            var (deletedCount, _) = await _service.BulkDeleteMediaItemsAsync(books.Select(b => b.Id).ToList());

            deletedCount.Should().Be(2);
            (await Context.MediaItems.CountAsync()).Should().Be(0);
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
