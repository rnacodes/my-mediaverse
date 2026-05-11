import { apiClient } from './apiClient';

// ============================================
// Real-Time Indexing Toggle
// ============================================

/**
 * Get the current real-time indexing status
 * @returns {Promise<Object>} Object with { enabled: boolean }
 */
export const getRealTimeIndexingStatus = async () => {
    try {
        const response = await apiClient.get('/search/realtime-indexing');
        return response.data;
    } catch (error) {
        console.error('Error getting real-time indexing status:', error);
        throw error;
    }
};

/**
 * Enable or disable real-time Typesense indexing for CRUD operations
 * @param {boolean} enabled - Whether to enable real-time indexing
 * @returns {Promise<Object>} Result with { enabled, message }
 */
export const setRealTimeIndexingStatus = async (enabled) => {
    try {
        const response = await apiClient.post('/search/realtime-indexing', { enabled });
        return response.data;
    } catch (error) {
        console.error('Error setting real-time indexing status:', error);
        throw error;
    }
};

// ============================================
// Typesense Admin API calls
// ============================================

/**
 * Trigger a bulk reindex of all media items in Typesense
 * @returns {Promise<Object>} Reindex result with statistics
 */
export const typesenseReindex = async () => {
    try {
        const response = await apiClient.post('/search/reindex');
        return response.data;
    } catch (error) {
        console.error('Error reindexing Typesense:', error);
        throw error;
    }
};

/**
 * Re-index a single media item in Typesense
 * @param {string} id - The media item ID
 * @returns {Promise<Object>} Reindex result
 */
export const reindexMediaItem = async (id) => {
    try {
        const response = await apiClient.post(`/search/reindex-media/${id}`);
        return response.data;
    } catch (error) {
        console.error(`Error reindexing media item ${id}:`, error);
        throw error;
    }
};

/**
 * Re-index a single mixlist in Typesense
 * @param {string} id - The mixlist ID
 * @returns {Promise<Object>} Reindex result
 */
export const reindexMixlist = async (id) => {
    try {
        const response = await apiClient.post(`/search/reindex-mixlist/${id}`);
        return response.data;
    } catch (error) {
        console.error(`Error reindexing mixlist ${id}:`, error);
        throw error;
    }
};

/**
 * Re-index a single note in Typesense
 * @param {string} id - The note ID
 * @returns {Promise<Object>} Reindex result
 */
export const reindexNote = async (id) => {
    try {
        const response = await apiClient.post(`/search/reindex-note/${id}`);
        return response.data;
    } catch (error) {
        console.error(`Error reindexing note ${id}:`, error);
        throw error;
    }
};

/**
 * Re-index a single highlight in Typesense
 * @param {string} id - The highlight ID
 * @returns {Promise<Object>} Reindex result
 */
export const reindexHighlight = async (id) => {
    try {
        const response = await apiClient.post(`/search/reindex-highlight/${id}`);
        return response.data;
    } catch (error) {
        console.error(`Error reindexing highlight ${id}:`, error);
        throw error;
    }
};

/**
 * Check Typesense health status
 * @returns {Promise<Object>} Health status information
 */
export const typesenseHealth = async () => {
    try {
        const response = await apiClient.get('/search/health');
        return response.data;
    } catch (error) {
        console.error('Error checking Typesense health:', error);
        throw error;
    }
};

/**
 * Reset the media_items collection in Typesense (deletes and recreates)
 * WARNING: This will delete all indexed media items!
 * @returns {Promise<Object>} Reset result
 */
export const typesenseResetMediaItems = async () => {
    try {
        const response = await apiClient.post('/search/reset');
        return response.data;
    } catch (error) {
        console.error('Error resetting media items collection:', error);
        throw error;
    }
};

/**
 * Reset the mixlists collection in Typesense (deletes and recreates)
 * WARNING: This will delete all indexed mixlists!
 * @returns {Promise<Object>} Reset result
 */
export const typesenseResetMixlists = async () => {
    try {
        const response = await apiClient.post('/search/reset-mixlists');
        return response.data;
    } catch (error) {
        console.error('Error resetting mixlists collection:', error);
        throw error;
    }
};

/**
 * Search media items using Typesense
 * @param {string} query - Search query
 * @param {string} mediaType - Media type filter ('all' or specific type)
 * @param {number} page - Page number (default: 1)
 * @param {number} perPage - Results per page (default: 20)
 * @returns {Promise<Object>} Search results
 */
export const typesenseSearch = async (query, mediaType = 'all', page = 1, perPage = 20) => {
    try {
        const params = {
            q: query,
            page: page,
            per_page: perPage,
        };

        let endpoint = '/search';
        if (mediaType !== 'all') {
            endpoint = `/search/by-type/${mediaType}`;
        }

        const response = await apiClient.get(endpoint, { params });
        return response.data;
    } catch (error) {
        console.error('Error searching Typesense:', error);
        throw error;
    }
};

