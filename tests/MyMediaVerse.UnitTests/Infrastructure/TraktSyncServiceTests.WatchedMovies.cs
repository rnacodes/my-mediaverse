using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Sync;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public partial class TraktSyncServiceTests
    {
        #region SyncWatchedAsync - Movies

        [Fact]
        public async Task SyncWatchedAsync_NoToken_ReturnsErrorMessage()
        {
            var result = await _service.SyncWatchedAsync();

            result.ErrorMessage.Should().Be("Not connected to Trakt");
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task SyncWatchedAsync_ExistingMovieByTmdbId_UpdatesTraktFields()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Test Movie",
                TmdbId = "12345",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var lastWatched = DateTime.UtcNow.AddDays(-1);
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Test Movie", 2023, tmdbId: 12345, traktId: 999, slug: "test-movie", plays: 3, lastWatchedAt: lastWatched, imdbId: "tt1234567")
            });
            SetupWatchedShows(new List<TraktWatchedShowDto>());

            var result = await _service.SyncWatchedAsync();

            result.Success.Should().BeTrue();
            result.MoviesUpdated.Should().Be(1);
            result.MoviesCreated.Should().Be(0);

            var updated = Context.Movies.First(m => m.TmdbId == "12345");
            updated.TraktId.Should().Be(999);
            updated.TraktSlug.Should().Be("test-movie");
            updated.TraktPlays.Should().Be(3);
            updated.TraktLastWatchedAt.Should().Be(lastWatched);
            updated.ImdbId.Should().Be("tt1234567");
            updated.Status.Should().Be(Status.Completed);
        }

        [Fact]
        public async Task SyncWatchedAsync_ExistingMovieByTitleAndYear_UpdatesTraktFields()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Inception",
                ReleaseYear = 2010,
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Inception", 2010, tmdbId: null, traktId: 100, slug: "inception-2010", plays: 2, lastWatchedAt: DateTime.UtcNow)
            });
            SetupWatchedShows(new List<TraktWatchedShowDto>());

            var result = await _service.SyncWatchedAsync();

            result.MoviesUpdated.Should().Be(1);
            var updated = Context.Movies.First(m => m.Title == "Inception");
            updated.TraktId.Should().Be(100);
            updated.Status.Should().Be(Status.Completed);
        }

        [Fact]
        public async Task SyncWatchedAsync_ExistingMovieWithImdbId_DoesNotOverwriteImdbId()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Test Movie",
                TmdbId = "12345",
                ImdbId = "tt0000001",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Test Movie", 2023, tmdbId: 12345, traktId: 999, imdbId: "tt9999999")
            });
            SetupWatchedShows(new List<TraktWatchedShowDto>());

            await _service.SyncWatchedAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "12345");
            updated.ImdbId.Should().Be("tt0000001"); // Original preserved
        }

        [Fact]
        public async Task SyncWatchedAsync_NewMovie_CreatesWithCompletedStatus()
        {
            await SetupValidToken();

            var lastWatched = DateTime.UtcNow.AddDays(-5);
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Brand New Movie", 2024, tmdbId: 99999, traktId: 555, slug: "brand-new-movie", plays: 1, lastWatchedAt: lastWatched, imdbId: "tt5555555")
            });
            SetupWatchedShows(new List<TraktWatchedShowDto>());

            var result = await _service.SyncWatchedAsync();

            result.Success.Should().BeTrue();
            result.MoviesCreated.Should().Be(1);

            var created = Context.Movies.First(m => m.Title == "Brand New Movie");
            created.MediaType.Should().Be(MediaType.Movie);
            created.Status.Should().Be(Status.Completed);
            created.DateCompleted.Should().Be(lastWatched);
            created.TmdbId.Should().Be("99999");
            created.ImdbId.Should().Be("tt5555555");
            created.TraktId.Should().Be(555);
            created.TraktSlug.Should().Be("brand-new-movie");
            created.TraktPlays.Should().Be(1);
            created.ReleaseYear.Should().Be(2024);
        }

        [Fact]
        public async Task SyncWatchedAsync_MovieAlreadyCompleted_DoesNotDowngradeStatus()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Already Completed",
                TmdbId = "11111",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                DateCompleted = DateTime.UtcNow.AddDays(-30),
                DateAdded = DateTime.UtcNow.AddDays(-60)
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Already Completed", 2023, tmdbId: 11111, traktId: 222)
            });
            SetupWatchedShows(new List<TraktWatchedShowDto>());

            await _service.SyncWatchedAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "11111");
            updated.Status.Should().Be(Status.Completed);
            updated.DateCompleted.Should().Be(movie.DateCompleted); // Original date preserved
        }

        #endregion
    }
}
