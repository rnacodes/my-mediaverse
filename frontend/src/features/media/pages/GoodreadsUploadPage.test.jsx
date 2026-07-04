import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor, fireEvent } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import GoodreadsUploadPage from './GoodreadsUploadPage';

// The page hardcodes 200 rows per chunk, so a 201-row export produces exactly
// two chunks (rows 1–200, then row 201). Tests exercise the real chunk boundary.
const CHUNK_SIZE = 200;

const makeCsvFile = (rowCount) => {
  const rows = Array.from({ length: rowCount }, (_, i) => `Book ${i + 1},Author ${i + 1}`);
  const csv = ['Title,Author', ...rows].join('\n');
  return new File([csv], 'goodreads_export.csv', { type: 'text/csv' });
};

// A chunked backend response: { chunkIndex, totalChunks, result }.
const chunkResponse = (chunkIndex, totalChunks, overrides = {}) =>
  HttpResponse.json({
    chunkIndex,
    totalChunks,
    result: {
      totalProcessed: 1,
      successCount: 1,
      createdCount: 1,
      updatedCount: 0,
      skippedCount: 0,
      errorCount: 0,
      errors: [],
      importedBooks: [
        { id: `book-${chunkIndex}`, title: `Book chunk ${chunkIndex}`, author: 'Author', wasUpdated: false },
      ],
      ...overrides,
    },
  });

const selectFile = async (user, container, file) => {
  const input = container.querySelector('#goodreads-file');
  await user.upload(input, file);
};

// Scope to the stats grid so labels like "Created" don't collide with the
// per-book status badges rendered further down the page.
const statValue = (container, label) => {
  const grid = container.querySelector('.stats-grid');
  const labelEl = within(grid).getByText(label);
  return within(labelEl.closest('.stat-card')).getByText(/^\d+$/).textContent;
};

describe('GoodreadsUploadPage', () => {
  it('renders the upload form', () => {
    renderWithProviders(<GoodreadsUploadPage />, { route: '/goodreads-upload' });

    expect(screen.getByRole('heading', { name: /import from goodreads/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /upload & import/i })).toBeInTheDocument();
  });

  it('splits the CSV into sequential chunks and aggregates the results', async () => {
    const calls = [];
    server.use(
      http.post(`${API_BASE}/upload/goodreads-csv`, ({ request }) => {
        const url = new URL(request.url);
        const chunkIndex = Number(url.searchParams.get('chunkIndex'));
        const totalChunks = Number(url.searchParams.get('totalChunks'));
        calls.push({ chunkIndex, totalChunks });
        return chunkResponse(chunkIndex, totalChunks);
      }),
    );

    const { user, container } = renderWithProviders(<GoodreadsUploadPage />, {
      route: '/goodreads-upload',
    });

    await selectFile(user, container, makeCsvFile(CHUNK_SIZE + 1));
    await user.click(screen.getByRole('button', { name: /upload & import/i }));

    // Both chunks are reported in the results once the loop finishes.
    expect(await screen.findByText('Book chunk 0')).toBeInTheDocument();
    expect(await screen.findByText('Book chunk 1')).toBeInTheDocument();

    // Two chunks, uploaded in index order (sequential), each aware of the total.
    expect(calls).toEqual([
      { chunkIndex: 0, totalChunks: 2 },
      { chunkIndex: 1, totalChunks: 2 },
    ]);

    // Aggregated totals: 1 created per chunk => 2 total processed / created.
    expect(statValue(container, 'Total Processed')).toBe('2');
    expect(statValue(container, 'Created')).toBe('2');

    // Progress reaches 100% of the 201 rows across 2 chunks.
    expect(
      screen.getByText(/imported 201 of 201 rows \(chunk 2 of 2\)/i),
    ).toBeInTheDocument();
  });

  it('reports a failed chunk without crashing the whole import', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.post(`${API_BASE}/upload/goodreads-csv`, ({ request }) => {
        const url = new URL(request.url);
        const chunkIndex = Number(url.searchParams.get('chunkIndex'));
        const totalChunks = Number(url.searchParams.get('totalChunks'));
        if (chunkIndex === 1) {
          return HttpResponse.json({ error: 'Chunk blew up' }, { status: 500 });
        }
        return chunkResponse(chunkIndex, totalChunks);
      }),
    );

    const { user, container } = renderWithProviders(<GoodreadsUploadPage />, {
      route: '/goodreads-upload',
    });

    await selectFile(user, container, makeCsvFile(CHUNK_SIZE + 1));
    await user.click(screen.getByRole('button', { name: /upload & import/i }));

    // The successful chunk still imported.
    expect(await screen.findByText('Book chunk 0')).toBeInTheDocument();

    // The failed chunk is reported with its row range and reason.
    const failedPanel = await screen.findByText(/failed chunks \(1\)/i);
    const panel = failedPanel.closest('.failed-chunks');
    expect(within(panel).getByText(/rows 201.*201: chunk blew up/i)).toBeInTheDocument();

    // Import completed (button re-enabled) rather than crashing mid-way.
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /upload & import/i })).toBeEnabled(),
    );

    consoleError.mockRestore();
  });

  it('rejects a file that is not a CSV', async () => {
    const { container } = renderWithProviders(<GoodreadsUploadPage />, {
      route: '/goodreads-upload',
    });

    // fireEvent.change bypasses the input's accept=".csv" filter so the
    // component's own extension validation is what we exercise here.
    const input = container.querySelector('#goodreads-file');
    fireEvent.change(input, {
      target: { files: [new File(['x'], 'notes.txt', { type: 'text/plain' })] },
    });

    expect(await screen.findByText(/please select a csv file/i)).toBeInTheDocument();
  });
});
