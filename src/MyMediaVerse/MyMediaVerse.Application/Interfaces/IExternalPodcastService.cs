using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;

namespace MyMediaVerse.Application.Interfaces
{
    // Business-level facade for importing/searching podcasts from external sources.
    // Today this delegates to ListenNotes. Surface is intentionally narrow (search + import);
    // ListenNotes-specific affordances (best/curated/playlists/genres/recommendations)
    // remain on IListenNotesService.
    public interface IExternalPodcastService
    {
        Task<SearchResultDto> SearchAsync(string query, string? type = null, int? offset = null);

        Task<PodcastSeries> ImportSeriesAsync(string sourceId);

        Task<PodcastEpisode> ImportEpisodeAsync(string sourceEpisodeId, Guid seriesId);

        Task<PodcastSeries?> ImportSeriesByNameAsync(string seriesName);
    }
}
