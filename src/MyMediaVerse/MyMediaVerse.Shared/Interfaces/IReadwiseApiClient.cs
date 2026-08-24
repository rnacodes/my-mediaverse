using MyMediaVerse.Shared.DTOs.Readwise;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Interface for Readwise API client
    /// API Documentation: https://readwise.io/api_deets
    /// </summary>
    public interface IReadwiseApiClient
    {
        /// <summary>
        /// Validates the Readwise API token
        /// GET https://readwise.io/api/v2/auth/
        /// </summary>
        Task<bool> ValidateTokenAsync();

        /// <summary>
        /// Creates or updates highlights in Readwise
        /// POST https://readwise.io/api/v2/highlights/
        /// </summary>
        Task<bool> CreateHighlightsAsync(List<CreateReadwiseHighlightDto> highlights);

        /// <summary>
        /// Exports highlights from Readwise with nested book data.
        /// More efficient than separate highlights + books calls.
        /// GET https://readwise.io/api/v2/export/
        /// </summary>
        /// <param name="updatedAfter">ISO 8601 timestamp to get only highlights updated after this date</param>
        /// <param name="pageCursor">Pagination cursor from previous response</param>
        Task<ReadwiseExportResponse> GetExportAsync(string? updatedAfter = null, string? pageCursor = null);
    }
}

