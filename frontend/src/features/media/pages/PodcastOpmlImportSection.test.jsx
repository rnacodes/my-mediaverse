import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, fireEvent } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import PodcastOpmlImportSection from './PodcastOpmlImportSection';

// POST /podcast/series/from-opml answers with a reporting-contract body.
const opmlResult = (overrides = {}) => ({
  success: true,
  operation: 'podcast-opml-import',
  totalProcessed: 0,
  createdCount: 0,
  updatedCount: 0,
  skippedCount: 0,
  failedCount: 0,
  errors: [],
  failures: [],
  errorMessage: null,
  warningMessage: null,
  startedAt: '2024-01-15T10:00:00Z',
  completedAt: '2024-01-15T10:00:01Z',
  reindexTriggered: false,
  ...overrides,
});

const makeOpmlFile = (name = 'subscriptions.opml') =>
  new File(['<opml><body></body></opml>'], name, { type: 'text/xml' });

const selectFile = async (user, container, file) => {
  const input = container.querySelector('#opml-file-input');
  await user.upload(input, file);
};

// Each summary metric renders as a card with the value (h4) above its label.
// Scope to the card so numeric values can't collide across metrics.
const statValue = (label) => {
  const labelEl = screen.getByText(label);
  return within(labelEl.closest('.MuiCardContent-root')).getByText(/^\d+$/).textContent;
};

describe('PodcastOpmlImportSection', () => {
  it('renders the upload form', () => {
    renderWithProviders(<PodcastOpmlImportSection />);

    expect(
      screen.getByRole('heading', { name: /import podcasts from opml/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /choose opml file/i })).toBeInTheDocument();
  });

  it('shows the result summary after a successful import', async () => {
    server.use(
      http.post(`${API_BASE}/podcast/series/from-opml`, () =>
        HttpResponse.json(
          opmlResult({ totalProcessed: 5, createdCount: 4, skippedCount: 1, reindexTriggered: true }),
        ),
      ),
    );

    const { user, container } = renderWithProviders(<PodcastOpmlImportSection />);

    await selectFile(user, container, makeOpmlFile());
    await user.click(screen.getByRole('button', { name: /import podcasts/i }));

    expect(await screen.findByText(/import complete/i)).toBeInTheDocument();
    expect(statValue('Total')).toBe('5');
    expect(statValue('Imported')).toBe('4');
    expect(statValue('Skipped')).toBe('1');
    expect(statValue('Failed')).toBe('0');
    expect(screen.getByText(/being added to search/i)).toBeInTheDocument();
  });

  it('lists the failed feeds with their reasons', async () => {
    server.use(
      http.post(`${API_BASE}/podcast/series/from-opml`, () =>
        HttpResponse.json(
          opmlResult({
            totalProcessed: 2,
            createdCount: 1,
            failedCount: 1,
            warningMessage: '1 feed could not be imported.',
            failures: [{ title: 'Broken Feed', reason: 'Missing xmlUrl attribute' }],
          }),
        ),
      ),
    );

    const { user, container } = renderWithProviders(<PodcastOpmlImportSection />);

    await selectFile(user, container, makeOpmlFile());
    await user.click(screen.getByRole('button', { name: /import podcasts/i }));

    // Expand the failures accordion, then assert the feed + reason are shown.
    await user.click(await screen.findByText(/failed feeds \(1\)/i));
    expect(await screen.findByText('Broken Feed')).toBeInTheDocument();
    expect(screen.getByText('Missing xmlUrl attribute')).toBeInTheDocument();
    expect(screen.getByText('1 feed could not be imported.')).toBeInTheDocument();
  });

  it('reads errorMessage from the result body when the run itself fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.post(`${API_BASE}/podcast/series/from-opml`, () =>
        HttpResponse.json(
          opmlResult({ success: false, errorMessage: 'The OPML import could not be completed.' }),
          { status: 500 },
        ),
      ),
    );

    const { user, container } = renderWithProviders(<PodcastOpmlImportSection />);

    await selectFile(user, container, makeOpmlFile());
    await user.click(screen.getByRole('button', { name: /import podcasts/i }));

    expect(await screen.findByText('The OPML import could not be completed.')).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('surfaces a server error without crashing', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.post(`${API_BASE}/podcast/series/from-opml`, () =>
        HttpResponse.json({ error: 'Failed to process podcast OPML import' }, { status: 500 }),
      ),
    );

    const { user, container } = renderWithProviders(<PodcastOpmlImportSection />);

    await selectFile(user, container, makeOpmlFile());
    await user.click(screen.getByRole('button', { name: /import podcasts/i }));

    expect(await screen.findByText(/import error/i)).toBeInTheDocument();
    expect(screen.getByText(/failed to process podcast opml import/i)).toBeInTheDocument();
    // Button re-enables rather than getting stuck in the loading state.
    expect(screen.getByRole('button', { name: /import podcasts/i })).toBeEnabled();

    consoleError.mockRestore();
  });

  it('rejects a file that is not .opml/.xml', async () => {
    const { container } = renderWithProviders(<PodcastOpmlImportSection />);

    // fireEvent.change bypasses the input's accept filter so the component's
    // own extension validation is what we exercise here.
    const input = container.querySelector('#opml-file-input');
    fireEvent.change(input, {
      target: { files: [new File(['x'], 'notes.txt', { type: 'text/plain' })] },
    });

    expect(await screen.findByText(/please select an opml file/i)).toBeInTheDocument();
  });
});
