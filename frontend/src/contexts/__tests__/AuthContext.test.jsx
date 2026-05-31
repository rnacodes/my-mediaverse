import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

vi.mock('axios', () => {
  const mockAxiosInstance = {
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() }
    },
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  };

  return {
    default: {
      create: vi.fn(() => mockAxiosInstance),
      post: vi.fn(),
      get: vi.fn()
    }
  };
});

import axios from 'axios';

vi.mock('../../api/apiClient', () => ({
  setAccessToken: vi.fn(),
  getAccessToken: vi.fn()
}));

import * as apiClient from '../../api/apiClient';
import { AuthProvider } from '../AuthProvider';
import { useAuth } from '../AuthContext';

const TestConsumer = () => {
  const { user, token, loading, login, logout, isAuthenticated } = useAuth();

  return (
    <div>
      <div data-testid="loading">{loading ? 'loading' : 'ready'}</div>
      <div data-testid="authenticated">{isAuthenticated ? 'authenticated' : 'not-authenticated'}</div>
      <div data-testid="user">{user ? user.username : 'no-user'}</div>
      <div data-testid="token">{token || 'no-token'}</div>
      <button
        data-testid="login-btn"
        onClick={async () => {
          const result = await login('testuser', 'testpass');
          return result;
        }}
      >
        Login
      </button>
      <button data-testid="logout-btn" onClick={logout}>
        Logout
      </button>
    </div>
  );
};

describe('AuthContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    axios.post.mockRejectedValue(new Error('No session'));
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Initial State', () => {
    it('should show loading state initially', async () => {
      axios.post.mockImplementation(() => new Promise(() => {}));

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      expect(screen.getByTestId('loading')).toHaveTextContent('loading');
    });

    it('should complete loading after refresh token check', async () => {
      axios.post.mockRejectedValue(new Error('No session'));

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });
    });

    it('should not be authenticated when no session exists', async () => {
      axios.post.mockRejectedValue(new Error('No session'));

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
        expect(screen.getByTestId('user')).toHaveTextContent('no-user');
        expect(screen.getByTestId('token')).toHaveTextContent('no-token');
      });
    });

    it('should restore session from refresh token if valid', async () => {
      axios.post.mockResolvedValue({
        data: {
          token: 'restored-access-token',
          username: 'restoreduser',
          expiresAt: new Date(Date.now() + 3600000).toISOString()
        }
      });

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
        expect(screen.getByTestId('user')).toHaveTextContent('restoreduser');
        expect(screen.getByTestId('token')).toHaveTextContent('restored-access-token');
      });
    });
  });

  describe('Login', () => {
    it('should successfully login with valid credentials', async () => {
      const user = userEvent.setup();

      axios.post.mockRejectedValueOnce(new Error('No session'));
      axios.post.mockResolvedValueOnce({
        data: {
          token: 'new-access-token',
          username: 'testuser',
          expiresAt: new Date(Date.now() + 3600000).toISOString()
        }
      });

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });

      await user.click(screen.getByTestId('login-btn'));

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
        expect(screen.getByTestId('user')).toHaveTextContent('testuser');
        expect(screen.getByTestId('token')).toHaveTextContent('new-access-token');
      });

      expect(apiClient.setAccessToken).toHaveBeenCalledWith('new-access-token');
    });

    it('should handle login failure', async () => {
      const user = userEvent.setup();

      axios.post.mockRejectedValueOnce(new Error('No session'));
      axios.post.mockRejectedValueOnce({
        response: { data: { message: 'Invalid credentials' } }
      });

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('ready');
      });

      await user.click(screen.getByTestId('login-btn'));

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
        expect(screen.getByTestId('user')).toHaveTextContent('no-user');
      });
    });
  });

  describe('Logout', () => {
    it('should clear auth state on logout', async () => {
      const user = userEvent.setup();

      axios.post.mockResolvedValueOnce({
        data: {
          token: 'existing-token',
          username: 'loggeduser',
          expiresAt: new Date(Date.now() + 3600000).toISOString()
        }
      });
      axios.post.mockResolvedValueOnce({});

      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated');
      });

      await user.click(screen.getByTestId('logout-btn'));

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated');
        expect(screen.getByTestId('user')).toHaveTextContent('no-user');
        expect(screen.getByTestId('token')).toHaveTextContent('no-token');
      });

      expect(apiClient.setAccessToken).toHaveBeenCalledWith(null);
    });
  });

  describe('useAuth hook', () => {
    it('should throw error when used outside AuthProvider', () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      const TestOutsideProvider = () => {
        useAuth();
        return <div>Should not render</div>;
      };

      expect(() => render(<TestOutsideProvider />)).toThrow(
        'useAuth must be used within an AuthProvider'
      );

      consoleSpy.mockRestore();
    });
  });
});
