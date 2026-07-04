import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import BackgroundJobsPage from './BackgroundJobsPage';

// Three status queries fire on mount and must all be mocked (onUnhandledRequest:'error').

describe('BackgroundJobsPage', () => {
  it('mounts and renders its primary heading', async () => {
    server.use(
      http.get(`${API_BASE}/bookenrichment/status`, () => HttpResponse.json({ booksNeedingEnrichment: 0 })),
      http.get(`${API_BASE}/movietvenrichment/status`, () =>
        HttpResponse.json({ moviesNeedingEnrichment: 0, tvShowsNeedingEnrichment: 0 }),
      ),
      http.get(`${API_BASE}/podcastenrichment/status`, () => HttpResponse.json({ podcastsNeedingEnrichment: 0 })),
    );

    renderWithProviders(<BackgroundJobsPage />, { route: '/admin/background-jobs' });

    expect(screen.getByRole('heading', { name: 'Background Jobs' })).toBeInTheDocument();
    // Book enrichment status card resolves from the mocked happy-path response.
    expect(await screen.findByText('All books enriched!')).toBeInTheDocument();
  });
});
