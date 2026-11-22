import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import userEvent from '@testing-library/user-event';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import PodcastDatasetImportSection from './PodcastDatasetImportSection';

// POST /podcast/series/from-dataset answers with a reporting-contract body.
const datasetResult = (overrides = {}) => ({
  success: true,
  operation: 'podcast-dataset-import',
  fileKind: 'podcasts',
  totalProcessed: 0,
  createdCount: 0,
  updatedCount: 0,
  skippedCount: 0,
  failedCount: 0,
  seriesCreatedCount: 0,
  episodesCreatedCount: 0,
  errors: [],
  failures: [],
  errorMessage: null,
  warningMessage: null,
  startedAt: '2026-11-21T10:00:00Z',
  completedAt: '2026-11-21T10:00:01Z',
  reindexTriggered: false,
  ...overrides,
});

const makeCsvFile = (name = 'listen-notes-podcasts.csv') =>
  new File(['title,publisher\nShow A,Publisher A\n'], name, { type: 'text/csv' });

const selectFile = async (user, container, file) => {
  const input = container.querySelector('#dataset-file-input');
  await user.upload(input, file);
};

// Each summary metric renders as a card with the value above its label.
const statValue = (label) => {
  const labelEl = screen.getByText(label);
  return within(labelEl.closest('.MuiCardContent-root')).getByText(/^\d+$/).textContent;
};

const respondWith = (body, status = 200) =>
  server.use(http.post(`${API_BASE}/podcast/series/from-dataset`, () => HttpResponse.json(body, { status })));

describe('PodcastDatasetImportSection', () => {
  it('renders the upload form with the Listen Notes credit', () => {
    renderWithProviders(<PodcastDatasetImportSection />);

    expect(
      screen.getByRole('heading', { name: /import podcasts from a listen notes dataset/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /choose csv file/i })).toBeInTheDocument();
    const logo = screen.getByAltText('Powered by Listen Notes');
    expect(logo.closest('a')).toHaveAttribute('href', 'https://www.listennotes.com');
  });

  it('shows new shows for a podcasts file', async () => {
    respondWith(datasetResult({
      totalProcessed: 90, createdCount: 88, seriesCreatedCount: 88, skippedCount: 2, reindexTriggered: true,
    }));

    const { user, container } = renderWithProviders(<PodcastDatasetImportSection />);
    await selectFile(user, container, makeCsvFile());
    await user.click(screen.getByRole('button', { name: /import dataset/i }));

    expect(await screen.findByText(/import complete/i)).toBeInTheDocument();
    expect(statValue('Total')).toBe('90');
    expect(statValue('New shows')).toBe('88');
    expect(statValue('Skipped')).toBe('2');
    expect(screen.queryByText('New episodes')).not.toBeInTheDocument();
    expect(screen.getByText(/being added to search/i)).toBeInTheDocument();
  });

  it('also shows new episodes for an episodes file', async () => {
    respondWith(datasetResult({
      fileKind: 'episodes', totalProcessed: 13, createdCount: 13, seriesCreatedCount: 12, episodesCreatedCount: 13,
    }));

    const { user, container } = renderWithProviders(<PodcastDatasetImportSection />);
    await selectFile(user, container, makeCsvFile('listen-notes-episodes.csv'));
    await user.click(screen.getByRole('button', { name: /import dataset/i }));

    expect(await screen.findByText(/import complete/i)).toBeInTheDocument();
    expect(statValue('New shows')).toBe('12');
    expect(statValue('New episodes')).toBe('13');
  });

  it('lists failed rows with their reasons', async () => {
    respondWith(datasetResult({
      totalProcessed: 2,
      createdCount: 1,
      seriesCreatedCount: 1,
      failedCount: 1,
      warningMessage: '1 of 2 rows could not be imported.',
      failures: [{ title: 'Broken Show', reason: 'Its feed URL is not valid.' }],
    }));

    const { user, container } = renderWithProviders(<PodcastDatasetImportSection />);
    await selectFile(user, container, makeCsvFile());
    await user.click(screen.getByRole('button', { name: /import dataset/i }));

    await user.click(await screen.findByText(/failed rows \(1\)/i));
    expect(await screen.findByText('Broken Show')).toBeInTheDocument();
    expect(screen.getByText('Its feed URL is not valid.')).toBeInTheDocument();
  });

  it('shows the reason when the file is not a Listen Notes export', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    respondWith(
      datasetResult({
        success: false,
        errorMessage: 'The file has no "title" column, so it is not a Listen Notes export.',
      }),
      400,
    );

    const { user, container } = renderWithProviders(<PodcastDatasetImportSection />);
    await selectFile(user, container, makeCsvFile());
    await user.click(screen.getByRole('button', { name: /import dataset/i }));

    expect(await screen.findByText(/not a listen notes export/i)).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('rejects a file that is not a CSV before uploading', async () => {
    const { user, container } = renderWithProviders(<PodcastDatasetImportSection />, {
      // user.upload honors the accept attribute by default; turn that off to reach the check.
      user: userEvent.setup({ applyAccept: false }),
    });

    await selectFile(user, container, new File(['x'], 'shows.txt', { type: 'text/plain' }));

    expect(screen.getByText(/select a listen notes csv export/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /import dataset/i })).toBeDisabled();
  });
});
