import { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';
import { setAccessToken } from '../api/apiClient';

const AuthContext = createContext(null);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [token, setToken] = useState(null);
    const [loading, setLoading] = useState(true);

    // Initialize auth state - NO LONGER using localStorage for tokens
    // Tokens are now managed in memory and via HttpOnly cookies
    useEffect(() => {
        // Try to refresh the token on mount to check if user has valid session
        const initializeAuth = async () => {
            try {
                const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
                const response = await axios.post(`${API_URL}/auth/refresh`, {}, {
                    withCredentials: true // Important: send cookies
                });
                
                const { token: newToken, username, expiresAt } = response.data;
                setToken(newToken);
                setAccessToken(newToken); // Update token in apiService
                setUser({ username, expiresAt });
            } catch {
                // No valid refresh token, user needs to login
                console.log('No valid session found');
            } finally {
                setLoading(false);
            }
        };

        initializeAuth();
    }, []);

    const login = async (username, password) => {
        try {
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            const response = await axios.post(`${API_URL}/auth/login`, {
                username,
                password
            }, {
                withCredentials: true // Important: allows server to set HttpOnly cookie
            });

            const { token: newToken, username: userName, expiresAt } = response.data;
            
            // Store ONLY the access token in memory (not localStorage)
            // Refresh token is in HttpOnly cookie, inaccessible to JavaScript
            setToken(newToken);
            setAccessToken(newToken); // Update token in apiService
            setUser({ username: userName, expiresAt });
            
            return { success: true };
        } catch (error) {
            console.error('Login failed:', error);
            const errorMessage = error.response?.data?.message || 'Login failed. Please check your credentials.';
            return { success: false, error: errorMessage };
        }
    };

    const logout = async () => {
        try {
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            
            // Call logout endpoint to revoke refresh tokens
            await axios.post(`${API_URL}/auth/logout`, {}, {
                withCredentials: true,
                headers: token ? { Authorization: `Bearer ${token}` } : {}
            });
        } catch (error) {
            console.error('Logout error:', error);
            // Continue with local cleanup even if server call fails
        } finally {
            // Clear local state
            setToken(null);
            setAccessToken(null); // Clear token in apiService
            setUser(null);
        }
    };

    const refreshToken = async () => {
        try {
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            const response = await axios.post(`${API_URL}/auth/refresh`, {}, {
                withCredentials: true // Send HttpOnly cookie
            });
            
            const { token: newToken, username, expiresAt } = response.data;
            setToken(newToken);
            setAccessToken(newToken); // Update token in apiService
            setUser({ username, expiresAt });
            
            return newToken;
        } catch (error) {
            console.error('Token refresh failed:', error);
            // If refresh fails, logout the user
            setToken(null);
            setAccessToken(null); // Clear token in apiService
            setUser(null);
            throw error;
        }
    };

    const isAuthenticated = () => {
        if (!token || !user) return false;
        
        // Check if token is expired
        if (user.expiresAt) {
            const expiryDate = new Date(user.expiresAt);
            // Add a small buffer (1 minute) before expiration
            const bufferTime = 60 * 1000; // 1 minute in milliseconds
            if (expiryDate.getTime() - bufferTime <= new Date().getTime()) {
                // Token is about to expire, will be refreshed by interceptor
                return !!token; // Still return true if we have a token
            }
        }
        
        return true;
    };

    const value = {
        user,
        token,
        loading,
        login,
        logout,
        refreshToken,
        isAuthenticated: isAuthenticated()
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};
