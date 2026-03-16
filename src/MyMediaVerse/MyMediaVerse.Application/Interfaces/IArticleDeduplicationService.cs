using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service for identifying and merging duplicate articles from different sources.
    /// Handles articles that may have been imported from both Instapaper and Readwise Reader.
    /// </summary>
    public interface IArticleDeduplicationService
    {
        /// <summary>
        /// Finds and merges duplicate articles based on normalized URLs.
        /// Articles from different sources (Instapaper, Reader) that point to the same URL
        /// will be merged into a single article with combined metadata.
        /// </summary>
        /// <returns>Result containing count of merged articles and details</returns>
        Task<DeduplicationResultDto> FindAndMergeDuplicatesAsync();
        
        /// <summary>
        /// Finds potential duplicates without merging them.
        /// Useful for previewing what would be merged before actually doing it.
        /// </summary>
        /// <returns>List of duplicate groups</returns>
        Task<List<DuplicateGroupDto>> FindDuplicatesAsync();
    }
}

