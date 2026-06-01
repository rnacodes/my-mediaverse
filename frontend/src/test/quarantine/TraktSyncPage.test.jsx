// QUARANTINED (RAS-17): excluded from the run via vitest.config.js.
// TODO(RAS-30): rewrite against the new test infra (MSW + renderWithProviders).
// Component moved to src/features/imports/pages/TraktSyncPage.jsx in the feature-folder reorg.
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import TraktSyncPage from '../TraktSyncPage';
import * as traktService from '../../api/traktService';

vi.mock('../../api/traktService', () => ({
  getTraktStatus: vi.fn(),
  startDeviceAuth: vi.fn(),
  pollDeviceToken: vi.fn(),
  disconnectTrakt: vi.fn(),
  syncWatched: vi.fn(),
  syncWatchlist: vi.fn(),
  syncRatings: vi.fn(),
  syncAll: vi.fn(),
}));

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('TraktSyncPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Default: disconnected
    traktService.getTraktStatus.mockResolvedValue({
      data: { connected: false }
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Page Rendering', () => {
    it('should render the page title and subtitle', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Trakt Sync')).toBeInTheDocument();
      });
      expect(screen.getByText(/Sync your watch history, watchlist, and ratings from Trakt/)).toBeInTheDocument();
    });

    it('should render the connection status section', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Connection Status')).toBeInTheDocument();
      });
    });

    it('should render how it works section', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('How It Works')).toBeInTheDocument();
      });
    });

    it('should render Trakt attribution', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Trakt')).toBeInTheDocument();
      });
    });
  });

  describe('Disconnected State', () => {
    it('should show not connected status when disconnected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Not connected to Trakt')).toBeInTheDocument();
      });
    });

    it('should show Connect to Trakt button when disconnected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Connect to Trakt')).toBeInTheDocument();
      });
    });

    it('should not show sync buttons when disconnected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Not connected to Trakt')).toBeInTheDocument();
      });

      expect(screen.queryByText('Sync Watch History')).not.toBeInTheDocument();
      expect(screen.queryByText('Sync Watchlist')).not.toBeInTheDocument();
      expect(screen.queryByText('Sync Ratings')).not.toBeInTheDocument();
      expect(screen.queryByText('Sync Everything')).not.toBeInTheDocument();
    });
  });

  describe('Connected State', () => {
    beforeEach(() => {
      traktService.getTraktStatus.mockResolvedValue({
        data: { connected: true, username: 'testuser' }
      });
    });

    it('should show connected status with username', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('testuser')).toBeInTheDocument();
      });
    });

    it('should show disconnect button when connected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Disconnect')).toBeInTheDocument();
      });
    });

    it('should show all sync sections when connected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getAllByText('Sync Watch History').length).toBeGreaterThan(0);
      });

      expect(screen.getAllByText('Sync Watchlist').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Sync Ratings').length).toBeGreaterThan(0);
      expect(screen.getByText('Sync All')).toBeInTheDocument();
    });

    it('should show sync action buttons when connected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Sync Everything')).toBeInTheDocument();
      });
    });

    it('should show rating mapping info when connected', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText(/Rating mapping/)).toBeInTheDocument();
      });
    });
  });

  describe('Device Authentication', () => {
    it('should start device auth when Connect button is clicked', async () => {
      traktService.startDeviceAuth.mockResolvedValue({
        data: {
          deviceCode: 'test-device-code',
          userCode: 'ABC123',
          verificationUrl: 'https://trakt.tv/activate',
          expiresIn: 600,
          interval: 5
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Connect to Trakt')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Connect to Trakt'));

      await waitFor(() => {
        expect(traktService.startDeviceAuth).toHaveBeenCalledTimes(1);
      });

      await waitFor(() => {
        expect(screen.getByText('ABC123')).toBeInTheDocument();
      });
    });

    it('should display verification URL during device auth', async () => {
      traktService.startDeviceAuth.mockResolvedValue({
        data: {
          deviceCode: 'test-code',
          userCode: 'XYZ789',
          verificationUrl: 'https://trakt.tv/activate',
          expiresIn: 600,
          interval: 5
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Connect to Trakt')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Connect to Trakt'));

      await waitFor(() => {
        expect(screen.getByText('https://trakt.tv/activate')).toBeInTheDocument();
      });
    });

    it('should show cancel button during device auth', async () => {
      traktService.startDeviceAuth.mockResolvedValue({
        data: {
          deviceCode: 'test-code',
          userCode: 'XYZ789',
          verificationUrl: 'https://trakt.tv/activate',
          expiresIn: 600,
          interval: 5
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Connect to Trakt')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Connect to Trakt'));

      await waitFor(() => {
        expect(screen.getByText('Cancel')).toBeInTheDocument();
      });
    });

    it('should show error when device auth fails', async () => {
      traktService.startDeviceAuth.mockRejectedValue({
        response: { data: { details: 'API unavailable' } }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Connect to Trakt')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Connect to Trakt'));

      await waitFor(() => {
        expect(screen.getByText(/Failed to start authentication/)).toBeInTheDocument();
      });
    });
  });

  describe('Disconnect', () => {
    it('should disconnect and update status', async () => {
      traktService.getTraktStatus.mockResolvedValue({
        data: { connected: true, username: 'testuser' }
      });
      traktService.disconnectTrakt.mockResolvedValue({});

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Disconnect')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Disconnect'));

      await waitFor(() => {
        expect(traktService.disconnectTrakt).toHaveBeenCalledTimes(1);
      });

      await waitFor(() => {
        expect(screen.getByText('Not connected to Trakt')).toBeInTheDocument();
      });
    });

    it('should show error when disconnect fails', async () => {
      traktService.getTraktStatus.mockResolvedValue({
        data: { connected: true, username: 'testuser' }
      });
      traktService.disconnectTrakt.mockRejectedValue({
        response: { data: { details: 'Server error' } }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Disconnect')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Disconnect'));

      await waitFor(() => {
        expect(screen.getByText(/Failed to disconnect/)).toBeInTheDocument();
      });
    });
  });

  describe('Sync Operations', () => {
    beforeEach(() => {
      traktService.getTraktStatus.mockResolvedValue({
        data: { connected: true, username: 'testuser' }
      });
    });

    it('should sync watch history and display results', async () => {
      traktService.syncWatched.mockResolvedValue({
        data: {
          success: true,
          moviesCreated: 3,
          moviesUpdated: 2,
          showsCreated: 1,
          episodesCreated: 10,
          errors: []
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getAllByText('Sync Watch History').length).toBeGreaterThan(0);
      });

      // Click the button (last element with this text is the button)
      const syncButtons = screen.getAllByText('Sync Watch History');
      fireEvent.click(syncButtons[syncButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });

      expect(screen.getByText('3')).toBeInTheDocument();
      expect(screen.getByText('10')).toBeInTheDocument();
    });

    it('should sync watchlist and display results', async () => {
      traktService.syncWatchlist.mockResolvedValue({
        data: {
          success: true,
          moviesCreated: 5,
          showsCreated: 2,
          watchlistItemsProcessed: 7,
          errors: []
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getAllByText('Sync Watchlist').length).toBeGreaterThan(0);
      });

      const syncButtons = screen.getAllByText('Sync Watchlist');
      fireEvent.click(syncButtons[syncButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });

      expect(traktService.syncWatchlist).toHaveBeenCalledTimes(1);
    });

    it('should sync ratings and display results', async () => {
      traktService.syncRatings.mockResolvedValue({
        data: {
          success: true,
          moviesUpdated: 8,
          showsUpdated: 3,
          ratingsProcessed: 11,
          errors: []
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getAllByText('Sync Ratings').length).toBeGreaterThan(0);
      });

      const syncButtons = screen.getAllByText('Sync Ratings');
      fireEvent.click(syncButtons[syncButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });

      expect(traktService.syncRatings).toHaveBeenCalledTimes(1);
    });

    it('should sync all and display combined results', async () => {
      traktService.syncAll.mockResolvedValue({
        data: {
          success: true,
          moviesCreated: 5,
          moviesUpdated: 10,
          showsCreated: 3,
          showsUpdated: 7,
          episodesCreated: 50,
          episodesUpdated: 20,
          watchlistItemsProcessed: 8,
          ratingsProcessed: 15,
          errors: []
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Sync Everything')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Sync Everything'));

      await waitFor(() => {
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });

      expect(traktService.syncAll).toHaveBeenCalledTimes(1);
    });

    it('should show error when sync fails', async () => {
      traktService.syncWatched.mockRejectedValue({
        response: { data: { errors: ['Token expired'] } }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getAllByText('Sync Watch History').length).toBeGreaterThan(0);
      });

      const syncButtons = screen.getAllByText('Sync Watch History');
      fireEvent.click(syncButtons[syncButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText(/Watch history sync failed/)).toBeInTheDocument();
      });
    });

    it('should show sync errors in results when present', async () => {
      traktService.syncWatched.mockResolvedValue({
        data: {
          success: true,
          moviesCreated: 2,
          errors: ['Failed to process movie: Missing TMDB ID']
        }
      });

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getAllByText('Sync Watch History').length).toBeGreaterThan(0);
      });

      const syncButtons = screen.getAllByText('Sync Watch History');
      fireEvent.click(syncButtons[syncButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText('Failed to process movie: Missing TMDB ID')).toBeInTheDocument();
      });
    });
  });

  describe('Connection Status Check', () => {
    it('should check status on mount', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(traktService.getTraktStatus).toHaveBeenCalledTimes(1);
      });
    });

    it('should show checking message while loading status', () => {
      traktService.getTraktStatus.mockImplementation(
        () => new Promise(() => {}) // Never resolves
      );

      renderWithRouter(<TraktSyncPage />);

      expect(screen.getByText('Checking connection...')).toBeInTheDocument();
    });

    it('should handle status check failure gracefully', async () => {
      traktService.getTraktStatus.mockRejectedValue(new Error('Network error'));

      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Not connected to Trakt')).toBeInTheDocument();
      });
    });
  });

  describe('How It Works Section', () => {
    it('should display how it works information', async () => {
      renderWithRouter(<TraktSyncPage />);

      await waitFor(() => {
        expect(screen.getByText(/Watch History/)).toBeInTheDocument();
      });
      expect(screen.getByText(/Watchlist/)).toBeInTheDocument();
      expect(screen.getByText(/Ratings/)).toBeInTheDocument();
      expect(screen.getByText(/Matching/)).toBeInTheDocument();
      expect(screen.getByText(/Safe/)).toBeInTheDocument();
    });
  });
});
