using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The TMDB refresh run: which items count as stale, which fields TMDB may overwrite, which
    /// it may only fill, and which it never touches.
    /// </summary>
    [Trait("Category", "Unit")]
    public class MovieTvRefreshStaleTests : InMemoryDbTestBase
    {
        private readonly ITmdbApiClient _mockTmdbClient = Substitute.For<ITmdbApiClient>();
        private readonly MovieTvEnrichmentService _service;

        public MovieTvRefreshStaleTests()
        {
            _service = new MovieTvEnrichmentService(
                Context, _mockTmdbClient, TestGenreMapping.Create(), NullLogger<MovieTvEnrichmentService>.Instance);
        }

        private async Task<Movie> AddMovieAsync(string title, string tmdbId, DateTime? refreshedAt, Action<Movie>? configure = null)
        {
            var movie = TestDataFactory.CreateMovie(title, tmdbId: tmdbId);
            movie.TmdbRefreshedAt = refreshedAt;
            configure?.Invoke(movie);
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();
            return movie;
        }

        private async Task<TvShow> AddShowAsync(string title, string tmdbId, DateTime? refreshedAt, Action<TvShow>? configure = null)
        {
            var show = TestDataFactory.CreateTvShow(title, tmdbId: tmdbId);
            show.TmdbRefreshedAt = refreshedAt;
            configure?.Invoke(show);
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();
            return show;
        }

        private void SetupMovie(int id, TmdbMovieDto details)
        {
            details.Id = id;
            _mockTmdbClient.GetMovieDetailsAsync(id, Arg.Any<string>()).Returns(details);
        }

        private void SetupShow(int id, TmdbTvShowDto details)
        {
            details.Id = id;
            _mockTmdbClient.GetTvShowDetailsAsync(id, Arg.Any<string>()).Returns(details);
        }

        private Task<MovieTvRefreshResultDto> RunAsync(int limit = 50, int olderThanDays = 180)
            => _service.RefreshStaleAsync(limit, olderThanDays, delayBetweenCallsMs: 0);

        #region Selection

        [Fact]
        public async Task RefreshStaleAsync_ShouldRefreshStaleAndNeverStampedItems_AndLeaveFreshOnesAlone()
        {
            var stale = await AddMovieAsync("Stale", "1", DateTime.UtcNow.AddDays(-200));
            var neverStamped = await AddMovieAsync("Never Stamped", "2", null);
            var fresh = await AddMovieAsync("Fresh", "3", DateTime.UtcNow.AddDays(-10));
            var noTmdbId = await AddMovieAsync("Manual", "", null);
            SetupMovie(1, new TmdbMovieDto { Title = "Stale" });
            SetupMovie(2, new TmdbMovieDto { Title = "Never Stamped" });

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("tmdb-refresh-stale");
            result.TotalProcessed.Should().Be(2);
            result.RemainingCount.Should().Be(0);
            result.CompletedAt.Should().NotBeNull();
            await _mockTmdbClient.DidNotReceive().GetMovieDetailsAsync(3, Arg.Any<string>());

            Context.Movies.First(m => m.Id == stale.Id).TmdbRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            Context.Movies.First(m => m.Id == neverStamped.Id).TmdbRefreshedAt.Should().NotBeNull();
            Context.Movies.First(m => m.Id == fresh.Id).TmdbRefreshedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-10), TimeSpan.FromMinutes(1));
            Context.Movies.First(m => m.Id == noTmdbId.Id).TmdbRefreshedAt.Should().BeNull();
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldHonorLimit_OldestFirst_AcrossMoviesAndShows()
        {
            await AddMovieAsync("Newer Movie", "1", DateTime.UtcNow.AddDays(-190));
            await AddShowAsync("Oldest Show", "2", DateTime.UtcNow.AddDays(-400));
            await AddMovieAsync("Never Stamped Movie", "3", null);
            SetupShow(2, new TmdbTvShowDto { Name = "Oldest Show" });
            SetupMovie(3, new TmdbMovieDto { Title = "Never Stamped Movie" });

            var result = await RunAsync(limit: 2);

            result.TotalProcessed.Should().Be(2);
            result.RemainingCount.Should().Be(1);
            await _mockTmdbClient.Received(1).GetTvShowDetailsAsync(2, Arg.Any<string>());
            await _mockTmdbClient.Received(1).GetMovieDetailsAsync(3, Arg.Any<string>());
            await _mockTmdbClient.DidNotReceive().GetMovieDetailsAsync(1, Arg.Any<string>());
        }

        [Fact]
        public async Task RefreshStaleAsync_NothingStale_ShouldReportZeroProcessed()
        {
            await AddMovieAsync("Fresh", "1", DateTime.UtcNow);

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.TotalProcessed.Should().Be(0);
            result.RemainingCount.Should().Be(0);
        }

        [Fact]
        public async Task RefreshStaleAsync_NonNumericTmdbId_ShouldSkipWithWarning_AndMakeNoRequest()
        {
            var movie = await AddMovieAsync("Odd Id", "tt-not-a-number", null);

            var result = await RunAsync();

            result.SkippedCount.Should().Be(1);
            result.Warnings.Should().ContainSingle().Which.Should().Contain("Odd Id");
            _mockTmdbClient.ReceivedCalls().Should().BeEmpty();
            Context.Movies.First(m => m.Id == movie.Id).TmdbRefreshedAt.Should().BeNull();
        }

        #endregion

        #region Field policy

        [Fact]
        public async Task RefreshStaleAsync_Movie_ShouldOverwriteTmdbOwnedFields()
        {
            var movie = await AddMovieAsync("Inception", "27205", null, m =>
            {
                m.TmdbRating = 7.0;
                m.RuntimeMinutes = 100;
                m.Tagline = "Old tagline";
                m.Homepage = "https://old.example.com";
                m.TmdbBackdropPath = "/old-backdrop.jpg";
                m.Thumbnail = "https://image.tmdb.org/t/p/w500/old-poster.jpg";
                m.OriginalLanguage = "fr";
                m.ImdbId = "tt0000000";
            });
            SetupMovie(27205, new TmdbMovieDto
            {
                Title = "Inception",
                VoteAverage = 8.4,
                Runtime = 148,
                Tagline = "Your mind is the scene of the crime.",
                Homepage = "https://new.example.com",
                BackdropPath = "/new-backdrop.jpg",
                PosterPath = "/new-poster.jpg",
                OriginalLanguage = "en",
                ImdbId = "tt1375666"
            });

            var result = await RunAsync();

            result.UpdatedCount.Should().Be(1);
            var updated = Context.Movies.First(m => m.Id == movie.Id);
            updated.TmdbRating.Should().Be(8.4);
            updated.RuntimeMinutes.Should().Be(148);
            updated.Tagline.Should().Be("Your mind is the scene of the crime.");
            updated.Homepage.Should().Be("https://new.example.com");
            updated.TmdbBackdropPath.Should().Be("/new-backdrop.jpg");
            updated.Thumbnail.Should().Be("https://image.tmdb.org/t/p/w500/new-poster.jpg");
            updated.OriginalLanguage.Should().Be("en");
            updated.ImdbId.Should().Be("tt1375666");
        }

        [Fact]
        public async Task RefreshStaleAsync_Movie_ShouldOnlyFillDescriptiveFields_NeverOverwriteThem()
        {
            var edited = await AddMovieAsync("My Title", "1", null, m =>
            {
                m.Description = "My own description";
                m.Director = "My Director";
                m.Cast = "My Cast";
                m.MpaaRating = "R";
                m.ReleaseYear = 1999;
                m.OriginalTitle = "My Original";
                m.Link = "https://my.example.com";
            });
            var empty = await AddMovieAsync("Empty", "2", null, m => m.ReleaseYear = null);
            var payload = new Func<TmdbMovieDto>(() => new TmdbMovieDto
            {
                Title = "TMDB Title",
                Overview = "TMDB overview",
                OriginalTitle = "TMDB Original",
                ReleaseDate = "2010-07-15",
                Credits = new TmdbCreditsDto
                {
                    Cast = { new TmdbCastMemberDto { Name = "TMDB Actor", Order = 0 } },
                    Crew = { new TmdbCrewMemberDto { Name = "TMDB Director", Job = "Director" } }
                },
                ReleaseDates = new TmdbReleaseDatesDto
                {
                    Results = { new TmdbCountryReleaseDatesDto { Iso31661 = "US", ReleaseDates = { new TmdbReleaseDateDto { Certification = "PG-13" } } } }
                }
            });
            SetupMovie(1, payload());
            SetupMovie(2, payload());

            await RunAsync();

            var kept = Context.Movies.First(m => m.Id == edited.Id);
            kept.Title.Should().Be("My Title");
            kept.Description.Should().Be("My own description");
            kept.Director.Should().Be("My Director");
            kept.Cast.Should().Be("My Cast");
            kept.MpaaRating.Should().Be("R");
            kept.ReleaseYear.Should().Be(1999);
            kept.OriginalTitle.Should().Be("My Original");
            kept.Link.Should().Be("https://my.example.com");

            var filled = Context.Movies.First(m => m.Id == empty.Id);
            filled.Title.Should().Be("Empty");
            filled.Description.Should().Be("TMDB overview");
            filled.Director.Should().Be("TMDB Director");
            filled.Cast.Should().Be("TMDB Actor");
            filled.MpaaRating.Should().Be("PG-13");
            filled.ReleaseYear.Should().Be(2010);
            filled.OriginalTitle.Should().Be("TMDB Original");
            filled.Link.Should().Be("https://www.themoviedb.org/movie/2");
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldNeverTouchPersonalFields()
        {
            var completed = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var movie = await AddMovieAsync("Inception", "27205", null, m =>
            {
                m.Status = Status.Completed;
                m.Rating = Rating.SuperLike;
                m.OwnershipStatus = OwnershipStatus.Own;
                m.DateCompleted = completed;
                m.Notes = "Watched twice";
                m.Topics.Add(new Topic { Name = "dreams" });
            });
            var dateAdded = movie.DateAdded;
            SetupMovie(27205, new TmdbMovieDto { Title = "Inception", VoteAverage = 8.4 });

            await RunAsync();

            var updated = Context.Movies.First(m => m.Id == movie.Id);
            updated.Status.Should().Be(Status.Completed);
            updated.Rating.Should().Be(Rating.SuperLike);
            updated.OwnershipStatus.Should().Be(OwnershipStatus.Own);
            updated.DateCompleted.Should().Be(completed);
            updated.DateAdded.Should().Be(dateAdded);
            updated.Notes.Should().Be("Watched twice");
            updated.Topics.Select(t => t.Name).Should().Equal("dreams");
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldPreserveACustomThumbnail()
        {
            var movie = await AddMovieAsync("Inception", "27205", null, m => m.Thumbnail = "https://cdn.example.com/my-poster.jpg");
            SetupMovie(27205, new TmdbMovieDto { Title = "Inception", PosterPath = "/new-poster.jpg" });

            await RunAsync();

            Context.Movies.First(m => m.Id == movie.Id).Thumbnail.Should().Be("https://cdn.example.com/my-poster.jpg");
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldFillGenresOnlyWhenTheItemHasNone()
        {
            var genreless = await AddMovieAsync("Genreless", "1", null);
            var curated = await AddMovieAsync("Curated", "2", null, m => m.Genres.Add(new Genre { Name = "heist" }));
            var genres = new Func<List<TmdbGenreDto>>(() => new List<TmdbGenreDto> { new() { Id = 28, Name = "Action" } });
            SetupMovie(1, new TmdbMovieDto { Title = "Genreless", Genres = genres() });
            SetupMovie(2, new TmdbMovieDto { Title = "Curated", Genres = genres() });

            await RunAsync();

            Context.Movies.First(m => m.Id == genreless.Id).Genres.Select(g => g.Name).Should().Equal("action");
            Context.Movies.First(m => m.Id == curated.Id).Genres.Select(g => g.Name).Should().Equal("heist");
        }

        [Fact]
        public async Task RefreshStaleAsync_TvShow_ShouldOverwriteCountsAndLastAirYear_AndFillCreator()
        {
            var show = await AddShowAsync("Severance", "95396", DateTime.UtcNow.AddDays(-365), s =>
            {
                s.NumberOfSeasons = 1;
                s.NumberOfEpisodes = 9;
                s.LastAirYear = 2022;
                s.TmdbPosterPath = "/old.jpg";
                s.Creator = null;
                s.ContentRating = "TV-14";
            });
            SetupShow(95396, new TmdbTvShowDto
            {
                Name = "Severance",
                NumberOfSeasons = 2,
                NumberOfEpisodes = 19,
                LastAirDate = "2025-03-20",
                PosterPath = "/new.jpg",
                CreatedBy = new List<TmdbCreatedByDto> { new() { Name = "Dan Erickson" } },
                Genres = new List<TmdbGenreDto> { new() { Id = 10765, Name = "Sci-Fi & Fantasy" } },
                ContentRatings = new TmdbContentRatingsDto { Results = { new TmdbCountryContentRatingDto { Iso31661 = "US", Rating = "TV-MA" } } }
            });

            var result = await RunAsync();

            result.UpdatedCount.Should().Be(1);
            var updated = Context.TvShows.First(t => t.Id == show.Id);
            updated.NumberOfSeasons.Should().Be(2);
            updated.NumberOfEpisodes.Should().Be(19);
            updated.LastAirYear.Should().Be(2025);
            updated.TmdbPosterPath.Should().Be("/new.jpg");
            updated.Creator.Should().Be("Dan Erickson");
            updated.ContentRating.Should().Be("TV-14");
            updated.Genres.Select(g => g.Name).Should().BeEquivalentTo(new[] { "science fiction", "fantasy" });
        }

        [Fact]
        public async Task RefreshStaleAsync_WhenTmdbHasNothingNew_ShouldCountUnchanged_ButStillStamp()
        {
            var movie = await AddMovieAsync("Inception", "27205", DateTime.UtcNow.AddDays(-200), m =>
            {
                m.TmdbRating = 8.4;
                m.Link = "https://www.themoviedb.org/movie/27205";
                m.Genres.Add(new Genre { Name = "action" });
            });
            SetupMovie(27205, new TmdbMovieDto { Title = "Inception", VoteAverage = 8.4 });

            var result = await RunAsync();

            result.UnchangedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(0);
            Context.Movies.First(m => m.Id == movie.Id).TmdbRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        #endregion

        #region Failures

        [Fact]
        public async Task RefreshStaleAsync_Tmdb404_ShouldCountFailed_LeaveTheItemUnstamped_AndKeepGoing()
        {
            var gone = await AddMovieAsync("Removed Upstream", "1", null);
            var fine = await AddMovieAsync("Still There", "2", DateTime.UtcNow.AddDays(-200));
            _mockTmdbClient.GetMovieDetailsAsync(1, Arg.Any<string>())
                .ThrowsAsync(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));
            SetupMovie(2, new TmdbMovieDto { Title = "Still There", VoteAverage = 7.1 });

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.FailedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().Contain("Removed Upstream");
            result.RemainingCount.Should().Be(1);
            Context.Movies.First(m => m.Id == gone.Id).TmdbRefreshedAt.Should().BeNull();
            Context.Movies.First(m => m.Id == fine.Id).TmdbRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldCapErrorsAtTwenty()
        {
            for (var i = 1; i <= 25; i++)
            {
                await AddMovieAsync($"Movie {i}", i.ToString(), null);
            }
            _mockTmdbClient.GetMovieDetailsAsync(Arg.Any<int>(), Arg.Any<string>())
                .ThrowsAsync(new HttpRequestException("Service Unavailable", null, HttpStatusCode.ServiceUnavailable));

            var result = await RunAsync();

            result.FailedCount.Should().Be(25);
            result.Errors.Should().HaveCount(20);
        }

        #endregion
    }
}
