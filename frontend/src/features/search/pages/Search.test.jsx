import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { Link } from 'react-router-dom';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import Search from './Search';

// Serves one highlight from the highlights search endpoint.
const serveOneHighlight = () => {
  server.use(
    http.get(`${API_BASE}/search/highlights`, () =>
      HttpResponse.json({
        found: 1,
        out_of: 1,
        page: 1,
        hits: [
          {
            document: {
              id: 'hl-1',
              text: 'Test highlight text',
              title: 'Test Highlight',
              category: 'books',
              tags: [],
              created_at: 1700000000,
            },
          },
        ],
      }),
    ),
  );
};

// Serves one mixlist from the mixlists search endpoint.
const serveOneMixlist = () => {
  server.use(
    http.get(`${API_BASE}/search/mixlists`, () =>
      HttpResponse.json({
        found: 1,
        out_of: 1,
        page: 1,
        hits: [
          {
            document: {
              id: 'mx-1',
              name: 'Test Mixlist',
              media_item_count: 2,
              date_created: 1700000000,
              topics: [],
              genres: [],
              description: '',
            },
          },
        ],
      }),
    ),
  );
};

describe('Search page', () => {
  it('browses all media by default (as served at /all-media)', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });

    // Both seeded media items render — no filter selection required.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(await screen.findByText('Test Movie')).toBeInTheDocument();

    // The "please select filters" empty prompt must NOT appear in browse-all mode.
    expect(screen.queryByText('Select filters to search')).not.toBeInTheDocument();
  });

  it('narrows to a single media type from the mediaType URL param', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media?mediaType=Book' });

    // Only the Book comes back — the filter param drove a media_type:=Book query.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(screen.queryByText('Test Movie')).not.toBeInTheDocument();
  });

  it('media mode never mixes in notes or highlights', async () => {
    serveOneHighlight();
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });

    // Only media items render even though the highlights endpoint has results.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(screen.queryByText('Test highlight text')).not.toBeInTheDocument();
  });
});

describe('Search podcast Series/Episodes filter', () => {
  vi.setConfig({ testTimeout: 30000 });

  // Records each /search filter string and answers with one series and one episode.
  const servePodcasts = () => {
    const filters = [];
    const doc = (id, title, podcastType, extra = {}) => ({
      document: {
        id, title, media_type: 'Podcast', podcast_type: podcastType, status: 'Uncharted',
        topics: [], genres: [], date_added: 1700000000, description: '', ...extra,
      },
    });
    server.use(
      http.get(`${API_BASE}/search`, ({ request }) => {
        const filter = new URL(request.url).searchParams.get('filter') || '';
        filters.push(filter);
        let hits = [
          doc('ts-series', 'The Show', 'Series'),
          doc('ts-episode', 'Pilot Episode', 'Episode', { series_id: 'ts-series', series_title: 'The Show' }),
        ];
        if (filter.includes('podcast_type:=Series')) hits = [hits[0]];
        else if (filter.includes('podcast_type:=Episode')) hits = [hits[1]];
        return HttpResponse.json({ found: hits.length, out_of: 2, page: 1, hits });
      }),
    );
    return filters;
  };

  it('only offers the toggle while Podcast is a selected media type', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media?mediaType=Book' });

    await screen.findByText('Test Book');
    expect(screen.queryByRole('group', { name: /podcast series or episodes/i })).not.toBeInTheDocument();
  });

  it('narrows podcasts to episodes and back, filtering on podcast_type', async () => {
    const filters = servePodcasts();
    const { user } = renderWithProviders(<Search defaultMediaTypes={['all']} />, {
      route: '/all-media?mediaType=Podcast',
    });

    expect(await screen.findByText('Pilot Episode')).toBeInTheDocument();
    expect(screen.getAllByText('The Show').length).toBeGreaterThan(0);

    const toggle = screen.getByRole('group', { name: /podcast series or episodes/i });
    await user.click(within(toggle).getByRole('button', { name: 'Series' }));

    await waitFor(() => expect(screen.queryByText('Pilot Episode')).not.toBeInTheDocument());
    expect(filters.at(-1)).toBe('((media_type:=Podcast && podcast_type:=Series))');
    expect(screen.getByText('Podcast series only')).toBeInTheDocument();

    await user.click(within(toggle).getByRole('button', { name: 'All' }));

    expect(await screen.findByText('Pilot Episode')).toBeInTheDocument();
    expect(filters.at(-1)).toBe('(media_type:=Podcast)');
  });

  it('reads the choice from the podcastType URL param', async () => {
    const filters = servePodcasts();
    renderWithProviders(<Search defaultMediaTypes={['all']} />, {
      route: '/all-media?mediaType=Podcast&podcastType=Episode',
    });

    expect(await screen.findByText('Pilot Episode')).toBeInTheDocument();
    expect(filters.at(-1)).toBe('((media_type:=Podcast && podcast_type:=Episode))');
  });

  it('keeps other selected media types in the results when podcasts are narrowed', async () => {
    const filters = servePodcasts();
    renderWithProviders(<Search defaultMediaTypes={['all']} />, {
      route: '/all-media?mediaType=Podcast,Book&podcastType=Series',
    });

    await screen.findByText('The Show');
    expect(filters.at(-1)).toBe('((media_type:=Podcast && podcast_type:=Series) || media_type:=Book)');
  });
});

