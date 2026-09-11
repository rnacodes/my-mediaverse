import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, fireEvent, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { importBookmarkFile } from '@/api/websiteService';
import BookmarkFileImport from './BookmarkFileImport';

// The multipart import is asserted at the service boundary: jsdom's transport does not carry a
// real multipart body to the mock server, and the service's own contract (file + options) is
// what the component is responsible for. Preview still goes through the mock server.
vi.mock('@/api/websiteService', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, importBookmarkFile: vi.fn() };
});

const html = '<!DOCTYPE NETSCAPE-Bookmark-file-1><DL><p><DT><A HREF="https://example.com/a">A</A></DL>';
const bookmarkFile = () => new File([html], 'bookmarks.html', { type: 'text/html' });

const PREVIEW = {
  totalCount: 4, validCount: 4, invalidCount: 0, nonWebLinkCount: 1,
  alreadyInLibraryCount: 1, duplicateInFileCount: 1, newCount: 2,
  folders: ['Dev', 'Dev/Rust'],
  sample: [
    { url: 'https://example.com/a', title: 'Page A', folderPath: 'Dev', alreadyInLibrary: false },
    { url: 'https://example.com/b', title: 'Page B', folderPath: 'Dev/Rust', alreadyInLibrary: true },
  ],
};

const RESULT = {
  success: true, operation: 'bookmark-import', totalProcessed: 4, createdCount: 2, updatedCount: 0,
  skippedCount: 2, failedCount: 0, nonWebLinkCount: 1, errors: [], foldersFound: 2, topicsCreatedCount: 2,
  pendingEnrichmentCount: 2, reindexTriggered: true, warningMessage: '1 entry was not a web link and was ignored.',
};

const pickFile = async (user, container, file) => user.upload(container.querySelector('#bookmark-file-input'), file);

describe('BookmarkFileImport', () => {
  it('rejects a file that is not a bookmarks export', () => {
    const { container } = renderWithProviders(<BookmarkFileImport />, { route: '/import-website?tab=file' });

    fireEvent.change(container.querySelector('#bookmark-file-input'), {
      target: { files: [new File(['x'], 'notes.txt', { type: 'text/plain' })] },
    });

    expect(screen.getByText(/please choose a bookmarks file/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^preview$/i })).toBeDisabled();
  });

  it('previews the file and shows counts, folders and the first entries', async () => {
    server.use(http.post(`${API_BASE}/website/from-bookmark-file/preview`, () => HttpResponse.json(PREVIEW)));
    const { user, container } = renderWithProviders(<BookmarkFileImport />, { route: '/import-website?tab=file' });

    await pickFile(user, container, bookmarkFile());
    expect(screen.getByText(/bookmarks\.html/)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /^preview$/i }));

    const preview = await screen.findByTestId('bookmark-preview');
    expect(within(preview).getByText('Already in library')).toBeInTheDocument();
    // The folder appears as a chip and again in the sample table.
    expect(within(preview).getAllByText('Dev/Rust').length).toBeGreaterThanOrEqual(1);
    expect(within(preview).getByText('Page B')).toBeInTheDocument();
    expect(within(preview).getByText('in library')).toBeInTheDocument();
    expect(within(preview).getByRole('checkbox', { name: /folders become topics/i })).toBeChecked();
  });

  it('imports with the chosen options and shows the result panel', async () => {
    server.use(http.post(`${API_BASE}/website/from-bookmark-file/preview`, () => HttpResponse.json(PREVIEW)));
    importBookmarkFile.mockResolvedValue(RESULT);
    const { user, container } = renderWithProviders(<BookmarkFileImport />, { route: '/import-website?tab=file' });

    await pickFile(user, container, bookmarkFile());
    await user.click(screen.getByRole('button', { name: /^preview$/i }));
    await user.click(await screen.findByRole('checkbox', { name: /folders become topics/i }));
    await user.click(screen.getByRole('button', { name: /import bookmarks/i }));

    const panel = await screen.findByTestId('bulk-import-result');
    expect(within(panel).getByText('Import complete')).toBeInTheDocument();
    expect(within(panel).getByText('Created').previousSibling).toHaveTextContent('2');
    expect(within(panel).getByText(/was not a web link/i)).toBeInTheDocument();
    expect(within(panel).getByRole('button', { name: /enrich now \(2 pending\)/i })).toBeEnabled();
    expect(importBookmarkFile).toHaveBeenCalledTimes(1);
    const [sentFile, sentOptions] = importBookmarkFile.mock.calls[0];
    expect(sentFile).toBeInstanceOf(File);
    expect(sentFile.name).toBe('bookmarks.html');
    expect(sentOptions).toEqual({ foldersAsTopics: false, tagsAsTopics: true, defaultStatus: 'Uncharted', extraTopics: [], extraGenres: [] });
  });
});
