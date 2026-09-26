import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeMovie, makeTvShow } from '@/test/factories/media';
import TmdbImportSection from './TmdbImportSection';

vi.setConfig({ testTimeout: 30000 });

const renderExpanded = () =>
  renderWithProviders(
    <TmdbImportSection expanded="tmdb" onAccordionChange={() => () => {}} />,
    { route: '/import-media' },
  );

const MOVIE_HIT = {
  id: 27205,
  media_type: 'movie',
  title: 'Inception',
  release_date: '2010-07-16',
  overview: 'A thief who steals corporate secrets.',
  vote_average: 8.4,
  poster_path: '/inception.jpg',
};

const TV_HIT = {
  id: 87108,
  media_type: 'tv',
  name: 'Chernobyl',
  first_air_date: '2019-05-06',
  overview: 'The story of the 1986 nuclear accident.',
  vote_average: 8.7,
  poster_path: null,
};

// What GET /tmdb/tv/{id} returns: no media_type, TV-specific fields.
const TV_DETAILS = {
  id: 87108,
  name: 'Chernobyl',
  first_air_date: '2019-05-06',
  overview: 'The story of the 1986 nuclear accident.',
  vote_average: 8.7,
  number_of_seasons: 1,
  genres: [{ id: 18, name: 'Drama' }],
};

const useMultiSearch = (hits) =>
  server.use(http.get(`${API_BASE}/tmdb/search/multi`, () => HttpResponse.json({ results: hits })));

const searchFor = async (user, query) => {
  await user.type(screen.getByRole('textbox', { name: /search movies & tv shows/i }), query);
  await user.click(screen.getByRole('button', { name: /^search$/i }));
};

describe('TmdbImportSection', () => {
  it('labels multi-search hits by their media_type and hotlinks TMDB posters', async () => {
    useMultiSearch([MOVIE_HIT, TV_HIT]);
    const { user } = renderExpanded();

    await searchFor(user, 'anything');

    expect(await screen.findByText('Inception')).toBeInTheDocument();
    expect(screen.getByText('Chernobyl')).toBeInTheDocument();
    expect(screen.getByText('Movie')).toBeInTheDocument();
    expect(screen.getByText('TV Show')).toBeInTheDocument();

    const posters = screen.getAllByRole('presentation');
    expect(posters[0]).toHaveAttribute('src', 'https://image.tmdb.org/t/p/w500/inception.jpg');
  });

  it('imports a TV show from the details modal in multi mode (201 → imported)', async () => {
    useMultiSearch([TV_HIT]);
    const importCalls = [];
    server.use(
      http.get(`${API_BASE}/tmdb/tv/:id`, () => HttpResponse.json(TV_DETAILS)),
      http.post(`${API_BASE}/tvshow/from-tmdb/:id`, ({ params }) => {
        importCalls.push(params.id);
        return HttpResponse.json(makeTvShow({ id: 'show-1', title: 'Chernobyl' }), { status: 201 });
      }),
    );
    const { user } = renderExpanded();

    await searchFor(user, 'chernobyl');
    await user.click(await screen.findByRole('button', { name: /view details/i }));

    const modal = (await screen.findByRole('heading', { level: 4, name: 'Chernobyl' })).closest('div');
    expect(within(modal.parentElement).getByText('1')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /import to library/i }));

    expect(await screen.findByText(/"Chernobyl" imported successfully!/)).toBeInTheDocument();
    expect(importCalls).toEqual(['87108']);
  });

  it('imports a movie from the result card and reports "already in your library" on 200', async () => {
    useMultiSearch([MOVIE_HIT]);
    server.use(
      http.post(`${API_BASE}/movie/from-tmdb/:id`, () =>
        HttpResponse.json(makeMovie({ id: 'movie-1', title: 'Inception' }), { status: 200 }),
      ),
    );
    const { user } = renderExpanded();

    await searchFor(user, 'inception');
    await user.click(await screen.findByRole('button', { name: /^import$/i }));

    expect(await screen.findByText(/"Inception" is already in your library\./)).toBeInTheDocument();
    expect(screen.queryByText(/imported successfully/i)).not.toBeInTheDocument();
  });

  it('uses the search type for hits without media_type (TV Shows Only)', async () => {
    const tvOnlyHit = { ...TV_HIT, media_type: undefined };
    server.use(
      http.get(`${API_BASE}/tmdb/search/tv`, () => HttpResponse.json({ results: [tvOnlyHit] })),
      http.post(`${API_BASE}/tvshow/from-tmdb/:id`, () =>
        HttpResponse.json(makeTvShow({ id: 'show-2', title: 'Chernobyl' }), { status: 201 }),
      ),
    );
    const { user } = renderExpanded();

    await user.click(screen.getByRole('combobox', { name: /search type/i }));
    await user.click(screen.getByRole('option', { name: /tv shows only/i }));
    await searchFor(user, 'chernobyl');

    expect(await screen.findByText('TV Show')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /^import$/i }));

    expect(await screen.findByText(/"Chernobyl" imported successfully!/)).toBeInTheDocument();
  });

  it('surfaces an import failure instead of a success message', async () => {
    useMultiSearch([MOVIE_HIT]);
    server.use(
      http.post(`${API_BASE}/movie/from-tmdb/:id`, () =>
        HttpResponse.json({ error: 'TMDB unavailable' }, { status: 502 }),
      ),
    );
    const { user } = renderExpanded();

    await searchFor(user, 'inception');
    await user.click(await screen.findByRole('button', { name: /^import$/i }));

    expect(await screen.findByText(/failed to import/i)).toBeInTheDocument();
    expect(screen.queryByText(/imported successfully/i)).not.toBeInTheDocument();
  });
});
