using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Maps TMDB genres to library genre names. Names map directly; ids go through an
    /// in-memory <c>id → TMDB name</c> map built from the TMDB genre-fetch service. The first id
    /// lookup builds the map (one API round-trip); later lookups are served from
    /// <see cref="IMemoryCache"/> until the entry expires.
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

        // TMDB spells this one differently from the rest of the library's sources.
        private static readonly Dictionary<string, string> Aliases = new()
        {
            ["sci-fi"] = "science fiction"
        };

        public IReadOnlyList<string> MapTmdbGenreNames(IEnumerable<string?>? tmdbGenreNames)
        {
            if (tmdbGenreNames == null) return Array.Empty<string>();

            // TV genres arrive as compounds ("Action & Adventure"); each half is its own genre.
            var parts = tmdbGenreNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .SelectMany(n => n!.Split('&'))
                .Select(GenreNames.Normalize)
                .Where(n => n != null)
                .Select(n => Aliases.TryGetValue(n!, out var alias) ? alias : n!);

            return parts.Distinct().ToList();
        }

        public async Task<IReadOnlyList<string>> GetGenreNamesAsync(GenreSource source, IEnumerable<int> genreIds)
        {
            var map = await GetMapAsync(source);
            var tmdbNames = new List<string>();

            foreach (var id in genreIds)
            {
                if (map.TryGetValue(id, out var name))
                {
                    tmdbNames.Add(name);
                }
                else
                {
                    _logger.LogWarning("Unknown {Source} genre id {GenreId}; skipped.", source, id);
                }
            }

            // Same rule as the name path, so an id lookup and a name lookup always agree.
            return MapTmdbGenreNames(tmdbNames);
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
