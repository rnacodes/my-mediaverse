import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import MixlistsPage from './MixlistsPage';

// MixlistsPage reads the whole list from useAllMixlists -> GET /mixlist (singular;
// the default handler seeds one mixlist). The component has no distinct error UI:
// `mixlists = query.data ?? []`, so a failed request falls through to the SAME
// "No mixlists yet" empty state. The observable states are loading / empty / success.

describe('MixlistsPage', () => {
  it('shows the loading state before the list resolves', async () => {
    renderWithProviders(<MixlistsPage />, { route: '/mixlists' });

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    expect(screen.getByText(/loading mixlists\.\.\./i)).toBeInTheDocument();

    // Settle so the in-flight request doesn't leak past the test.
    await waitFor(() =>
      expect(screen.queryByText(/loading mixlists\.\.\./i)).not.toBeInTheDocument(),
    );
  });

  it('renders the seeded mixlists with the heading and count', async () => {
    renderWithProviders(<MixlistsPage />, { route: '/mixlists' });

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'My Mixlists' })).toBeInTheDocument(),
    );
    // Default /mixlist handler seeds a single "Test Mixlist".
    expect(screen.getByText('Test Mixlist')).toBeInTheDocument();
    expect(screen.getByText(/1 mixlist found/i)).toBeInTheDocument();
  });

  it('shows the empty state with a create CTA when there are no mixlists', async () => {
    server.use(http.get(`${API_BASE}/mixlist`, () => HttpResponse.json([])));

    renderWithProviders(<MixlistsPage />, { route: '/mixlists' });

    await waitFor(() =>
      expect(screen.getByText(/no mixlists yet/i)).toBeInTheDocument(),
    );
    expect(screen.getByRole('button', { name: /create first mixlist/i })).toBeInTheDocument();
  });

  it('falls back to the empty state when the request fails (no dedicated error UI)', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(http.get(`${API_BASE}/mixlist`, () => new HttpResponse(null, { status: 500 })));

    renderWithProviders(<MixlistsPage />, { route: '/mixlists' });

    // data ?? [] => the error degrades gracefully into the empty state.
    await waitFor(() =>
      expect(screen.getByText(/no mixlists yet/i)).toBeInTheDocument(),
    );

    consoleError.mockRestore();
  });
});
