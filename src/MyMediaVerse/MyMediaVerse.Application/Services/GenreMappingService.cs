using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Builds and caches in-memory <c>externalId → lowercase genre name</c> maps from the
    /// existing TMDB genre-fetch service. The first lookup per source builds the map (one API
    /// round-trip); subsequent lookups are served from <see cref="IMemoryCache"/> until the entry
    /// expires.
    /// </summary>
    public class GenreMappingService : IGenreMappingService
    {
        private const string TmdbCacheKey = "genremap:tmdb";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private readonly ITmdbService _tmdbService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GenreMappingService> _logger;

        public GenreMappingService(
            ITmdbService tmdbService,
            IMemoryCache cache,
            ILogger<GenreMappingService> logger)
        {
            _tmdbService = tmdbService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> GetGenreNameAsync(GenreSource source, int genreId)
        {
            var map = await GetMapAsync(source);
            if (map.TryGetValue(genreId, out var name))
            {
                return name;
            }

            _logger.LogWarning("Unknown {Source} genre id {GenreId}; no mapping found.", source, genreId);
            return null;
        }

        public async Task<IReadOnlyList<string>> GetGenreNamesAsync(GenreSource source, IEnumerable<int> genreIds)
        {
            var map = await GetMapAsync(source);
            var names = new List<string>();

            foreach (var id in genreIds)
            {
                if (map.TryGetValue(id, out var name))
                {
                    names.Add(name);
                }
                else
                {
                    _logger.LogWarning("Unknown {Source} genre id {GenreId}; skipped.", source, id);
                }
            }

            return names;
        }

        private Task<IReadOnlyDictionary<int, string>> GetMapAsync(GenreSource source) => source switch
        {
            GenreSource.Tmdb => GetTmdbMapAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unsupported genre source.")
        };

        private async Task<IReadOnlyDictionary<int, string>> GetTmdbMapAsync()
        {
            return (await _cache.GetOrCreateAsync(TmdbCacheKey, async entry =>
            {
                entry.SlidingExpiration = CacheTtl;

                // TMDB movie and TV genre ids share a namespace and do not collide, so they
                // merge into a single map. A collision (should one ever appear) keeps the first.
                var map = new Dictionary<int, string>();
                var movieGenres = await _tmdbService.GetMovieGenresAsync();
                var tvGenres = await _tmdbService.GetTvGenresAsync();

                foreach (var genre in movieGenres.Genres.Concat(tvGenres.Genres))
                {
                    AddGenre(map, genre.Id, genre.Name, GenreSource.Tmdb);
                }

                return (IReadOnlyDictionary<int, string>)map;
            }))!;
        }

        private void AddGenre(Dictionary<int, string> map, int id, string name, GenreSource source)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var normalized = name.Trim().ToLowerInvariant();
            if (map.TryGetValue(id, out var existing))
            {
                if (!string.Equals(existing, normalized, StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "{Source} genre id {GenreId} collision: keeping '{Existing}', ignoring '{New}'.",
                        source, id, existing, normalized);
                }
                return;
            }

            map[id] = normalized;
        }
    }
}
