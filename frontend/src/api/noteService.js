import { apiClient } from './apiClient';

// ============================================
// Note CRUD Operations
// ============================================

/**
 * Gets all notes, optionally filtered by vault
 * @param {string} vault - Optional vault name filter ('general' or 'programming')
 */
export const getAllNotes = async (vault = null) => {
    try {
        const params = vault ? { vault } : {};
        const response = await apiClient.get('/note', { params });
        return response.data;
    } catch (error) {
        console.error('Error getting all notes:', error);
        throw error;
    }
};

/**
 * Gets a note by ID
 * @param {string} id - The note ID
 */
export const getNoteById = async (id) => {
    try {
        const response = await apiClient.get(`/note/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error getting note:', error);
        throw error;
    }
};

/**
 * Gets a note by vault and slug
 * @param {string} vault - The vault name
 * @param {string} slug - The note slug
 */
export const getNoteBySlug = async (vault, slug) => {
    try {
        const response = await apiClient.get(`/note/slug/${vault}/${slug}`);
        return response.data;
    } catch (error) {
        console.error('Error getting note by slug:', error);
        throw error;
    }
};

/**
 * Creates a new note
 * @param {Object} noteData - The note data
 */
export const createNote = async (noteData) => {
    try {
        const response = await apiClient.post('/note', noteData);
        return response.data;
    } catch (error) {
        console.error('Error creating note:', error);
        throw error;
    }
};

/**
 * Updates an existing note
 * @param {string} id - The note ID
 * @param {Object} noteData - The updated note data
 */
export const updateNote = async (id, noteData) => {
    try {
        const response = await apiClient.put(`/note/${id}`, noteData);
        return response.data;
    } catch (error) {
        console.error('Error updating note:', error);
        throw error;
    }
};

/**
 * Deletes a note
 * @param {string} id - The note ID
 */
export const deleteNote = async (id) => {
    try {
        const response = await apiClient.delete(`/note/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error deleting note:', error);
        throw error;
    }
};

/**
 * Deletes multiple notes; unknown IDs are skipped by the server.
 * @param {string[]} ids - The note IDs
 * @returns {Promise<{message: string, deletedCount: number}>}
 */
export const bulkDeleteNotes = async (ids) => {
    try {
        const response = await apiClient.delete('/note/bulk', {
            data: { ids }
        });
        return response.data;
    } catch (error) {
        console.error('Error bulk deleting notes:', error);
        throw error;
    }
};

// ============================================
// Note Linking Operations
// ============================================

/**
 * Links a note to a media item
 * @param {string} noteId - The note ID
 * @param {string} mediaItemId - The media item ID
 * @param {string} linkDescription - Optional description for the link
 */
export const linkNoteToMedia = async (noteId, mediaItemId, linkDescription = null) => {
    try {
        const response = await apiClient.post(`/note/${noteId}/link`, {
            mediaItemId,
            linkDescription
        });
        return response.data;
    } catch (error) {
        console.error('Error linking note to media:', error);
        throw error;
    }
};

/**
 * Unlinks a note from a media item
 * @param {string} noteId - The note ID
 * @param {string} mediaItemId - The media item ID
 */
export const unlinkNoteFromMedia = async (noteId, mediaItemId) => {
    try {
        const response = await apiClient.delete(`/note/${noteId}/link/${mediaItemId}`);
        return response.data;
    } catch (error) {
        console.error('Error unlinking note from media:', error);
        throw error;
    }
};

/**
 * Gets all media items linked to a note
 * @param {string} noteId - The note ID
 */
export const getMediaForNote = async (noteId) => {
    try {
        const response = await apiClient.get(`/note/${noteId}/media`);
        return response.data;
    } catch (error) {
        console.error('Error getting media for note:', error);
        throw error;
    }
};

/**
 * Gets all notes linked to a media item
 * @param {string} mediaItemId - The media item ID
 */
export const getNotesForMedia = async (mediaItemId) => {
    try {
        const response = await apiClient.get(`/note/for-media/${mediaItemId}`);
        return response.data;
    } catch (error) {
        console.error('Error getting notes for media:', error);
        throw error;
    }
};

// ============================================
// Note Sync Operations
// ============================================

/**
 * Syncs notes from a specific Quartz vault
 * @param {string} vault - The vault name ('general' or 'programming')
 * @param {string} url - Optional vault URL override
 * @param {string} authToken - Optional auth token override
 */
export const syncVault = async (vault, url = null, authToken = null) => {
    try {
        const params = {};
        if (url) params.url = url;
        if (authToken) params.authToken = authToken;

        const response = await apiClient.post(`/note/sync/${vault}`, null, { params });
        return response.data;
    } catch (error) {
        console.error('Error syncing vault:', error);
        throw error;
    }
};

/**
 * Syncs notes from all configured vaults
 */
export const syncAllVaults = async () => {
    try {
        const response = await apiClient.post('/note/sync');
        return response.data;
    } catch (error) {
        console.error('Error syncing all vaults:', error);
        throw error;
    }
};

/**
 * Gets the current sync configuration status
 */
export const getSyncStatus = async () => {
    try {
        const response = await apiClient.get('/note/sync/status');
        return response.data;
    } catch (error) {
        console.error('Error getting sync status:', error);
        throw error;
    }
};

// ============================================
// Note Search Operations
// ============================================

const NOTE_SORT_BY = {
    title: 'title:asc',
};

/**
 * Searches notes using Typesense
 * @param {string} query - The search query
 * @param {string} filter - Optional filter string
 * @param {number} page - Page number (default 1)
 * @param {number} perPage - Results per page (default 20)
 * @param {string} sortBy - Optional sort key (unmapped keys fall back to relevance)
 */
export const searchNotes = async (query, filter = null, page = 1, perPage = 20, sortBy = null) => {
    try {
        const params = { q: query, page, per_page: perPage };
        if (filter) params.filter = filter;

        const sortExpr = NOTE_SORT_BY[sortBy];
        if (sortExpr) {
            params.sort_by = sortExpr;
        }

        const response = await apiClient.get('/search/notes', { params });
        return response.data;
    } catch (error) {
        console.error('Error searching notes:', error);
        throw error;
    }
};

/**
 * Searches notes by vault
 * @param {string} vault - The vault name
 * @param {string} query - The search query
 * @param {number} page - Page number (default 1)
 * @param {number} perPage - Results per page (default 20)
 */
export const searchNotesByVault = async (vault, query, page = 1, perPage = 20) => {
    try {
        const params = { q: query, page, per_page: perPage };
        const response = await apiClient.get(`/search/notes/by-vault/${vault}`, { params });
        return response.data;
    } catch (error) {
        console.error('Error searching notes by vault:', error);
        throw error;
    }
};

/**
 * Multi-search across media items, mixlists, and notes
 * @param {string} query - The search query
 * @param {string} filter - Optional filter string
 * @param {number} page - Page number (default 1)
 * @param {number} perPage - Results per page (default 20)
 */
export const multiSearch = async (query, filter = null, page = 1, perPage = 20) => {
    try {
        const params = { q: query, page, per_page: perPage };
        if (filter) params.filter = filter;

        const response = await apiClient.get('/search/all', { params });
        return response.data;
    } catch (error) {
        console.error('Error performing multi-search:', error);
        throw error;
    }
};

/**
 * Reindexes all notes in Typesense
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
 * Resets the notes Typesense collection
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
