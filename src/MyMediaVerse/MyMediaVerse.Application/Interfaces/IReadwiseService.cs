using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IReadwiseService
    {
        /// <summary>
        /// Validates the configured Readwise API token
        /// </summary>
        Task<bool> ValidateConnectionAsync();

        /// <summary>
        /// Links highlights to existing Articles and Books in PLB database
        /// </summary>
        Task<int> LinkHighlightsToMediaAsync();
        
        /// <summary>
        /// Exports a highlight to Readwise
        /// </summary>
        Task<bool> ExportHighlightToReadwiseAsync(Guid highlightId);
    }
}

