import { apiClient } from './apiClient';

// ============================================
// Unified Readwise Sync API Methods
// ============================================

/**
 * Validates the Readwise API connection
 */
export const validateReadwiseConnection = async () => {
    try {
        const response = await apiClient.get('/readwise/validate');
        return response;
    } catch (error) {
        console.error('Error validating Readwise connection:', error);
        throw error;
    }
};

/**
 * Unified sync: syncs Reader documents and Readwise highlights in one operation.
 * Auto-links highlights to articles during import.
 * @param {boolean} incremental - If true (default), only syncs items changed since the last
 *   successful sync (falling back to the last 7 days until one has been recorded)
 */
export const syncAll = async (incremental = true) => {
    try {
        const response = await apiClient.post('/readwise/sync', null, {
            params: { incremental }
        });
        return response;
    } catch (error) {
        console.error('Error in unified Readwise sync:', error);
        throw error;
    }
};

/**
 * Fetches full HTML content for archived articles.
 * Only fetches content for articles with Status = Completed.
 * @param {number} batchSize - Number of articles to fetch (default 50)
 * @param {boolean} recentOnly - If true, only fetch articles synced in the last 7 days
 */
export const fetchArticleContent = async (batchSize = 50, recentOnly = false) => {
    try {
        const response = await apiClient.post('/readwise/fetch-content', null, {
            params: { batchSize, recentOnly }
        });
        return response;
    } catch (error) {
        console.error('Error fetching article content:', error);
        throw error;
    }
};
