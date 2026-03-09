using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Infrastructure.Services;
using ProjectLoopbreaker.Shared.DTOs.Trakt;
using ProjectLoopbreaker.Shared.Interfaces;
using ProjectLoopbreaker.UnitTests.TestHelpers;

namespace ProjectLoopbreaker.UnitTests.Infrastructure
{
    public class TraktSyncServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<ITraktApiClient> _mockTraktClient;
        private readonly Mock<ILogger<TraktSyncService>> _mockLogger;
        private readonly TraktSyncService _service;

        public TraktSyncServiceTests()
        {
            _mockTraktClient = new Mock<ITraktApiClient>();
            _mockLogger = new Mock<ILogger<TraktSyncService>>();
            _service = new TraktSyncService(Context, _mockTraktClient.Object, _mockLogger.Object);
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
            _mockTraktClient.Setup(c => c.RefreshTokenAsync("test-refresh"))
                .ReturnsAsync(new TraktOAuthTokenDto
                {
                    AccessToken = "refreshed-token",
                    RefreshToken = "new-refresh",
                    ExpiresIn = 7776000,
                    CreatedAt = unixTimestamp
                });

            var result = await _service.GetValidAccessTokenAsync();

            result.Should().Be("refreshed-token");
            _mockTraktClient.Verify(c => c.RefreshTokenAsync("test-refresh"), Times.Once);
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

            _mockTraktClient.Setup(c => c.RefreshTokenAsync("test-refresh"))
                .ThrowsAsync(new Exception("Refresh failed"));

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

            _mockTraktClient.Setup(c => c.RefreshTokenAsync("test-refresh"))
                .ThrowsAsync(new Exception("Refresh failed"));

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
            _mockTraktClient.Verify(c => c.RevokeTokenAsync("test-token"), Times.Once);
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

            _mockTraktClient.Setup(c => c.RevokeTokenAsync("test-token"))
                .ThrowsAsync(new Exception("Revoke failed"));

            await _service.DisconnectAsync();

            Context.TraktTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task DisconnectAsync_NoToken_DoesNothing()
        {
            await _service.DisconnectAsync();

            _mockTraktClient.Verify(c => c.RevokeTokenAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

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

        #region SyncWatchedAsync - Shows

        [Fact]
        public async Task SyncWatchedAsync_NewShow_CreatesWithActivelyExploringStatus()
        {
            await SetupValidToken();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("New Show", 2023, tmdbId: 55555, traktId: 777, slug: "new-show",
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1, lastWatchedAt: DateTime.UtcNow.AddDays(-3)),
                            CreateWatchedEpisodeDto(2, plays: 1, lastWatchedAt: DateTime.UtcNow.AddDays(-2))
                        })
                    })
            });

            var result = await _service.SyncWatchedAsync();

            result.Success.Should().BeTrue();
            result.ShowsCreated.Should().Be(1);
            result.EpisodesCreated.Should().Be(2);

            var show = Context.TvShows.First(s => s.Title == "New Show");
            show.Status.Should().Be(Status.ActivelyExploring);
            show.MediaType.Should().Be(MediaType.TVShow);
            show.TmdbId.Should().Be("55555");
            show.TraktId.Should().Be(777);
            show.TraktSlug.Should().Be("new-show");
            show.TraktPlays.Should().Be(2); // Sum of episode plays
        }

        [Fact]
        public async Task SyncWatchedAsync_ShowEpisodes_CreatesWithCorrectTitle()
        {
            await SetupValidToken();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Episode Title Test", 2023, tmdbId: 44444,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(2, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(5, plays: 2, lastWatchedAt: DateTime.UtcNow)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var episode = Context.TvShowEpisodes.First();
            episode.Title.Should().Be("S2E5");
            episode.SeasonNumber.Should().Be(2);
            episode.EpisodeNumber.Should().Be(5);
            episode.Status.Should().Be(Status.Completed);
            episode.TraktPlays.Should().Be(2);
            episode.MediaType.Should().Be(MediaType.TVShow);
        }

        [Fact]
        public async Task SyncWatchedAsync_ExistingEpisode_UpdatesInsteadOfDuplicating()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Existing Show",
                TmdbId = "33333",
                MediaType = MediaType.TVShow,
                Status = Status.ActivelyExploring,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            var existingEpisode = new TvShowEpisode
            {
                Id = Guid.NewGuid(),
                Title = "S1E1",
                ShowId = show.Id,
                SeasonNumber = 1,
                EpisodeNumber = 1,
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                TraktPlays = 1,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShowEpisodes.Add(existingEpisode);
            await Context.SaveChangesAsync();

            var newLastWatched = DateTime.UtcNow.AddDays(-1);
            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Existing Show", 2023, tmdbId: 33333,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 3, lastWatchedAt: newLastWatched)
                        })
                    })
            });

            var result = await _service.SyncWatchedAsync();

            result.EpisodesUpdated.Should().Be(1);
            result.EpisodesCreated.Should().Be(0);

            var episodes = Context.TvShowEpisodes.Where(e => e.ShowId == show.Id).ToList();
            episodes.Should().HaveCount(1);
            episodes[0].TraktPlays.Should().Be(3);
            episodes[0].TraktLastWatchedAt.Should().Be(newLastWatched);
            episodes[0].Status.Should().Be(Status.Completed);
        }

        [Fact]
        public async Task SyncWatchedAsync_AllEpisodesWatched_SetsShowToCompleted()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Short Show",
                TmdbId = "22222",
                MediaType = MediaType.TVShow,
                Status = Status.ActivelyExploring,
                NumberOfEpisodes = 3,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Short Show", 2023, tmdbId: 22222,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1),
                            CreateWatchedEpisodeDto(2, plays: 1),
                            CreateWatchedEpisodeDto(3, plays: 1)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var updated = Context.TvShows.First(s => s.TmdbId == "22222");
            updated.Status.Should().Be(Status.Completed);
        }

        [Fact]
        public async Task SyncWatchedAsync_NotAllEpisodesWatched_KeepsActivelyExploring()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Long Show",
                TmdbId = "88888",
                MediaType = MediaType.TVShow,
                Status = Status.ActivelyExploring,
                NumberOfEpisodes = 100,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Long Show", 2023, tmdbId: 88888,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var updated = Context.TvShows.First(s => s.TmdbId == "88888");
            updated.Status.Should().Be(Status.ActivelyExploring);
        }

        [Fact]
        public async Task SyncWatchedAsync_ShowAlreadyCompleted_DoesNotDowngradeStatus()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Completed Show",
                TmdbId = "77777",
                MediaType = MediaType.TVShow,
                Status = Status.Completed,
                NumberOfEpisodes = 10,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Completed Show", 2023, tmdbId: 77777,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var updated = Context.TvShows.First(s => s.TmdbId == "77777");
            updated.Status.Should().Be(Status.Completed); // Not downgraded
        }

        [Fact]
        public async Task SyncWatchedAsync_ShowAlreadyAbandoned_DoesNotDowngradeStatus()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Abandoned Show",
                TmdbId = "66666",
                MediaType = MediaType.TVShow,
                Status = Status.Abandoned,
                NumberOfEpisodes = 5,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Abandoned Show", 2023, tmdbId: 66666,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1),
                            CreateWatchedEpisodeDto(2, plays: 1),
                            CreateWatchedEpisodeDto(3, plays: 1),
                            CreateWatchedEpisodeDto(4, plays: 1),
                            CreateWatchedEpisodeDto(5, plays: 1)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var updated = Context.TvShows.First(s => s.TmdbId == "66666");
            updated.Status.Should().Be(Status.Abandoned); // Not changed even with all episodes watched
        }

        [Fact]
        public async Task SyncWatchedAsync_ShowTraktPlays_IsSumOfAllEpisodePlays()
        {
            await SetupValidToken();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Plays Test Show", 2023, tmdbId: 11111,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 3),
                            CreateWatchedEpisodeDto(2, plays: 2)
                        }),
                        CreateWatchedSeasonDto(2, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 5)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var show = Context.TvShows.First(s => s.Title == "Plays Test Show");
            show.TraktPlays.Should().Be(10); // 3 + 2 + 5
        }

        [Fact]
        public async Task SyncWatchedAsync_ExistingShowByTitleAndYear_MatchesFallback()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Fallback Show",
                FirstAirYear = 2020,
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Fallback Show", 2020, tmdbId: null, traktId: 888,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1)
                        })
                    })
            });

            var result = await _service.SyncWatchedAsync();

            result.ShowsUpdated.Should().Be(1);
            result.ShowsCreated.Should().Be(0);

            var updated = Context.TvShows.First(s => s.Title == "Fallback Show");
            updated.TraktId.Should().Be(888);
        }

        [Fact]
        public async Task SyncWatchedAsync_ShowWithUnchartedStatus_ChangesToActivelyExploring()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Uncharted Show",
                TmdbId = "50000",
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                NumberOfEpisodes = 10,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupWatchedMovies(new List<TraktWatchedMovieDto>());
            SetupWatchedShows(new List<TraktWatchedShowDto>
            {
                CreateWatchedShowDto("Uncharted Show", 2023, tmdbId: 50000,
                    seasons: new List<TraktWatchedSeasonDto>
                    {
                        CreateWatchedSeasonDto(1, new List<TraktWatchedEpisodeDto>
                        {
                            CreateWatchedEpisodeDto(1, plays: 1)
                        })
                    })
            });

            await _service.SyncWatchedAsync();

            var updated = Context.TvShows.First(s => s.TmdbId == "50000");
            updated.Status.Should().Be(Status.ActivelyExploring);
        }

        #endregion

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

        #region SyncRatingsAsync

        [Fact]
        public async Task SyncRatingsAsync_NoToken_ReturnsErrorMessage()
        {
            var result = await _service.SyncRatingsAsync();

            result.ErrorMessage.Should().Be("Not connected to Trakt");
        }

        [Fact]
        public async Task SyncRatingsAsync_MovieNotFound_SkipsWithoutCreating()
        {
            await SetupValidToken();

            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 8,
                    Movie = new TraktMovieDto
                    {
                        Title = "Nonexistent Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 99999 }
                    }
                }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            var result = await _service.SyncRatingsAsync();

            result.Success.Should().BeTrue();
            result.RatingsProcessed.Should().Be(0);
            Context.Movies.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncRatingsAsync_MovieFound_StoresTraktRating()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Rated Movie",
                TmdbId = "50000",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 7,
                    Movie = new TraktMovieDto
                    {
                        Title = "Rated Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 50000, Trakt = 600 }
                    }
                }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            var result = await _service.SyncRatingsAsync();

            result.RatingsProcessed.Should().Be(1);
            result.MoviesUpdated.Should().Be(1);

            var updated = Context.Movies.First(m => m.TmdbId == "50000");
            updated.TraktRating.Should().Be(7);
            updated.TraktId.Should().Be(600);
        }

        [Fact]
        public async Task SyncRatingsAsync_MovieWithNoRating_MapsToAppRating()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Unrated Movie",
                TmdbId = "51000",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                Rating = null,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 9,
                    Movie = new TraktMovieDto
                    {
                        Title = "Unrated Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 51000 }
                    }
                }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            await _service.SyncRatingsAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "51000");
            updated.Rating.Should().Be(Rating.SuperLike); // 9 -> SuperLike
        }

        [Fact]
        public async Task SyncRatingsAsync_MovieWithExistingRating_DoesNotOverwriteAppRating()
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Already Rated Movie",
                TmdbId = "52000",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                Rating = Rating.Dislike,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 10,
                    Movie = new TraktMovieDto
                    {
                        Title = "Already Rated Movie",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 52000 }
                    }
                }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            await _service.SyncRatingsAsync();

            var updated = Context.Movies.First(m => m.TmdbId == "52000");
            updated.TraktRating.Should().Be(10); // TraktRating always stored
            updated.Rating.Should().Be(Rating.Dislike); // App rating NOT overwritten
        }

        [Theory]
        [InlineData(1, Rating.Dislike)]
        [InlineData(2, Rating.Dislike)]
        [InlineData(3, Rating.Dislike)]
        [InlineData(4, Rating.Neutral)]
        [InlineData(5, Rating.Neutral)]
        [InlineData(6, Rating.Like)]
        [InlineData(7, Rating.Like)]
        [InlineData(8, Rating.Like)]
        [InlineData(9, Rating.SuperLike)]
        [InlineData(10, Rating.SuperLike)]
        public async Task SyncRatingsAsync_RatingMapping_MapsCorrectly(int traktRating, Rating expectedAppRating)
        {
            await SetupValidToken();

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = $"Rating Test {traktRating}",
                TmdbId = $"{60000 + traktRating}",
                MediaType = MediaType.Movie,
                Status = Status.Completed,
                Rating = null,
                DateAdded = DateTime.UtcNow
            };
            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = traktRating,
                    Movie = new TraktMovieDto
                    {
                        Title = $"Rating Test {traktRating}",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 60000 + traktRating }
                    }
                }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            await _service.SyncRatingsAsync();

            var updated = Context.Movies.First(m => m.TmdbId == $"{60000 + traktRating}");
            updated.Rating.Should().Be(expectedAppRating);
        }

        [Fact]
        public async Task SyncRatingsAsync_ShowFound_StoresTraktRating()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Rated Show",
                TmdbId = "53000",
                MediaType = MediaType.TVShow,
                Status = Status.Completed,
                Rating = null,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupRatingsMovies(new List<TraktRatingItemDto>());
            SetupRatingsShows(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 6,
                    Show = new TraktShowDto
                    {
                        Title = "Rated Show",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 53000, Trakt = 700 }
                    }
                }
            });

            var result = await _service.SyncRatingsAsync();

            result.RatingsProcessed.Should().Be(1);
            result.ShowsUpdated.Should().Be(1);

            var updated = Context.TvShows.First(s => s.TmdbId == "53000");
            updated.TraktRating.Should().Be(6);
            updated.TraktId.Should().Be(700);
            updated.Rating.Should().Be(Rating.Like); // 6 -> Like
        }

        [Fact]
        public async Task SyncRatingsAsync_ShowNotFound_Skips()
        {
            await SetupValidToken();

            SetupRatingsMovies(new List<TraktRatingItemDto>());
            SetupRatingsShows(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 8,
                    Show = new TraktShowDto
                    {
                        Title = "Missing Show",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 99998 }
                    }
                }
            });

            var result = await _service.SyncRatingsAsync();

            result.RatingsProcessed.Should().Be(0);
            Context.TvShows.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncRatingsAsync_ShowWithExistingRating_DoesNotOverwriteAppRating()
        {
            await SetupValidToken();

            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Already Rated Show",
                TmdbId = "54000",
                MediaType = MediaType.TVShow,
                Status = Status.Completed,
                Rating = Rating.Like,
                DateAdded = DateTime.UtcNow
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();

            SetupRatingsMovies(new List<TraktRatingItemDto>());
            SetupRatingsShows(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto
                {
                    Rating = 2,
                    Show = new TraktShowDto
                    {
                        Title = "Already Rated Show",
                        Year = 2023,
                        Ids = new TraktIdsDto { Tmdb = 54000 }
                    }
                }
            });

            await _service.SyncRatingsAsync();

            var updated = Context.TvShows.First(s => s.TmdbId == "54000");
            updated.TraktRating.Should().Be(2); // TraktRating always stored
            updated.Rating.Should().Be(Rating.Like); // App rating NOT overwritten
        }

        [Fact]
        public async Task SyncRatingsAsync_NullMovieInRatingItem_Skips()
        {
            await SetupValidToken();

            SetupRatingsMovies(new List<TraktRatingItemDto>
            {
                new TraktRatingItemDto { Rating = 5, Movie = null }
            });
            SetupRatingsShows(new List<TraktRatingItemDto>());

            var result = await _service.SyncRatingsAsync();

            result.Success.Should().BeTrue();
            result.RatingsProcessed.Should().Be(0);
        }

        #endregion

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
            _mockTraktClient.Setup(c => c.GetWatchedMoviesAsync(It.IsAny<string>()))
                .ReturnsAsync(movies);
        }

        private void SetupWatchedShows(List<TraktWatchedShowDto> shows)
        {
            _mockTraktClient.Setup(c => c.GetWatchedShowsAsync(It.IsAny<string>()))
                .ReturnsAsync(shows);
        }

        private void SetupWatchlistMovies(List<TraktWatchlistItemDto> items)
        {
            _mockTraktClient.Setup(c => c.GetWatchlistMoviesAsync(It.IsAny<string>()))
                .ReturnsAsync(items);
        }

        private void SetupWatchlistShows(List<TraktWatchlistItemDto> items)
        {
            _mockTraktClient.Setup(c => c.GetWatchlistShowsAsync(It.IsAny<string>()))
                .ReturnsAsync(items);
        }

        private void SetupRatingsMovies(List<TraktRatingItemDto> ratings)
        {
            _mockTraktClient.Setup(c => c.GetRatingsMoviesAsync(It.IsAny<string>()))
                .ReturnsAsync(ratings);
        }

        private void SetupRatingsShows(List<TraktRatingItemDto> ratings)
        {
            _mockTraktClient.Setup(c => c.GetRatingsShowsAsync(It.IsAny<string>()))
                .ReturnsAsync(ratings);
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
