using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IPodcastService
    {
        // Podcast Series methods
        Task<IEnumerable<PodcastSeries>> GetAllPodcastSeriesAsync();
        Task<PodcastSeries?> GetPodcastSeriesByIdAsync(Guid id);
        Task<IEnumerable<PodcastSeries>> SearchPodcastSeriesAsync(string query);
        Task<PodcastSeries> CreatePodcastSeriesAsync(CreatePodcastSeriesDto dto);
        Task<bool> DeletePodcastSeriesAsync(Guid id);
        Task<bool> PodcastSeriesExistsAsync(string title, string? publisher = null);
        Task<PodcastSeries?> GetPodcastSeriesByTitleAsync(string title, string? publisher = null);
        
        // Podcast Episode methods
        Task<IEnumerable<PodcastEpisode>> GetEpisodesBySeriesIdAsync(Guid seriesId);
        Task<PodcastEpisode?> GetPodcastEpisodeByIdAsync(Guid id);
        Task<IEnumerable<PodcastEpisode>> GetAllPodcastEpisodesAsync();
        Task<PodcastEpisode> CreatePodcastEpisodeAsync(CreatePodcastEpisodeDto dto);
        Task<bool> DeletePodcastEpisodeAsync(Guid id);
        Task<bool> PodcastEpisodeExistsAsync(Guid seriesId, string episodeTitle);
        Task<PodcastEpisode?> GetPodcastEpisodeByTitleAsync(Guid seriesId, string episodeTitle);
        
        // Subscription management methods
        Task<PodcastSeries?> SubscribeToPodcastSeriesAsync(Guid seriesId);
        Task<PodcastSeries?> UnsubscribeFromPodcastSeriesAsync(Guid seriesId);
        Task<IEnumerable<PodcastSeries>> GetSubscribedPodcastSeriesAsync();
        Task<PodcastSyncResultDto?> SyncPodcastSeriesEpisodesAsync(Guid seriesId);
    }
}
