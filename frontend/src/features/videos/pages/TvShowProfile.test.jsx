import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor, within, stubHostname } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeTvShow } from '@/test/factories/media';
import TvShowProfile from './TvShowProfile';

const SHOW_ID = 'tvshow-1';
const render = () =>
  renderWithProviders(<TvShowProfile />, { route: `/tv-show/${SHOW_ID}`, path: '/tv-show/:id' });

const importResult = (overrides = {}) => ({
  success: true,
  operation: 'tv-episodes-from-tmdb',
  showId: SHOW_ID,
  showTitle: 'Breaking Bad',
  createdCount: 5,
  updatedCount: 1,
  skippedCount: 2,
  failedCount: 0,
  seasonsProcessed: 2,
  totalProcessed: 8,
  errors: [],
  warnings: [],
  reindexTriggered: true,
  ...overrides,
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

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
    expect(screen.getAllByRole('button', { name: /import episodes from tmdb/i }).length).toBeGreaterThan(0);
    expect(screen.queryByText(/watch progress/i)).not.toBeInTheDocument();
  });

  describe('episode import from TMDB', () => {
    it('imports episodes and shows the run result', async () => {
      seedShow({ tmdbId: 87108 }, []);
      const calls = [];
      server.use(
        http.post(`${API_BASE}/tvshow/:id/episodes/from-tmdb`, ({ params }) => {
          calls.push(params.id);
          return HttpResponse.json(importResult(), { status: 200 });
        }),
      );
      const { user } = render();

      await screen.findByRole('heading', { name: 'Breaking Bad' });
      // One in the header actions, one in the empty state.
      const buttons = screen.getAllByRole('button', { name: /import episodes from tmdb/i });
      expect(buttons).toHaveLength(2);
      await user.click(buttons[0]);

      const panel = await screen.findByTestId('tv-episode-import-result');
      expect(within(panel).getByText(/episode import complete/i)).toBeInTheDocument();
      expect(within(panel).getByText('5')).toBeInTheDocument();
      expect(within(panel).getByText('Created')).toBeInTheDocument();
      expect(within(panel).getByText(/2 seasons checked on tmdb/i)).toBeInTheDocument();
      expect(within(panel).getByText(/search index updated/i)).toBeInTheDocument();
      expect(calls).toEqual([SHOW_ID]);
    });

    it('shows the aborted run body when the import answers 500', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      seedShow({ tmdbId: 87108 }, []);
      server.use(
        http.post(`${API_BASE}/tvshow/:id/episodes/from-tmdb`, () =>
          HttpResponse.json(
            importResult({ success: false, createdCount: 0, updatedCount: 0, skippedCount: 0, seasonsProcessed: 0, reindexTriggered: false, errorMessage: 'TMDB could not read the show.' }),
            { status: 500 },
          ),
        ),
      );
      const { user } = render();

      await screen.findByRole('heading', { name: 'Breaking Bad' });
      await user.click(screen.getAllByRole('button', { name: /import episodes from tmdb/i })[0]);

      const panel = await screen.findByTestId('tv-episode-import-result');
      expect(within(panel).getByText(/episode import failed/i)).toBeInTheDocument();
      expect(within(panel).getByText('TMDB could not read the show.')).toBeInTheDocument();

      consoleError.mockRestore();
    });

    it('disables the import when the show has no TMDB id', async () => {
      seedShow({ tmdbId: null }, []);
      render();

      await screen.findByRole('heading', { name: 'Breaking Bad' });

      for (const button of screen.getAllByRole('button', { name: /import episodes from tmdb/i })) {
        expect(button).toBeDisabled();
      }
    });

    it('keeps the Trakt prompt off the public demo empty state', async () => {
      vi.stubEnv('VITE_DEMO_MODE', 'true');
      stubHostname('demo.mymediaverseuniverse.com');
      seedShow({ tmdbId: 87108 }, []);
      render();

      await screen.findByText(/no episodes tracked yet/i);

      expect(screen.queryByRole('button', { name: /go to trakt sync/i })).not.toBeInTheDocument();
      expect(screen.getAllByRole('button', { name: /import episodes from tmdb/i }).length).toBeGreaterThan(0);
    });
  });

  it('renders the Reindex and Edit Media header buttons and no Delete button', async () => {
    seedShow();
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Breaking Bad' })).toBeInTheDocument(),
    );

    expect(screen.getByRole('button', { name: /reindex/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /edit media/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^delete$/i })).not.toBeInTheDocument();
  });
});
