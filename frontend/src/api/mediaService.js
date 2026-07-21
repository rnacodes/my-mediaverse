import { apiClient } from './apiClient';

// ============================================
// Media API calls
// ============================================

export const addMedia = (mediaData) => {
    return apiClient.post('/media', mediaData);
};

export const getMediaById = (id) => {
    return apiClient.get(`/media/${id}`);
};

export const getAllMedia = () => {
    return apiClient.get('/media');
};

export const getMediaByType = (mediaType) => {
    return apiClient.get(`/media/by-type/${encodeURIComponent(mediaType)}`);
};

export const updateMedia = (id, mediaData) => {
    return apiClient.put(`/media/${id}`, mediaData);
};

export const deleteMedia = (id) => {
    return apiClient.delete(`/media/${id}`);
};

export const bulkDeleteMedia = (ids) => {
    return apiClient.delete('/media/bulk', {
        data: { ids }
    });
};

// Media filtering API calls
export const getMediaByTopic = (topicId) => {
    return apiClient.get(`/media/by-topic/${topicId}`);
};

export const getMediaByGenre = (genreId) => {
    return apiClient.get(`/media/by-genre/${genreId}`);
};

// Update media item's topics and genres
export const updateMediaTopicsGenres = async (mediaId, topics, genres) => {
    // Get the current media item first
    const response = await apiClient.get(`/media/${mediaId}`);
    const currentMedia = response.data;

    // Update with new topics and genres
    return apiClient.put(`/media/${mediaId}`, {
        title: currentMedia.title,
        mediaType: currentMedia.mediaType,
        status: currentMedia.status,
        rating: currentMedia.rating || null,
        ownershipStatus: currentMedia.ownershipStatus || null,
        link: currentMedia.link || null,
        description: currentMedia.description || null,
        notes: currentMedia.notes || null,
        relatedNotes: currentMedia.relatedNotes || null,
        thumbnail: currentMedia.thumbnail || null,
        genre: currentMedia.genre || null,
        dateCompleted: currentMedia.dateCompleted || null,
        topics: topics,
        genres: genres
    });
};
