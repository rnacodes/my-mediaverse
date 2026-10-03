import { apiClient } from './apiClient';

// ============================================
// Document API calls
// ============================================

/**
 * Gets all documents.
 */
export const getAllDocuments = async () => {
    try {
        const response = await apiClient.get('/document');
        return response.data;
    } catch (error) {
        console.error('Error getting all documents:', error);
        throw error;
    }
};

/**
 * Gets a single document by ID.
 * @param {string} id - Document ID
 */
export const getDocumentById = async (id) => {
    try {
        const response = await apiClient.get(`/document/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error getting document:', error);
        throw error;
    }
};

/**
 * Creates a new document.
 * @param {object} documentData - Document data
 */
export const createDocument = async (documentData) => {
    try {
        const response = await apiClient.post('/document', documentData);
        return response.data;
    } catch (error) {
        console.error('Error creating document:', error);
        throw error;
    }
};

/**
 * Updates an existing document.
 * @param {string} id - Document ID
 * @param {object} documentData - Updated document data
 */
export const updateDocument = async (id, documentData) => {
    try {
        const response = await apiClient.put(`/document/${id}`, documentData);
        return response.data;
    } catch (error) {
        console.error('Error updating document:', error);
        throw error;
    }
};

// ============================================
// Document Query API calls
// ============================================

/**
 * Gets documents by document type.
 * @param {string} documentType - Document type (e.g., "Invoice", "Receipt")
 */
export const getDocumentsByType = async (documentType) => {
    try {
        const response = await apiClient.get(`/document/by-type/${encodeURIComponent(documentType)}`);
        return response.data;
    } catch (error) {
        console.error('Error getting documents by type:', error);
        throw error;
    }
};

/**
 * Gets documents by correspondent.
 * @param {string} correspondent - Correspondent name
 */
export const getDocumentsByCorrespondent = async (correspondent) => {
    try {
        const response = await apiClient.get(`/document/by-correspondent/${encodeURIComponent(correspondent)}`);
        return response.data;
    } catch (error) {
        console.error('Error getting documents by correspondent:', error);
        throw error;
    }
};

/**
 * Gets archived documents.
 */
export const getArchivedDocuments = async () => {
    try {
        const response = await apiClient.get('/document/archived');
        return response.data;
    } catch (error) {
        console.error('Error getting archived documents:', error);
        throw error;
    }
};

/**
 * Searches documents by title, correspondent, type, or description.
 * @param {string} query - Search query
 */
export const searchDocuments = async (query) => {
    try {
        const response = await apiClient.get('/document/search', {
            params: { query }
        });
        return response.data;
    } catch (error) {
        console.error('Error searching documents:', error);
        throw error;
    }
};

/**
 * Gets documents within a date range.
 * @param {Date|string} startDate - Start date
 * @param {Date|string} endDate - End date
 */
export const getDocumentsByDateRange = async (startDate, endDate) => {
    try {
        const response = await apiClient.get('/document/by-date-range', {
            params: {
                startDate: new Date(startDate).toISOString(),
                endDate: new Date(endDate).toISOString()
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error getting documents by date range:', error);
        throw error;
    }
};

// ============================================
// Paperless-ngx Sync API calls
// ============================================

/**
 * Synchronizes all documents from Paperless-ngx.
 * Creates new documents and updates existing ones.
 */
export const syncDocumentsFromPaperless = async () => {
    try {
        const response = await apiClient.post('/document/sync-paperless');
        return response.data;
    } catch (error) {
        console.error('Error syncing from Paperless:', error);
        throw error;
    }
};

/**
 * Synchronizes a single document from Paperless-ngx by its Paperless ID.
 * @param {number} paperlessId - Paperless document ID
 */
export const syncSingleDocumentFromPaperless = async (paperlessId) => {
    try {
        const response = await apiClient.post(`/document/sync-paperless/${paperlessId}`);
        return response.data;
    } catch (error) {
        console.error('Error syncing single document from Paperless:', error);
        throw error;
    }
};

/**
 * Checks if Paperless-ngx API is available and configured.
 */
export const getPaperlessStatus = async () => {
    try {
        const response = await apiClient.get('/document/paperless-status');
        return response.data;
    } catch (error) {
        console.error('Error checking Paperless status:', error);
        throw error;
    }
};
