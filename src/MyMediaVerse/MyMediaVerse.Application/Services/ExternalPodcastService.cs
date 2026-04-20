using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;

namespace MyMediaVerse.Application.Services
{
    public class ExternalPodcastService : IExternalPodcastService
    {
        private readonly IListenNotesService _listenNotes;

        public ExternalPodcastService(IListenNotesService listenNotes)
        {
            _listenNotes = listenNotes;
        }

        public Task<SearchResultDto> SearchAsync(string query, string? type = null, int? offset = null)
            => _listenNotes.SearchAsync(query, type, offset);

        public Task<PodcastSeries> ImportSeriesAsync(string sourceId)
            => _listenNotes.ImportPodcastSeriesAsync(sourceId);

        public Task<PodcastEpisode> ImportEpisodeAsync(string sourceEpisodeId, Guid seriesId)
            => _listenNotes.ImportPodcastEpisodeAsync(sourceEpisodeId, seriesId);

        public Task<PodcastSeries?> ImportSeriesByNameAsync(string seriesName)
            => _listenNotes.ImportPodcastSeriesByNameAsync(seriesName);
    }
}
