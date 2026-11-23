using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IPodcastDatasetImportService
    {
        /// <summary>
        /// Imports a purchased Listen Notes dataset export: either the podcasts CSV (one show per row)
        /// or the episodes CSV (one episode per row, with its show's feed). Any subset of the columns
        /// is accepted. Shows land as unsubscribed stubs marked as dataset data, with no external API
        /// calls; podcast enrichment later finds each show's feed and replaces the dataset details.
        /// </summary>
        /// <param name="csvStream">The CSV file stream.</param>
        /// <returns>Import result with per-row counts and failures, plus the ids of every created show and episode.</returns>
        Task<ChangedItemsResult<PodcastDatasetImportResultDto>> ImportFromCsvAsync(Stream csvStream);
    }
}
