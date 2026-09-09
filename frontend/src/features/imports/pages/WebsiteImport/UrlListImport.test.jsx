import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import UrlListImport from './UrlListImport';

const LINKS = { name: /links/i };

describe('UrlListImport', () => {
  it('previews the pasted list', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/website/from-url-list/preview`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json({ totalCount: 2, validCount: 2, invalidCount: 0, nonWebLinkCount: 1, alreadyInLibraryCount: 0, duplicateInFileCount: 0, newCount: 2, folders: [], sample: [] });
      }),
    );
    const { user } = renderWithProviders(<UrlListImport />, { route: '/import-website?tab=paste' });

    await user.type(screen.getByRole('textbox', LINKS), 'https://example.com/a{enter}example.org hello');
    await user.click(screen.getByRole('button', { name: /^preview$/i }));

    const preview = await screen.findByTestId('url-list-preview');
    expect(within(preview).getByText('New').previousSibling).toHaveTextContent('2');
    expect(within(preview).getByText('Not links').previousSibling).toHaveTextContent('1');
    expect(within(preview).queryByRole('checkbox', { name: /folders become topics/i })).not.toBeInTheDocument();
    expect(captured.urls).toContain('example.org hello');
  });

  it('imports the list with options and shows the result panel', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/website/from-url-list`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json({ success: true, operation: 'url-list-import', totalProcessed: 2, createdCount: 2, updatedCount: 0, skippedCount: 0, failedCount: 0, nonWebLinkCount: 0, errors: [], foldersFound: 0, topicsCreatedCount: 0, pendingEnrichmentCount: 2, reindexTriggered: true });
      }),
    );
    const { user } = renderWithProviders(<UrlListImport />, { route: '/import-website?tab=paste' });

    await user.type(screen.getByRole('textbox', LINKS), 'https://example.com/a');
    await user.click(screen.getByRole('button', { name: /import links/i }));

    expect(await screen.findByTestId('bulk-import-result')).toBeInTheDocument();
    expect(captured.urls).toBe('https://example.com/a');
    expect(captured.options.foldersAsTopics).toBe(true);
    expect(captured.options.defaultStatus).toBe('Uncharted');
  });

  it('disables the buttons until something is pasted', () => {
    renderWithProviders(<UrlListImport />, { route: '/import-website?tab=paste' });

    expect(screen.getByRole('button', { name: /^preview$/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /import links/i })).toBeDisabled();
  });
});
