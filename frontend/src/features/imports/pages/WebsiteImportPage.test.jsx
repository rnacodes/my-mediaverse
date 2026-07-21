import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import WebsiteImportPage from './WebsiteImportPage';

// WebsiteImportPage fetches nothing on mount: scrape/import are mutations and the
// topic/genre autocompletes only query on non-empty input. Preview validates the
// URL client-side (new URL(...)) before hitting POST /website/scrape-preview;
// Import posts to /website/import and, on success, shows a success Alert then
// navigates after a 1.5s timer (the alert is asserted; the timed nav is not).
const URL_FIELD = { name: 'URL' };

describe('WebsiteImportPage', () => {
  it('renders the import form', () => {
    renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    expect(screen.getByRole('heading', { name: /import website/i })).toBeInTheDocument();
    expect(screen.getByRole('textbox', URL_FIELD)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /preview/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^import website$/i })).toBeInTheDocument();
  });

  it('rejects an invalid URL on preview without hitting the network', async () => {
    const { user } = renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    await user.type(screen.getByRole('textbox', URL_FIELD), 'not a url');
    await user.click(screen.getByRole('button', { name: /preview/i }));

    expect(await screen.findByText(/please enter a valid url/i)).toBeInTheDocument();
  });

  it('scrapes and shows a preview for a valid URL', async () => {
    server.use(
      http.post(`${API_BASE}/website/scrape-preview`, () =>
        HttpResponse.json({
          title: 'Scraped Example',
          domain: 'example.com',
          description: 'A scraped description.',
          rssFeedUrl: 'https://example.com/feed.xml',
        }),
      ),
    );

    const { user } = renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    await user.type(screen.getByRole('textbox', URL_FIELD), 'https://example.com');
    await user.click(screen.getByRole('button', { name: /preview/i }));

    expect(await screen.findByText('Scraped Example')).toBeInTheDocument();
    expect(screen.getByText(/rss feed detected/i)).toBeInTheDocument();
  });

  it('imports a website and shows the success message', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/website/import`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json({ id: 'new-site', title: 'Imported Example' });
      }),
    );

    const { user } = renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    await user.type(screen.getByRole('textbox', URL_FIELD), '  https://example.com  ');
    await user.click(screen.getByRole('button', { name: /^import website$/i }));

    expect(await screen.findByText(/imported successfully/i)).toBeInTheDocument();
    // URL is trimmed before posting.
    expect(captured.url).toBe('https://example.com');
  });

  it('shows an error alert when the import fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.post(`${API_BASE}/website/import`, () => new HttpResponse(null, { status: 500 })),
    );

    const { user } = renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    await user.type(screen.getByRole('textbox', URL_FIELD), 'https://example.com');
    await user.click(screen.getByRole('button', { name: /^import website$/i }));

    expect(await screen.findByText(/failed to import website/i)).toBeInTheDocument();

    consoleError.mockRestore();
  });
});
