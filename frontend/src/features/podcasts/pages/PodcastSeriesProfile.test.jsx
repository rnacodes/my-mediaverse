import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import {
  makePodcastSeries,
  makePodcastEpisode,
  makeSyncResult,
  makeFeedEpisodeItem,
  makeFeedEpisodesPage,
} from '@/test/factories/podcast';
import PodcastSeriesProfile from './PodcastSeriesProfile';

vi.setConfig({ testTimeout: 30000 });
const SERIES_ID = 'podcast-series-1';
const render = () =>
  renderWithProviders(<PodcastSeriesProfile />, {
    route: `/podcast-series/${SERIES_ID}`,
    path: '/podcast-series/:id',
  });

// Two episodes; episodeNumber drives the descending sort the component applies.
const EPISODES = [
  makePodcastEpisode({
    id: 'ep-1',
    title: 'The First Episode',
    episodeNumber: 1,
    status: 'Completed',
    releaseDate: '2024-02-01T10:00:00Z',
  }),
  makePodcastEpisode({
    id: 'ep-2',
    title: 'The Second Episode',
    episodeNumber: 2,
    status: 'Uncharted',
    releaseDate: '2024-03-01T10:00:00Z',
  }),
];

const seedSeries = (overrides = {}, episodes = EPISODES) => {
  server.use(
    http.get(`${API_BASE}/podcast/series/:id/episodes`, () => HttpResponse.json(episodes)),
    http.get(`${API_BASE}/podcast/series/:id`, ({ params }) =>
      HttpResponse.json(makePodcastSeries({ id: params.id, title: 'The Test Show', ...overrides })),
    ),
  );
};

const loaded = () =>
  waitFor(() => expect(screen.getByRole('heading', { name: 'The Test Show' })).toBeInTheDocument());