describe('Search TV shows/episodes filter', () => {
  vi.setConfig({ testTimeout: 30000 });

  // Records each /search filter string and answers with one show and one episode.
  // Unlike podcasts, the default is shows only: an episode needs the Episodes/All choice.
  const serveTv = () => {
    const filters = [];
    const doc = (id, title, tvType, extra = {}) => ({
      document: {
        id, title, media_type: 'TVShow', tv_type: tvType, status: 'Uncharted',
        topics: [], genres: [], date_added: 1700000000, description: '', ...extra,
      },
    });
    server.use(
      http.get(`${API_BASE}/search`, ({ request }) => {
        const filter = new URL(request.url).searchParams.get('filter') || '';
        filters.push(filter);
        let hits = [
          doc('ts-show', 'Chernobyl', 'Show', { release_year: 2019, tmdb_rating: 8.7 }),
          doc('ts-tv-episode', '1:23:45', 'Episode', { show_id: 'ts-show', show_title: 'Chernobyl', season_number: 1, episode_number: 1 }),
        ];
        if (filter.includes('tv_type:!=Episode')) hits = [hits[0]];
        else if (filter.includes('tv_type:=Episode')) hits = [hits[1]];
        return HttpResponse.json({ found: hits.length, out_of: 2, page: 1, hits });
      }),
    );
    return filters;
  };

  it('only offers the toggle while TVShow is a selected media type', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media?mediaType=Book' });

    await screen.findByText('Test Book');
    expect(screen.queryByRole('group', { name: /tv shows or episodes/i })).not.toBeInTheDocument();
  });

  it('hides episodes in browse-all mode', async () => {
    const filters = serveTv();
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });

    expect(await screen.findByText('Chernobyl')).toBeInTheDocument();
    expect(screen.queryByText('1:23:45')).not.toBeInTheDocument();
    expect(filters.at(-1)).toBe('(media_type:!=TVShow || tv_type:!=Episode)');
  });

  it('shows only shows by default, then episodes and both through the toggle', async () => {
    const filters = serveTv();
    const { user } = renderWithProviders(<Search defaultMediaTypes={['all']} />, {
      route: '/all-media?mediaType=TVShow',
    });

    expect(await screen.findByText('Chernobyl')).toBeInTheDocument();
    expect(screen.queryByText('1:23:45')).not.toBeInTheDocument();
    expect(filters.at(-1)).toBe('((media_type:=TVShow && tv_type:!=Episode))');
    // The show card links straight to the show page with its year and rating.
    expect(screen.getByRole('link', { name: /chernobyl/i })).toHaveAttribute('href', '/tv-show/ts-show');
    expect(screen.getByText('2019 • 8.7★')).toBeInTheDocument();

    const toggle = screen.getByRole('group', { name: /tv shows or episodes/i });
    await user.click(within(toggle).getByRole('button', { name: 'Episodes' }));

    expect(await screen.findByText('1:23:45')).toBeInTheDocument();
    expect(filters.at(-1)).toBe('((media_type:=TVShow && tv_type:=Episode))');
    expect(screen.getByText('TV episodes only')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /1:23:45/i })).toHaveAttribute('href', '/media/ts-tv-episode');
    expect(screen.getByText('S1E1')).toBeInTheDocument();

    await user.click(within(toggle).getByRole('button', { name: 'All' }));

    await waitFor(() => expect(filters.at(-1)).toBe('(media_type:=TVShow)'));
    // Both cards now: the show, and the episode credited to it.
    await waitFor(() => expect(screen.getAllByText('Chernobyl').length).toBe(2));
    expect(screen.getByText('TV shows and episodes')).toBeInTheDocument();

    await user.click(within(toggle).getByRole('button', { name: 'Shows' }));

    await waitFor(() => expect(filters.at(-1)).toBe('((media_type:=TVShow && tv_type:!=Episode))'));
    expect(screen.queryByText('TV shows and episodes')).not.toBeInTheDocument();
  });

  it('reads the choice from the tvType URL param', async () => {
    const filters = serveTv();
    renderWithProviders(<Search defaultMediaTypes={['all']} />, {
      route: '/all-media?mediaType=TVShow&tvType=Episode',
    });

    expect(await screen.findByText('1:23:45')).toBeInTheDocument();
    expect(filters.at(-1)).toBe('((media_type:=TVShow && tv_type:=Episode))');
  });

  it('ignores the tvType URL param when TVShow is not selected', async () => {
    const filters = serveTv();
    renderWithProviders(<Search defaultMediaTypes={['all']} />, {
      route: '/all-media?mediaType=Book&tvType=Episode',
    });

    await waitFor(() => expect(filters.length).toBeGreaterThan(0));
    expect(filters.at(-1)).toBe('(media_type:=Book)');
  });
});

