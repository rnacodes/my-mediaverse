import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { QueryClientProvider } from '@tanstack/react-query';
import { renderHook, makeTestQueryClient } from '../test/test-utils';
import { server } from '../test/mocks/server';
import { API_BASE } from '../test/mocks/handlers';
import { useCreatePodcastSeries, useCreatePodcastEpisode } from './usePodcast';
import { useCreateVideo } from './useVideo';

function createWrapper() {
  const client = makeTestQueryClient();
  const Wrapper = ({ children }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  Wrapper.displayName = 'TestQueryClientWrapper';
  return Wrapper;
}

const create = async (useHook, path, status) => {
  server.use(
    http.post(`${API_BASE}${path}`, () => HttpResponse.json({ id: 'item-1', title: 'Stored' }, { status })),
  );
  const { result } = renderHook(() => useHook(), { wrapper: createWrapper() });
  return result.current.mutateAsync({ title: 'Typed' });
};

describe.each([
  ['useCreatePodcastSeries', useCreatePodcastSeries, '/podcast/series'],
  ['useCreatePodcastEpisode', useCreatePodcastEpisode, '/podcast/episodes'],
  ['useCreateVideo', useCreateVideo, '/video'],
])('%s', (_name, useHook, path) => {
  it('flags an item the library already held (200)', async () => {
    const created = await create(useHook, path, 200);
    expect(created).toMatchObject({ id: 'item-1', title: 'Stored', alreadyInLibrary: true });
  });

  it('does not flag a newly created item (201)', async () => {
    const created = await create(useHook, path, 201);
    expect(created.alreadyInLibrary).toBe(false);
  });
});
