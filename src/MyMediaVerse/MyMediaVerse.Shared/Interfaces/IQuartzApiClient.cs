using MyMediaVerse.Shared.DTOs.Obsidian;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Client for fetching content from Quartz-published Obsidian vaults.
    /// </summary>
    public interface IQuartzApiClient
    {
        /// <summary>
        /// Fetches the content index from a Quartz vault.
        /// The content index is a JSON file at /static/contentIndex.json that contains all note metadata.
        /// </summary>
        /// <param name="vaultBaseUrl">The base URL of the Quartz vault (e.g., "https://garden.mymediaverseuniverse.com")</param>
        /// <param name="authToken">Optional authentication token for protected vaults</param>
        /// <returns>Dictionary where keys are slugs and values are note data</returns>
        Task<Dictionary<string, QuartzNoteDto>> GetContentIndexAsync(string vaultBaseUrl, string? authToken = null);
    }
}
