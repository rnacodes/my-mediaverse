using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Trakt;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public partial class TraktSyncServiceTests
    {
        #region Genre linking

        [Fact]
        public async Task SyncWatchedAsync_NewMovieWithGenres_CreatesAndLinksLowercaseGenres()
        {
            await SetupValidToken();
            SetupWatchedShows(new List<TraktWatchedShowDto>());
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Dune", 2021, tmdbId: 1, genres: new List<string> { "Action", "science-fiction" })
            });

            var result = await _service.SyncWatchedAsync();

            result.Success.Should().BeTrue();
            var movie = Context.Movies.Include(m => m.Genres).Single();
            // Slugs are lowercased and hyphens become spaces so genres unify across sources.
            movie.Genres.Select(g => g.Name).Should().BeEquivalentTo("action", "science fiction");
            Context.Genres.Should().HaveCount(2);
        }

        [Fact]
        public async Task SyncWatchedAsync_GenreAlreadyExists_ReusesGenreRecord()
        {
            await SetupValidToken();
            Context.Genres.Add(new Genre { Name = "action" });
            await Context.SaveChangesAsync();

            SetupWatchedShows(new List<TraktWatchedShowDto>());
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Mad Max", 2015, tmdbId: 2, genres: new List<string> { "action" })
            });

            await _service.SyncWatchedAsync();

            Context.Genres.Should().HaveCount(1, "the existing genre should be reused, not duplicated");
            var movie = Context.Movies.Include(m => m.Genres).Single();
            movie.Genres.Single().Name.Should().Be("action");
        }

        [Fact]
        public async Task SyncWatchedAsync_TwoMoviesShareGenreInSameBatch_CreatesGenreOnce()
        {
            await SetupValidToken();
            SetupWatchedShows(new List<TraktWatchedShowDto>());
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Movie A", 2020, tmdbId: 10, genres: new List<string> { "comedy" }),
                CreateWatchedMovieDto("Movie B", 2021, tmdbId: 11, genres: new List<string> { "comedy" })
            });

            await _service.SyncWatchedAsync();

            Context.Genres.Should().HaveCount(1, "the shared genre must not be created twice within one sync batch");
            var movies = Context.Movies.Include(m => m.Genres).ToList();
            movies.Should().HaveCount(2);
            movies.Should().OnlyContain(m => m.Genres.Any(g => g.Name == "comedy"));
        }

        [Fact]
        public async Task SyncWatchedAsync_ReimportSameMovie_DoesNotDuplicateGenreLinks()
        {
            await SetupValidToken();
            SetupWatchedShows(new List<TraktWatchedShowDto>());
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Drama Film", 2019, tmdbId: 20, genres: new List<string> { "drama" })
            });

            await _service.SyncWatchedAsync();
            // Re-run the identical sync; the genre is already linked to the movie.
            await _service.SyncWatchedAsync();

            Context.Genres.Should().HaveCount(1);
            var movie = Context.Movies.Include(m => m.Genres).Single();
            movie.Genres.Should().ContainSingle(g => g.Name == "drama");
        }

        [Fact]
        public async Task SyncWatchlistAsync_NewShowWithGenres_LinksGenres()
        {
            await SetupValidToken();
            SetupWatchlistMovies(new List<TraktWatchlistItemDto>());
            SetupWatchlistShows(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Show = new TraktShowDto
                    {
                        Title = "Severance",
                        Year = 2022,
                        Ids = new TraktIdsDto { Tmdb = 30, Trakt = 31, Slug = "severance" },
                        Genres = new List<string> { "drama", "science-fiction" }
                    }
                }
            });

            await _service.SyncWatchlistAsync();

            var show = Context.TvShows.Include(s => s.Genres).Single();
            show.Genres.Select(g => g.Name).Should().BeEquivalentTo("drama", "science fiction");
        }

        [Fact]
        public async Task SyncRatingsAsync_ExistingMovie_LinksGenres()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Arrival",
                TmdbId = "40",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupRatingsShows(new List<TraktRatingItemDto>());
            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 9,
                    Movie = new TraktMovieDto
                    {
                        Title = "Arrival",
                        Year = 2016,
                        Ids = new TraktIdsDto { Tmdb = 40 },
                        Genres = new List<string> { "science-fiction" }
                    }
                }
            });

            await _service.SyncRatingsAsync();

            var updated = Context.Movies.Include(m => m.Genres).Single();
            updated.Genres.Single().Name.Should().Be("science fiction");
        }

        #endregion
    }
}
