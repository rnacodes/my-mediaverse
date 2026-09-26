import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderHook, waitFor } from '../test/test-utils';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { server } from '../test/mocks/server';
import { API_BASE } from '../test/mocks/handlers';
import { makeMedia, makeTvShow, makeTvShowEpisode } from '../test/factories/media';
import { useMergedMediaItem } from './useMergedMediaItem';

// The profile page reads one merged object: the generic /media/:id row plus the type-specific
// detail on top. For websites the detail carries link health and archive fields the generic
// response never has, and a failing detail must not leave the page waiting forever.

const makeClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  });

function createWrapper(client) {
  const Wrapper = ({ children }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  Wrapper.displayName = 'TestQueryClientWrapper';
  return Wrapper;
}

const WEBSITE_ID = 'site-1';

const websiteBase = () =>
  makeMedia({
    id: WEBSITE_ID,
    mediaType: 'Website',
    title: 'Generic title',
    link: 'https://example.com/page',
    description: 'Generic description',
  });

describe('useMergedMediaItem — Website', () => {
  it('merges the website detail (link status, Wayback, enrichedAt) over the generic item', async () => {
    server.use(
      http.get(`${API_BASE}/media/:id`, () => HttpResponse.json(websiteBase())),
      http.get(`${API_BASE}/website/:id`, ({ params }) =>
        HttpResponse.json({
          id: params.id,
          mediaType: 'Website',
          title: 'Detail title',
          lastHttpStatus: 404,
          waybackUrl: 'https://web.archive.org/web/2024/https://example.com/page',
          enrichedAt: '2026-09-01T10:00:00Z',
          domain: 'example.com',
        }),
      ),
    );

    const { result } = renderHook(() => useMergedMediaItem(WEBSITE_ID), {
      wrapper: createWrapper(makeClient()),
    });

    await waitFor(() => expect(result.current.isDetailReady).toBe(true));

    expect(result.current.mediaType).toBe('Website');
    expect(result.current.mediaItem).toMatchObject({
      id: WEBSITE_ID,
      // detail wins over the generic row
      title: 'Detail title',
      lastHttpStatus: 404,
      waybackUrl: 'https://web.archive.org/web/2024/https://example.com/page',
      enrichedAt: '2026-09-01T10:00:00Z',
      domain: 'example.com',
      // generic-only fields survive the merge
      description: 'Generic description',
      link: 'https://example.com/page',
    });
    expect(result.current.podcastKind).toBeNull();
    expect(result.current.error).toBeNull();
  });

  it('still settles when the website detail fails, falling back to the generic item', async () => {
    server.use(
      http.get(`${API_BASE}/media/:id`, () => HttpResponse.json(websiteBase())),
      http.get(`${API_BASE}/website/:id`, () =>
        HttpResponse.json({ error: 'boom' }, { status: 500 }),
      ),
    );

    const { result } = renderHook(() => useMergedMediaItem(WEBSITE_ID), {
      wrapper: createWrapper(makeClient()),
    });

    await waitFor(() => expect(result.current.isDetailReady).toBe(true));

    expect(result.current.mediaItem).toMatchObject({ id: WEBSITE_ID, title: 'Generic title' });
    expect(result.current.mediaItem).not.toHaveProperty('lastHttpStatus');
    expect(result.current.error).toBeNull();
  });

  it('does not request the website detail for a non-website item', async () => {
    let websiteCalls = 0;
    server.use(
      http.get(`${API_BASE}/media/:id`, () =>
        HttpResponse.json(makeMedia({ id: 'movie-1', mediaType: 'Movie', title: 'A movie' })),
      ),
      http.get(`${API_BASE}/website/:id`, () => {
        websiteCalls += 1;
        return HttpResponse.json({});
      }),
      http.get(`${API_BASE}/movie/:id`, ({ params }) =>
        HttpResponse.json({ id: params.id, mediaType: 'Movie', title: 'A movie', director: 'Someone' }),
      ),
    );

    const { result } = renderHook(() => useMergedMediaItem('movie-1'), {
      wrapper: createWrapper(makeClient()),
    });

    await waitFor(() => expect(result.current.isDetailReady).toBe(true));

    expect(result.current.mediaType).toBe('Movie');
    expect(websiteCalls).toBe(0);
  });
});

// TV episodes share the TVShow media type. The show endpoint is probed first; when
// it misses, the episode endpoint has the details and the item is flagged.
describe('useMergedMediaItem — TV show vs. episode', () => {
  it('flags an episode when the show probe misses and the episode endpoint resolves', async () => {
    server.use(
      http.get(`${API_BASE}/media/:id`, ({ params }) =>
        HttpResponse.json(makeMedia({ id: params.id, mediaType: 'TVShow', title: 'Generic title' })),
      ),
      http.get(`${API_BASE}/tvshow/:id`, () => new HttpResponse(null, { status: 404 })),
      http.get(`${API_BASE}/tvshow/episodes/:id`, ({ params }) =>
        HttpResponse.json(makeTvShowEpisode({ id: params.id, title: '1:23:45', showTitle: 'Chernobyl', showId: 'show-9' })),
      ),
    );

    const { result } = renderHook(() => useMergedMediaItem('ep-1'), {
      wrapper: createWrapper(makeClient()),
    });

    await waitFor(() => expect(result.current.isDetailReady).toBe(true));

    expect(result.current.isTvEpisode).toBe(true);
    expect(result.current.tvShowData).toBeNull();
    expect(result.current.mediaItem).toMatchObject({
      id: 'ep-1',
      title: '1:23:45',
      isTvEpisode: true,
      showId: 'show-9',
      showTitle: 'Chernobyl',
      episodeIdentifier: 'S1E3',
    });
  });

  it('does not probe the episode endpoint when the show resolves', async () => {
    let episodeProbed = false;
    server.use(
      http.get(`${API_BASE}/media/:id`, ({ params }) =>
        HttpResponse.json(makeMedia({ id: params.id, mediaType: 'TVShow' })),
      ),
      http.get(`${API_BASE}/tvshow/:id`, ({ params }) =>
        HttpResponse.json(makeTvShow({ id: params.id, title: 'Chernobyl' })),
      ),
      http.get(`${API_BASE}/tvshow/episodes/:id`, () => {
        episodeProbed = true;
        return new HttpResponse(null, { status: 404 });
      }),
    );

    const { result } = renderHook(() => useMergedMediaItem('show-1'), {
      wrapper: createWrapper(makeClient()),
    });

    await waitFor(() => expect(result.current.isDetailReady).toBe(true));

    expect(result.current.isTvEpisode).toBe(false);
    expect(result.current.tvShowData).toMatchObject({ title: 'Chernobyl' });
    expect(result.current.mediaItem.isTvEpisode).toBeUndefined();
    expect(episodeProbed).toBe(false);
  });

  it('still settles when both the show and episode probes miss', async () => {
    server.use(
      http.get(`${API_BASE}/media/:id`, ({ params }) =>
        HttpResponse.json(makeMedia({ id: params.id, mediaType: 'TVShow', title: 'Orphan' })),
      ),
      http.get(`${API_BASE}/tvshow/:id`, () => new HttpResponse(null, { status: 404 })),
      http.get(`${API_BASE}/tvshow/episodes/:id`, () => new HttpResponse(null, { status: 404 })),
    );

    const { result } = renderHook(() => useMergedMediaItem('orphan-1'), {
      wrapper: createWrapper(makeClient()),
    });

    await waitFor(() => expect(result.current.isDetailReady).toBe(true));

    expect(result.current.isTvEpisode).toBe(false);
    expect(result.current.mediaItem).toMatchObject({ title: 'Orphan' });
  });
});
