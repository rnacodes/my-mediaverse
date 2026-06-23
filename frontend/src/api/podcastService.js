import { apiClient } from './apiClient';

// ============================================
// Podcast Search Functions - Using real ListenNotes API only
// ============================================

export const searchPodcasts = async (query) => {
    try {
        const response = await apiClient.get(`/ListenNotes/search?query=${encodeURIComponent(query)}&type=podcast`);
        return response.data;
    } catch (error) {
        console.error('Error searching podcasts:', error);
        throw error;
    }
};

export const getPodcastFromApi = async (id) => {
    try {
        const response = await apiClient.get(`/ListenNotes/podcasts/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error getting podcast:', error);
        throw error;
    }
};

export const importPodcastFromApi = async (podcastData) => {
    try {
        let response;

        if (podcastData.podcastId || podcastData.PodcastId) {
            // Import by ID
            response = await apiClient.post(`/podcast/from-api/${podcastData.podcastId || podcastData.PodcastId}`);
        } else if (podcastData.podcastName || podcastData.PodcastName) {
            // Import by name
            response = await apiClient.post('/podcast/from-api/by-name', {
                podcastName: podcastData.podcastName || podcastData.PodcastName
            });
        } else {
            throw new Error('Either podcastId or podcastName must be provided');
        }

        return response.data;
    } catch (error) {
        console.error('Error importing podcast:', error);
        throw error;
    }
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

export const importPodcastSeriesFromApi = (podcastId) => {
    return apiClient.post(`/podcast/series/from-api/${podcastId}`);
};

export const importPodcastSeriesByName = (podcastName) => {
    return apiClient.post('/podcast/series/from-api/by-name', { podcastName });
};

// ============================================
// Podcast Episode API calls
// ============================================

export const importPodcastEpisodeFromApi = async (episodeId, seriesId) => {
    try {
        const response = await apiClient.post(`/podcast/episodes/from-api/${episodeId}?seriesId=${seriesId}`);
        return response.data;
    } catch (error) {
        console.error('Error importing podcast episode from API:', error);
        throw error;
    }
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
