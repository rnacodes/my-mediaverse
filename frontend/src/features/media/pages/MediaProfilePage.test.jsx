import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeBook, makeWebsite, makeMedia, makeTvShowEpisode } from '@/test/factories/media';
import { makeHighlight } from '@/test/factories/note';
import MediaProfilePage from './MediaProfilePage';

// Integration via MSW. The page is mounted under a /media/:id route so useParams()
// resolves. The success tree mounts several children that fire real network on
// mount, so each test seeds exactly the endpoints its media type touches. The
// component collapses its four conceptual states into three: the "not found"
// fallback IS the error UI (a failed GET /media/:id leaves mediaItem null), and
// there is no per-item empty state.
const MEDIA_ID = 'media-profile-1';
const render = () =>
  renderWithProviders(<MediaProfilePage />, {
    route: `/media/${MEDIA_ID}`,
    path: '/media/:id',
  });

describe('MediaProfilePage', () => {
  it('shows the loading state before the media resolves', async () => {
    // A Website touches the fewest children on success: only note-for-media (the
    // /mixlist default already covers the carousel), so the test settles cleanly.
    server.use(
      http.get(`${API_BASE}/media/:id`, ({ params }) =>
        HttpResponse.json(makeWebsite({ id: params.id, title: 'Loaded Website' })),
      ),
      http.get(`${API_BASE}/note/for-media/:id`, () => HttpResponse.json([])),
    );

    render();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    expect(screen.getByText(/loading media item\.\.\./i)).toBeInTheDocument();

    // Settle so the in-flight request doesn't leak past the test.
    await waitFor(() =>
      expect(screen.queryByText(/loading media item\.\.\./i)).not.toBeInTheDocument(),
    );
  });

  it('renders the "not found" fallback when the media request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/media/:id`, () => new HttpResponse(null, { status: 500 })),
    );

    render();

    await waitFor(() =>
      expect(screen.getByText(/media item not found\./i)).toBeInTheDocument(),
    );
    expect(screen.getByRole('button', { name: /back to all media/i })).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('renders a Book and wires highlights from the hook through to the section', async () => {
    server.use(
      http.get(`${API_BASE}/media/:id`, ({ params }) =>
        HttpResponse.json(makeBook({ id: params.id, title: 'Wired Book' })),
      ),
      http.get(`${API_BASE}/book/:id`, ({ params }) =>
        HttpResponse.json(makeBook({ id: params.id, title: 'Wired Book' })),
      ),
      http.get(`${API_BASE}/highlight/book/:id`, () =>
        HttpResponse.json([
          makeHighlight({ text: 'A highlight from the book.' }),
          makeHighlight({ text: 'Another highlight from the book.' }),
        ]),
      ),
      http.get(`${API_BASE}/note/for-media/:id`, () => HttpResponse.json([])),
    );

    const { user } = render();

    // Reaching the Highlights header proves the Book success render was hit.
    const heading = await screen.findByRole('heading', { name: 'Highlights' });
    // The count chip reflects the seeded /highlight/book/:id payload — proves the
    // type-gated hook fetched and the result flowed down as a prop.
    expect(within(heading.parentElement).getByText('2')).toBeInTheDocument();

    await user.click(heading);

    expect(await screen.findByText('A highlight from the book.')).toBeInTheDocument();
    expect(screen.getByText('Another highlight from the book.')).toBeInTheDocument();
  });

  it('renders a TV episode with its identifier and a link back to the show', async () => {
    server.use(
      http.get(`${API_BASE}/media/:id`, ({ params }) =>
        HttpResponse.json(makeMedia({ id: params.id, mediaType: 'TVShow', title: '1:23:45' })),
      ),
      http.get(`${API_BASE}/tvshow/:id`, () => new HttpResponse(null, { status: 404 })),
      http.get(`${API_BASE}/tvshow/episodes/:id`, ({ params }) =>
        HttpResponse.json(makeTvShowEpisode({ id: params.id, title: '1:23:45', showId: 'show-9', showTitle: 'Chernobyl', episodeIdentifier: 'S1E1' })),
      ),
      http.get(`${API_BASE}/note/for-media/:id`, () => HttpResponse.json([])),
    );

    render();

    expect(await screen.findByRole('heading', { name: '1:23:45' })).toBeInTheDocument();
    expect(await screen.findByText('S1E1')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Chernobyl' })).toHaveAttribute('href', '/tv-show/show-9');
    expect(screen.getByRole('button', { name: /tv episode details/i })).toBeInTheDocument();
  });
});
