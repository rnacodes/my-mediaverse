using AwesomeAssertions;
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
    [Trait("Category", "Unit")]
    public partial class TraktSyncServiceTests : InMemoryDbTestBase
    {
        private readonly ITraktApiClient _mockTraktClient;
        private readonly ILogger<TraktSyncService> _mockLogger;
        private readonly TraktSyncService _service;

        public TraktSyncServiceTests()
        {
            _mockTraktClient = Substitute.For<ITraktApiClient>();
            _mockLogger = Substitute.For<ILogger<TraktSyncService>>();
            _service = new TraktSyncService(Context, _mockTraktClient, _mockLogger);
        }

        #region IsConnectedAsync

        [Fact]
        public async Task IsConnectedAsync_NoToken_ReturnsFalse()
        {
            var result = await _service.IsConnectedAsync();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsConnectedAsync_TokenExists_ReturnsTrue()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            var result = await _service.IsConnectedAsync();

            result.Should().BeTrue();
        }

        #endregion

        #region GetStatusAsync

        [Fact]
        public async Task GetStatusAsync_NoToken_ReturnsNotConnected()
        {
            var result = await _service.GetStatusAsync();

            result.Connected.Should().BeFalse();
            result.Username.Should().BeNull();
        }

        [Fact]
        public async Task GetStatusAsync_TokenExists_ReturnsConnectedWithUsername()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
                TraktUsername = "testuser"
            });
            await Context.SaveChangesAsync();

            var result = await _service.GetStatusAsync();

            result.Connected.Should().BeTrue();
            result.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetStatusAsync_TokenExistsWithoutUsername_ReturnsConnectedWithNullUsername()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            var result = await _service.GetStatusAsync();

            result.Connected.Should().BeTrue();
            result.Username.Should().BeNull();
        }

        #endregion

        #region SaveTokenAsync

        [Fact]
        public async Task SaveTokenAsync_NoExistingToken_CreatesNewToken()
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var dto = new TraktOAuthTokenDto
            {
                AccessToken = "new-access",
                RefreshToken = "new-refresh",
                ExpiresIn = 7776000, // 90 days
                CreatedAt = unixTimestamp
            };

            await _service.SaveTokenAsync(dto);

            var token = Context.TraktTokens.FirstOrDefault();
            token.Should().NotBeNull();
            token!.AccessToken.Should().Be("new-access");
            token.RefreshToken.Should().Be("new-refresh");
            var expectedExpiry = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp + 7776000).UtcDateTime;
            token.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task SaveTokenAsync_ExistingToken_UpdatesToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "old-access",
                RefreshToken = "old-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var dto = new TraktOAuthTokenDto
            {
                AccessToken = "updated-access",
                RefreshToken = "updated-refresh",
                ExpiresIn = 7776000,
                CreatedAt = unixTimestamp
            };

            await _service.SaveTokenAsync(dto);

            var tokens = Context.TraktTokens.ToList();
            tokens.Should().HaveCount(1);
            tokens[0].AccessToken.Should().Be("updated-access");
            tokens[0].RefreshToken.Should().Be("updated-refresh");
        }

        #endregion

        #region GetValidAccessTokenAsync

        [Fact]
        public async Task GetValidAccessTokenAsync_NoToken_ReturnsNull()
        {
            var result = await _service.GetValidAccessTokenAsync();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetValidAccessTokenAsync_ValidToken_ReturnsAccessToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "valid-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            var result = await _service.GetValidAccessTokenAsync();

            result.Should().Be("valid-token");
        }

        [Fact]
        public async Task GetValidAccessTokenAsync_TokenExpiringSoon_RefreshesToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "expiring-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Less than 1 hour
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _mockTraktClient.RefreshTokenAsync("test-refresh")
                .Returns(new TraktOAuthTokenDto
                {
                    AccessToken = "refreshed-token",
                    RefreshToken = "new-refresh",
                    ExpiresIn = 7776000,
                    CreatedAt = unixTimestamp
                });

            var result = await _service.GetValidAccessTokenAsync();

            result.Should().Be("refreshed-token");
            _mockTraktClient.Received(1).RefreshTokenAsync("test-refresh");
        }

        [Fact]
        public async Task GetValidAccessTokenAsync_RefreshFailsButTokenNotExpired_ReturnsExistingToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "still-valid-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Less than 1 hour but not expired
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            _mockTraktClient.RefreshTokenAsync("test-refresh")
                .Throws(new Exception("Refresh failed"));

            var result = await _service.GetValidAccessTokenAsync();

            result.Should().Be("still-valid-token");
        }

        [Fact]
        public async Task GetValidAccessTokenAsync_RefreshFailsAndTokenExpired_ReturnsNull()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "expired-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Already expired
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            });
            await Context.SaveChangesAsync();

            _mockTraktClient.RefreshTokenAsync("test-refresh")
                .Throws(new Exception("Refresh failed"));

            var result = await _service.GetValidAccessTokenAsync();

            result.Should().BeNull();
        }

        #endregion

        #region DisconnectAsync

        [Fact]
        public async Task DisconnectAsync_TokenExists_RevokesAndRemovesToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            await _service.DisconnectAsync();

            Context.TraktTokens.Should().BeEmpty();
            _mockTraktClient.Received(1).RevokeTokenAsync("test-token");
        }

        [Fact]
        public async Task DisconnectAsync_RevokeThrows_StillRemovesToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            _mockTraktClient.RevokeTokenAsync("test-token")
                .Throws(new Exception("Revoke failed"));

            await _service.DisconnectAsync();

            Context.TraktTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task DisconnectAsync_NoToken_DoesNothing()
        {
            await _service.DisconnectAsync();

            _mockTraktClient.DidNotReceive().RevokeTokenAsync(Arg.Any<string>());
        }

        #endregion

        #region Helper Methods

        private async Task SetupValidToken()
        {
            Context.TraktTokens.Add(new TraktToken
            {
                AccessToken = "valid-token",
                RefreshToken = "valid-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();
        }

        private void SetupWatchedMovies(List<TraktWatchedMovieDto> movies)
        {
            _mockTraktClient.GetWatchedMoviesAsync(Arg.Any<string>())
                .Returns(movies);
        }

        private void SetupWatchedShows(List<TraktWatchedShowDto> shows)
        {
            _mockTraktClient.GetWatchedShowsAsync(Arg.Any<string>())
                .Returns(shows);
        }

        private void SetupWatchlistMovies(List<TraktWatchlistItemDto> items)
        {
            _mockTraktClient.GetWatchlistMoviesAsync(Arg.Any<string>())
                .Returns(items);
        }

        private void SetupWatchlistShows(List<TraktWatchlistItemDto> items)
        {
            _mockTraktClient.GetWatchlistShowsAsync(Arg.Any<string>())
                .Returns(items);
        }

        private void SetupRatingsMovies(List<TraktRatingItemDto> ratings)
        {
            _mockTraktClient.GetRatingsMoviesAsync(Arg.Any<string>())
                .Returns(ratings);
        }

        private void SetupRatingsShows(List<TraktRatingItemDto> ratings)
        {
            _mockTraktClient.GetRatingsShowsAsync(Arg.Any<string>())
                .Returns(ratings);
        }

        private static TraktWatchedMovieDto CreateWatchedMovieDto(
            string title, int? year = null, int? tmdbId = null, int? traktId = null,
            string? slug = null, int plays = 1, DateTime? lastWatchedAt = null, string? imdbId = null)
        {
            return new TraktWatchedMovieDto
            {
                Plays = plays,
                LastWatchedAt = lastWatchedAt ?? DateTime.UtcNow,
                Movie = new TraktMovieDto
                {
                    Title = title,
                    Year = year,
                    Ids = new TraktIdsDto
                    {
                        Tmdb = tmdbId,
                        Trakt = traktId,
                        Slug = slug,
                        Imdb = imdbId
                    }
                }
            };
        }

        private static TraktWatchedShowDto CreateWatchedShowDto(
            string title, int? year = null, int? tmdbId = null, int? traktId = null,
            string? slug = null, List<TraktWatchedSeasonDto>? seasons = null)
        {
            return new TraktWatchedShowDto
            {
                Plays = 0,
                LastWatchedAt = DateTime.UtcNow,
                Show = new TraktShowDto
                {
                    Title = title,
                    Year = year,
                    Ids = new TraktIdsDto
                    {
                        Tmdb = tmdbId,
                        Trakt = traktId,
                        Slug = slug
                    }
                },
                Seasons = seasons ?? new List<TraktWatchedSeasonDto>()
            };
        }

        private static TraktWatchedSeasonDto CreateWatchedSeasonDto(int number, List<TraktWatchedEpisodeDto> episodes)
        {
            return new TraktWatchedSeasonDto
            {
                Number = number,
                Episodes = episodes
            };
        }

        private static TraktWatchedEpisodeDto CreateWatchedEpisodeDto(int number, int plays = 1, DateTime? lastWatchedAt = null)
        {
            return new TraktWatchedEpisodeDto
            {
                Number = number,
                Plays = plays,
                LastWatchedAt = lastWatchedAt ?? DateTime.UtcNow
            };
        }

        #endregion
    }
}
