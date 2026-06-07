import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeHighlight } from '@/test/factories/note';
import ReadwiseSyncPage from './ReadwiseSyncPage';

// MVP coverage (RAS-84): hit every feature of the page once, both outcome directions at
// least once, and exercise the unlinked-highlight link flow (in-scope, not a smoke test).
//
// The page uses React Query hooks. On mount it auto-fetches THREE endpoints
// (GET /highlight/unlinked, GET /book, GET /article) — all must be mocked every test or the
// onUnhandledRequest:'error' guard fails it. Connection validation is gated to a button click
// (the query is enabled:false), so it does NOT fire on mount.

const mockMount = ({ unlinked = [], books = [], articles = [] } = {}) =>
  server.use(
    http.get(`${API_BASE}/highlight/unlinked`, () => HttpResponse.json(unlinked)),
    http.get(`${API_BASE}/book`, () => HttpResponse.json(books)),
    http.get(`${API_BASE}/article`, () => HttpResponse.json(articles)),
  );

describe('ReadwiseSyncPage', () => {
  it('renders the page sections and the empty unlinked-highlights state', async () => {
    mockMount();
    renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    expect(screen.getByRole('heading', { name: 'Readwise Sync' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Connection Status' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Sync Articles & Highlights' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Manage Unlinked Highlights' })).toBeInTheDocument();

    expect(await screen.findByText(/no unlinked highlights found/i)).toBeInTheDocument();
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

  it('links an unlinked highlight to a book and refreshes the list', async () => {
    const highlight = makeHighlight({
      id: 'hl-1',
      text: 'A linkable highlight from a great book.',
      title: 'Some Source',
    });
    const book = { id: 'book-1', title: 'Matching Book', author: 'An Author' };

    // The unlinked list refetches after the PUT (highlight list invalidation); flip it to
    // empty once linked so the success path is unambiguous.
    let linked = false;
    server.use(
      http.get(`${API_BASE}/highlight/unlinked`, () =>
        HttpResponse.json(linked ? [] : [highlight]),
      ),
      http.get(`${API_BASE}/book`, () => HttpResponse.json([book])),
      http.get(`${API_BASE}/article`, () => HttpResponse.json([])),
      http.put(`${API_BASE}/highlight/hl-1`, () => {
        linked = true;
        return HttpResponse.json({ ...highlight, bookId: 'book-1' });
      }),
    );

    const { user } = renderWithProviders(<ReadwiseSyncPage />, { route: '/readwise-sync' });

    // The highlight card renders, then we open its link panel.
    expect(await screen.findByText(/a linkable highlight/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Link to Media' }));

    // Searching surfaces the matching book as a clickable result.
    await user.type(screen.getByPlaceholderText('Search books or articles...'), 'Matching');
    await user.click(await screen.findByRole('button', { name: /Matching Book/ }));

    expect(await screen.findByText(/highlight linked to book successfully/i)).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.queryByText(/a linkable highlight/i)).not.toBeInTheDocument(),
    );
  });
});
