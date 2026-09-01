import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { Link } from 'react-router-dom';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { renderWithProviders, screen, within } from '@/test/test-utils';
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
