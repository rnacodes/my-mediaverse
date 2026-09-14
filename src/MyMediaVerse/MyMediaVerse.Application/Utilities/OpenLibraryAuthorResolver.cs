using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Resolves the author references on an Open Library work to display names. A work only
    /// carries author keys, so each name takes one author lookup.
    /// </summary>
    public static class OpenLibraryAuthorResolver
    {
        /// <summary>
        /// Returns the names in the work's author order. An author whose lookup fails is left out
        /// (and logged), so callers that must not store a partial list should compare the count
        /// with the work's author references. Empty when the work lists no authors.
        /// </summary>
        public static async Task<List<string>> ResolveNamesAsync(
            IOpenLibraryApiClient client,
            IEnumerable<OpenLibraryAuthorReference>? authors,
            ILogger logger)
        {
            var names = new List<string>();
            if (authors == null) return names;

            foreach (var reference in authors)
            {
                var authorKey = reference.Author?.Key?.Replace("/authors/", "");
                if (string.IsNullOrWhiteSpace(authorKey)) continue;

                try
                {
                    var authorData = await client.GetAuthorAsync(authorKey);
                    names.Add(authorData.Name ?? authorData.PersonalName ?? authorKey);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not fetch author details for key: {AuthorKey}", authorKey);
                }
            }

            return names;
        }
    }
}
