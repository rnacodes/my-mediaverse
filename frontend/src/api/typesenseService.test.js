import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { typesenseAdvancedSearch, typesenseSearch, DEFAULT_TV_FILTER } from './typesenseService';

// The filter string is what the API forwards to Typesense. TV episodes are indexed
// but hidden unless asked for, so every default path must carry the TV clause and
// the TVShow term must widen only when `tvType` says so.

const captureSearchFilters = () => {
  const filters = [];
  server.use(
    http.get(`${API_BASE}/search`, ({ request }) => {
      filters.push(new URL(request.url).searchParams.get('filter'));
      return HttpResponse.json({ found: 0, out_of: 0, page: 1, hits: [] });
    }),
    http.get(`${API_BASE}/search/by-type/:type`, ({ request }) => {
      filters.push(new URL(request.url).searchParams.get('filter'));
      return HttpResponse.json({ found: 0, out_of: 0, page: 1, hits: [] });
    }),
  );
  return filters;
};

describe('typesenseAdvancedSearch filter string', () => {
  it('hides TV episodes when no media type is selected (browse-all, quick search)', async () => {
    const filters = captureSearchFilters();

    await typesenseAdvancedSearch({ query: 'chernobyl' });

    expect(filters).toEqual([DEFAULT_TV_FILTER]);
    expect(DEFAULT_TV_FILTER).toBe('(media_type:!=TVShow || tv_type:!=Episode)');
  });

  it('combines the default TV clause with the other filters', async () => {
    const filters = captureSearchFilters();

    await typesenseAdvancedSearch({ query: '*', status: 'ActivelyExploring', topics: ['history'] });

    expect(filters).toEqual([
      '(media_type:!=TVShow || tv_type:!=Episode) && (topics:=`history`) && (status:=ActivelyExploring)',
    ]);
  });

  it.each([
    [null, '((media_type:=TVShow && tv_type:!=Episode))'],
    ['Show', '((media_type:=TVShow && tv_type:!=Episode))'],
    ['Episode', '((media_type:=TVShow && tv_type:=Episode))'],
    ['All', '(media_type:=TVShow)'],
  ])('with TVShow selected and tvType=%s sends %s', async (tvType, expected) => {
    const filters = captureSearchFilters();

    await typesenseAdvancedSearch({ query: '*', mediaTypes: ['TVShow'], tvType });

    expect(filters).toEqual([expected]);
  });

  it('narrows only the TVShow term when other types are selected too', async () => {
    const filters = captureSearchFilters();

    await typesenseAdvancedSearch({ query: '*', mediaTypes: ['TVShow', 'Book'], tvType: 'Episode' });

    expect(filters).toEqual(['((media_type:=TVShow && tv_type:=Episode) || media_type:=Book)']);
  });

  it('leaves non-TV selections alone (no TV clause)', async () => {
    const filters = captureSearchFilters();

    await typesenseAdvancedSearch({ query: '*', mediaTypes: ['Podcast', 'Book'], podcastType: 'Series' });

    expect(filters).toEqual(['((media_type:=Podcast && podcast_type:=Series) || media_type:=Book)']);
  });
});

describe('typesenseSearch (plain)', () => {
  it('hides TV episodes on the all-types endpoint', async () => {
    const filters = captureSearchFilters();

    await typesenseSearch('chernobyl');

    expect(filters).toEqual([DEFAULT_TV_FILTER]);
  });

  it('sends no filter to the by-type endpoint (the server fixes the type)', async () => {
    const filters = captureSearchFilters();

    await typesenseSearch('chernobyl', 'Book');

    expect(filters).toEqual([null]);
  });
});
