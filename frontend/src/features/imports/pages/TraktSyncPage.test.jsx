import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import TraktSyncPage from './TraktSyncPage';

// MVP coverage (RAS-30): hit every page state once, both outcome directions at least once.
// TraktSyncPage drives the Trakt import flow via React Query hooks. On mount it fetches
// GET /trakt/status (retry disabled; an error is treated as disconnected). "Connect" is an
// OAuth device-code flow; once connected, four sync actions POST and render a summary.
//
// Polling note: the device-auth panel begins polling POST /trakt/auth/poll on a
// timer. We register a 'pending' handler so any timer-driven poll has a handler, but we do
// not drive the pending -> authorized transition here.
//
// "Sync Watch History" / "Sync Watchlist" / "Sync Ratings" each render as both a heading and
// a button, so those are always selected by role 'button'.

const STATUS = `${API_BASE}/trakt/status`;

const mockStatus = (data) => server.use(http.get(STATUS, () => HttpResponse.json(data)));

describe('TraktSyncPage', () => {
  it('renders the disconnected state with a Connect button and no sync actions', async () => {
    mockStatus({ connected: false });
    renderWithProviders(<TraktSyncPage />, { route: '/trakt-sync' });

    expect(screen.getByRole('heading', { name: 'Trakt Sync' })).toBeInTheDocument();
    expect(await screen.findByText('Not connected to Trakt')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Connect to Trakt' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Sync Watch History' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Sync Everything' })).not.toBeInTheDocument();
  });

  it('renders the connected state with the username, disconnect, and all sync actions', async () => {
    mockStatus({ connected: true, username: 'mediawatcher' });
    renderWithProviders(<TraktSyncPage />, { route: '/trakt-sync' });

    expect(await screen.findByText('mediawatcher')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Disconnect' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sync Watch History' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sync Watchlist' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sync Ratings' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sync Everything' })).toBeInTheDocument();
  });

  it('starts device auth and shows the user code, verification URL, and Cancel', async () => {
    mockStatus({ connected: false });
    server.use(
      http.post(`${API_BASE}/trakt/auth/device-code`, () =>
        HttpResponse.json({
          deviceCode: 'device-code-123',
          userCode: 'ABC123',
          verificationUrl: 'https://trakt.tv/activate',
          expiresIn: 600,
          interval: 5,
        }),
      ),
      // Keep the poll pending so the timer-driven poll has a handler (Option A).
      http.post(`${API_BASE}/trakt/auth/poll`, () => HttpResponse.json({ status: 'pending' })),
    );

    const { user } = renderWithProviders(<TraktSyncPage />, { route: '/trakt-sync' });

    await user.click(await screen.findByRole('button', { name: 'Connect to Trakt' }));

    expect(await screen.findByText('ABC123')).toBeInTheDocument();
    expect(screen.getByText('https://trakt.tv/activate')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
  });

  it('runs Sync All and renders the results summary', async () => {
    mockStatus({ connected: true, username: 'testuser' });
    server.use(
      http.post(`${API_BASE}/trakt/sync/all`, () =>
        HttpResponse.json({
          success: true,
          moviesCreated: 5,
          watchlistItemsProcessed: 8,
          ratingsProcessed: 15,
          errors: [],
        }),
      ),
    );

    const { user } = renderWithProviders(<TraktSyncPage />, { route: '/trakt-sync' });

    await user.click(await screen.findByRole('button', { name: 'Sync Everything' }));

    expect(await screen.findByText('Sync Results')).toBeInTheDocument();
    expect(screen.getByText('✅ Success')).toBeInTheDocument();
    expect(screen.getByText('Movies Created:')).toBeInTheDocument();
  });

  it('shows an error alert when a sync request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    mockStatus({ connected: true, username: 'testuser' });
    server.use(
      http.post(`${API_BASE}/trakt/sync/watched`, () =>
        HttpResponse.json({ errors: ['Token expired'] }, { status: 500 }),
      ),
    );

    const { user } = renderWithProviders(<TraktSyncPage />, { route: '/trakt-sync' });

    await user.click(await screen.findByRole('button', { name: 'Sync Watch History' }));

    expect(await screen.findByText(/watch history sync failed/i)).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('disconnects and reflects the disconnected status', async () => {
    let connected = true;
    server.use(
      http.get(STATUS, () =>
        HttpResponse.json(
          connected ? { connected: true, username: 'testuser' } : { connected: false },
        ),
      ),
      http.post(`${API_BASE}/trakt/disconnect`, () => {
        connected = false;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    const { user } = renderWithProviders(<TraktSyncPage />, { route: '/trakt-sync' });

    await user.click(await screen.findByRole('button', { name: 'Disconnect' }));

    expect(await screen.findByText('Not connected to Trakt')).toBeInTheDocument();
  });
});
