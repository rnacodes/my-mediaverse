import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeTvShow } from '@/test/factories/media';
import TvShowProfile from './TvShowProfile';

// TvShowProfile is mounted under /tv-show/:id so useParams() resolves. On the success
// path it fetches: GET /tvshow/:id (the show), GET /tvshow/:id/episodes, GET
// /note/for-media/:id (RelatedNotesSection, unconditional), and GET /mixlist (default
// handler). Like MediaProfilePage, error and not-found collapse: a failed GET
// /tvshow/:id leaves `show` null and renders the "TV show not found" Alert. The
// closest thing to an "empty" state is a show with zero tracked episodes.
const SHOW_ID = 'tvshow-1';
const render = () =>
  renderWithProviders(<TvShowProfile />, { route: `/tv-show/${SHOW_ID}`, path: '/tv-show/:id' });

// Three episodes across two seasons; two Completed -> 2/3 = 67% watch progress.
const EPISODES = [
  { id: 'ep-1', title: 'Pilot', seasonNumber: 1, episodeNumber: 1, status: 'Completed', episodeIdentifier: 'S01E01' },
  { id: 'ep-2', title: "Cat's in the Bag", seasonNumber: 1, episodeNumber: 2, status: 'Completed', episodeIdentifier: 'S01E02' },
  { id: 'ep-3', title: 'Grilled', seasonNumber: 2, episodeNumber: 1, status: 'InProgress', episodeIdentifier: 'S02E01' },
];

const seedShow = (overrides = {}, episodes = EPISODES) => {
  server.use(
    http.get(`${API_BASE}/tvshow/:id/episodes`, () => HttpResponse.json(episodes)),
    http.get(`${API_BASE}/tvshow/:id`, ({ params }) =>
      HttpResponse.json(makeTvShow({ id: params.id, title: 'Breaking Bad', ...overrides })),
    ),
    http.get(`${API_BASE}/note/for-media/:id`, () => HttpResponse.json([])),
  );
};

describe('TvShowProfile', () => {
  it('shows the loading spinner before the show resolves', async () => {
    seedShow();
    render();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Breaking Bad' })).toBeInTheDocument(),
    );
  });

  it('renders the not-found alert when the show request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/tvshow/:id`, () => new HttpResponse(null, { status: 500 })),
      http.get(`${API_BASE}/tvshow/:id/episodes`, () => HttpResponse.json([])),
    );

    render();

    await waitFor(() =>
      expect(screen.getByText(/tv show not found/i)).toBeInTheDocument(),
    );

    consoleError.mockRestore();
  });

  it('renders the show with episodes grouped by season and watch progress', async () => {
    seedShow();
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Breaking Bad' })).toBeInTheDocument(),
    );

    // Seasons grouped (descending), with per-season episode counts.
    expect(screen.getByText(/season 1 \(2\)/i)).toBeInTheDocument();
    expect(screen.getByText(/season 2 \(1\)/i)).toBeInTheDocument();

    // Episode titles + identifiers (accordions default-expanded when <= 3 seasons).
    expect(screen.getByText('Pilot')).toBeInTheDocument();
    expect(screen.getByText("Cat's in the Bag")).toBeInTheDocument();
    expect(screen.getByText('Grilled')).toBeInTheDocument();
    expect(screen.getByText('S01E01')).toBeInTheDocument();
    expect(screen.getByText('S02E01')).toBeInTheDocument();

    // 2 of 3 episodes Completed -> 67%.
    expect(screen.getByText(/2 \/ 3 episodes \(67%\)/)).toBeInTheDocument();
  });

  it('shows the Trakt sync prompt and no progress bar when there are no episodes', async () => {
    seedShow({}, []);
    render();

    await waitFor(() =>
      expect(screen.getByText(/no episodes tracked yet/i)).toBeInTheDocument(),
    );
    expect(screen.getByRole('button', { name: /go to trakt sync/i })).toBeInTheDocument();
    expect(screen.queryByText(/watch progress/i)).not.toBeInTheDocument();
  });

  it('opens the delete confirmation dialog and cancels it', async () => {
    seedShow();
    const { user } = render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Breaking Bad' })).toBeInTheDocument(),
    );

    await user.click(screen.getByRole('button', { name: /^delete$/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/delete tv show\?/i)).toBeInTheDocument();
    expect(within(dialog).getByText(/this will remove "breaking bad"/i)).toBeInTheDocument();

    await user.click(within(dialog).getByRole('button', { name: /cancel/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });
});
