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
    }
}
