namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// A pasted list of URLs to import as website stubs. Either the raw text (one URL per line,
    /// separators tolerated) or an explicit array; both may be given.
    /// </summary>
    public class UrlListImportRequestDto
    {
        /// <summary>Raw pasted text.</summary>
        public string? Urls { get; set; }

        /// <summary>Already-split URLs.</summary>
        public List<string>? UrlList { get; set; }

        public BookmarkImportOptionsDto? Options { get; set; }
    }
}
