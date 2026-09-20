import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import BackgroundJobsPage from './BackgroundJobsPage';

// Three status queries fire on mount and must all be mocked (onUnhandledRequest:'error').
vi.setConfig({ testTimeout: 30000 });

const seedStatuses = ({ podcastsNeedingEnrichment = 0 } = {}) =>
  server.use(
    http.get(`${API_BASE}/bookenrichment/status`, () => HttpResponse.json({ booksNeedingEnrichment: 0 })),
    http.get(`${API_BASE}/movietvenrichment/status`, () =>
      HttpResponse.json({ moviesNeedingEnrichment: 0, tvShowsNeedingEnrichment: 0 }),
    ),
    http.get(`${API_BASE}/podcastenrichment/status`, () => HttpResponse.json({ podcastsNeedingEnrichment })),
  );

describe('BackgroundJobsPage', () => {
  it('mounts and renders its primary heading', async () => {
    seedStatuses();

    renderWithProviders(<BackgroundJobsPage />, { route: '/admin/background-jobs' });

    expect(screen.getByRole('heading', { name: 'Background Jobs' })).toBeInTheDocument();
    // Book enrichment status card resolves from the mocked happy-path response.
    expect(await screen.findByText('All books enriched!')).toBeInTheDocument();
  });

  it('describes podcast enrichment as feed-based', async () => {
    seedStatuses();

    renderWithProviders(<BackgroundJobsPage />, { route: '/admin/background-jobs' });

    expect(screen.getByRole('heading', { name: 'Podcast Feed Enrichment' })).toBeInTheDocument();
    expect(await screen.findByText('All podcasts enriched!')).toBeInTheDocument();
  });

  it('renders the full podcast enrichment result, including unchanged, skipped, pending and the warning', async () => {
    seedStatuses({ podcastsNeedingEnrichment: 12 });
    server.use(
      http.post(`${API_BASE}/podcastenrichment/run`, () =>
        HttpResponse.json({
          success: true,
          operation: 'podcast-enrichment',
          totalProcessed: 10,
          enrichedCount: 4,
          unchangedCount: 3,
          skippedCount: 0,
          notFoundCount: 2,
          failedCount: 1,
          pendingCount: 2,
          errors: [],
          warningMessage: '1 feed could not be read and will be retried later.',
        }),
      ),
    );

    const { user } = renderWithProviders(<BackgroundJobsPage />, { route: '/admin/background-jobs' });

    await user.click(await screen.findByRole('button', { name: /run batch \(25 podcasts\)/i }));

    expect(await screen.findByText('Podcast Batch Complete')).toBeInTheDocument();
    expect(screen.getByText('Unchanged')).toBeInTheDocument();
    expect(screen.getByText('Still Pending')).toBeInTheDocument();
    expect(screen.getByText('1 feed could not be read and will be retried later.')).toBeInTheDocument();
  });

  it('syncs all subscribed podcasts and renders the result', async () => {
    seedStatuses();
    server.use(
      http.post(`${API_BASE}/podcast/series/sync-all`, () =>
        HttpResponse.json({
          success: true,
          operation: 'podcast-sync-all',
          seriesChecked: 7,
          seriesSucceeded: 6,
          seriesFailed: 1,
          pendingSeriesCount: 0,
          createdCount: 13,
          series: [],
          errors: ['Broken Show: the feed could not be read.'],
          warningMessage: null,
        }),
      ),
    );

    const { user } = renderWithProviders(<BackgroundJobsPage />, { route: '/admin/background-jobs' });

    await user.click(screen.getByRole('button', { name: /sync all subscribed podcasts/i }));

    expect(await screen.findByText('Podcast Sync Complete')).toBeInTheDocument();
    expect(screen.getByText('13')).toBeInTheDocument();
    expect(screen.getByText('Series Checked')).toBeInTheDocument();
    expect(screen.getByText(/broken show: the feed could not be read/i)).toBeInTheDocument();
  });

  it('shows the result body\'s message when the sync-all run fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    seedStatuses();
    server.use(
      http.post(`${API_BASE}/podcast/series/sync-all`, () =>
        HttpResponse.json({ success: false, errorMessage: 'Podcast sync failed.' }, { status: 500 }),
      ),
    );

    const { user } = renderWithProviders(<BackgroundJobsPage />, { route: '/admin/background-jobs' });

    await user.click(screen.getByRole('button', { name: /sync all subscribed podcasts/i }));

    expect(await screen.findByText(/podcast sync failed\./i)).toBeInTheDocument();
    consoleError.mockRestore();
  });
});
