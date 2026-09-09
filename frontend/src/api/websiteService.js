import { apiClient } from './apiClient';

// ============================================
// Website API calls
// ============================================

/**
 * Scrapes a website URL for preview without saving
 * @param {string} url - The URL to scrape
 */
export const scrapeWebsitePreview = async (url) => {
    try {
        const response = await apiClient.post('/website/scrape-preview', { url });
        return response.data;
    } catch (error) {
        console.error('Error scraping website:', error);
        throw error;
    }
};

/**
 * Imports a website from a URL. Returns the existing website (HTTP 200) when the
 * URL is already in the library, or the newly created one (HTTP 201).
 * @param {object} websiteData - { url, titleOverride?, notes?, topics?, genres? }
 */
export const importWebsite = async (websiteData) => {
    try {
        const response = await apiClient.post('/website/from-url', websiteData);
        return response.data;
    } catch (error) {
        console.error('Error importing website:', error);
        throw error;
    }
};

/**
 * Gets all websites
 */
export const getAllWebsites = async () => {
    try {
        const response = await apiClient.get('/website');
        return response.data;
    } catch (error) {
        console.error('Error fetching websites:', error);
        throw error;
    }
};

/**
 * Gets a website by ID
 * @param {string} id - The website ID
 */
export const getWebsiteById = async (id) => {
    try {
        const response = await apiClient.get(`/website/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching website:', error);
        throw error;
    }
};

/**
 * Gets websites by domain
 * @param {string} domain - The domain name
 */
export const getWebsitesByDomain = async (domain) => {
    try {
        const response = await apiClient.get(`/website/by-domain/${encodeURIComponent(domain)}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching websites by domain:', error);
        throw error;
    }
};

/**
 * Gets websites with RSS feeds
 */
export const getWebsitesWithRss = async () => {
    try {
        const response = await apiClient.get('/website/with-rss');
        return response.data;
    } catch (error) {
        console.error('Error fetching websites with RSS:', error);
        throw error;
    }
};

/**
 * Creates a website manually
 * @param {object} websiteData - The website data
 */
export const createWebsite = async (websiteData) => {
    try {
        const response = await apiClient.post('/website', websiteData);
        return response.data;
    } catch (error) {
        console.error('Error creating website:', error);
        throw error;
    }
};

/**
 * Updates a website
 * @param {string} id - The website ID
 * @param {object} websiteData - The updated website data
 */
export const updateWebsite = async (id, websiteData) => {
    try {
        const response = await apiClient.put(`/website/${id}`, websiteData);
        return response.data;
    } catch (error) {
        console.error('Error updating website:', error);
        throw error;
    }
};

/**
 * Deletes a website
 * @param {string} id - The website ID
 */
export const deleteWebsite = async (id) => {
    try {
        await apiClient.delete(`/website/${id}`);
    } catch (error) {
        console.error('Error deleting website:', error);
        throw error;
    }
};

// ============================================
// Bulk import (bookmark file, URL list) and export
// ============================================

// Multipart posts must name the content type (see MULTIPART below): with the client's default
// application/json header, axios would serialize the FormData to JSON. In browsers axios then
// replaces the value with the boundary-carrying one, the same way uploadService.js posts.
const MULTIPART = { headers: { 'Content-Type': 'multipart/form-data' } };

const bookmarkFileForm = (file, options = {}) => {
    const form = new FormData();
    form.append('file', file);
    Object.entries(options).forEach(([key, value]) => {
        if (value === undefined || value === null) return;
        if (Array.isArray(value)) value.forEach((v) => form.append(key, v));
        else form.append(key, value);
    });
    return form;
};

/**
 * Counts what a bookmark export (browser or bookmark-manager HTML) would import, without saving
 * @param {File} file - The exported .html file
 */
export const previewBookmarkFile = async (file) => {
    try {
        const response = await apiClient.post('/website/from-bookmark-file/preview', bookmarkFileForm(file), MULTIPART);
        return response.data;
    } catch (error) {
        console.error('Error previewing bookmark file:', error);
        throw error;
    }
};

/**
 * Imports a bookmark export as website stubs (enrich afterwards)
 * @param {File} file - The exported .html file
 * @param {object} [options] - { foldersAsTopics, tagsAsTopics, defaultStatus, extraTopics[], extraGenres[] }
 */
export const importBookmarkFile = async (file, options = {}) => {
    try {
        const response = await apiClient.post('/website/from-bookmark-file', bookmarkFileForm(file, options), MULTIPART);
        return response.data;
    } catch (error) {
        console.error('Error importing bookmark file:', error);
        throw error;
    }
};

/**
 * Counts what a pasted list of URLs would import, without saving
 * @param {string} urls - Pasted text, one URL per line (separators tolerated)
 */
export const previewUrlList = async (urls) => {
    try {
        const response = await apiClient.post('/website/from-url-list/preview', { urls });
        return response.data;
    } catch (error) {
        console.error('Error previewing URL list:', error);
        throw error;
    }
};

/**
 * Imports a pasted list of URLs as website stubs (enrich afterwards)
 * @param {string} urls - Pasted text
 * @param {object} [options] - Same shape as importBookmarkFile options
 */
export const importUrlList = async (urls, options = {}) => {
    try {
        const response = await apiClient.post('/website/from-url-list', { urls, options });
        return response.data;
    } catch (error) {
        console.error('Error importing URL list:', error);
        throw error;
    }
};

/**
 * Downloads the library as a Netscape bookmark file (topics as TAGS)
 * @returns {{ blob: Blob, fileName: string }}
 */
export const exportBookmarks = async () => {
    try {
        const response = await apiClient.get('/website/export', { responseType: 'blob' });
        const disposition = response.headers?.['content-disposition'] ?? '';
        const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
        // The header is only readable when the API exposes it; otherwise use the same dated name.
        const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        return { blob: response.data, fileName: match ? decodeURIComponent(match[1]) : `mymediaverse-bookmarks-${stamp}.html` };
    } catch (error) {
        console.error('Error exporting bookmarks:', error);
        throw error;
    }
};

// ============================================
// Enrichment (metadata fill, screenshots, link health)
// ============================================

/**
 * Gets the enrichment backlog and the screenshot budget left this month
 * @returns {{ pendingCount: number, screenshotQuotaRemaining: number }}
 */
export const getWebsiteEnrichmentStatus = async () => {
    try {
        const response = await apiClient.get('/website/enrichment/status');
        return response.data;
    } catch (error) {
        console.error('Error fetching website enrichment status:', error);
        throw error;
    }
};

/**
 * Enriches one page of pending websites (oldest first). Call repeatedly until
 * the returned pendingCount is 0.
 * @param {number} [limit] - Websites to process in this call (server default 50, max 200)
 */
export const runWebsiteEnrichment = async (limit) => {
    try {
        const response = await apiClient.post('/website/enrichment/run', null, {
            params: limit ? { limit } : undefined,
        });
        return response.data;
    } catch (error) {
        console.error('Error running website enrichment:', error);
        throw error;
    }
};

/**
 * Re-checks stored links that have never been checked or are stale
 * @param {object} [options] - { limit?, olderThanDays? }
 */
export const checkWebsiteLinks = async (options = {}) => {
    try {
        const response = await apiClient.post('/website/enrichment/check-links', null, {
            params: options,
        });
        return response.data;
    } catch (error) {
        console.error('Error checking website links:', error);
        throw error;
    }
};

/**
 * Re-renders thumbnails that point at the screenshot provider or an animated placeholder
 * @param {number} [limit] - Thumbnails to repair in this call
 */
export const repairWebsiteThumbnails = async (limit) => {
    try {
        const response = await apiClient.post('/website/enrichment/repair-thumbnails', null, {
            params: limit ? { limit } : undefined,
        });
        return response.data;
    } catch (error) {
        console.error('Error repairing website thumbnails:', error);
        throw error;
    }
};

/**
 * Enriches a single website. Fill-only; pass force to re-run on an already enriched row.
 * @param {string} id - The website ID
 * @param {boolean} [force=false]
 */
export const enrichWebsite = async (id, force = false) => {
    try {
        const response = await apiClient.post(`/website/${id}/enrich`, null, {
            params: force ? { force: true } : undefined,
        });
        return response.data;
    } catch (error) {
        console.error('Error enriching website:', error);
        throw error;
    }
};

/**
 * Renders a fresh screenshot for a website. Skipped when it already has a thumbnail
 * unless force is set, which replaces the current thumbnail.
 * @param {string} id - The website ID
 * @param {boolean} [force=false]
 */
export const regenerateWebsiteScreenshot = async (id, force = false) => {
    try {
        const response = await apiClient.post(`/website/${id}/screenshot`, null, {
            params: force ? { force: true } : undefined,
        });
        return response.data;
    } catch (error) {
        console.error('Error regenerating website screenshot:', error);
        throw error;
    }
};

/**
 * Gets the latest RSS feed items for a website
 * @param {string} id - The website ID
 * @param {number} maxItems - Maximum number of items to return (default 3)
 * @returns {Array} - Array of RSS feed items
 */
export const getWebsiteRssFeedItems = async (id, maxItems = 3) => {
    try {
        const response = await apiClient.get(`/website/${id}/rss-items?maxItems=${maxItems}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching RSS feed items:', error);
        throw error;
    }
};
