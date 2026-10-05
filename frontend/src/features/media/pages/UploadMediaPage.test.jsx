import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import UploadMediaPage from './UploadMediaPage';

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});

afterEach(() => {
  vi.restoreAllMocks();
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

// POST /upload/csv answers with a reporting-contract body.
const csvResult = (overrides = {}) => ({
  success: true,
  operation: 'csv-upload',
  totalProcessed: 0,
  createdCount: 0,
  skippedCount: 0,
  failedCount: 0,
  errors: [],
  skipped: [],
  importedItems: [],
  errorMessage: null,
  warningMessage: null,
  reindexTriggered: false,
  startedAt: '2024-01-15T10:00:00Z',
  completedAt: '2024-01-15T10:00:01Z',
  ...overrides,
});

const makeCsvFile = () => new File(['Title\nHeat'], 'movies.csv', { type: 'text/csv' });

const selectCsv = (user, container) =>
  user.upload(container.querySelector('#csv-file-input'), makeCsvFile());

describe('UploadMediaPage', () => {
  it('shows the picked type\'s columns and sends that type with the upload', async () => {
    // jsdom does not serialize a multipart body, so the form fields are read as they are appended.
    const append = vi.spyOn(FormData.prototype, 'append');
    server.use(
      http.post(`${API_BASE}/upload/csv`, () =>
        HttpResponse.json(csvResult({ totalProcessed: 1, createdCount: 1 })),
      ),
    );

    const { user, container } = renderWithProviders(<UploadMediaPage />);

    // Book is the default type.
    expect(screen.getByText('Book columns')).toBeInTheDocument();
    expect(screen.getByText('ISBN')).toBeInTheDocument();

    await user.click(screen.getByRole('combobox', { name: /media type/i }));
    await user.click(screen.getByRole('option', { name: 'Movie' }));

    expect(screen.getByText('Movie columns')).toBeInTheDocument();
    expect(screen.getByText('Director')).toBeInTheDocument();
    expect(screen.queryByText('ISBN')).not.toBeInTheDocument();

    await selectCsv(user, container);
    await user.click(screen.getByRole('button', { name: /upload csv/i }));

    expect(await screen.findByTestId('csv-upload-result')).toBeInTheDocument();
    expect(append).toHaveBeenCalledWith('mediaType', 'Movie');
  });

  it('lists the failed and the skipped rows of a result', async () => {
    server.use(
      http.post(`${API_BASE}/upload/csv`, () =>
        HttpResponse.json(
          csvResult({
            totalProcessed: 3,
            createdCount: 1,
            skippedCount: 1,
            failedCount: 1,
            errors: ['Row 3: TMDB has no movie with id 999'],
            skipped: ['Row 2: a book with this ISBN/ASIN or title+author already exists; skipped'],
            importedItems: [{ id: 'book-1', title: 'Dune', mediaType: 'Book' }],
            warningMessage: '1 video row(s) named a channel that is not in the library.',
          }),
        ),
      ),
    );

    const { user, container } = renderWithProviders(<UploadMediaPage />);

    await selectCsv(user, container);
    await user.click(screen.getByRole('button', { name: /upload csv/i }));

    const panel = await screen.findByTestId('csv-upload-result');
    expect(within(panel).getByText('Problems (1)')).toBeInTheDocument();
    expect(within(panel).getByText('Row 3: TMDB has no movie with id 999')).toBeInTheDocument();
    expect(within(panel).getByText(/named a channel that is not in the library/)).toBeInTheDocument();

    expect(within(screen.getByTestId('csv-skipped-rows')).getByText(/Row 2: a book with this ISBN/)).toBeInTheDocument();
    expect(within(screen.getByTestId('csv-imported-items')).getByText('Dune')).toBeInTheDocument();
  });

  it('disables the upload, Goodreads and OPML buttons when the demo blocks writes', () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);

    renderWithProviders(<UploadMediaPage />);

    expect(screen.getByRole('button', { name: /upload csv/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /import goodreads library/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /import podcasts/i })).toBeDisabled();
  });
});
