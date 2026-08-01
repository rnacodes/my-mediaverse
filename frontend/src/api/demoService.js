import { apiClient } from './apiClient';

/**
 * Demo write-access endpoints. These are same-origin calls through the shared api
 * client, so the unlock response body (including the access token) is readable and
 * the write-window cookie is set on the API the app actually talks to.
 */

/** Read the current write-access status. */
export const getDemoStatus = async () => {
    const response = await apiClient.get('/demo/status');
    return response.data;
};

/**
 * Exchange a 6-digit TOTP code for a 20-minute write window.
 * Returns { token, username, expiresAt, expiresInMinutes, message }.
 */
export const unlockDemo = async (code) => {
    const response = await apiClient.get('/demo/unlock', { params: { code } });
    return response.data;
};

/** Revoke the write window early by clearing the cookie. */
export const lockDemo = async () => {
    const response = await apiClient.post('/demo/lock');
    return response.data;
};
