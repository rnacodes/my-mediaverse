import { useState, useEffect } from 'react';
import axios from 'axios';
import { setAccessToken } from '../api/apiClient';
import { AuthContext } from './AuthContext';

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [token, setToken] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const initializeAuth = async () => {
            try {
                const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
                const response = await axios.post(`${API_URL}/auth/refresh`, {}, {
                    withCredentials: true
                });

                const { token: newToken, username, expiresAt } = response.data;
                setToken(newToken);
                setAccessToken(newToken);
                setUser({ username, expiresAt });
            } catch {
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
                withCredentials: true
            });

            const { token: newToken, username: userName, expiresAt } = response.data;

            setToken(newToken);
            setAccessToken(newToken);
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

            await axios.post(`${API_URL}/auth/logout`, {}, {
                withCredentials: true,
                headers: token ? { Authorization: `Bearer ${token}` } : {}
            });
        } catch (error) {
            console.error('Logout error:', error);
        } finally {
            setToken(null);
            setAccessToken(null);
            setUser(null);
        }
    };

    const refreshToken = async () => {
        try {
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            const response = await axios.post(`${API_URL}/auth/refresh`, {}, {
                withCredentials: true
            });

            const { token: newToken, username, expiresAt } = response.data;
            setToken(newToken);
            setAccessToken(newToken);
            setUser({ username, expiresAt });

            return newToken;
        } catch (error) {
            console.error('Token refresh failed:', error);
            setToken(null);
            setAccessToken(null);
            setUser(null);
            throw error;
        }
    };

    const isAuthenticated = () => {
        if (!token || !user) return false;

        if (user.expiresAt) {
            const expiryDate = new Date(user.expiresAt);
            const bufferTime = 60 * 1000;
            if (expiryDate.getTime() - bufferTime <= new Date().getTime()) {
                return !!token;
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
