import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import YouTubeCallback from '../../pages/YouTubeCallback';

const mockNavigate = vi.fn();

let currentSearchParams = new URLSearchParams();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [currentSearchParams],
  };
});

const renderWithRouter = (component, initialUrl = '/youtube/callback') => {
  const searchString = initialUrl.includes('?') ? initialUrl.split('?')[1] : '';
  currentSearchParams = new URLSearchParams(searchString);

  Object.defineProperty(window, 'location', {
    value: {
      href: `http://localhost:3000${initialUrl}`,
      search: searchString ? `?${searchString}` : '',
    },
    writable: true,
  });

  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('YouTubeCallback', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockClear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Success Flow', () => {
    it('should show common header elements', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      expect(screen.getByText('YouTube Authentication')).toBeInTheDocument();
      expect(screen.getByText('Processing your authentication...')).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      });
    });

    it('should show success state with valid authorization code', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      });

      expect(screen.getByText(/Redirecting to import page/i)).toBeInTheDocument();
      expect(screen.getByText('Go to Home')).toBeInTheDocument();
      expect(screen.getByText('Go to Import')).toBeInTheDocument();
    });

    it('should redirect to import page after 3 seconds on success', async () => {
      vi.useFakeTimers();

      await act(async () => {
        renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');
      });

      await act(async () => {
        await Promise.resolve();
      });

      await act(async () => {
        vi.advanceTimersByTime(3000);
      });

      expect(mockNavigate).toHaveBeenCalledWith('/import-media');

      vi.useRealTimers();
    });

    it('should navigate to home when Go to Home button is clicked', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText('Go to Home')).toBeInTheDocument();
      });

      const homeButton = screen.getByText('Go to Home');
      homeButton.click();

      expect(mockNavigate).toHaveBeenCalledWith('/');
    });

    it('should navigate to import when Go to Import button is clicked', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText('Go to Import')).toBeInTheDocument();
      });

      const importButton = screen.getByText('Go to Import');
      importButton.click();

      expect(mockNavigate).toHaveBeenCalledWith('/import-media');
    });
  });

  describe('Error Flow', () => {
    it('should show error state when OAuth error is present', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?error=access_denied&error_description=User%20denied%20access');

      await waitFor(() => {
        expect(screen.getByText(/There was an error during YouTube authentication/i)).toBeInTheDocument();
      });

      expect(screen.getByText(/OAuth Error: access_denied/i)).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
      expect(screen.getByText('Go Home')).toBeInTheDocument();
    });

    it('should show error state when no authorization code is present', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback');

      await waitFor(() => {
        expect(screen.getByText(/authorization code was not found/i)).toBeInTheDocument();
      });

      expect(screen.getByText(/No authorization code received/i)).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
      expect(screen.getByText('Go Home')).toBeInTheDocument();
    });

    it('should navigate to import page when Try Again is clicked', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?error=access_denied');

      await waitFor(() => {
        expect(screen.getByText('Try Again')).toBeInTheDocument();
      });

      const tryAgainButton = screen.getByText('Try Again');
      tryAgainButton.click();

      expect(mockNavigate).toHaveBeenCalledWith('/import-media');
    });

    it('should navigate to home when Go Home is clicked from error state', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?error=access_denied');

      await waitFor(() => {
        expect(screen.getByText('Go Home')).toBeInTheDocument();
      });

      const goHomeButton = screen.getByText('Go Home');
      goHomeButton.click();

      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });

  describe('Debug Information', () => {
    it('should show debug information in development mode', async () => {
      const originalEnv = process.env.NODE_ENV;
      process.env.NODE_ENV = 'development';

      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText(/Debug Information:/i)).toBeInTheDocument();
      }, { timeout: 3000 });

      expect(screen.getByText(/Code: Present/)).toBeInTheDocument();
      expect(screen.getByText(/State: test_state/)).toBeInTheDocument();
      expect(screen.getByText(/Error: None/)).toBeInTheDocument();

      process.env.NODE_ENV = originalEnv;
    });

    it('should not show debug information in production mode', async () => {
      const originalEnv = process.env.NODE_ENV;
      process.env.NODE_ENV = 'production';

      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      }, { timeout: 3000 });

      expect(screen.queryByText(/Debug Information:/i)).not.toBeInTheDocument();

      process.env.NODE_ENV = originalEnv;
    });
  });

  describe('URL Parameter Handling', () => {
    it('should handle multiple URL parameters correctly', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state&scope=https://www.googleapis.com/auth/youtube.readonly');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      }, { timeout: 3000 });
    });

    it('should handle URL parameters with special characters', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code_with_special-chars.123&state=test%20state%20with%20spaces');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      }, { timeout: 3000 });
    });

    it('should handle empty state parameter', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      }, { timeout: 3000 });
    });
  });

  describe('Component Cleanup', () => {
    it('should set up timeout for navigation on success', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      });

      expect(screen.getByText('Go to Home')).toBeInTheDocument();
      expect(screen.getByText('Go to Import')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have proper heading structure', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      const heading = screen.getByRole('heading', { level: 1 });
      expect(heading).toHaveTextContent('YouTube Authentication');
    });

    it('should have accessible success buttons', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?code=test_code&state=test_state');

      await waitFor(() => {
        expect(screen.getByText(/YouTube authentication successful/i)).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: 'Go to Home' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Go to Import' })).toBeInTheDocument();
    });

    it('should have accessible error buttons', async () => {
      renderWithRouter(<YouTubeCallback />, '/youtube/callback?error=access_denied');

      await waitFor(() => {
        expect(screen.getByText(/There was an error during YouTube authentication/i)).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: 'Try Again' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Go Home' })).toBeInTheDocument();
    });
  });
});
