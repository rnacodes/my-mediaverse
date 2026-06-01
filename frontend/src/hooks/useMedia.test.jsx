import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderHook, waitFor, makeTestQueryClient } from '../test/test-utils';
import { QueryClientProvider } from '@tanstack/react-query';
import { server } from '../test/mocks/server';
import { API_BASE } from '../test/mocks/handlers';
import { useMediaItem, useAllMedia } from './useMedia';



function createWrapper() {
  const client = makeTestQueryClient();
  return ({ children }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
}

describe('useMedia hooks', () => {
  describe('useMediaItem', () => {
    it('starts loading, then resolves with the media item', async () => {
      const { result } = renderHook(() => useMediaItem('media-42'), {
        wrapper: createWrapper(),
      });

      // Synchronously after render the request is still in flight.
      expect(result.current.isLoading).toBe(true);

      // React Query flips to success once MSW responds.
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      // The default /media/:id handler echoes the requested id back via the factory.
      expect(result.current.data).toMatchObject({ id: 'media-42' });
    });

    it('reports an error when the request fails', async () => {
      // Override just for this test; setup.js resetHandlers() restores the default after.
      server.use(
        http.get(`${API_BASE}/media/:id`, () => new HttpResponse(null, { status: 500 })),
      );

      const { result } = renderHook(() => useMediaItem('media-99'), {
        wrapper: createWrapper(),
      });

      // retry:false (from makeTestQueryClient) means the failure surfaces immediately.
      await waitFor(() => expect(result.current.isError).toBe(true));
      expect(result.current.data).toBeUndefined();
    });
  });

  describe('useAllMedia', () => {
    it('resolves with the seeded media list', async () => {
      const { result } = renderHook(() => useAllMedia(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
      // Default /media handler seeds a book + a movie.
      expect(result.current.data).toHaveLength(2);
    });
  });
});
