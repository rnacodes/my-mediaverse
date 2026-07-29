import { useState, useEffect, useCallback, useRef } from 'react';
import { login as loginRequest, logout as logoutRequest, refresh as refreshRequest } from '../api/authService';
import { setAccessToken } from '../api/apiClient';
import { AuthContext } from './AuthContext';

const REFRESH_LEAD_MS = 2 * 60 * 1000;

// setTimeout stores its delay in a 32-bit signed int: anything larger overflows and
// fires almost immediately. 
const MAX_TIMEOUT_MS = 2 ** 31 - 1;

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [token, setToken] = useState(null);
    const [loading, setLoading] = useState(true);
    // True while a proactive renewal is in flight. Consumers treat this like `loading`:
    // the session is neither known-good nor known-dead, so redirecting would be wrong.
    const [refreshing, setRefreshing] = useState(false);
    const refreshTimerRef = useRef(null);
    // Bumped when the current token reaches its hard expiry.
    const [expiryTick, setExpiryTick] = useState(0);
    const expiryTimerRef = useRef(null);

    const applySession = useCallback(({ token: newToken, username, expiresAt }) => {
        setToken(newToken);
        setAccessToken(newToken);
        setUser({ username, expiresAt });
    }, []);

    const clearSession = useCallback(() => {
        setToken(null);
        setAccessToken(null);
        setUser(null);
    }, []);

    const refreshToken = useCallback(async () => {
        setRefreshing(true);
        try {
            const data = await refreshRequest();
            applySession(data);
            return data.token;
        } catch (error) {
            clearSession();
            throw error;
        } finally {
            setRefreshing(false);
        }
    }, [applySession, clearSession]);

    useEffect(() => {
        const initializeAuth = async () => {
            try {
                applySession(await refreshRequest());
            } catch {
                // No refresh cookie, or it has expired — the visitor is simply not signed in.
                console.log('No valid session found');
            } finally {
                setLoading(false);
            }
        };

        initializeAuth();
    }, [applySession]);

    // Renew shortly before the current token expires
    useEffect(() => {
        const clearTimers = () => {
            if (refreshTimerRef.current) {
                clearTimeout(refreshTimerRef.current);
                refreshTimerRef.current = null;
            }
            if (expiryTimerRef.current) {
                clearTimeout(expiryTimerRef.current);
                expiryTimerRef.current = null;
            }
        };

        clearTimers();

        if (!token || !user?.expiresAt) return;

        const expiresAt = new Date(user.expiresAt).getTime();
        if (Number.isNaN(expiresAt)) return;

        if (expiresAt <= Date.now()) return;

        const reArm = () => setExpiryTick(tick => tick + 1);

        const schedule = (delayMs, onDue) => {
            if (delayMs > MAX_TIMEOUT_MS) {
                return setTimeout(reArm, MAX_TIMEOUT_MS);
            }
            return setTimeout(onDue, Math.max(0, delayMs));
        };

        refreshTimerRef.current = schedule(expiresAt - Date.now() - REFRESH_LEAD_MS, () => {
            // Rejection is already handled by refreshToken (it clears the session).
            refreshToken().catch(() => {});
        });

        expiryTimerRef.current = schedule(expiresAt - Date.now(), reArm);

        return clearTimers;
    }, [token, user?.expiresAt, refreshToken, expiryTick]);

    const login = async (username, password) => {
        try {
            applySession(await loginRequest(username, password));
            return { success: true };
        } catch (error) {
            const errorMessage = error.response?.data?.message || 'Login failed. Please check your credentials.';
            return { success: false, error: errorMessage };
        }
    };

    const logout = async () => {
        try {
            await logoutRequest();
        } catch (error) {
            console.error('Logout error:', error);
        } finally {
            clearSession();
        }
    };

    const isAuthenticated = () => {
        if (!token || !user) return false;

        if (user.expiresAt && new Date(user.expiresAt).getTime() <= Date.now()) {
            return false;
        }

        return true;
    };

    const value = {
        user,
        token,
        loading,
        refreshing,
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
