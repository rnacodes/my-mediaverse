using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Infrastructure.Services;
using ProjectLoopbreaker.Shared.DTOs.TMDB;
using ProjectLoopbreaker.Shared.Interfaces;
using ProjectLoopbreaker.UnitTests.TestData;
using ProjectLoopbreaker.UnitTests.TestHelpers;

namespace ProjectLoopbreaker.UnitTests.Infrastructure
{
    public class MovieTvEnrichmentServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ITmdbApiClient> _mockTmdbClient;
        private readonly Mock<ILogger<MovieTvEnrichmentService>> _mockLogger;
        private readonly MovieTvEnrichmentService _service;

        public MovieTvEnrichmentServiceTests()
        {
            _mockTmdbClient = new Mock<ITmdbApiClient>();
            _mockLogger = new Mock<ILogger<MovieTvEnrichmentService>>();
            _service = new MovieTvEnrichmentService(Context, _mockTmdbClient.Object, _mockLogger.Object);
        }

        #region GetMoviesNeedingEnrichmentCountAsync

        [Fact]
        public async Task GetMoviesNeedingEnrichmentCountAsync_NoMovies_ReturnsZero()
        {
            var result = await _service.GetMoviesNeedingEnrichmentCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetMoviesNeedingEnrichmentCountAsync_MoviesWithoutTmdbId_ReturnsCount()
        {
            var movieNeedsEnrichment = TestDataFactory.CreateMovie("Movie 1");
            movieNeedsEnrichment.TmdbId = null;

            var movieAlreadyEnriched = TestDataFactory.CreateMovie("Movie 2");
            movieAlreadyEnriched.TmdbId = "12345";

            Context.Movies.AddRange(movieNeedsEnrichment, movieAlreadyEnriched);
            await Context.SaveChangesAsync();

            var result = await _service.GetMoviesNeedingEnrichmentCountAsync();

            result.Should().Be(1);
        }

        #endregion

        #region GetTvShowsNeedingEnrichmentCountAsync

        [Fact]
        public async Task GetTvShowsNeedingEnrichmentCountAsync_NoTvShows_ReturnsZero()
        {
            var result = await _service.GetTvShowsNeedingEnrichmentCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetTvShowsNeedingEnrichmentCountAsync_TvShowsWithoutTmdbId_ReturnsCount()
        {
            var tvShowNeedsEnrichment = TestDataFactory.CreateTvShow("Show 1");
            tvShowNeedsEnrichment.TmdbId = null;

            var tvShowAlreadyEnriched = TestDataFactory.CreateTvShow("Show 2");
            tvShowAlreadyEnriched.TmdbId = "67890";

            Context.TvShows.AddRange(tvShowNeedsEnrichment, tvShowAlreadyEnriched);
            await Context.SaveChangesAsync();

            var result = await _service.GetTvShowsNeedingEnrichmentCountAsync();

            result.Should().Be(1);
        }

        #endregion

        #region EnrichMoviesWithoutTmdbDataAsync

        [Fact]
        public async Task EnrichMoviesWithoutTmdbDataAsync_NoMoviesNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.EnrichMoviesWithoutTmdbDataAsync();

            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task EnrichMoviesWithoutTmdbDataAsync_WithMovie_SearchesAndEnriches()
        {
            var movie = TestDataFactory.CreateMovie("The Matrix");
            movie.TmdbId = null;
            movie.Description = null;
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            _mockTmdbClient.Setup(c => c.SearchMoviesAsync("The Matrix", It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TmdbMovieSearchResultDto
                {
                    Results = new[]
                    {
                        new TmdbMovieDto
                        {
                            Id = 603,
                            Title = "The Matrix",
                            Overview = "A computer hacker learns about the true nature of reality.",
                            VoteAverage = 8.7,
                            ReleaseDate = "1999-03-31"
                        }
                    },
                    TotalResults = 1
                });

            _mockTmdbClient.Setup(c => c.GetMovieDetailsAsync(603, It.IsAny<string>()))
                .ReturnsAsync(new TmdbMovieDto
                {
                    Id = 603,
                    Title = "The Matrix",
                    Overview = "A computer hacker learns about the true nature of reality.",
                    VoteAverage = 8.7,
                    Runtime = 136,
                    ReleaseDate = "1999-03-31",
                    ImdbId = "tt0133093",
                    Tagline = "Welcome to the Real World.",
                    BackdropPath = "/backdrop.jpg",
                    PosterPath = "/poster.jpg"
                });

            var result = await _service.EnrichMoviesWithoutTmdbDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);

            var updatedMovie = Context.Movies.First(m => m.Id == movie.Id);
            updatedMovie.TmdbId.Should().Be("603");
            updatedMovie.Description.Should().Be("A computer hacker learns about the true nature of reality.");
        }

        [Fact]
        public async Task EnrichMoviesWithoutTmdbDataAsync_NoSearchResults_IncrementsNotFound()
        {
            var movie = TestDataFactory.CreateMovie("Unknown Movie XYZ");
            movie.TmdbId = null;
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            _mockTmdbClient.Setup(c => c.SearchMoviesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TmdbMovieSearchResultDto
                {
                    Results = Array.Empty<TmdbMovieDto>(),
                    TotalResults = 0
                });

            var result = await _service.EnrichMoviesWithoutTmdbDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.NotFoundCount.Should().Be(1);
            result.EnrichedCount.Should().Be(0);
        }

        [Fact]
        public async Task EnrichMoviesWithoutTmdbDataAsync_RespectsCancellation()
        {
            var movie = TestDataFactory.CreateMovie("Test Movie");
            movie.TmdbId = null;
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.EnrichMoviesWithoutTmdbDataAsync(cancellationToken: cts.Token);

            result.WasCancelled.Should().BeTrue();
        }

        #endregion

        #region EnrichTvShowsWithoutTmdbDataAsync

        [Fact]
        public async Task EnrichTvShowsWithoutTmdbDataAsync_NoTvShowsNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.EnrichTvShowsWithoutTmdbDataAsync();

            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task EnrichTvShowsWithoutTmdbDataAsync_WithTvShow_SearchesAndEnriches()
        {
            var tvShow = TestDataFactory.CreateTvShow("Breaking Bad");
            tvShow.TmdbId = null;
            tvShow.Description = null;
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            _mockTmdbClient.Setup(c => c.SearchTvShowsAsync("Breaking Bad", It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TmdbTvSearchResultDto
                {
                    Results = new[]
                    {
                        new TmdbTvShowDto
                        {
                            Id = 1396,
                            Name = "Breaking Bad",
                            Overview = "A high school chemistry teacher turned meth dealer.",
                            VoteAverage = 9.5,
                            FirstAirDate = "2008-01-20",
                            NumberOfSeasons = 5,
                            NumberOfEpisodes = 62
                        }
                    },
                    TotalResults = 1
                });

            _mockTmdbClient.Setup(c => c.GetTvShowDetailsAsync(1396, It.IsAny<string>()))
                .ReturnsAsync(new TmdbTvShowDto
                {
                    Id = 1396,
                    Name = "Breaking Bad",
                    Overview = "A high school chemistry teacher turned meth dealer.",
                    VoteAverage = 9.5,
                    FirstAirDate = "2008-01-20",
                    LastAirDate = "2013-09-29",
                    NumberOfSeasons = 5,
                    NumberOfEpisodes = 62,
                    Tagline = "All Hail the King",
                    PosterPath = "/poster.jpg"
                });

            var result = await _service.EnrichTvShowsWithoutTmdbDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);

            var updatedTvShow = Context.TvShows.First(t => t.Id == tvShow.Id);
            updatedTvShow.TmdbId.Should().Be("1396");
            updatedTvShow.Description.Should().Be("A high school chemistry teacher turned meth dealer.");
        }

        [Fact]
        public async Task EnrichTvShowsWithoutTmdbDataAsync_PreservesExistingData()
        {
            var tvShow = TestDataFactory.CreateTvShow("Test Show");
            tvShow.TmdbId = null;
            tvShow.Description = "Existing description";
            Context.TvShows.Add(tvShow);
            await Context.SaveChangesAsync();

            _mockTmdbClient.Setup(c => c.SearchTvShowsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TmdbTvSearchResultDto
                {
                    Results = new[]
                    {
                        new TmdbTvShowDto
                        {
                            Id = 100,
                            Name = "Test Show",
                            Overview = "New description from TMDB"
                        }
                    },
                    TotalResults = 1
                });

            _mockTmdbClient.Setup(c => c.GetTvShowDetailsAsync(100, It.IsAny<string>()))
                .ReturnsAsync(new TmdbTvShowDto
                {
                    Id = 100,
                    Name = "Test Show",
                    Overview = "New description from TMDB",
                    NumberOfSeasons = 3
                });

            await _service.EnrichTvShowsWithoutTmdbDataAsync(batchSize: 10, delayBetweenCallsMs: 0);

            var updatedTvShow = Context.TvShows.First(t => t.Id == tvShow.Id);
            // Existing description should be preserved (only updates null fields)
            updatedTvShow.Description.Should().Be("Existing description");
        }

        #endregion
    }
}
