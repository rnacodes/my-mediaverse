import { apiClient } from './apiClient';

// ============================================
// Video API calls
// ============================================

export const getVideoById = (id) => {
    return apiClient.get(`/video/${id}`);
};

export const createVideo = (videoData) => {
    return apiClient.post('/video', videoData);
};

export const updateVideo = (id, videoData) => {
    return apiClient.put(`/video/${id}`, videoData);
};

export const getPlaylistsForVideo = async (videoId) => {
    try {
        const response = await apiClient.get(`/video/${videoId}/playlists`);
        return response.data;
    } catch (error) {
        console.error('Error getting playlists for video:', error);
        throw error;
    }
};
