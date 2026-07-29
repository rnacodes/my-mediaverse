import { apiClient } from './apiClient';

/**
 * Login with username and password
 * @param {string} username - The username
 * @param {string} password - The password
 * @returns {Promise} Response with token
 */
export const login = async (username, password) => {
    try {
        const response = await apiClient.post('/auth/login', { username, password });
        return response.data;
    } catch (error) {
        console.error('Login error:', error);
        throw error;
    }
};

/**
 * Exchange the HttpOnly refresh cookie for a new access token.
 * @returns {Promise} Response with token, username and expiresAt
 */
export const refresh = async () => {
    const response = await apiClient.post('/auth/refresh');
    return response.data;
};

/**
 * Validate the current token
 * @returns {Promise} Token validation result
 */
export const validateToken = async () => {
    try {
        const response = await apiClient.get('/auth/validate');
        return response.data;
    } catch (error) {
        console.error('Token validation error:', error);
        throw error;
    }
};

/**
 * Logout (server-side notification)
 * @returns {Promise} Logout confirmation
 */
export const logout = async () => {
    try {
        const response = await apiClient.post('/auth/logout');
        return response.data;
    } catch (error) {
        console.error('Logout error:', error);
        throw error;
    }
};

/**
 * Cleanup expired refresh tokens
 * @returns {Promise} Cleanup result
 */
export const cleanupRefreshTokens = async () => {
    try {
        const response = await apiClient.post('/auth/cleanup-tokens');
        return response.data;
    } catch (error) {
        console.error('Error cleaning up refresh tokens:', error);
        throw error;
    }
};
