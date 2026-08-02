import { apiClient } from './apiClient';

// ============================================
// Recommendation Service Status
// ============================================

/**
 * Gets the recommendation service status (checks AI and pgvector availability)
 */
export const getRecommendationStatus = async () => {
    try {
        const response = await apiClient.get('/recommendation/status');
        return response.data;
    } catch (error) {
        console.error('Error getting recommendation status:', error);
        throw error;
    }
};

// ============================================
// Similar Items (Embedding-based)
// ============================================

/**
 * Gets media items similar to the given media item based on embeddings
 * @param {string} id - The media item ID
 * @param {number} limit - Optional result limit (default 10)
 * @param {string} mediaType - Optional media type filter
 */
export const getSimilarMedia = async (id, limit = 10, mediaType = null) => {
    try {
        const params = { count: limit };
        if (mediaType) params.mediaType = mediaType;
        const response = await apiClient.get(`/recommendation/similar/media/${id}`, { params });
        return response.data.items || [];
    } catch (error) {
        console.error('Error getting similar media:', error);
        throw error;
    }
};

/**
 * Gets notes similar to the given note based on embeddings
 * @param {string} id - The note ID
 * @param {number} limit - Optional result limit (default 10)
 * @param {string} vault - Optional vault filter
 */
export const getSimilarNotes = async (id, limit = 10, vault = null) => {
    try {
        const params = { count: limit };
        if (vault) params.vault = vault;
        const response = await apiClient.get(`/recommendation/similar/note/${id}`, { params });
        return response.data.notes || [];
    } catch (error) {
        console.error('Error getting similar notes:', error);
        throw error;
    }
};

// ============================================
// Vibe Search (Natural Language)
// ============================================

/**
 * Searches for media items by natural language description (vibe)
 * @param {string} query - Natural language description
 * @param {string} mediaType - Optional media type filter
 * @param {number} limit - Optional result limit (default 20)
 */
export const searchByVibe = async (query, mediaType = null, limit = 20) => {
    try {
        const params = { description: query, count: limit };
        if (mediaType) params.mediaType = mediaType;
        const response = await apiClient.get('/recommendation/by-vibe', { params });
        return response.data.items || [];
    } catch (error) {
        console.error('Error searching by vibe:', error);
        throw error;
    }
};

// ============================================
// Personalized Recommendations
// ============================================

/**
 * Gets personalized recommendations based on user's liked/explored items
 * @param {number} limit - Optional result limit (default 20)
 * @param {string} mediaType - Optional media type filter
 */
export const getForYouRecommendations = async (limit = 20, mediaType = null) => {
    try {
        const params = { count: limit };
        if (mediaType) params.mediaType = mediaType;
        const response = await apiClient.get('/recommendation/for-you', { params });
        return response.data.items || [];
    } catch (error) {
        console.error('Error getting personalized recommendations:', error);
        throw error;
    }
};

// ============================================
// Cross-Type Recommendations
// ============================================

/**
 * Gets media items related to a note based on embeddings
 * @param {string} noteId - The note ID
 * @param {number} limit - Optional result limit (default 10)
 * @param {string} mediaType - Optional media type filter
 */
export const getMediaForNote = async (noteId, limit = 10, mediaType = null) => {
    try {
        const params = { count: limit };
        if (mediaType) params.mediaType = mediaType;
        const response = await apiClient.get(`/recommendation/media-for-note/${noteId}`, { params });
        return response.data.items || [];
    } catch (error) {
        console.error('Error getting media for note:', error);
        throw error;
    }
};

/**
 * Gets notes related to a media item based on embeddings
 * @param {string} mediaItemId - The media item ID
 * @param {number} limit - Optional result limit (default 10)
 * @param {string} vault - Optional vault filter
 */
export const getNotesForMedia = async (mediaItemId, limit = 10, vault = null) => {
    try {
        const params = { count: limit };
        if (vault) params.vault = vault;
        const response = await apiClient.get(`/recommendation/notes-for-media/${mediaItemId}`, { params });
        return response.data.notes || [];
    } catch (error) {
        console.error('Error getting notes for media:', error);
        throw error;
    }
};
