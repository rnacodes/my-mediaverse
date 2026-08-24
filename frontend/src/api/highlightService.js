import { apiClient } from './apiClient';

// Readwise validate/sync live in readwiseService.js (/api/readwise/*)

/**
 * Gets all highlights
 */
export const getAllHighlights = async () => {
    try {
        const response = await apiClient.get('/highlight');
        return response.data;
    } catch (error) {
        console.error('Error fetching highlights:', error);
        throw error;
    }
};

/**
 * Gets a specific highlight by ID
 * @param {string} id - The highlight ID
 */
export const getHighlightById = async (id) => {
    try {
        const response = await apiClient.get(`/highlight/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching highlight:', error);
        throw error;
    }
};

/**
 * Gets highlights for a specific article
 * @param {string} articleId - The article ID
 */
export const getHighlightsByArticle = async (articleId) => {
    try {
        const response = await apiClient.get(`/highlight/article/${articleId}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching highlights for article:', error);
        throw error;
    }
};

/**
 * Gets highlights for a specific book
 * @param {string} bookId - The book ID
 */
export const getHighlightsByBook = async (bookId) => {
    try {
        const response = await apiClient.get(`/highlight/book/${bookId}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching highlights for book:', error);
        throw error;
    }
};

/**
 * Gets highlights by tag
 * @param {string} tag - The tag to filter by
 */
export const getHighlightsByTag = async (tag) => {
    try {
        const response = await apiClient.get(`/highlight/tag/${encodeURIComponent(tag)}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching highlights by tag:', error);
        throw error;
    }
};

/**
 * Gets all unlinked highlights (not associated with any book or article)
 */
export const getUnlinkedHighlights = async () => {
    try {
        const response = await apiClient.get('/highlight/unlinked');
        return response.data;
    } catch (error) {
        console.error('Error fetching unlinked highlights:', error);
        throw error;
    }
};

/**
 * Bulk creates multiple highlights
 * @param {Array} highlights - Array of highlight data objects
 * @returns {{ created: number, linked: number, errors: string[] }}
 */
export const bulkCreateHighlights = async (highlights) => {
    try {
        const response = await apiClient.post('/highlight/bulk', highlights);
        return response.data;
    } catch (error) {
        console.error('Error bulk creating highlights:', error);
        throw error;
    }
};

/**
 * Creates a new highlight
 * @param {Object} highlightData - The highlight data
 */
export const createHighlight = async (highlightData) => {
    try {
        const response = await apiClient.post('/highlight', highlightData);
        return response.data;
    } catch (error) {
        console.error('Error creating highlight:', error);
        throw error;
    }
};

/**
 * Updates a highlight
 * @param {string} id - The highlight ID
 * @param {Object} highlightData - The updated highlight data
 */
export const updateHighlight = async (id, highlightData) => {
    try {
        const response = await apiClient.put(`/highlight/${id}`, highlightData);
        return response.data;
    } catch (error) {
        console.error('Error updating highlight:', error);
        throw error;
    }
};

/**
 * Sets a highlight's media link (article OR book, or neither to unlink)
 * @param {string} id - The highlight ID
 * @param {{ articleId?: string|null, bookId?: string|null }} link - The link target
 */
export const setHighlightLink = async (id, { articleId = null, bookId = null } = {}) => {
    try {
        const response = await apiClient.put(`/highlight/${id}/link`, { articleId, bookId });
        return response.data;
    } catch (error) {
        console.error('Error setting highlight link:', error);
        throw error;
    }
};

/**
 * Deletes a highlight
 * @param {string} id - The highlight ID
 */
export const deleteHighlight = async (id) => {
    try {
        await apiClient.delete(`/highlight/${id}`);
    } catch (error) {
        console.error('Error deleting highlight:', error);
        throw error;
    }
};

/**
 * Deletes multiple highlights; unknown IDs are skipped by the server.
 * @param {string[]} ids - The highlight IDs
 * @returns {Promise<{message: string, deletedCount: number}>}
 */
export const bulkDeleteHighlights = async (ids) => {
    try {
        const response = await apiClient.delete('/highlight/bulk', {
            data: { ids }
        });
        return response.data;
    } catch (error) {
        console.error('Error bulk deleting highlights:', error);
        throw error;
    }
};

/**
 * Links highlights to media items
 */
export const linkHighlightsToMedia = async () => {
    try {
        const response = await apiClient.post('/highlight/link');
        return response;
    } catch (error) {
        console.error('Error linking highlights to media:', error);
        throw error;
    }
};

/**
 * Exports a highlight to Readwise
 * @param {string} id - The highlight ID
 */
export const exportHighlightToReadwise = async (id) => {
    try {
        const response = await apiClient.post(`/highlight/${id}/export`);
        return response.data;
    } catch (error) {
        console.error('Error exporting highlight to Readwise:', error);
        throw error;
    }
};

/**
 * Cleans HTML/CSS from all highlight text in the database
 * Removes any CSS contamination that may have been accidentally stored
 */
export const cleanHighlightText = async () => {
    try {
        const response = await apiClient.post('/highlight/clean-text');
        return response.data;
    } catch (error) {
        console.error('Error cleaning highlight text:', error);
        throw error;
    }
};
