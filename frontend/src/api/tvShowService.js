import { apiClient } from './apiClient';

// ============================================
// TV Show API calls
// ============================================

export const getAllTvShows = () => {
    return apiClient.get('/tvshow');
};

export const getTvShowById = (id) => {
    return apiClient.get(`/tvshow/${id}`);
};

export const getTvShowsByCreator = (creator) => {
    return apiClient.get(`/tvshow/by-creator/${encodeURIComponent(creator)}`);
};

export const getTvShowsByYear = (year) => {
    return apiClient.get(`/tvshow/by-year/${year}`);
};

export const createTvShow = (tvShowData) => {
    return apiClient.post('/tvshow', tvShowData);
};

export const updateTvShow = (id, tvShowData) => {
    return apiClient.put(`/tvshow/${id}`, tvShowData);
};

export const deleteTvShow = (id) => {
    return apiClient.delete(`/tvshow/${id}`);
};

export const importTvShowFromTmdb = async (tvShowId) => {
    try {
        const response = await apiClient.post(`/tvshow/from-tmdb/${tvShowId}`);
        return response.data;
    } catch (error) {
        console.error('Error importing TV show from TMDB:', error);
        throw error;
    }
};

export const searchTvShowsFromTmdb = async (query, page = 1) => {
    try {
        const response = await apiClient.get(`/tvshow/search-tmdb?query=${encodeURIComponent(query)}&page=${page}`);
        return response.data;
    } catch (error) {
        console.error('Error searching TV shows from TMDB:', error);
        throw error;
    }
};

// ============================================
// TV Show Episode API calls
// ============================================

export const getEpisodesByShowId = async (showId) => {
    try {
        const response = await apiClient.get(`/tvshow/${showId}/episodes`);
        return response;
    } catch (error) {
        console.error('Error fetching TV show episodes:', error);
        throw error;
    }
};

export const getTvShowEpisodeById = async (id) => {
    try {
        const response = await apiClient.get(`/tvshow/episodes/${id}`);
        return response;
    } catch (error) {
        console.error('Error fetching TV show episode:', error);
        throw error;
    }
};

export const deleteTvShowEpisode = async (id) => {
    try {
        const response = await apiClient.delete(`/tvshow/episodes/${id}`);
        return response;
    } catch (error) {
        console.error('Error deleting TV show episode:', error);
        throw error;
    }
};
