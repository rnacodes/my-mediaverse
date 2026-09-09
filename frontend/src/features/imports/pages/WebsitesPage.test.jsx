import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeWebsite } from '@/test/factories/media';
import WebsitesPage from './WebsitesPage';

// WebsitesPage reads the list from useAllWebsites -> GET /website (singular; no
// default handler, so each test seeds it). The default "All Websites" filter keeps
// the rss-only query disabled, so only /website fires on mount. Unlike the pages that
// swallow query errors, this one DOES render a distinct error Alert (activeQuery.error),
// so all four states are observable: loading / error / empty / success.

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

describe('WebsitesPage bookmarks export and link health', () => {
  it('downloads the bookmarks file the API returns', async () => {
    server.use(
      http.get(`${API_BASE}/website`, () => HttpResponse.json([makeWebsite({ id: 'w1', title: 'Alpha Site' })])),
      http.get(`${API_BASE}/website/export`, () =>
        new HttpResponse('<!DOCTYPE NETSCAPE-Bookmark-file-1>', {
          headers: {
            'Content-Type': 'text/html',
            'Content-Disposition': 'attachment; filename="mymediaverse-bookmarks-20260907.html"',
          },
        })),
    );
    const createObjectURL = vi.fn(() => 'blob:bookmarks');
    const revokeObjectURL = vi.fn();
    window.URL.createObjectURL = createObjectURL;
    window.URL.revokeObjectURL = revokeObjectURL;
    let downloadedAs;
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function stubClick() {
      downloadedAs = this.download;
    });

    const { user } = renderWithProviders(<WebsitesPage />, { route: '/websites' });
    await waitFor(() => expect(screen.getByText('Alpha Site')).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: /export bookmarks/i }));

    await waitFor(() => expect(createObjectURL).toHaveBeenCalled());
    expect(downloadedAs).toBe('mymediaverse-bookmarks-20260907.html');
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:bookmarks');

    click.mockRestore();
  });

  it('flags websites whose link is broken', async () => {
    server.use(
      http.get(`${API_BASE}/website`, () =>
        HttpResponse.json([
          makeWebsite({ id: 'w1', title: 'Dead Site', lastHttpStatus: 404 }),
          makeWebsite({ id: 'w2', title: 'Live Site', lastHttpStatus: 200 }),
        ])),
    );

    renderWithProviders(<WebsitesPage />, { route: '/websites' });

    await waitFor(() => expect(screen.getByText('Dead Site')).toBeInTheDocument());
    expect(screen.getAllByText('Link broken')).toHaveLength(1);
  });
});
