using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    /// Pins the upsert rules of the TMDB episode import: new episodes are created uncharted,
    /// stored episodes are only filled in (a placeholder title is the one value replaced), and
    /// a failed season never stops the rest of the run.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TvEpisodeImportServiceTests : InMemoryDbTestBase
    {
        private readonly ITmdbApiClient _tmdbClient = Substitute.For<ITmdbApiClient>();
        private readonly TvEpisodeImportService _service;

        public TvEpisodeImportServiceTests()
        {
            _tmdbClient.GetImageUrl(Arg.Any<string>(), Arg.Any<string>())
                .Returns(call => $"https://image.tmdb.org/t/p/{call.ArgAt<string>(1)}{call.ArgAt<string>(0)}");
            _service = new TvEpisodeImportService(Context, _tmdbClient, Substitute.For<ILogger<TvEpisodeImportService>>());
        }

        #region Arrange helpers

        private async Task<TvShow> SeedShow(string? tmdbId = "1399")
        {
            var show = TestDataFactory.CreateTvShow("Game of Thrones", 2011, tmdbId);
            show.TmdbId = tmdbId;
            show.TmdbRefreshedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();
            return show;
        }

        private async Task<TvShowEpisode> SeedEpisode(TvShow show, int season, int episode, string title, Action<TvShowEpisode>? customize = null)
        {
            var row = new TvShowEpisode
            {
                Title = title,
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                ShowId = show.Id,
                SeasonNumber = season,
                EpisodeNumber = episode
            };
            customize?.Invoke(row);
            Context.TvShowEpisodes.Add(row);
            await Context.SaveChangesAsync();
            return row;
        }

        private void GivenDetails(int tmdbId, params (int Season, int Count)[] seasons)
        {
            var details = TestDataFactory.CreateTmdbTvShowDto(tmdbId);
            details.NumberOfSeasons = seasons.Count(s => s.Season > 0);
            details.NumberOfEpisodes = seasons.Where(s => s.Season > 0).Sum(s => s.Count);
            details.Seasons = seasons
                .Select(s => new TmdbSeasonSummaryDto { SeasonNumber = s.Season, EpisodeCount = s.Count, Name = s.Season == 0 ? "Specials" : $"Season {s.Season}" })
                .ToList();
            _tmdbClient.GetTvShowDetailsAsync(tmdbId).Returns(details);
        }

        private void GivenSeason(int tmdbId, int seasonNumber, params TmdbTvEpisodeDto[] episodes)
        {
            _tmdbClient.GetTvSeasonAsync(tmdbId, seasonNumber)
                .Returns(new TmdbSeasonDto { SeasonNumber = seasonNumber, Episodes = episodes.ToList() });
        }

        private static TmdbTvEpisodeDto Episode(int season, int number, string name, int id = 0)
            => new()
            {
                Id = id == 0 ? season * 1000 + number : id,
                Name = name,
                SeasonNumber = season,
                EpisodeNumber = number,
                Overview = $"Overview of {name}",
                AirDate = $"2011-04-{10 + number:00}",
                Runtime = 55 + number,
                StillPath = $"/still-{season}-{number}.jpg"
            };

        private Task<List<TvShowEpisode>> StoredEpisodes(Guid showId)
            => Context.TvShowEpisodes.AsNoTracking().Where(e => e.ShowId == showId)
                .OrderBy(e => e.SeasonNumber).ThenBy(e => e.EpisodeNumber).ToListAsync();

        #endregion

        [Fact]
        public async Task ImportFromTmdb_CreatesEveryEpisodeOfEverySeason_SpecialsIncluded()
        {
            var show = await SeedShow();
            GivenDetails(1399, (0, 1), (1, 2));
            GivenSeason(1399, 0, Episode(0, 1, "Inside Game of Thrones"));
            GivenSeason(1399, 1, Episode(1, 1, "Winter Is Coming"), Episode(1, 2, "The Kingsroad"));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("tv-episodes-from-tmdb");
            result.ShowId.Should().Be(show.Id);
            result.ShowTitle.Should().Be("Game of Thrones");
            result.CreatedCount.Should().Be(3);
            result.UpdatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            result.SeasonsProcessed.Should().Be(2);
            result.TotalProcessed.Should().Be(3);
            result.CompletedAt.Should().NotBeNull();
            result.Errors.Should().BeEmpty();

            var stored = await StoredEpisodes(show.Id);
            stored.Select(e => (e.SeasonNumber, e.EpisodeNumber, e.Title)).Should().Equal(
                (0, 1, "Inside Game of Thrones"), (1, 1, "Winter Is Coming"), (1, 2, "The Kingsroad"));

            var pilot = stored[1];
            pilot.MediaType.Should().Be(MediaType.TVShow);
            pilot.Status.Should().Be(Status.Uncharted);
            pilot.Description.Should().Be("Overview of Winter Is Coming");
            pilot.AirDate.Should().Be(new DateTime(2011, 4, 11, 0, 0, 0, DateTimeKind.Utc));
            pilot.AirDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
            pilot.DurationInMinutes.Should().Be(56);
            pilot.TmdbEpisodeId.Should().Be(1001);
            pilot.StillPath.Should().Be("/still-1-1.jpg");
            pilot.Thumbnail.Should().Be("https://image.tmdb.org/t/p/w500/still-1-1.jpg");
            pilot.Link.Should().Be("https://www.themoviedb.org/tv/1399/season/1/episode/1");
        }

        [Fact]
        public async Task ImportFromTmdb_UpgradesAPlaceholderEpisode_AndKeepsItsWatchData()
        {
            var show = await SeedShow();
            var watched = new DateTime(2026, 3, 4, 20, 0, 0, DateTimeKind.Utc);
            var shell = await SeedEpisode(show, 1, 3, "S1E3", e =>
            {
                e.Status = Status.Completed;
                e.DateCompleted = watched;
                e.TraktPlays = 2;
                e.TraktLastWatchedAt = watched;
                e.TraktEpisodeId = 73641;
                e.Rating = Rating.SuperLike;
                e.Notes = "Watched with friends";
            });
            GivenDetails(1399, (1, 3));
            GivenSeason(1399, 1, Episode(1, 3, "Lord Snow"));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);

            var stored = await Context.TvShowEpisodes.AsNoTracking().SingleAsync(e => e.Id == shell.Id);
            stored.Title.Should().Be("Lord Snow");
            stored.Description.Should().Be("Overview of Lord Snow");
            stored.TmdbEpisodeId.Should().Be(1003);
            stored.DurationInMinutes.Should().Be(58);
            stored.StillPath.Should().Be("/still-1-3.jpg");
            stored.Thumbnail.Should().Be("https://image.tmdb.org/t/p/w500/still-1-3.jpg");

            // Everything the user or Trakt owns is exactly as it was.
            stored.Status.Should().Be(Status.Completed);
            stored.DateCompleted.Should().Be(watched);
            stored.TraktPlays.Should().Be(2);
            stored.TraktLastWatchedAt.Should().Be(watched);
            stored.TraktEpisodeId.Should().Be(73641);
            stored.Rating.Should().Be(Rating.SuperLike);
            stored.Notes.Should().Be("Watched with friends");

            (await Context.TvShowEpisodes.CountAsync()).Should().Be(1, "the placeholder is upgraded in place, not duplicated");
        }

        [Fact]
        public async Task ImportFromTmdb_KeepsAHandTitledEpisodesTitle_AndFillsOnlyItsGaps()
        {
            var show = await SeedShow();
            var handTitled = await SeedEpisode(show, 1, 1, "My favorite pilot", e =>
            {
                e.Description = "My own summary";
                e.Thumbnail = "https://cdn.example.com/my-upload.jpg";
            });
            GivenDetails(1399, (1, 1));
            GivenSeason(1399, 1, Episode(1, 1, "Winter Is Coming"));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.UpdatedCount.Should().Be(1, "air date, runtime, still, and TMDB id were empty");

            var stored = await Context.TvShowEpisodes.AsNoTracking().SingleAsync(e => e.Id == handTitled.Id);
            stored.Title.Should().Be("My favorite pilot");
            stored.Description.Should().Be("My own summary");
            stored.Thumbnail.Should().Be("https://cdn.example.com/my-upload.jpg");
            stored.StillPath.Should().Be("/still-1-1.jpg");
            stored.AirDate.Should().NotBeNull();
            stored.DurationInMinutes.Should().Be(56);
            stored.TmdbEpisodeId.Should().Be(1001);
        }

        [Fact]
        public async Task ImportFromTmdb_CountsACompleteEpisodeAsSkipped_SoARepeatRunChangesNothing()
        {
            var show = await SeedShow();
            GivenDetails(1399, (1, 2));
            GivenSeason(1399, 1, Episode(1, 1, "Winter Is Coming"), Episode(1, 2, "The Kingsroad"));
            await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            var second = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            second.CreatedCount.Should().Be(0);
            second.UpdatedCount.Should().Be(0);
            second.SkippedCount.Should().Be(2);
            second.TotalProcessed.Should().Be(2);
            (await Context.TvShowEpisodes.CountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task ImportFromTmdb_CountsAFailedSeason_AndStillImportsTheRest()
        {
            var show = await SeedShow();
            GivenDetails(1399, (1, 1), (2, 1), (3, 1));
            GivenSeason(1399, 1, Episode(1, 1, "Winter Is Coming"));
            _tmdbClient.GetTvSeasonAsync(1399, 2)
                .ThrowsAsync(new HttpRequestException("not found", null, HttpStatusCode.NotFound));
            GivenSeason(1399, 3, Episode(3, 1, "Valar Dohaeris"));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.Success.Should().BeTrue("a season failure completes the run, it does not abort it");
            result.CreatedCount.Should().Be(2);
            result.FailedCount.Should().Be(1);
            result.SeasonsProcessed.Should().Be(2);
            result.Errors.Should().ContainSingle().Which.Should().Contain("season 2");
            result.CompletedAt.Should().NotBeNull();

            (await StoredEpisodes(show.Id)).Select(e => e.SeasonNumber).Should().Equal(1, 3);
        }

        [Fact]
        public async Task ImportFromTmdb_ReportsANonHttpSeasonFailure_WithoutAborting()
        {
            var show = await SeedShow();
            GivenDetails(1399, (1, 1));
            _tmdbClient.GetTvSeasonAsync(1399, 1).ThrowsAsync(new InvalidOperationException("malformed payload"));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.Success.Should().BeTrue();
            result.FailedCount.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().Contain("malformed payload");
        }

        [Fact]
        public async Task ImportFromTmdb_AbortsWithAClearMessage_WhenTheShowHasNoTmdbId()
        {
            var show = await SeedShow(tmdbId: null);

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.Success.Should().BeFalse();
            result.ShowTitle.Should().Be("Game of Thrones");
            result.ErrorMessage.Should().Contain("no usable TMDB id");
            result.CompletedAt.Should().BeNull();
            await _tmdbClient.DidNotReceive().GetTvShowDetailsAsync(Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ImportFromTmdb_AbortsWithAClearMessage_WhenTheShowIsUnknown()
        {
            var result = await _service.ImportFromTmdbAsync(Guid.NewGuid(), delayBetweenCallsMs: 0);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");
            await _tmdbClient.DidNotReceive().GetTvShowDetailsAsync(Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ImportFromTmdb_Aborts_WhenTmdbNoLongerHasTheShow()
        {
            var show = await SeedShow();
            _tmdbClient.GetTvShowDetailsAsync(1399)
                .ThrowsAsync(new HttpRequestException("not found", null, HttpStatusCode.NotFound));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("TMDB no longer has id 1399");
            await _tmdbClient.DidNotReceive().GetTvSeasonAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ImportFromTmdb_RefreshesTheShowsCounts_ButNotItsRefreshStamp()
        {
            var show = await SeedShow();
            show.NumberOfSeasons = 1;
            show.NumberOfEpisodes = 5;
            await Context.SaveChangesAsync();
            var stampBefore = show.TmdbRefreshedAt;
            GivenDetails(1399, (0, 2), (1, 10), (2, 10));
            GivenSeason(1399, 0);
            GivenSeason(1399, 1);
            GivenSeason(1399, 2);

            await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            var stored = await Context.TvShows.AsNoTracking().SingleAsync(s => s.Id == show.Id);
            stored.NumberOfSeasons.Should().Be(2, "specials do not count as a season");
            stored.NumberOfEpisodes.Should().Be(20);
            stored.TmdbRefreshedAt.Should().Be(stampBefore, "episode import is not a show-level refresh");
        }

        [Fact]
        public async Task ImportFromTmdb_WarnsAndCompletes_WhenTmdbListsNoSeasons()
        {
            var show = await SeedShow();
            GivenDetails(1399);

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            result.Success.Should().BeTrue();
            result.SeasonsProcessed.Should().Be(0);
            result.Warnings.Should().ContainSingle().Which.Should().Contain("no seasons");
            await _tmdbClient.DidNotReceive().GetTvSeasonAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ImportFromTmdb_UsesThePlaceholderTitle_WhenTmdbHasNoNameYet()
        {
            var show = await SeedShow();
            GivenDetails(1399, (2, 1));
            GivenSeason(1399, 2, Episode(2, 7, ""));

            await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0);

            (await StoredEpisodes(show.Id)).Single().Title.Should().Be("S2E7", "a later run can then name it");
        }

        [Fact]
        public async Task ImportFromTmdb_SavesTheSeasonsAlreadyWalked_WhenCancelled()
        {
            var show = await SeedShow();
            GivenDetails(1399, (1, 1), (2, 1));
            using var cts = new CancellationTokenSource();
            _tmdbClient.GetTvSeasonAsync(1399, 1).Returns(call =>
            {
                cts.Cancel();
                return new TmdbSeasonDto { SeasonNumber = 1, Episodes = { Episode(1, 1, "Winter Is Coming") } };
            });
            GivenSeason(1399, 2, Episode(2, 1, "The North Remembers"));

            var result = await _service.ImportFromTmdbAsync(show.Id, delayBetweenCallsMs: 0, cts.Token);

            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            result.SeasonsProcessed.Should().Be(1);
            result.Warnings.Should().ContainSingle().Which.Should().Contain("cancelled");
            (await StoredEpisodes(show.Id)).Should().ContainSingle(e => e.SeasonNumber == 1);
        }
    }
}
