import { describe, it, expect, afterEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { setAccessToken } from '../api/apiClient';
import { AuthProvider } from './AuthProvider';
import { useAuth } from './AuthContext';

// AuthProvider talks to /auth/* over real axios; MSW (default handlers in
// src/test/mocks/handlers.js) answers at the HTTP boundary. The default
// /auth/refresh handler returns a valid session, so tests that want the
// logged-out path override it with a 401 via server.use(...).

const noSessionRefresh = () =>
  server.use(
    http.post(`${API_BASE}/auth/refresh`, () => new HttpResponse(null, { status: 401 })),
  );

const TestConsumer = () => {
  const { user, token, loading, login, logout, isAuthenticated } = useAuth();

  return (
    <div>
      <div data-testid="loading">{loading ? 'loading' : 'ready'}</div>
      <div data-testid="authenticated">
        {isAuthenticated ? 'authenticated' : 'not-authenticated'}
      </div>
      <div data-testid="user">{user ? user.username : 'no-user'}</div>
      <div data-testid="token">{token || 'no-token'}</div>
      <button data-testid="login-btn" onClick={() => login('testuser', 'testpass')}>
        Login
      </button>
      <button data-testid="logout-btn" onClick={logout}>
        Logout
      </button>
    </div>
  );
};

const renderProvider = () =>
  render(
    <AuthProvider>
      <TestConsumer />
    </AuthProvider>,
  );

afterEach(() => {
  // AuthProvider stashes the access token in the apiClient module singleton;
  // reset it so tokens don't leak between tests.
  setAccessToken(null);
});

describe('AuthContext', () => {
  describe('Initial State', () => {
    it('shows loading state before the refresh check resolves', async () => {
      renderProvider();

      // loading starts true synchronously, before the mount refresh settles.
      expect(screen.getByTestId('loading')).toHaveTextContent('loading');

      // settle the pending refresh so the test doesn't leak act() warnings.
      await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('ready'));
    });

    it('completes loading after the refresh token check', async () => {
      noSessionRefresh();
      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });
    });

    it('is not authenticated when no session exists', async () => {
      noSessionRefresh();
      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
      expect(screen.getByTestId('user')).toHaveTextContent('no-user');
      expect(screen.getByTestId('token')).toHaveTextContent('no-token');
    });

    it('restores the session from a valid refresh token', async () => {
      // Default /auth/refresh handler returns a valid session.
      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
      });
      expect(screen.getByTestId('user')).toHaveTextContent('testuser');
      expect(screen.getByTestId('token')).toHaveTextContent('test-access-token');
    });
  });

  describe('Login', () => {
    it('logs in successfully with valid credentials', async () => {
      const user = userEvent.setup();
      noSessionRefresh(); // start logged out

      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');

      // Default /auth/login handler returns a valid session.
      await user.click(screen.getByTestId('login-btn'));

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
      });
      expect(screen.getByTestId('user')).toHaveTextContent('testuser');
      expect(screen.getByTestId('token')).toHaveTextContent('test-access-token');
    });

    it('stays unauthenticated when login fails', async () => {
      const user = userEvent.setup();
      // AuthProvider.login() logs the rejected request via console.error on the
      // failure path we're deliberately exercising; silence that expected noise.
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      noSessionRefresh();
      server.use(
        http.post(`${API_BASE}/auth/login`, () =>
          HttpResponse.json({ message: 'Invalid credentials' }, { status: 401 }),
        ),
      );

      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });

      await user.click(screen.getByTestId('login-btn'));

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
      });
      expect(screen.getByTestId('user')).toHaveTextContent('no-user');
      expect(screen.getByTestId('token')).toHaveTextContent('no-token');

      consoleSpy.mockRestore();
    });
  });

  describe('Logout', () => {
    it('clears auth state on logout', async () => {
      const user = userEvent.setup();
      // Default /auth/refresh restores a session; default /auth/logout returns 204.
      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
      });

      await user.click(screen.getByTestId('logout-btn'));

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
      });
      expect(screen.getByTestId('user')).toHaveTextContent('no-user');
      expect(screen.getByTestId('token')).toHaveTextContent('no-token');
    });
  });

  describe('Token expiry', () => {
    it('is not authenticated once the token has expired', async () => {
      server.use(
        http.post(`${API_BASE}/auth/refresh`, () =>
          HttpResponse.json({
            token: 'expired-access-token',
            username: 'testuser',
            expiresAt: '2020-01-01T00:00:00Z',
          }),
        ),
      );

      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
      // The token itself is still held — it is the expiry that makes it unusable.
      expect(screen.getByTestId('token')).toHaveTextContent('expired-access-token');
    });

    it('does not repeatedly renew an already-expired token', async () => {
      // A renewal that keeps returning an expired token must not re-arm forever.
      let refreshCalls = 0;
      server.use(
        http.post(`${API_BASE}/auth/refresh`, () => {
          refreshCalls += 1;
          return HttpResponse.json({
            token: 'expired-access-token',
            username: 'testuser',
            expiresAt: '2020-01-01T00:00:00Z',
          });
        }),
      );

      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });

      // Only the mount refresh should have run.
      expect(refreshCalls).toBe(1);
    });

    it('renews proactively when the token is close to expiring', async () => {
      // First response expires inside the renewal lead window, so a renewal is due
      // immediately; the second hands back a long-lived token.
      let refreshCalls = 0;
      server.use(
        http.post(`${API_BASE}/auth/refresh`, () => {
          refreshCalls += 1;
          return HttpResponse.json({
            token: refreshCalls === 1 ? 'near-expiry-token' : 'renewed-token',
            username: 'testuser',
            expiresAt:
              refreshCalls === 1
                ? new Date(Date.now() + 30 * 1000).toISOString()
                : '2099-01-01T00:00:00Z',
          });
        }),
      );

      renderProvider();

      await waitFor(() => {
        expect(screen.getByTestId('token')).toHaveTextContent('renewed-token');
      });
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
      expect(refreshCalls).toBe(2);
    });
  });

  describe('useAuth hook', () => {
    it('throws when used outside an AuthProvider', () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      const TestOutsideProvider = () => {
        useAuth();
        return <div>Should not render</div>;
      };

      expect(() => render(<TestOutsideProvider />)).toThrow(
        'useAuth must be used within an AuthProvider',
      );

      consoleSpy.mockRestore();
    });
  });
});
