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
        #region SyncWatchlistAsync

        [Fact]
        public async Task SyncWatchlistAsync_NoToken_ReturnsErrorMessage()
        {
            var result = await _service.SyncWatchlistAsync();

            result.ErrorMessage.Should().Be("Not connected to Trakt");
        }

        [Fact]
        public async Task SyncWatchlistAsync_ExistingMovie_UpdatesTraktIdAndSlugOnlyIfNull()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Existing Watchlist Movie",
                TmdbId = "40000",
                MediaType = MediaType.Movie,
                Status = Status.ActivelyExploring,
                TraktId = 111,
                TraktSlug = "already-set",
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "Existing Watchlist Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 40000, Trakt = 222, Slug = "new-slug" }
                    },
                    Notes = "Watch this soon"
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            await _service.SyncWatchlistAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "40000");
            updated.TraktId.Should().Be(111); // Not overwritten
            updated.TraktSlug.Should().Be("already-set"); // Not overwritten
            updated.Status.Should().Be(Status.ActivelyExploring); // Not overwritten
        }

        [Fact]
        public async Task SyncWatchlistAsync_ExistingMovieWithNullTraktFields_SetsTraktIdAndSlug()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "No Trakt Fields",
                TmdbId = "41000",
                MediaType = MediaType.Movie,
                Status = Status.ActivelyExploring,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "No Trakt Fields",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 41000, Trakt = 333, Slug = "no-trakt-fields" }
                    }
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            await _service.SyncWatchlistAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "41000");
            updated.TraktId.Should().Be(333);
            updated.TraktSlug.Should().Be("no-trakt-fields");
        }

        [Fact]
        public async Task SyncWatchlistAsync_ExistingMovieWithEmptyNotes_AddsNotes()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Notes Test Movie",
                TmdbId = "42000",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "Notes Test Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 42000 }
                    },
                    Notes = "Friend recommended this"
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            await _service.SyncWatchlistAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "42000");
            updated.Notes.Should().Be("Friend recommended this");
        }

        [Fact]
        public async Task SyncWatchlistAsync_ExistingMovieWithExistingNotes_DoesNotOverwriteNotes()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Existing Notes Movie",
                TmdbId = "43000",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                Notes = "My original notes",
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "Existing Notes Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 43000 }
                    },
                    Notes = "Trakt notes that should not overwrite"
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            await _service.SyncWatchlistAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "43000");
            updated.Notes.Should().Be("My original notes");
        }

        [Fact]
        public async Task SyncWatchlistAsync_NewMovie_CreatesWithUnchartedStatus()
        {
            await SetupValidToken();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "New Watchlist Movie",
                        Year = 2025,
                        Ids = new TraktIdsDto { Tmdb = 60000, Trakt = 444, Slug = "new-watchlist-movie", Imdb = "tt6000000" }
                    },
                    Notes = "Must watch"
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            var result = await _service.SyncWatchlistAsync();

            result.Success.Should().BeTrue();
            result.MoviesCreated.Should().Be(1);
            result.WatchlistItemsProcessed.Should().Be(1);

            var created = Context.Movies.First(m => m.Title == "New Watchlist Movie");
            created.Status.Should().Be(Status.Uncharted);
            created.TmdbId.Should().Be("60000");
            created.ImdbId.Should().Be("tt6000000");
            created.TraktId.Should().Be(444);
            created.TraktSlug.Should().Be("new-watchlist-movie");
            created.Notes.Should().Be("Must watch");
            created.ReleaseYear.Should().Be(2025);
        }

        [Fact]
        public async Task SyncWatchlistAsync_DoesNotOverwriteExistingStatus()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Completed Movie",
                TmdbId = "45000",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "Completed Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 45000 }
                    }
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            await _service.SyncWatchlistAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "45000");
            updated.Status.Should().Be(Status.Completed); // Not changed to Uncharted
        }

        [Fact]
        public async Task SyncWatchlistAsync_IncrementsWatchlistItemsProcessed()
        {
            await SetupValidToken();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto { Title = "Movie 1", Year = 2023, Ids = new TraktIdsDto { Tmdb = 70001 } }
                },
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto { Title = "Movie 2", Year = 2023, Ids = new TraktIdsDto { Tmdb = 70002 } }
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Show = new TraktShowDto { Title = "Show 1", Year = 2023, Ids = new TraktIdsDto { Tmdb = 70003 } }
                }
            });

            var result = await _service.SyncWatchlistAsync();

            result.WatchlistItemsProcessed.Should().Be(3);
        }

        [Fact]
        public async Task SyncWatchlistAsync_NewShow_CreatesWithUnchartedStatus()
        {
            await SetupValidToken();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>());
            SetupWatchlistShows(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Show = new TraktShowDto
                    {
                        Title = "New Watchlist Show",
                        Year = 2024,
                        Ids = new TraktIdsDto { Tmdb = 80000, Trakt = 555, Slug = "new-watchlist-show" }
                    },
                    Notes = "Looks interesting"
                }
            });

            var result = await _service.SyncWatchlistAsync();

            result.ShowsCreated.Should().Be(1);

            var created = Context.TvShows.First(s => s.Title == "New Watchlist Show");
            created.Status.Should().Be(Status.Uncharted);
            created.TmdbId.Should().Be("80000");
            created.TraktId.Should().Be(555);
            created.Notes.Should().Be("Looks interesting");
        }

        [Fact]
        public async Task SyncWatchlistAsync_NullMovieInItem_Skips()
        {
            await SetupValidToken();

            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto { Movie = null }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            var result = await _service.SyncWatchlistAsync();

            result.Success.Should().BeTrue();
            result.MoviesCreated.Should().Be(0);
            result.MoviesUpdated.Should().Be(0);
        }

        #endregion
    }
}
