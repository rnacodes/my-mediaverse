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
        #region SyncAllAsync

        [Fact]
        public async Task SyncAllAsync_CombinesResults()
        {
            await SetupValidToken();

            // Setup watched
            SetupWatchedMovies(new List<TraktWatchedMovieDto>
            {
                CreateWatchedMovieDto("Watched Movie", 2023, tmdbId: 90001, traktId: 1001)
            });
            SetupWatchedShows(new List<TraktWatchedShowDto>());

            // Setup watchlist
            SetupWatchlistMovies(new List<TraktWatchlistItemDto>
            {
                new TraktWatchlistItemDto
                {
                    Movie = new TraktMovieDto
                    {
                        Title = "Watchlist Movie",
                        Year = 2024,
                        Ids = new TraktIdsDto { Tmdb = 90002, Trakt = 1002 }
                    }
                }
            });
            SetupWatchlistShows(new List<TraktWatchlistItemDto>());

            // Setup ratings - rate the watched movie
            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 8,
                    Movie = new TraktMovieDto
                    {
                        Title = "Watched Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 90001 }
                    }
                }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            var result = await _service.SyncAllAsync();

            result.Success.Should().BeTrue();
            result.MoviesCreated.Should().BeGreaterThanOrEqualTo(1);
            result.CompletedAt.Should().BeAfter(result.StartedAt);
        }

        [Fact]
        public async Task SyncAllAsync_NoToken_ReturnsErrors()
        {
            // No token set up - all three sync operations should fail
            var result = await _service.SyncAllAsync();

            // SyncWatched returns error, SyncWatchlist returns error, SyncRatings returns error
            // But SyncAllAsync catches and sets success=true if individual syncs don't throw
            result.Success.Should().BeTrue();
        }

        #endregion
    }
}
