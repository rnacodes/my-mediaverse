import { apiClient } from './apiClient';

// ============================================
// Trakt API Methods
// ============================================

/**
 * Get Trakt connection status
 */
export const getTraktStatus = async () => {
    try {
        const response = await apiClient.get('/trakt/status');
        return response;
    } catch (error) {
        console.error('Error getting Trakt status:', error);
        throw error;
    }
};

/**
 * Start device auth flow - returns device code and verification URL
 */
export const startDeviceAuth = async () => {
    try {
        const response = await apiClient.post('/trakt/auth/device-code');
        return response;
    } catch (error) {
        console.error('Error starting Trakt device auth:', error);
        throw error;
    }
};

/**
 * Poll for device token after user has entered the code
 * @param {string} deviceCode - The device code from startDeviceAuth
 */
export const pollDeviceToken = async (deviceCode) => {
    try {
        const response = await apiClient.post('/trakt/auth/poll', { deviceCode });
        return response;
    } catch (error) {
        console.error('Error polling Trakt device token:', error);
        throw error;
    }
};

/**
 * Disconnect from Trakt
 */
export const disconnectTrakt = async () => {
    try {
        const response = await apiClient.post('/trakt/disconnect');
        return response;
    } catch (error) {
        console.error('Error disconnecting from Trakt:', error);
        throw error;
    }
};

/**
 * Sync watched movies and TV shows
 */
export const syncWatched = async () => {
    try {
        const response = await apiClient.post('/trakt/sync/watched');
        return response;
    } catch (error) {
        console.error('Error syncing Trakt watched:', error);
        throw error;
    }
};

/**
 * Sync watchlist items
 */
export const syncWatchlist = async () => {
    try {
        const response = await apiClient.post('/trakt/sync/watchlist');
        return response;
    } catch (error) {
        console.error('Error syncing Trakt watchlist:', error);
        throw error;
    }
};

/**
 * Sync ratings
 */
export const syncRatings = async () => {
    try {
        const response = await apiClient.post('/trakt/sync/ratings');
        return response;
    } catch (error) {
        console.error('Error syncing Trakt ratings:', error);
        throw error;
    }
};

/**
 * Sync all Trakt data (watched + watchlist + ratings)
 */
export const syncAll = async () => {
    try {
        const response = await apiClient.post('/trakt/sync/all');
        return response;
    } catch (error) {
        console.error('Error in full Trakt sync:', error);
        throw error;
    }
};
