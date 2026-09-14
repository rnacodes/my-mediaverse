using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Outcome of a series create: the saved series, and whether it was newly inserted (false when
    /// an existing row with the same identity was returned instead).
    /// </summary>
    public record PodcastSeriesCreationResult(PodcastSeries Series, bool Created);

    /// <summary>
    /// Outcome of an episode create: the saved episode, and whether it was newly inserted (false
    /// when an existing episode in the series with the same identity was returned instead).
    /// </summary>
    public record PodcastEpisodeCreationResult(PodcastEpisode Episode, bool Created);

    public interface IPodcastService
    {
        // Podcast Series methods
        Task<IEnumerable<PodcastSeries>> GetAllPodcastSeriesAsync();
        Task<PodcastSeries?> GetPodcastSeriesByIdAsync(Guid id);
        Task<IEnumerable<PodcastSeries>> SearchPodcastSeriesAsync(string query);

        /// <summary>
        /// Creates a series, or returns the existing one when its feed, directory ids or title +
        /// publisher already identify a row (fill-only merge of any new metadata and ids).
        /// Throws <see cref="ArgumentException"/> for an invalid feed URL.
        /// </summary>
        Task<PodcastSeriesCreationResult> CreatePodcastSeriesAsync(
            CreatePodcastSeriesDto dto, string metadataSource = Domain.Constants.PodcastMetadataSources.Manual);

        /// <summary>
        /// Throws <see cref="KeyNotFoundException"/> when the series does not exist and
        /// <see cref="InvalidOperationException"/> when the new feed URL or Apple id belongs to
        /// another series.
        /// </summary>
        Task<PodcastSeries> UpdatePodcastSeriesAsync(Guid id, CreatePodcastSeriesDto dto);
        Task<bool> DeletePodcastSeriesAsync(Guid id);
        Task<bool> PodcastSeriesExistsAsync(string title, string? publisher = null);
        Task<PodcastSeries?> GetPodcastSeriesByTitleAsync(string title, string? publisher = null);
        
        // Podcast Episode methods
        Task<IEnumerable<PodcastEpisode>> GetEpisodesBySeriesIdAsync(Guid seriesId);
        Task<PodcastEpisode?> GetPodcastEpisodeByIdAsync(Guid id);
        Task<IEnumerable<PodcastEpisode>> GetAllPodcastEpisodesAsync();
        Task<PodcastEpisodeCreationResult> CreatePodcastEpisodeAsync(CreatePodcastEpisodeDto dto);
        Task<PodcastEpisode> UpdatePodcastEpisodeAsync(Guid id, CreatePodcastEpisodeDto dto);
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
