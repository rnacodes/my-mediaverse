using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Find-or-create for Genre entities, scoped to one operation, mirroring
    /// <see cref="TopicResolver"/>. The cache guarantees that a genre shared by many items
    /// within the operation resolves to a single instance before SaveChanges runs, so two new
    /// items naming the same new genre cannot collide on the unique name index.
    /// </summary>
    public sealed class GenreResolver
    {
        private readonly IApplicationDbContext _context;
        private readonly Dictionary<string, Genre> _cache = new();

        public GenreResolver(IApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Genres this resolver created (as opposed to found) so far.</summary>
        public int CreatedCount { get; private set; }

        /// <summary>
        /// Resolves a normalized (trimmed, lowercased) genre name to its Genre, creating one if
        /// none exists. Returns null for a name the Genre table cannot hold (blank, or over the
        /// 100-character limit). A created Genre is explicitly registered as Added because its id
        /// is set at construction (see <see cref="TopicResolver"/>).
        /// </summary>
        public async Task<Genre?> GetOrCreateAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                return null;

            if (_cache.TryGetValue(name, out var cached))
                return cached;

            var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == name);
            if (genre == null)
            {
                genre = new Genre { Name = name };
                _context.Add(genre);
                CreatedCount++;
            }

            _cache[name] = genre;
            return genre;
        }
    }
}