/**
 * Advanced search with multiple filters
 * @param {Object} options - Search options
 * @param {string} options.query - Search query (default: '*' for all)
 * @param {Array<string>} options.mediaTypes - Array of media types to filter by
 * @param {Array<string>} options.topics - Array of topics to filter by
 * @param {Array<string>} options.genres - Array of genres to filter by
 * @param {string} options.status - Status filter (Uncharted, ActivelyExploring, Completed, Abandoned)
 * @param {Array<string>} options.ratings - Array of ratings to filter by (SuperLike, Like, Neutral, Dislike)
 * @param {number} options.page - Page number (default: 1)
 * @param {number} options.perPage - Results per page (default: 20)
 * @param {string} options.sortBy - Sort field (default: relevance)
 * @returns {Promise<Object>} Search results
 */
export const typesenseAdvancedSearch = async (options) => {
    try {
        const {
            query = '*',
            mediaTypes = [],
            topics = [],
            genres = [],
            status = null,
            ratings = [],
            page = 1,
            perPage = 20
        } = options;

        // Build filter string
        const filters = [];

        // Media type filter
        if (mediaTypes.length > 0 && !mediaTypes.includes('all')) {
            const mediaTypeFilter = mediaTypes.map(type => `media_type:=${type}`).join(' || ');
            filters.push(`(${mediaTypeFilter})`);
        }

        // Topics filter - wrap values in backticks for Typesense (handles spaces/special chars)
        if (topics.length > 0) {
            const topicFilter = topics.map(topic => `topics:=\`${topic}\``).join(' || ');
            filters.push(`(${topicFilter})`);
        }

        // Genres filter - wrap values in backticks for Typesense (handles spaces/special chars)
        if (genres.length > 0) {
            const genreFilter = genres.map(genre => `genres:=\`${genre}\``).join(' || ');
            filters.push(`(${genreFilter})`);
        }

        // Status filter
        if (status && status !== 'all') {
            filters.push(`status:=${status}`);
        }

        // Ratings filter
        if (ratings.length > 0) {
            const ratingFilter = ratings.map(rating => `rating:=${rating}`).join(' || ');
            filters.push(`(${ratingFilter})`);
        }

        const params = {
            q: query || '*',
            page: page,
            per_page: perPage,
        };

        if (filters.length > 0) {
            params.filter = filters.join(' && ');
        }

        const response = await apiClient.get('/search', { params });
        return response.data;
    } catch (error) {
        console.error('Error performing advanced search:', error);
        throw error;
    }
};

/**
 * Search mixlists using Typesense
 * @param {string} query - Search query
 * @param {string} filter - Optional filter string (e.g., "topics:=productivity")
 * @param {number} page - Page number (default: 1)
 * @param {number} perPage - Results per page (default: 20)
 * @returns {Promise<Object>} Search results
 */
export const typesenseSearchMixlists = async (query, filter = null, page = 1, perPage = 20) => {
    try {
        const params = {
            q: query,
            page: page,
            per_page: perPage,
        };

        if (filter) {
            params.filter = filter;
        }

        const response = await apiClient.get('/search/mixlists', { params });
        return response.data;
    } catch (error) {
        console.error('Error searching mixlists:', error);
        throw error;
    }
};

/**
 * Advanced mixlist search with multiple filters
 * @param {Object} options - Search options
 * @param {string} options.query - Search query (default: '*' for all)
 * @param {Array<string>} options.topics - Array of topics to filter by
 * @param {Array<string>} options.genres - Array of genres to filter by
 * @param {number} options.page - Page number (default: 1)
 * @param {number} options.perPage - Results per page (default: 20)
 * @param {string} options.sortBy - Sort field (default: relevance)
 * @returns {Promise<Object>} Search results
 */
export const typesenseAdvancedSearchMixlists = async (options) => {
    try {
        const {
            query = '*',
            topics = [],
            genres = [],
            page = 1,
            perPage = 20
        } = options;

        // Build filter string
        const filters = [];

        // Topics filter - wrap values in backticks for Typesense (handles spaces/special chars)
        if (topics.length > 0) {
            const topicFilter = topics.map(topic => `topics:=\`${topic}\``).join(' || ');
            filters.push(`(${topicFilter})`);
        }

        // Genres filter - wrap values in backticks for Typesense (handles spaces/special chars)
        if (genres.length > 0) {
            const genreFilter = genres.map(genre => `genres:=\`${genre}\``).join(' || ');
            filters.push(`(${genreFilter})`);
        }

        const params = {
            q: query || '*',
            page: page,
            per_page: perPage,
        };

        if (filters.length > 0) {
            params.filter = filters.join(' && ');
        }

        const response = await apiClient.get('/search/mixlists', { params });
        return response.data;
    } catch (error) {
        console.error('Error performing advanced mixlist search:', error);
        throw error;
    }
};

