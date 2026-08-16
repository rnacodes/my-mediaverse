import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import TypesenseAdminPage from './TypesenseAdminPage';

// Smoke test only - left as a future decomposition

describe('TypesenseAdminPage', () => {
  it('mounts and renders its primary heading', async () => {
    server.use(
      http.get(`${API_BASE}/search/health`, () => HttpResponse.json({ status: 'healthy', message: 'OK' })),
      http.get(`${API_BASE}/note/sync/status`, () => HttpResponse.json({ backgroundSyncEnabled: true })),
    );

    renderWithProviders(<TypesenseAdminPage />, { route: '/admin/typesense' });

    expect(screen.getByRole('heading', { name: 'Typesense Administration' })).toBeInTheDocument();
    // Health card resolves from the mocked happy-path response.
    expect(await screen.findByText('HEALTHY')).toBeInTheDocument();
  });
});
