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
    public partial class TraktSyncServiceTests
    {
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
    }
}
