import { apiClient } from './apiClient';

// ============================================
// AI Service Status
// ============================================

/**
 * Gets the AI service status including availability and pending counts
 */
export const getAiStatus = async () => {
    try {
        const response = await apiClient.get('/ai/status');
        return response.data;
    } catch (error) {
        console.error('Error getting AI status:', error);
        throw error;
    }
};

// ============================================
// Note Description Generation
// ============================================

/**
 * Generates an AI description for a single note
 * @param {string} id - The note ID
 */
export const generateNoteDescription = async (id) => {
    try {
        const response = await apiClient.post(`/ai/notes/${id}/generate-description`);
        return response.data;
    } catch (error) {
        console.error('Error generating note description:', error);
        throw error;
    }
};

/**
 * Generates AI descriptions for a batch of notes
 * @param {number} batchSize - Optional batch size (default handled by server)
 */
export const generateNoteDescriptionsBatch = async (batchSize = null) => {
    try {
        const data = batchSize ? { batchSize } : {};
        const response = await apiClient.post('/ai/notes/generate-descriptions-batch', data);
        return response.data;
    } catch (error) {
        console.error('Error generating note descriptions batch:', error);
        throw error;
    }
};

/**
 * Gets the count of notes pending AI description generation
 */
export const getPendingNoteDescriptions = async () => {
    try {
        const response = await apiClient.get('/ai/notes/pending-descriptions');
        return response.data;
    } catch (error) {
        console.error('Error getting pending note descriptions count:', error);
        throw error;
    }
};