describe('PodcastSeriesProfile', () => {
  it('shows the loading spinner before the series resolves', async () => {
    seedSeries();
    render();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Show' })).toBeInTheDocument(),
    );
  });

  it('renders the not-found alert when the series request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/podcast/series/:id`, () => new HttpResponse(null, { status: 500 })),
      http.get(`${API_BASE}/podcast/series/:id/episodes`, () => HttpResponse.json([])),
    );

    render();

    await waitFor(() =>
      expect(screen.getByText(/podcast series not found/i)).toBeInTheDocument(),
    );

    consoleError.mockRestore();
  });

  it('renders the series with its metadata and imported episodes', async () => {
    seedSeries();
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Show' })).toBeInTheDocument(),
    );

    // Media + podcast-type chips and the description come from MediaInfoCard.
    expect(screen.getByText('Podcast')).toBeInTheDocument();
    expect(screen.getByText('Series')).toBeInTheDocument();
    expect(screen.getByText('A test podcast series.')).toBeInTheDocument();

    // Publisher is rendered in the podcast-specific MediaDetailAccordion section.
    expect(screen.getByText('Test Network')).toBeInTheDocument();

    // Imported-episodes list with its count and each episode title.
    expect(screen.getByText(/my episodes \(2\)/i)).toBeInTheDocument();
    expect(screen.getByText('The First Episode')).toBeInTheDocument();
    expect(screen.getByText('The Second Episode')).toBeInTheDocument();

    // Action bar.
    expect(screen.getByRole('button', { name: /^sync$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^subscribe$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /all episodes/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^delete$/i })).toBeInTheDocument();
  });

  it('shows a zero episode count when the series has no imported episodes', async () => {
    seedSeries({}, []);
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Show' })).toBeInTheDocument(),
    );

    expect(screen.getByText(/my episodes \(0\)/i)).toBeInTheDocument();
    expect(screen.queryByText('The First Episode')).not.toBeInTheDocument();
  });

  it('links to Apple Podcasts when the series has an Apple id, and names where its details came from', async () => {
    seedSeries({ applePodcastsId: '1296350485', metadataSource: 'rss' });
    render();

    await loaded();

    expect(screen.getByRole('link', { name: /apple podcasts/i })).toHaveAttribute(
      'href',
      'https://podcasts.apple.com/podcast/id1296350485',
    );
    expect(screen.getByText(/details from the show's rss feed/i)).toBeInTheDocument();
  });

  it('falls back to the series link when there is no Apple id', async () => {
    seedSeries({ applePodcastsId: null, link: 'https://example.com/show' });
    render();

    await loaded();

    expect(screen.getByRole('link', { name: /website/i })).toHaveAttribute('href', 'https://example.com/show');
  });

  it('subscribes an unsubscribed series', async () => {
    let subscribed = false;
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/episodes`, () => HttpResponse.json([])),
      http.get(`${API_BASE}/podcast/series/:id`, ({ params }) =>
        HttpResponse.json(makePodcastSeries({ id: params.id, title: 'The Test Show', isSubscribed: subscribed })),
      ),
      http.post(`${API_BASE}/podcast/series/:id/subscribe`, () => {
        subscribed = true;
        return HttpResponse.json({ message: 'Subscribed' });
      }),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /^subscribe$/i }));

    // The mutation invalidates the series, so the refetch flips the button.
    expect(await screen.findByRole('button', { name: /^unsubscribe$/i })).toBeInTheDocument();
    expect(screen.getByText(/subscribed!/i)).toBeInTheDocument();
  });

  it('unsubscribes a subscribed series', async () => {
    let called = false;
    seedSeries({ isSubscribed: true });
    server.use(
      http.post(`${API_BASE}/podcast/series/:id/unsubscribe`, () => {
        called = true;
        return HttpResponse.json({ message: 'Unsubscribed' });
      }),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /^unsubscribe$/i }));

    await waitFor(() => expect(called).toBe(true));
    expect(await screen.findByText(/unsubscribed/i)).toBeInTheDocument();
  });

  it('reports the sync result: new episodes, the backlog, and any warning', async () => {
    seedSeries();
    server.use(
      http.post(`${API_BASE}/podcast/series/:id/sync`, () =>
        HttpResponse.json(makeSyncResult({ createdCount: 50, backlogCount: 130 })),
      ),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /^sync$/i }));

    expect(await screen.findByText(/50 new episodes added/i)).toBeInTheDocument();
    expect(screen.getByText(/130 older episodes are available in all episodes/i)).toBeInTheDocument();
  });

  it('explains on hover that a first sync adds the 25 newest episodes', async () => {
    seedSeries({ lastSyncDate: null });
    const { user } = render();

    await loaded();
    await user.hover(screen.getByRole('button', { name: /^sync$/i }));

    expect(await screen.findByRole('tooltip')).toHaveTextContent(/adds the 25 newest episodes to your library/i);
  });

  it('explains on hover that a later sync adds only what is new', async () => {
    seedSeries({ lastSyncDate: '2026-09-01T10:00:00Z' });
    const { user } = render();

    await loaded();
    await user.hover(screen.getByRole('button', { name: /^sync$/i }));

    expect(await screen.findByRole('tooltip')).toHaveTextContent(/released since the last sync/i);
  });

  it('shows the result body\'s message when a sync fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    seedSeries();
    server.use(
      http.post(`${API_BASE}/podcast/series/:id/sync`, () =>
        HttpResponse.json(
          makeSyncResult({ success: false, errorMessage: 'The feed could not be read.' }),
          { status: 500 },
        ),
      ),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /^sync$/i }));

    expect(await screen.findByText('The feed could not be read.')).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('offers "Enrich now" for a series that was never filled, and calls enrich without force', async () => {
    let force;
    seedSeries({ enrichedAt: null });
    server.use(
      http.post(`${API_BASE}/podcast/series/:id/enrich`, ({ request }) => {
        force = new URL(request.url).searchParams.get('force');
        return HttpResponse.json({ success: true, operation: 'podcast-enrichment', enrichedCount: 1 });
      }),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /enrich now/i }));

    expect(await screen.findByText(/details updated from the show's feed/i)).toBeInTheDocument();
    expect(force).toBe('false');
  });

  it('confirms before a forced refresh of an already-filled series', async () => {
    let force;
    seedSeries({ enrichedAt: '2024-01-15T10:00:00Z' });
    server.use(
      http.post(`${API_BASE}/podcast/series/:id/enrich`, ({ request }) => {
        force = new URL(request.url).searchParams.get('force');
        return HttpResponse.json({ success: true, operation: 'podcast-enrichment', unchangedCount: 1 });
      }),
    );
    const { user } = render();

    await loaded();
    expect(screen.queryByRole('button', { name: /enrich now/i })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /refresh from feed/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/overwrites the stored details/i)).toBeInTheDocument();
    expect(force).toBeUndefined();

    await user.click(within(dialog).getByRole('button', { name: /^refresh$/i }));

    expect(await screen.findByText(/already up to date/i)).toBeInTheDocument();
    expect(force).toBe('true');
  });

  it('opens the feed browser from "All Episodes"', async () => {
    seedSeries();
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, () =>
        HttpResponse.json(
          makeFeedEpisodesPage({ feedItemCount: 1, items: [makeFeedEpisodeItem({ title: 'From The Feed' })] }),
        ),
      ),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /all episodes/i }));

    const dialog = await screen.findByRole('dialog');
    expect(await within(dialog).findByText('From The Feed')).toBeInTheDocument();
  });

  it('opens the delete confirmation dialog and cancels it', async () => {
    seedSeries();
    const { user } = render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Show' })).toBeInTheDocument(),
    );

    await user.click(screen.getByRole('button', { name: /^delete$/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/delete series\?/i)).toBeInTheDocument();
    expect(within(dialog).getByText(/this will remove "the test show"/i)).toBeInTheDocument();

    await user.click(within(dialog).getByRole('button', { name: /cancel/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('confirms the delete instead of flashing "not found" once the series leaves the cache', async () => {
    seedSeries();
    server.use(
      http.delete(`${API_BASE}/podcast/series/:id`, () => new HttpResponse(null, { status: 204 })),
    );
    const { user } = render();

    await loaded();
    await user.click(screen.getByRole('button', { name: /^delete$/i }));
    await user.click(await screen.findByRole('button', { name: /delete forever/i }));

    expect(await screen.findByText('Podcast series deleted')).toBeInTheDocument();
    expect(screen.queryByText(/podcast series not found/i)).not.toBeInTheDocument();
  });
});
