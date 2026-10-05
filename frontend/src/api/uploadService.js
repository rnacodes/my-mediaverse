import { apiClient } from './apiClient';

// ============================================
// Upload API calls
// ============================================

export const uploadCsv = (file, mediaType = null) => {
    const formData = new FormData();
    formData.append('file', file);

    if (mediaType) {
        formData.append('mediaType', mediaType);
    }

    return apiClient.post('/upload/csv', formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
};

export const uploadThumbnail = (file) => {
    const formData = new FormData();
    formData.append('file', file);

    return apiClient.post('/upload/thumbnail', formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
};

export const uploadThumbnailFromUrl = (url) => {
    return apiClient.post('/upload/thumbnail-from-url', { url });
};

/**
 * Upload a Goodreads CSV export file
 * @param {File} file - The CSV file to upload
 * @param {boolean} updateExisting - Whether to update existing books on match (default: true)
 * @param {number|null} chunkIndex - Optional chunk index for chunked uploads
 * @param {number|null} totalChunks - Optional total chunks for chunked uploads
 * @returns {Promise} API response with import results
 */
export const uploadGoodreadsCsv = (file, updateExisting = true, chunkIndex = null, totalChunks = null) => {
    const formData = new FormData();
    formData.append('file', file);

    let url = `/upload/goodreads-csv?updateExisting=${updateExisting}`;
    if (chunkIndex !== null && totalChunks !== null) {
        url += `&chunkIndex=${chunkIndex}&totalChunks=${totalChunks}`;
    }

    return apiClient.post(url, formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
    });
};
