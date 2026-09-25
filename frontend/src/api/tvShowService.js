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

// The API answers 201 when the show was just created and 200 when a show with
// the same TMDB id was already in the library (the existing row is returned
// untouched). Callers need both the item and which of the two happened.
export const importTvShowFromTmdb = async (tvShowId) => {
    try {
        const response = await apiClient.post(`/tvshow/from-tmdb/${tvShowId}`);
        return { item: response.data, created: response.status === 201 };
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

// Walks the show's seasons on TMDB and upserts its episodes. Answers 200 with the
// run result when the walk completed (per-season failures included) and 500 with
// the same body when it aborted; the caller reads `errorMessage` from that body.
export const importTvShowEpisodesFromTmdb = async (showId) => {
    try {
        const response = await apiClient.post(`/tvshow/${showId}/episodes/from-tmdb`);
        return response;
    } catch (error) {
        console.error('Error importing TV show episodes from TMDB:', error);
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
