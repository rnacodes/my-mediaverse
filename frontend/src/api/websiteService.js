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
