import axios from 'axios';

// Use environment variable or fall back to localhost for development
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';

export const DEMO_READ_ONLY_CODE = 'demo_read_only';

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
            const isAuthEndpoint = originalRequest.url?.includes('/auth/login') ||
                                  originalRequest.url?.includes('/auth/refresh') ||
                                  originalRequest.url?.includes('/auth/logout');

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

                // Hand off to the app rather than assigning window.location: a hard
                // navigation reloads the whole SPA, discards the route the user was on,
                // and gives no explanation. The listener routes within the router and
                // preserves the destination for redirect after sign-in.
                const currentPath = window.location.pathname;
                if (currentPath !== '/login') {
                    window.dispatchEvent(new CustomEvent('sessionExpired', {
                        detail: { path: error.config?.url }
                    }));
                }

                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        if (error.response?.status === 403) {
            const errorData = error.response?.data;

            // The demo write gate is identified by a stable code rather than by its
            // message text. The legacy message match is kept until the API emits the
            // code, so the read-only dialog keeps working in the meantime.
            const isDemoReadOnly = errorData?.code === DEMO_READ_ONLY_CODE ||
                errorData?.error === 'Write operations are disabled in demo mode';

            if (isDemoReadOnly) {
                window.dispatchEvent(new CustomEvent('demoWriteBlocked', {
                    detail: {
                        blockedOperation: errorData.blockedOperation,
                        path: error.config?.url,
                        message: errorData.message
                    }
                }));
            } else {
                // Any other 403 previously surfaced nothing at all — the user clicked a
                // button and no error appeared anywhere.
                window.dispatchEvent(new CustomEvent('apiForbidden', {
                    detail: {
                        path: error.config?.url,
                        message: errorData?.message || errorData?.error ||
                            'You do not have permission to perform this action.'
                    }
                }));
            }
        }

        return Promise.reject(error);
    }
);

export { API_URL };
