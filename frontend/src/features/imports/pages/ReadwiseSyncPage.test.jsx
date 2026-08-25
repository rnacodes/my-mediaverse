import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import ReadwiseSyncPage from './ReadwiseSyncPage';

const mockMount = () =>
  server.use(
    http.get(`${API_BASE}/book`, () => HttpResponse.json([])),
    http.get(`${API_BASE}/article`, () => HttpResponse.json([])),
  );

describe('ReadwiseSyncPage', () => {
  it('renders the page sections without the removed unlinked-highlights section', () => {
    mockMount();
    renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    expect(screen.getByRole('heading', { name: 'Readwise Sync' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Connection Status' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Sync Articles & Highlights' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Maintenance' })).toBeInTheDocument();

    // Unlinked-highlight management now lives on the Bulk Link Highlights page.
    expect(screen.queryByRole('heading', { name: 'Manage Unlinked Highlights' })).not.toBeInTheDocument();
  });

  it('validates the connection and shows the connected status', async () => {
    mockMount();
    server.use(
      http.get(`${API_BASE}/readwise/validate`, () =>
        HttpResponse.json({ connected: true, message: 'Connected to Readwise' }),
      ),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Validate Connection' }));

    expect(await screen.findByText('Connected to Readwise')).toBeInTheDocument();
  });

  it('runs a full sync and renders the results summary', async () => {
    mockMount();
    server.use(
      http.post(`${API_BASE}/readwise/sync`, () =>
        HttpResponse.json({
          success: true,
          articlesCreated: 4,
          articlesUpdated: 1,
          highlightsCreated: 10,
          highlightsUpdated: 2,
          highlightsLinked: 8,
          duration: '00:00:05',
        }),
      ),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Full Sync' }));

    expect(await screen.findByText('Sync Results')).toBeInTheDocument();
    expect(screen.getByText('✅ Success')).toBeInTheDocument();
    expect(screen.getByText('Articles Created:')).toBeInTheDocument();
    expect(screen.getByText('Full sync')).toBeInTheDocument();
  });

  it('runs an incremental sync and reports the window and checkpoint', async () => {
    mockMount();
    let requestedUrl;
    server.use(
      http.post(`${API_BASE}/readwise/sync`, ({ request }) => {
        requestedUrl = request.url;
        return HttpResponse.json({
          success: true,
          articlesCreated: 0,
          articlesUpdated: 0,
          highlightsCreated: 1,
          highlightsUpdated: 0,
          highlightsLinked: 0,
          highlightsDeleted: 2,
          syncedSince: '2026-08-01T00:00:00Z',
          syncWindowSource: 'cursor',
          cursorAdvanced: true,
        });
      }),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Sync Recent Changes' }));

    expect(await screen.findByText('Sync Results')).toBeInTheDocument();
    expect(requestedUrl).toContain('incremental=true');
    expect(screen.getByText(/since last successful sync/)).toBeInTheDocument();
    expect(screen.getByText(/checkpoint saved/)).toBeInTheDocument();
    expect(screen.getByText('Highlights Deleted:')).toBeInTheDocument();
  });

  it('hides the deleted-highlights row when nothing was deleted', async () => {
    mockMount();
    server.use(
      http.post(`${API_BASE}/readwise/sync`, () =>
        HttpResponse.json({
          success: true,
          articlesCreated: 0,
          articlesUpdated: 0,
          highlightsCreated: 0,
          highlightsUpdated: 0,
          highlightsLinked: 0,
          highlightsDeleted: 0,
        }),
      ),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Full Sync' }));

    expect(await screen.findByText('Sync Results')).toBeInTheDocument();
    expect(screen.queryByText('Highlights Deleted:')).not.toBeInTheDocument();
  });

  it('shows an error alert when a sync fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    mockMount();
    server.use(
      http.post(`${API_BASE}/readwise/sync`, () =>
        HttpResponse.json({ details: 'Readwise token invalid' }, { status: 500 }),
      ),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Full Sync' }));

    expect(await screen.findByText(/sync failed/i)).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('fetches article content and renders the fetch results', async () => {
    mockMount();
    server.use(
      http.post(`${API_BASE}/readwise/fetch-content`, () =>
        HttpResponse.json({ fetchedCount: 3, message: 'Fetched 3 articles' }),
      ),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Fetch 25' }));

    expect(await screen.findByText('Fetch Results')).toBeInTheDocument();
    expect(screen.getByText('3 articles')).toBeInTheDocument();
  });

  it('cleans highlight text and renders the cleaned count', async () => {
    mockMount();
    server.use(
      http.post(`${API_BASE}/highlight/clean-text`, () =>
        HttpResponse.json({ cleanedCount: 7, message: 'Cleaned 7 highlights' }),
      ),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    await user.click(screen.getByRole('button', { name: 'Clean Highlight Text' }));

    expect(await screen.findByText('Highlights Cleaned:')).toBeInTheDocument();
    expect(screen.getByText('Cleaned 7 highlights')).toBeInTheDocument();
  });

});
