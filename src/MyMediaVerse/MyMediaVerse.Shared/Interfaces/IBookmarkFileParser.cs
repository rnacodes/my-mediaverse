namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// One bookmark as read from an export file or a pasted list. Only <see cref="Url"/> is
    /// guaranteed; the rest is whatever the source recorded.
    /// </summary>
    public record ParsedBookmark(
        string Url,
        string? Title,
        DateTime? AddedAt,
        IReadOnlyList<string> FolderPath,
        IReadOnlyList<string> Tags);

    /// <summary>
    /// Everything a parser found: the web bookmarks, how many entries were not web links
    /// (bookmarklets, browser-internal pages, local files) and were ignored, and the distinct
    /// folder paths seen, for the folders-to-topics preview.
    /// </summary>
    public record BookmarkParseResult(
        IReadOnlyList<ParsedBookmark> Bookmarks,
        int NonWebLinkCount,
        IReadOnlyList<string> Folders);

    /// <summary>
    /// Reads a bookmark export into <see cref="ParsedBookmark"/>s. One implementation per file
    /// format; the import endpoint picks the first parser that claims the upload.
    /// </summary>
    public interface IBookmarkFileParser
    {
        /// <summary>Whether this parser handles the file, judged by name and the first few lines.</summary>
        bool CanParse(string fileName, string head);

        /// <summary>Parses the whole file. Tolerant: never throws for a malformed entry, it skips it.</summary>
        BookmarkParseResult Parse(string content);
    }
}
