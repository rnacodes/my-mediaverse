import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import TypesenseAdminPage from './TypesenseAdminPage';

// Smoke test only (RAS-34): this big admin page is left as a future decomposition
// project, so we just mount it with happy-path data and assert the primary heading.
// Deep coverage waits until the page is split into smaller components.
//
// Three queries fire on mount and must all be mocked (onUnhandledRequest:'error'):
// GET /search/realtime-indexing, GET /search/health, GET /note/sync/status.

describe('TypesenseAdminPage', () => {
  it('mounts and renders its primary heading', async () => {
    server.use(
      http.get(`${API_BASE}/search/realtime-indexing`, () => HttpResponse.json({ enabled: true })),
      http.get(`${API_BASE}/search/health`, () => HttpResponse.json({ status: 'healthy', message: 'OK' })),
      http.get(`${API_BASE}/note/sync/status`, () => HttpResponse.json({ backgroundSyncEnabled: true })),
    );

    renderWithProviders(<TypesenseAdminPage />, { route: '/admin/typesense' });

    expect(screen.getByRole('heading', { name: 'Typesense Administration' })).toBeInTheDocument();
    // Health card resolves from the mocked happy-path response.
    expect(await screen.findByText('HEALTHY')).toBeInTheDocument();
  });
});
