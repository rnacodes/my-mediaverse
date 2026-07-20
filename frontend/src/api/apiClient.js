import axios from 'axios';
import { isDemoMode } from '@/utils/demoMode';

// Use environment variable or fall back to localhost for development
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';

export const apiClient = axios.create({
    baseURL: API_URL,
    timeout: 30000,
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true, // Always send cookies with requests
});

// Store the current access token in memory (not localStorage)
let currentAccessToken = null;

// Function to set the access token (called by AuthContext)
export const setAccessToken = (token) => {
    currentAccessToken = token;
};

// Function to get the access token
export const getAccessToken = () => {
    return currentAccessToken;
};

// Flag to prevent multiple simultaneous refresh attempts
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });

    failedQueue = [];
};

// Request Interceptor - Attach JWT token and demo admin key to all requests
apiClient.interceptors.request.use(
    (config) => {
        // Get token from memory (not localStorage)
        const token = currentAccessToken;

        if (token) {
            // Attach the token as a Bearer token
            config.headers['Authorization'] = `Bearer ${token}`;
        }

        // Check for demo admin key in sessionStorage
        const demoAdminKey = sessionStorage.getItem('demoAdminKey');
        if (demoAdminKey) {
            config.headers['X-Demo-Admin-Key'] = demoAdminKey;
        }

        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response Interceptor - Handle token expiration with automatic refresh
apiClient.interceptors.response.use(
    (response) => {
        return response;
    },
    async (error) => {
        const originalRequest = error.config;

        // If the error is 401 and we haven't already tried to refresh
        if (error.response?.status === 401 && !originalRequest._retry) {
            // In demo mode, don't try to refresh or redirect - just reject the error
            if (isDemoMode()) {
                console.log('Demo mode: Skipping authentication for 401 error');
                return Promise.reject(error);
            }

            // Don't try to refresh on login or refresh endpoints
            const isAuthEndpoint = originalRequest.url?.includes('/auth/login') ||
                                  originalRequest.url?.includes('/auth/refresh');

            if (isAuthEndpoint) {
                return Promise.reject(error);
            }

            // If already refreshing, queue this request
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                .then(token => {
                    originalRequest.headers['Authorization'] = `Bearer ${token}`;
                    return apiClient(originalRequest);
                })
                .catch(err => {
                    return Promise.reject(err);
                });
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                // Attempt to refresh the access token
                const response = await axios.post(`${API_URL}/auth/refresh`, {}, {
                    withCredentials: true // Send HttpOnly cookie with refresh token
                });

                const { token: newToken } = response.data;
                currentAccessToken = newToken;

                // Update the authorization header
                originalRequest.headers['Authorization'] = `Bearer ${newToken}`;

                // Process any queued requests
                processQueue(null, newToken);

                // Retry the original request
                return apiClient(originalRequest);
            } catch (refreshError) {
                // Refresh failed - user needs to login again
                processQueue(refreshError, null);
                currentAccessToken = null;

                // Only redirect to login if we're not already there
                const currentPath = window.location.pathname;
                if (currentPath !== '/login') {
                    console.warn('Session expired. Please login again.');
                    window.location.href = '/login';
                }

                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        // Check for demo mode 403 error and dispatch custom event
        if (error.response?.status === 403) {
            const errorData = error.response?.data;
            if (errorData?.error === 'Write operations are disabled in demo mode') {
                window.dispatchEvent(new CustomEvent('demoWriteBlocked', {
                    detail: {
                        blockedOperation: errorData.blockedOperation,
                        path: error.config?.url,
                        message: errorData.message
                    }
                }));
            }
        }

        return Promise.reject(error);
    }
);

export { API_URL };
