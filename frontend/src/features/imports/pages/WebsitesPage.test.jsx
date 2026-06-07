import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeWebsite } from '@/test/factories/media';
import WebsitesPage from './WebsitesPage';

// WebsitesPage reads the list from useAllWebsites -> GET /website (singular; no
// default handler, so each test seeds it). The default "All Websites" filter keeps
// the rss-only query disabled, so only /website fires on mount. Unlike MixlistsPage,
// this page DOES render a distinct error Alert (activeQuery.error), so all four
// states are observable: loading / error / empty / success.

describe('WebsitesPage', () => {
  it('shows the loading spinner before the list resolves', async () => {
    server.use(http.get(`${API_BASE}/website`, () => HttpResponse.json([])));

    renderWithProviders(<WebsitesPage />, { route: '/websites' });

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    // Settle so the in-flight request doesn't leak past the test.
    await waitFor(() => expect(screen.queryByRole('progressbar')).not.toBeInTheDocument());
  });

  it('renders the seeded websites with the heading, stats, and count', async () => {
    server.use(
      http.get(`${API_BASE}/website`, () =>
        HttpResponse.json([
          makeWebsite({ id: 'w1', title: 'Alpha Site' }),
          makeWebsite({ id: 'w2', title: 'Beta Site' }),
        ]),
      ),
    );

    renderWithProviders(<WebsitesPage />, { route: '/websites' });

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: /websites/i })).toBeInTheDocument(),
    );
    expect(screen.getByText('Alpha Site')).toBeInTheDocument();
    expect(screen.getByText('Beta Site')).toBeInTheDocument();
    expect(screen.getByText('2 Total')).toBeInTheDocument();
    expect(screen.getByText(/showing 2 of 2 websites/i)).toBeInTheDocument();
  });

  it('shows the empty state with an import CTA when there are no websites', async () => {
    server.use(http.get(`${API_BASE}/website`, () => HttpResponse.json([])));

    renderWithProviders(<WebsitesPage />, { route: '/websites' });

    await waitFor(() => expect(screen.getByText(/no websites yet/i)).toBeInTheDocument());
    // Header + empty-state both expose an "Import Website" button.
    expect(screen.getAllByRole('button', { name: /import website/i }).length).toBeGreaterThan(0);
  });

  it('shows an error alert when the request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(http.get(`${API_BASE}/website`, () => new HttpResponse(null, { status: 500 })));

    renderWithProviders(<WebsitesPage />, { route: '/websites' });

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());

    consoleError.mockRestore();
  });
});
