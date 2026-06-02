import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderHook, waitFor } from '../test/test-utils';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { server } from '../test/mocks/server';
import { API_BASE } from '../test/mocks/handlers';
import {
  mediaKeys,
  relatedMediaKeys,
  recommendationKeys,
  typesenseKeys,
  topicKeys,
  genreKeys,
} from '../api/queryKeys';
import { useDeleteMedia, useBulkDeleteMedia, useUpdateMediaTopicsGenres } from './useMedia';

// These hooks are interesting only for their onSuccess cache effects, so we
// assert on cache STATE (which keys remain / were removed / were invalidated)
// rather than spying on queryClient call counts. `getQueryState(key)` does an
// exact-key lookup: undefined => the query was removed; `.isInvalidated` => it
// was invalidated (marked stale) but left in cache.

// gcTime: Infinity (not makeTestQueryClient's 0) so the inactive queries we
// seed via setQueryData aren't garbage-collected before we inspect them.
const makeClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: Infinity, staleTime: 0 },
      mutations: { retry: false },
    },
  });

function createWrapper(client) {
  return ({ children }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
}

// undefined when the query no longer exists in the cache.
const stateOf = (client, key) => client.getQueryState(key);

describe('useMedia mutation hooks (cache invalidation)', () => {
  describe('useDeleteMedia', () => {
    it('invalidates lists + related + recommendations and removes the deleted detail', async () => {
      const client = makeClient();
      const id = 'media-1';
      const otherId = 'media-2';

      // Seed the caches the hook is expected to touch...
      client.setQueryData(mediaKeys.lists(), ['seeded-list']);
      client.setQueryData(mediaKeys.detail(id), { id });
      client.setQueryData(relatedMediaKeys.byMedia(id), ['related']);
      client.setQueryData(recommendationKeys.byMedia(id), ['rec']);
      // ...plus an unrelated item's detail that must survive untouched.
      client.setQueryData(mediaKeys.detail(otherId), { id: otherId });

      server.use(
        http.delete(`${API_BASE}/media/:id`, () => new HttpResponse(null, { status: 204 })),
      );

      const { result } = renderHook(() => useDeleteMedia(), { wrapper: createWrapper(client) });
      result.current.mutate(id);
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      // detail(id) is REMOVED outright.
      expect(stateOf(client, mediaKeys.detail(id))).toBeUndefined();

      // lists / related / recommendations are INVALIDATED (still present, stale).
      expect(stateOf(client, mediaKeys.lists())?.isInvalidated).toBe(true);
      expect(stateOf(client, relatedMediaKeys.byMedia(id))?.isInvalidated).toBe(true);
      expect(stateOf(client, recommendationKeys.byMedia(id))?.isInvalidated).toBe(true);

      // The other item's detail is left alone.
      const other = stateOf(client, mediaKeys.detail(otherId));
      expect(other).toBeDefined();
      expect(other.isInvalidated).toBe(false);
    });
  });

  describe('useBulkDeleteMedia', () => {
    it('invalidates lists and removes each deleted detail, leaving others (and Typesense) untouched', async () => {
      const client = makeClient();
      const ids = ['media-1', 'media-2'];
      const survivorId = 'media-3';

      client.setQueryData(mediaKeys.lists(), ['seeded-list']);
      client.setQueryData(mediaKeys.detail('media-1'), { id: 'media-1' });
      client.setQueryData(mediaKeys.detail('media-2'), { id: 'media-2' });
      client.setQueryData(mediaKeys.detail(survivorId), { id: survivorId });
      // Typesense cache is deliberately NOT cleared by this hook (Typesense is
      // being phased out); pin that it survives un-invalidated.
      client.setQueryData(typesenseKeys.search({ q: 'anything' }), ['typesense-hit']);

      server.use(
        http.delete(`${API_BASE}/media/bulk`, () => HttpResponse.json({ deleted: ids.length })),
      );

      const { result } = renderHook(() => useBulkDeleteMedia(), {
        wrapper: createWrapper(client),
      });
      result.current.mutate(ids);
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      // Each deleted id's detail is REMOVED.
      expect(stateOf(client, mediaKeys.detail('media-1'))).toBeUndefined();
      expect(stateOf(client, mediaKeys.detail('media-2'))).toBeUndefined();

      // lists is INVALIDATED.
      expect(stateOf(client, mediaKeys.lists())?.isInvalidated).toBe(true);

      // Survivor detail untouched.
      expect(stateOf(client, mediaKeys.detail(survivorId))).toBeDefined();

      // Typesense untouched — present and not invalidated.
      const typesense = stateOf(client, typesenseKeys.search({ q: 'anything' }));
      expect(typesense).toBeDefined();
      expect(typesense.isInvalidated).toBe(false);
    });
  });

  describe('useUpdateMediaTopicsGenres', () => {
    it('invalidates only lists + the item detail, not topic/genre caches', async () => {
      const client = makeClient();
      const mediaId = 'media-1';

      client.setQueryData(mediaKeys.lists(), ['seeded-list']);
      client.setQueryData(mediaKeys.detail(mediaId), { id: mediaId });
      // Topic/genre caches must NOT be invalidated — the hook does not touch them.
      client.setQueryData(topicKeys.lists(), ['topic']);
      client.setQueryData(genreKeys.lists(), ['genre']);

      // The service GETs the current item (default /media/:id handler serves it)
      // then PUTs the merged payload back.
      server.use(
        http.put(`${API_BASE}/media/:id`, () => HttpResponse.json({ id: mediaId })),
      );

      const { result } = renderHook(() => useUpdateMediaTopicsGenres(), {
        wrapper: createWrapper(client),
      });
      result.current.mutate({ mediaId, topics: ['history'], genres: ['nonfiction'] });
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      // detail(mediaId) is INVALIDATED, not removed (still present).
      const detail = stateOf(client, mediaKeys.detail(mediaId));
      expect(detail).toBeDefined();
      expect(detail.isInvalidated).toBe(true);

      // lists is INVALIDATED.
      expect(stateOf(client, mediaKeys.lists())?.isInvalidated).toBe(true);

      // Topic/genre caches are left alone.
      expect(stateOf(client, topicKeys.lists())?.isInvalidated).toBe(false);
      expect(stateOf(client, genreKeys.lists())?.isInvalidated).toBe(false);
    });
  });
});