describe('Search modes', () => {
  it('reacts to URL changes while mounted (Browse Media menu bug)', async () => {
    const { user } = renderWithProviders(
      <>
        <Link to="/search?mediaType=Book">Browse Books</Link>
        <Search />
      </>,
      { route: '/search?searchMode=mixlists', path: '/search' },
    );

    // Arrives in mixlists mode at its search prompt.
    expect(await screen.findByText('Search your mixlists')).toBeInTheDocument();

    // Navigating in-app to a media-type URL must switch mode and fetch — no refresh.
    await user.click(screen.getByText('Browse Books'));
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Media Items' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.queryByText('Search your mixlists')).not.toBeInTheDocument();
  });

  it('resolves the legacy ?mediaType=Highlight deep link to highlights mode', async () => {
    serveOneHighlight();
    renderWithProviders(<Search />, { route: '/search?mediaType=Highlight' });

    // The old-style link browses all highlights, with the Highlights toggle active.
    expect(await screen.findByText('Test highlight text')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Highlights' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('searches highlights in highlights mode', async () => {
    serveOneHighlight();
    renderWithProviders(<Search />, { route: '/search?searchMode=highlights&q=test' });

    expect(await screen.findByText('Test highlight text')).toBeInTheDocument();
    // Media results never mix in.
    expect(screen.queryByText('Test Book')).not.toBeInTheDocument();
  });

  it('shows all mixlists via the View All Mixlists control', async () => {
    serveOneMixlist();
    const { user } = renderWithProviders(<Search />, { route: '/search?searchMode=mixlists' });

    // Starts at the search prompt, with View All controls in the bar and empty state.
    expect(await screen.findByText('Search your mixlists')).toBeInTheDocument();
    await user.click(screen.getAllByRole('button', { name: 'View All Mixlists' })[0]);

    expect(await screen.findByText('Test Mixlist')).toBeInTheDocument();
  });
});

describe('Search bulk actions', () => {
  it('bulk delete sends media ids to the media bulk endpoint', async () => {
    const deleted = { media: null, highlights: null };
    server.use(
      http.delete(`${API_BASE}/media/bulk`, async ({ request }) => {
        deleted.media = (await request.json()).ids;
        return HttpResponse.json({ deletedCount: deleted.media.length });
      }),
      http.delete(`${API_BASE}/highlight/bulk`, async ({ request }) => {
        deleted.highlights = (await request.json()).ids;
        return HttpResponse.json({ deletedCount: deleted.highlights.length });
      }),
    );

    const { user } = renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });
    expect(await screen.findByText('Test Book')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Select All' }));
    await user.click(screen.getByRole('button', { name: /^Delete \(2\)$/ }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/2 media items/)).toBeInTheDocument();
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(await screen.findByText(/Successfully deleted 2 media!/)).toBeInTheDocument();
    expect(deleted.media).toEqual(['ts-book', 'ts-movie']);
    expect(deleted.highlights).toBeNull(); // media mode never touches highlights
  });

  it('bulk delete sends highlight ids to the highlight bulk endpoint', async () => {
    serveOneHighlight();
    const deleted = { highlights: null };
    server.use(
      http.delete(`${API_BASE}/highlight/bulk`, async ({ request }) => {
        deleted.highlights = (await request.json()).ids;
        return HttpResponse.json({ deletedCount: deleted.highlights.length });
      }),
    );

    const { user } = renderWithProviders(<Search />, { route: '/search?searchMode=highlights&q=test' });
    expect(await screen.findByText('Test highlight text')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Select All' }));
    await user.click(screen.getByRole('button', { name: /^Delete \(1\)$/ }));

    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(await screen.findByText(/Successfully deleted 1 highlights!/)).toBeInTheDocument();
    expect(deleted.highlights).toEqual(['hl-1']);
  });

  it('hides Add to Mixlist in highlights mode but keeps Delete', async () => {
    serveOneHighlight();
    renderWithProviders(<Search />, { route: '/search?searchMode=highlights&q=test' });
    expect(await screen.findByText('Test highlight text')).toBeInTheDocument();

    // Highlights have no mixlist relationship, so the action is not offered at all.
    expect(screen.queryByRole('button', { name: 'Add to Mixlist' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^Delete \(0\)$/ })).toBeInTheDocument();
  });
});
