import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeBook } from '@/test/factories/media';
import AllMedia from './AllMedia';

// AllMedia switches data source on the `mediaType` URL param:
//   no param   -> useAllMedia        -> GET /media           (default handler: book + movie)
//   ?mediaType -> useMediaByType     -> GET /media/by-type/:type  (no default handler — add per test)
// The inactive query is disabled, so only one endpoint is ever requested per route.

describe('AllMedia', () => {
  describe('All-media route (no mediaType param)', () => {
    it('shows the loading indicator before the list resolves', async () => {
      renderWithProviders(<AllMedia />, { route: '/all-media' });

      expect(screen.getByRole('progressbar')).toBeInTheDocument();
      expect(screen.getByText(/loading media/i)).toBeInTheDocument();

      // settle so the pending request doesn't leak past the test
      await waitFor(() => expect(screen.queryByRole('progressbar')).not.toBeInTheDocument());
    });

    it('renders the seeded media with the "All Media" title and count', async () => {
      renderWithProviders(<AllMedia />, { route: '/all-media' });

      await waitFor(() =>
        expect(screen.getByRole('heading', { name: 'All Media' })).toBeInTheDocument(),
      );
      // Default /media handler seeds a book + a movie.
      expect(screen.getByText('Test Book')).toBeInTheDocument();
      expect(screen.getByText('Seeded Movie')).toBeInTheDocument();
      expect(screen.getByText(/2 media items found/i)).toBeInTheDocument();
    });

    it('shows the empty state when no media exists', async () => {
      server.use(http.get(`${API_BASE}/media`, () => HttpResponse.json([])));
      renderWithProviders(<AllMedia />, { route: '/all-media' });

      await waitFor(() =>
        expect(screen.getByText(/no media items found/i)).toBeInTheDocument(),
      );
      // Empty state offers the add/import calls to action.
      expect(screen.getByRole('link', { name: /add media/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /import media/i })).toBeInTheDocument();
    });

    it('shows an error message with a Retry action when the request fails', async () => {
      server.use(
        http.get(`${API_BASE}/media`, () => new HttpResponse(null, { status: 500 })),
      );
      renderWithProviders(<AllMedia />, { route: '/all-media' });

      await waitFor(() =>
        expect(screen.getByText(/failed to load media items/i)).toBeInTheDocument(),
      );
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  describe('By-type route (?mediaType=Book)', () => {
    it('fetches via the by-type endpoint, titling the page and excluding all-media items', async () => {
      // Distinct payload so we can prove the by-type endpoint (not /media) was used.
      server.use(
        http.get(`${API_BASE}/media/by-type/:type`, () =>
          HttpResponse.json([makeBook({ title: 'Typed Book' })]),
        ),
      );

      renderWithProviders(<AllMedia />, { route: '/all-media?mediaType=Book' });

      await waitFor(() =>
        expect(screen.getByRole('heading', { name: 'Book Media' })).toBeInTheDocument(),
      );
      expect(screen.getByText('Typed Book')).toBeInTheDocument();
      expect(screen.getByText(/1 media item found/i)).toBeInTheDocument();
      // The all-media seed (Seeded Movie) must NOT appear — proves we hit by-type, not /media.
      expect(screen.queryByText('Seeded Movie')).not.toBeInTheDocument();
    });

    it('shows the type-specific empty state when the filter returns nothing', async () => {
      server.use(
        http.get(`${API_BASE}/media/by-type/:type`, () => HttpResponse.json([])),
      );

      renderWithProviders(<AllMedia />, { route: '/all-media?mediaType=Book' });

      await waitFor(() =>
        expect(screen.getByText(/no book items found/i)).toBeInTheDocument(),
      );
    });

    it('shows an error message when the by-type request fails', async () => {
      server.use(
        http.get(`${API_BASE}/media/by-type/:type`, () => new HttpResponse(null, { status: 500 })),
      );

      renderWithProviders(<AllMedia />, { route: '/all-media?mediaType=Movie' });

      await waitFor(() =>
        expect(screen.getByText(/failed to load media items/i)).toBeInTheDocument(),
      );
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });
});
