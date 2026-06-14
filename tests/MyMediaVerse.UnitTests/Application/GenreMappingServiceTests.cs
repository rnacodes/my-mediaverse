using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class GenreMappingServiceTests
    {
        private readonly ITmdbService _mockTmdbService;
        private readonly IListenNotesApiClient _mockListenNotesApiClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GenreMappingService> _mockLogger;
        private readonly GenreMappingService _service;

        public GenreMappingServiceTests()
        {
            _mockTmdbService = Substitute.For<ITmdbService>();
            _mockListenNotesApiClient = Substitute.For<IListenNotesApiClient>();
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
                    new TmdbGenreDto { Id = 10759, Name = "Action & Adventure" }
                }
            });
            _mockListenNotesApiClient.GetGenresAsync().Returns(new ListenNotesGenresDto
            {
                Genres = new List<GenreDto>
                {
                    new GenreDto { Id = 68, Name = "TV & Film" },
                    new GenreDto { Id = 133, Name = "Comedy" }
                }
            });

            _service = new GenreMappingService(
                _mockTmdbService,
                _mockListenNotesApiClient,
                _cache,
                _mockLogger);
        }

        [Fact]
        public async Task GetGenreNameAsync_ShouldResolveKnownTmdbId_ToLowercaseName()
        {
            // Act
            var result = await _service.GetGenreNameAsync(GenreSource.Tmdb, 28);

            // Assert
            result.Should().Be("action");
        }

        [Fact]
        public async Task GetGenreNameAsync_ShouldResolveBothMovieAndTvIds_FromMergedTmdbMap()
        {
            // Act
            var movieGenre = await _service.GetGenreNameAsync(GenreSource.Tmdb, 35);
            var tvGenre = await _service.GetGenreNameAsync(GenreSource.Tmdb, 10759);

            // Assert
            movieGenre.Should().Be("comedy");
            tvGenre.Should().Be("action & adventure");
        }

        [Fact]
        public async Task GetGenreNameAsync_ShouldResolveListenNotesId_FromItsOwnMap()
        {
            // Act
            var result = await _service.GetGenreNameAsync(GenreSource.ListenNotes, 68);

            // Assert
            result.Should().Be("tv & film");
        }

        [Fact]
        public async Task GetGenreNameAsync_ShouldReturnNull_WhenIdIsUnknown()
        {
            // Act
            var result = await _service.GetGenreNameAsync(GenreSource.Tmdb, 999999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldResolveKnownIds_AndOmitUnknownOnes()
        {
            // Act
            var result = await _service.GetGenreNamesAsync(GenreSource.Tmdb, new[] { 28, 999999, 35 });

            // Assert
            result.Should().Equal("action", "comedy");
        }

        [Fact]
        public async Task GetGenreNamesAsync_ShouldReturnEmpty_WhenAllIdsUnknown()
        {
            // Act
            var result = await _service.GetGenreNamesAsync(GenreSource.ListenNotes, new[] { 111, 222 });

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetGenreNameAsync_ShouldBuildTmdbMapOnce_AcrossMultipleLookups()
        {
            // Act
            await _service.GetGenreNameAsync(GenreSource.Tmdb, 28);
            await _service.GetGenreNameAsync(GenreSource.Tmdb, 35);

            // Assert — the cached map is built on the first lookup only.
            await _mockTmdbService.Received(1).GetMovieGenresAsync();
            await _mockTmdbService.Received(1).GetTvGenresAsync();
        }

        [Fact]
        public async Task GetGenreNameAsync_ShouldBuildListenNotesMapOnce_AcrossMultipleLookups()
        {
            // Act
            await _service.GetGenreNameAsync(GenreSource.ListenNotes, 68);
            await _service.GetGenreNameAsync(GenreSource.ListenNotes, 133);

            // Assert
            await _mockListenNotesApiClient.Received(1).GetGenresAsync();
        }
    }
}
