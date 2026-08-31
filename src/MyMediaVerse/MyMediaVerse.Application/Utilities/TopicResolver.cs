using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Find-or-create for Topic entities, scoped to one operation (one sync run, one
    /// bulk import, one request). The cache guarantees that a tag appearing on many
    /// items within the operation resolves to a single Topic instance even before
    /// SaveChanges runs — without it, two new items sharing a new tag would each
    /// create the Topic and collide on the unique name index.
    /// </summary>
    public sealed class TopicResolver
    {
        private readonly IApplicationDbContext _context;
        private readonly Dictionary<string, Topic> _cache = new();

        public TopicResolver(IApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves a normalized (trimmed, lowercased) tag name to its Topic, creating
        /// one if none exists. Returns null for a name the Topic table cannot hold
        /// (blank, or over the 100-character limit) — callers skip those.
        /// A created Topic is explicitly registered as Added: Topic ids are set at
        /// construction, so an instance first discovered through navigation fixup would
        /// be inferred as an existing row and fail the save with a concurrency error.
        /// </summary>
        public async Task<Topic?> GetOrCreateAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                return null;

            if (_cache.TryGetValue(name, out var cached))
                return cached;

            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == name);
            if (topic == null)
            {
                topic = new Topic { Name = name };
                _context.Add(topic);
            }

            _cache[name] = topic;
            return topic;
        }
    }
}
