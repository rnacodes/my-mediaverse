namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// What an import would do, computed without writing anything, so the user can check the
    /// counts and the folder mapping before committing.
    /// </summary>
    public class WebsiteBulkImportPreviewDto
    {
        /// <summary>Entries the parser recognized as web links.</summary>
        public int TotalCount { get; set; }

        /// <summary>Entries with a usable http(s) URL.</summary>
        public int ValidCount { get; set; }

        /// <summary>Entries whose URL could not be normalized.</summary>
        public int InvalidCount { get; set; }

        /// <summary>Entries that were not web links at all (bookmarklets, browser pages, local files).</summary>
        public int NonWebLinkCount { get; set; }

        /// <summary>Valid entries whose URL is already in the library.</summary>
        public int AlreadyInLibraryCount { get; set; }

        /// <summary>Valid entries repeated within the file (the first occurrence wins).</summary>
        public int DuplicateInFileCount { get; set; }

        /// <summary>Valid entries the import would create.</summary>
        public int NewCount { get; set; }

        /// <summary>Distinct folder paths seen, joined with "/", for the folders-to-topics preview.</summary>
        public List<string> Folders { get; set; } = new();

        /// <summary>The first few entries, so the user can eyeball the parse.</summary>
        public List<BookmarkPreviewItemDto> Sample { get; set; } = new();
    }

    public class BookmarkPreviewItemDto
    {
        public string Url { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? FolderPath { get; set; }
        public bool AlreadyInLibrary { get; set; }
    }
}
