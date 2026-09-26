using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using NSubstitute;

namespace MyMediaVerse.UnitTests.TestHelpers
{
    /// <summary>
    /// Builds the real TMDB genre mapper for tests that exercise its name path, which is pure.
    /// The TMDB service behind the id path is a substitute, so nothing here reaches the network.
    /// </summary>
    public static class TestGenreMapping
    {
        public static GenreMappingService Create()
            => new(
                Substitute.For<ITmdbService>(),
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<GenreMappingService>.Instance);
    }
}
