import { apiClient } from './apiClient';

// ============================================
// Podcast directory (Apple Podcasts, with Podcast Index as a fallback)
// ============================================

/**
 * Search the podcast directory for shows. Results carry the feed URL and,
 * when the show is already in the library, its existingSeriesId.
 */
export const searchPodcastDirectory = (term, limit) => {
    return apiClient.get('/podcast/directory/search', { params: { term, limit } });
};

// ============================================
// Podcast Series API calls
// ============================================

export const getAllPodcastSeries = () => {
    return apiClient.get('/podcast/series');
};

export const getPodcastSeriesById = (id) => {
    return apiClient.get(`/podcast/series/${id}`);
};

export const searchPodcastSeries = (query) => {
    return apiClient.get(`/podcast/series/search?query=${encodeURIComponent(query)}`);
};

export const createPodcastSeries = (seriesData) => {
    return apiClient.post('/podcast/series', seriesData);
};

export const updatePodcastSeries = (id, seriesData) => {
    return apiClient.put(`/podcast/series/${id}`, seriesData);
};

export const deletePodcastSeries = (id) => {
    return apiClient.delete(`/podcast/series/${id}`);
};

export const subscribeToPodcastSeries = (seriesId) => {
    return apiClient.post(`/podcast/series/${seriesId}/subscribe`);
};

export const unsubscribeFromPodcastSeries = (seriesId) => {
    return apiClient.post(`/podcast/series/${seriesId}/unsubscribe`);
};

export const getSubscribedPodcastSeries = () => {
    return apiClient.get('/podcast/series/subscriptions');
};

export const syncPodcastSeriesEpisodes = (seriesId) => {
    return apiClient.post(`/podcast/series/${seriesId}/sync`);
};

/**
 * Sync every subscribed series from its feed.
 * @returns {Promise} API response with a PodcastSyncAllResultDto payload
 */
export const syncAllPodcastSeries = () => {
    return apiClient.post('/podcast/series/sync-all');
};

/**
 * Fill a series from its feed. Without force an already-filled series is
 * reported as skipped; with it the feed overwrites the stored details.
 */
export const enrichPodcastSeries = (seriesId, force = false) => {
    return apiClient.post(`/podcast/series/${seriesId}/enrich`, null, { params: { force } });
};

/**
 * Import a series from its feed. Pass a feed URL, an Apple Podcasts id, or both.
 * Responds 201 when the series was created and 200 when it was already in the library.
 */
export const importPodcastSeriesFromFeed = ({ feedUrl, applePodcastsId }) => {
    return apiClient.post('/podcast/series/from-feed', { feedUrl, applePodcastsId });
};

// ============================================
// Podcast OPML Import
// ============================================

/**
 * Import podcast subscriptions from an OPML export file.
 * Single multipart request — feeds land as lightweight stubs (no chunking).
 * @param {File} file - The .opml/.xml file to upload
 * @returns {Promise} API response with an OpmlImportResultDto payload
 */
export const importPodcastsFromOpml = (file) => {
    const formData = new FormData();
    formData.append('file', file);

    return apiClient.post('/podcast/series/from-opml', formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
};

// ============================================
// Podcast Episode API calls
// ============================================

/**
 * Read a page of episodes straight from the series' feed (not the library).
 * refresh bypasses the server's short-lived feed cache.
 */
export const getPodcastFeedEpisodes = (seriesId, { offset = 0, limit = 20, refresh = false } = {}) => {
    return apiClient.get(`/podcast/series/${seriesId}/feed-episodes`, { params: { offset, limit, refresh } });
};

/**
 * Import one episode from the series' feed, identified by its guid
 * (or its audio URL when the feed item has no guid).
 */
export const importPodcastEpisodeFromFeed = ({ seriesId, guid, audioUrl }) => {
    return apiClient.post('/podcast/episodes/from-feed', { seriesId, guid, audioUrl });
};

export const getEpisodesBySeriesId = (seriesId) => {
    return apiClient.get(`/podcast/series/${seriesId}/episodes`);
};

export const getPodcastEpisodeById = (id) => {
    return apiClient.get(`/podcast/episodes/${id}`);
};

export const getAllPodcastEpisodes = () => {
    return apiClient.get('/podcast/episodes');
};

export const createPodcastEpisode = (episodeData) => {
    return apiClient.post('/podcast/episodes', episodeData);
};

export const updatePodcastEpisode = (id, episodeData) => {
    return apiClient.put(`/podcast/episodes/${id}`, episodeData);
};

export const deletePodcastEpisode = (id) => {
    return apiClient.delete(`/podcast/episodes/${id}`);
};
