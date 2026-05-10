using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class TvShowMappingServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<TvShowMappingService> _mockLogger;
        private readonly TvShowMappingService _service;

        public TvShowMappingServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<TvShowMappingService>>();
            _service = new TvShowMappingService(Context, _mockLogger);
        }

        #region MapFromDtoAsync

        [Fact]
        public async Task MapFromDtoAsync_ValidDto_MapsAllProperties()
        {
            var dto = new CreateTvShowDto
            {
                Title = "Breaking Bad",
                MediaType = MediaType.TVShow,
                Status = Status.Completed,
                Creator = "Vince Gilligan",
                Cast = "Bryan Cranston, Aaron Paul",
                FirstAirYear = 2008,
                LastAirYear = 2013,
                NumberOfSeasons = 5,
                NumberOfEpisodes = 62,
                ContentRating = "TV-MA",
                TmdbId = "1396",
                TmdbRating = 8.9,
                Tagline = "All Hail the King",
                OriginalLanguage = "en",
                OriginalName = "Breaking Bad",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            var result = await _service.MapFromDtoAsync(dto);

            result.Should().NotBeNull();
            result.Title.Should().Be("Breaking Bad");
            result.MediaType.Should().Be(MediaType.TVShow);
            result.Creator.Should().Be("Vince Gilligan");
            result.FirstAirYear.Should().Be(2008);
            result.LastAirYear.Should().Be(2013);
            result.NumberOfSeasons.Should().Be(5);
            result.NumberOfEpisodes.Should().Be(62);
            result.TmdbId.Should().Be("1396");
            result.TmdbRating.Should().Be(8.9);
            result.DateAdded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task MapFromDtoAsync_WithTopics_NormalizesToLowercase()
        {
            var dto = TestDataFactory.CreateTvShowDto();
            dto.Topics = new[] { "Crime Drama", "  CHEMISTRY  " };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(2);
            result.Topics.Select(t => t.Name).Should().BeEquivalentTo("crime drama", "chemistry");
        }

        [Fact]
        public async Task MapFromDtoAsync_WithExistingTopic_ReusesExistingTopic()
        {
            var existingTopic = new Topic { Name = "drama" };
            Context.Topics.Add(existingTopic);
            await Context.SaveChangesAsync();

            var dto = TestDataFactory.CreateTvShowDto();
            dto.Topics = new[] { "Drama" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(1);
            result.Topics.First().Id.Should().Be(existingTopic.Id);
        }

        [Fact]
        public async Task MapFromDtoAsync_WithGenres_NormalizesToLowercase()
        {
            var dto = TestDataFactory.CreateTvShowDto();
            dto.Genres = new[] { "Drama", "  THRILLER  " };

            var result = await _service.MapFromDtoAsync(dto);

            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().BeEquivalentTo("drama", "thriller");
        }

        [Fact]
        public async Task MapFromDtoAsync_SkipsWhitespaceTopicsAndGenres()
        {
            var dto = TestDataFactory.CreateTvShowDto();
            dto.Topics = new[] { "", "valid" };
            dto.Genres = new[] { "  ", "valid" };

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().HaveCount(1);
            result.Genres.Should().HaveCount(1);
        }

        [Fact]
        public async Task MapFromDtoAsync_NullTopicsAndGenres_CreatesEmptyCollections()
        {
            var dto = TestDataFactory.CreateTvShowDto();
            dto.Topics = null;
            dto.Genres = null;

            var result = await _service.MapFromDtoAsync(dto);

            result.Topics.Should().BeEmpty();
            result.Genres.Should().BeEmpty();
        }

        #endregion

        #region MapToResponseDtoAsync

        [Fact]
        public async Task MapToResponseDtoAsync_ValidTvShow_MapsAllProperties()
        {
            var tvShow = TestDataFactory.CreateTvShow("Breaking Bad", 2008, "1396");
            tvShow.Creator = "Vince Gilligan";
            tvShow.LastAirYear = 2013;
            tvShow.NumberOfSeasons = 5;
            tvShow.NumberOfEpisodes = 62;
            tvShow.TmdbRating = 8.9;
            tvShow.Topics.Add(new Topic { Name = "crime" });

            var result = await _service.MapToResponseDtoAsync(tvShow);

            result.Should().NotBeNull();
            result.Id.Should().Be(tvShow.Id);
            result.Title.Should().Be("Breaking Bad");
            result.Creator.Should().Be("Vince Gilligan");
            result.FirstAirYear.Should().Be(2008);
            result.LastAirYear.Should().Be(2013);
            result.NumberOfSeasons.Should().Be(5);
            result.NumberOfEpisodes.Should().Be(62);
            result.TmdbRating.Should().Be(8.9);
            result.Topics.Should().Contain("crime");
        }

        #endregion

        #region MapFromTmdbAsync

        [Fact]
        public async Task MapFromTmdbAsync_ValidTmdbTvShow_MapsCorrectly()
        {
            var tmdbTvShow = TestDataFactory.CreateTmdbTvShowDto();

            var result = await _service.MapFromTmdbAsync(tmdbTvShow);

            result.Title.Should().Be("Game of Thrones");
            result.MediaType.Should().Be(MediaType.TVShow);
            result.Status.Should().Be(Status.Uncharted);
            result.TmdbId.Should().Be("1399");
            result.TmdbRating.Should().Be(8.4);
            result.NumberOfSeasons.Should().Be(8);
            result.NumberOfEpisodes.Should().Be(73);
            result.Tagline.Should().Be("Winter Is Coming");
            result.FirstAirYear.Should().Be(2011);
            result.LastAirYear.Should().Be(2019);
            result.Thumbnail.Should().Contain("image.tmdb.org/t/p/w500");
        }

        [Fact]
        public async Task MapFromTmdbAsync_NullName_DefaultsToUnknownTitle()
        {
            var tmdbTvShow = new TmdbTvShowDto { Name = null, Id = 1 };

            var result = await _service.MapFromTmdbAsync(tmdbTvShow);

            result.Title.Should().Be("Unknown Title");
        }

        [Fact]
        public async Task MapFromTmdbAsync_NullPosterPath_ThumbnailIsNull()
        {
            var tmdbTvShow = new TmdbTvShowDto { Name = "Test", PosterPath = null };

            var result = await _service.MapFromTmdbAsync(tmdbTvShow);

            result.Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task MapFromTmdbAsync_InvalidAirDates_YearsAreNull()
        {
            var tmdbTvShow = new TmdbTvShowDto
            {
                Name = "Test",
                FirstAirDate = "invalid",
                LastAirDate = "also-invalid"
            };

            var result = await _service.MapFromTmdbAsync(tmdbTvShow);

            result.FirstAirYear.Should().BeNull();
            result.LastAirYear.Should().BeNull();
        }

        [Fact]
        public async Task MapFromTmdbAsync_NullAirDates_YearsAreNull()
        {
            var tmdbTvShow = new TmdbTvShowDto { Name = "Test", FirstAirDate = null, LastAirDate = null };

            var result = await _service.MapFromTmdbAsync(tmdbTvShow);

            result.FirstAirYear.Should().BeNull();
            result.LastAirYear.Should().BeNull();
        }

        #endregion

        #region MapToSearchResultDtoAsync

        [Fact]
        public async Task MapToSearchResultDtoAsync_ValidTmdbTvShow_MapsCorrectly()
        {
            var tmdbTvShow = TestDataFactory.CreateTmdbTvShowDto();

            var result = await _service.MapToSearchResultDtoAsync(tmdbTvShow);

            result.Id.Should().Be(1399);
            result.Name.Should().Be("Game of Thrones");
            result.Overview.Should().NotBeNullOrEmpty();
            result.PosterUrl.Should().Contain("image.tmdb.org/t/p/w500");
            result.BackdropUrl.Should().Contain("image.tmdb.org/t/p/w1280");
            result.VoteAverage.Should().Be(8.4);
            result.NumberOfSeasons.Should().Be(8);
            result.NumberOfEpisodes.Should().Be(73);
        }

        [Fact]
        public async Task MapToSearchResultDtoAsync_NullPaths_UrlsAreNull()
        {
            var tmdbTvShow = new TmdbTvShowDto { Name = "Test", PosterPath = null, BackdropPath = null };

            var result = await _service.MapToSearchResultDtoAsync(tmdbTvShow);

            result.PosterUrl.Should().BeNull();
            result.BackdropUrl.Should().BeNull();
        }

        #endregion
    }
}
