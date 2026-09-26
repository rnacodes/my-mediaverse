using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class GenreMappingServiceTests
    {
        private readonly ITmdbService _mockTmdbService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GenreMappingService> _mockLogger;
        private readonly GenreMappingService _service;

        public GenreMappingServiceTests()
        {
            _mockTmdbService = Substitute.For<ITmdbService>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = Substitute.For<ILogger<GenreMappingService>>();

            _mockTmdbService.GetMovieGenresAsync().Returns(new TmdbGenreListDto
            {
                Genres = new[]
                {
                    new TmdbGenreDto { Id = 28, Name = "Action" },
                    new TmdbGenreDto { Id = 35, Name = "Comedy" }
                }
            });
            _mockTmdbService.GetTvGenresAsync().Returns(new TmdbGenreListDto
            {
                Genres = new[]
                {
                    new TmdbGenreDto { Id = 10759, Name = "Action & Adventure" },
                    new TmdbGenreDto { Id = 10765, Name = "Sci-Fi & Fantasy" }
                }
            });

            _service = new GenreMappingService(
                _mockTmdbService,
                _cache,
                _mockLogger);
        }

        #region GetGenreNamesAsync (id path)

        [Fact]
        public async Task GetGenreNamesAsync_ShouldResolveKnownTmdbId_ToLowercaseName()
        {
            var result = await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 28 });

            result.Should().Equal("action");
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldResolveBothMovieAndTvIds_FromMergedTmdbMap()
        {
            var result = await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 35, 10765 });

            result.Should().Equal("comedy", "science fiction", "fantasy");
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldSplitCompoundName_AndDropTheHalfAlreadyPresent()
        {
            // 28 = "Action", 10759 = "Action & Adventure": the id path applies the same rule as the name path.
            var result = await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 28, 10759 });

            result.Should().Equal("action", "adventure");
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldResolveKnownIds_AndOmitUnknownOnes()
        {
            var result = await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 28, 999999, 35 });

            result.Should().Equal("action", "comedy");
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldReturnEmpty_WhenAllIdsUnknown()
        {
            var result = await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 111, 222 });

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldBuildTmdbMapOnce_AcrossMultipleLookups()
        {
            await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 28 });
            await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 35 });

            // The cached map is built on the first lookup only.
            await _mockTmdbService.Received(1).GetMovieGenresAsync();
            await _mockTmdbService.Received(1).GetTvGenresAsync();
        }

        #endregion

        #region MapTmdbGenreNames (name path)

        [Fact]
        public void MapTmdbGenreNames_ShouldLowercaseAndTrim()
        {
            _service.MapTmdbGenreNames(new[] { "  Drama ", "Science Fiction" })
                .Should().Equal("drama", "science fiction");
        }

        [Theory]
        [InlineData("Action & Adventure", new[] { "action", "adventure" })]
        [InlineData("Sci-Fi & Fantasy", new[] { "science fiction", "fantasy" })]
        [InlineData("War & Politics", new[] { "war", "politics" })]
        public void MapTmdbGenreNames_ShouldSplitCompoundTvGenres(string tmdbName, string[] expected)
        {
            _service.MapTmdbGenreNames(new[] { tmdbName }).Should().Equal(expected);
        }

        [Fact]
        public void MapTmdbGenreNames_ShouldReturnDistinctNames_InFirstAppearanceOrder()
        {
            // TV compounds overlap each other and the movie vocabulary.
            var result = _service.MapTmdbGenreNames(new[] { "Action & Adventure", "Adventure", "Sci-Fi & Fantasy", "Science Fiction" });

            result.Should().Equal("action", "adventure", "science fiction", "fantasy");
        }

        [Fact]
        public void MapTmdbGenreNames_ShouldDropBlanks_AndTolerateNull()
        {
            _service.MapTmdbGenreNames(new[] { "", "  ", null, "Drama &" }).Should().Equal("drama");
            _service.MapTmdbGenreNames(null).Should().BeEmpty();
        }

        [Fact]
        public void MapTmdbGenreNames_ShouldNotCallTmdb()
        {
            _service.MapTmdbGenreNames(new[] { "Drama" });

            _mockTmdbService.DidNotReceive().GetMovieGenresAsync();
            _mockTmdbService.DidNotReceive().GetTvGenresAsync();
        }

        #endregion
    }
}
