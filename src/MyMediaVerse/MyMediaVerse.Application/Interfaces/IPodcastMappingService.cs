using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.ListenNotes;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IPodcastMappingService
    {
        // Map ListenNotes DTOs to Create DTOs
        CreatePodcastSeriesDto MapFromListenNotesSeriesDto(PodcastSeriesDto podcastDto);
        CreatePodcastEpisodeDto MapFromListenNotesEpisodeDto(PodcastEpisodeDto episodeDto);
    }
}
