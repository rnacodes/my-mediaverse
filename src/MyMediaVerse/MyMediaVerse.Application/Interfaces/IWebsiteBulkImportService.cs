using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Turns parsed bookmarks (from an export file or a pasted list) into website stubs in one
    /// pass: normalize, deduplicate against the library and within the batch, map folders and
    /// tags to topics, and save in batches. No page is fetched here; enrichment runs afterwards.
    /// </summary>
    public interface IWebsiteBulkImportService
    {
        /// <summary>Counts what an import would do without writing anything.</summary>
        Task<WebsiteBulkImportPreviewDto> PreviewAsync(BookmarkParseResult parsed, CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports the bookmarks. <paramref name="operation"/> names the source for the result
        /// (<see cref="WebsiteBulkImportResultDto.BookmarkImportOperation"/> or
        /// <see cref="WebsiteBulkImportResultDto.UrlListImportOperation"/>).
        /// </summary>
        Task<WebsiteBulkImportResultDto> ImportAsync(
            BookmarkParseResult parsed,
            BookmarkImportOptionsDto options,
            string operation,
            CancellationToken cancellationToken = default);
    }
}
