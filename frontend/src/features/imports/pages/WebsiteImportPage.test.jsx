import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import WebsiteImportPage from './WebsiteImportPage';

const URL_FIELD = { name: 'URL' };

describe('WebsiteImportPage', () => {
  it('renders the import form', () => {
    renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    expect(screen.getByRole('heading', { name: /import website/i })).toBeInTheDocument();
    expect(screen.getByRole('textbox', URL_FIELD)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /preview/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^import website$/i })).toBeInTheDocument();
  });

  it('offers three import methods and opens the one named in the query string', () => {
    renderWithProviders(<WebsiteImportPage />, { route: '/import-website?tab=paste' });

    expect(screen.getAllByRole('tab')).toHaveLength(3);
    expect(screen.getByRole('tab', { name: /paste urls/i })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('textbox', { name: /links/i })).toBeInTheDocument();
    expect(screen.queryByRole('textbox', URL_FIELD)).not.toBeInTheDocument();
  });

  it('shows the bookmarklet on the single-URL tab', () => {
    renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    const link = screen.getByTestId('bookmarklet-link');
    expect(link).toHaveAttribute('href', expect.stringMatching(/^javascript:location\.href=.*\/import-website\?url=/));
    expect(link).toHaveAttribute('draggable', 'true');
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

  it('prefills and previews the page handed over by the bookmarklet', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/website/scrape-preview`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json({ title: 'Handed Off', domain: 'example.com' });
      }),
    );

    renderWithProviders(<WebsiteImportPage />, { route: '/import-website?url=https%3A%2F%2Fexample.com%2Fhand-off' });

    expect(await screen.findByText('Handed Off')).toBeInTheDocument();
    expect(screen.getByRole('textbox', URL_FIELD)).toHaveValue('https://example.com/hand-off');
    expect(captured.url).toBe('https://example.com/hand-off');
  });

  it('warns when the preview says the page is already in the library', async () => {
    server.use(
      http.post(`${API_BASE}/website/scrape-preview`, () =>
        HttpResponse.json({
          title: 'Known Page',
          domain: 'example.com',
          existingWebsiteId: 'site-1',
          existingTitle: 'Known Page (saved)',
        }),
      ),
    );

    const { user } = renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    await user.type(screen.getByRole('textbox', URL_FIELD), 'https://example.com/known');
    await user.click(screen.getByRole('button', { name: /preview/i }));

    expect(await screen.findByText(/already in your library/i)).toBeInTheDocument();
    expect(screen.getByText('Known Page (saved)')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /open it/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^import website$/i })).toBeEnabled();
  });

  it('imports a website and shows the success message', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/website/from-url`, async ({ request }) => {
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
      http.post(`${API_BASE}/website/from-url`, () => new HttpResponse(null, { status: 500 })),
    );

    const { user } = renderWithProviders(<WebsiteImportPage />, { route: '/import-website' });

    await user.type(screen.getByRole('textbox', URL_FIELD), 'https://example.com');
    await user.click(screen.getByRole('button', { name: /^import website$/i }));

    expect(await screen.findByText(/failed to import website/i)).toBeInTheDocument();

    consoleError.mockRestore();
  });
});
