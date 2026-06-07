import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makePodcastSeries, makePodcastEpisode } from '@/test/factories/podcast';
import PodcastSeriesProfile from './PodcastSeriesProfile';

// PodcastSeriesProfile is mounted under /podcast-series/:id so useParams() resolves.
// On the success path it fetches: GET /podcast/series/:id (the series), GET
// /podcast/series/:id/episodes (locally imported episodes), and GET /mixlist (default
// handler, for the MixlistCarousel). The real children (MediaInfoCard,
// MediaDetailAccordion, TopicsGenresSection, MixlistCarousel) render but fire no
// requests on mount. Like the other profile pages, error and not-found collapse: a
// failed GET /podcast/series/:id leaves `series` null and renders the "Podcast series
// not found" Alert. The "empty" state is a series with zero imported episodes.
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

  it('renders the ListenNotes link when the series has an external id', async () => {
    seedSeries({ externalId: 'abc123' });
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Show' })).toBeInTheDocument(),
    );

    const link = screen.getByRole('link', { name: /listennotes/i });
    expect(link).toHaveAttribute('href', 'https://www.listennotes.com/podcasts/abc123/');
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
});
