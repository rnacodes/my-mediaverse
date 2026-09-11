using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// How a bookmark file or URL list is turned into websites. Every value has a default, so
    /// an import with no options behaves the way the import page's toggles start out.
    /// </summary>
    public class BookmarkImportOptionsDto
    {
        /// <summary>Each folder in the bookmark path becomes a (lowercase) topic on the website.</summary>
        public bool FoldersAsTopics { get; set; } = true;

        /// <summary>The export's TAGS attribute becomes topics on the website.</summary>
        public bool TagsAsTopics { get; set; } = true;

        /// <summary>Status given to every created website.</summary>
        public Status DefaultStatus { get; set; } = Status.Uncharted;

        /// <summary>Topics added to every website in the import (created and existing).</summary>
        public List<string> ExtraTopics { get; set; } = new();

        /// <summary>Genres added to every website in the import (created and existing).</summary>
        public List<string> ExtraGenres { get; set; } = new();
    }
}