/**
 * Reindex all mixlists in Typesense
 * @returns {Promise<Object>} Reindex results
 */
export const reindexMixlists = async () => {
    try {
        const response = await apiClient.post('/search/reindex-mixlists');
        return response.data;
    } catch (error) {
        console.error('Error reindexing mixlists:', error);
        throw error;
    }
};

// ============================================
// Notes Reindex/Reset
// ============================================

/**
 * Reindex all notes in Typesense
 * @returns {Promise<Object>} Reindex results with count
 */
export const reindexNotes = async () => {
    try {
        const response = await apiClient.post('/search/reindex-notes');
        return response.data;
    } catch (error) {
        console.error('Error reindexing notes:', error);
        throw error;
    }
};

/**
 * Reset the obsidian_notes collection in Typesense
 * WARNING: This will delete all indexed notes!
 * @returns {Promise<Object>} Reset result
 */
export const resetNotesCollection = async () => {
    try {
        const response = await apiClient.post('/search/reset-notes');
        return response.data;
    } catch (error) {
        console.error('Error resetting notes collection:', error);
        throw error;
    }
};

// ============================================
// Highlights Search
// ============================================

/**
 * Search highlights using Typesense
 * @param {string} query - The search query (searches text, note, title, author, tags)
 * @param {string} filter - Optional filter string (e.g., "category:=books", "is_favorite:=true")
 * @param {number} page - Page number (default 1)
 * @param {number} perPage - Results per page (default 20)
 * @returns {Promise<Object>} Typesense search response with hits
 */
export const searchHighlights = async (query = '*', filter = null, page = 1, perPage = 20) => {
    try {
        const params = { q: query, page, per_page: perPage };
        if (filter) params.filter = filter;

        const response = await apiClient.get('/search/highlights', { params });
        return response.data;
    } catch (error) {
        console.error('Error searching highlights:', error);
        throw error;
    }
};

/**
 * Advanced highlight search with multiple filters
 * @param {Object} options - Search options
 * @param {string} options.query - Search query text
 * @param {string[]} options.categories - Filter by categories (books, articles, etc.)
 * @param {string[]} options.tags - Filter by tags
 * @param {boolean} options.isFavorite - Filter by favorite status
 * @param {string} options.linkedMediaType - Filter by linked media type (article, book, or null for unlinked)
 * @param {number} options.page - Page number
 * @param {number} options.perPage - Results per page
 * @returns {Promise<Object>} Typesense search response
 */
export const searchHighlightsAdvanced = async (options) => {
    const {
        query = '*',
        categories = [],
        tags = [],
        isFavorite = null,
        linkedMediaType = null,
        page = 1,
        perPage = 20
    } = options;

    try {
        const filters = [];

        // Category filter - wrap values in backticks for Typesense (handles spaces/special chars)
        if (categories.length > 0) {
            const categoryFilter = categories.map(c => `category:=\`${c}\``).join(' || ');
            filters.push(`(${categoryFilter})`);
        }

        // Tags filter - wrap values in backticks for Typesense (handles spaces/special chars)
        if (tags.length > 0) {
            const tagFilter = tags.map(t => `tags:=\`${t}\``).join(' || ');
            filters.push(`(${tagFilter})`);
        }

        // Favorite filter
        if (isFavorite !== null) {
            filters.push(`is_favorite:=${isFavorite}`);
        }

        // Linked media type filter
        if (linkedMediaType !== null) {
            if (linkedMediaType === 'unlinked') {
                // Unlinked means no article_id and no book_id
                // Typesense doesn't support null checks directly, so we filter for empty linked_media_type
                filters.push(`linked_media_type:=null`);
            } else {
                filters.push(`linked_media_type:=\`${linkedMediaType}\``);
            }
        }

        const filterString = filters.length > 0 ? filters.join(' && ') : null;
        return await searchHighlights(query, filterString, page, perPage);
    } catch (error) {
        console.error('Error performing advanced highlight search:', error);
        throw error;
    }
};

/**
 * Reindex all highlights in Typesense
 * @returns {Promise<Object>} Reindex results with count
 */
export const reindexHighlights = async () => {
    try {
        const response = await apiClient.post('/search/reindex-highlights');
        return response.data;
    } catch (error) {
        console.error('Error reindexing highlights:', error);
        throw error;
    }
};

/**
 * Reset the highlights collection in Typesense
 * WARNING: This will delete all indexed highlights!
 * @returns {Promise<Object>} Reset result
 */
export const resetHighlightsCollection = async () => {
    try {
        const response = await apiClient.post('/search/reset-highlights');
        return response.data;
    } catch (error) {
        console.error('Error resetting highlights collection:', error);
        throw error;
    }
};
