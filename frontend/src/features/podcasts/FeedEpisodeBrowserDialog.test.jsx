import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeFeedEpisodeItem, makeFeedEpisodesPage, makePodcastEpisode } from '@/test/factories/podcast';
import FeedEpisodeBrowserDialog from './FeedEpisodeBrowserDialog';

// FeedEpisodeBrowserDialog pages through GET /podcast/series/:id/feed-episodes
// (offset/limit; refresh=true makes the server re-read the feed) and imports a
// row with POST /podcast/episodes/from-feed (201 new, 200 already stored). It
// fetches nothing while closed. After an import it refetches the loaded pages,
// which is how a row flips from "import" to "in your library".
vi.setConfig({ testTimeout: 30000 });

const SERIES_ID = 'podcast-series-1';

const renderDialog = (props = {}) => {
  const onSnackbar = vi.fn();
  const onClose = vi.fn();
  return {
    onSnackbar,
    onClose,
    ...renderWithProviders(
      <FeedEpisodeBrowserDialog open seriesId={SERIES_ID} onClose={onClose} onSnackbar={onSnackbar} {...props} />,
    ),
  };
};

const items = (count, start = 1) =>
  Array.from({ length: count }, (_, i) =>
    makeFeedEpisodeItem({ guid: `guid-${start + i}`, title: `Episode ${start + i}` }),
  );

describe('FeedEpisodeBrowserDialog', () => {
  it('does not read the feed while closed', () => {
    let called = false;
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, () => {
        called = true;
        return HttpResponse.json(makeFeedEpisodesPage());
      }),
    );

    renderDialog({ open: false });

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(called).toBe(false);
  });

  it('lists the first page with date and duration, and loads the next page by offset', async () => {
    const offsets = [];
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, ({ request }) => {
        const offset = Number(new URL(request.url).searchParams.get('offset'));
        offsets.push(offset);
        return HttpResponse.json(
          makeFeedEpisodesPage({
            feedItemCount: 25,
            offset,
            items: offset === 0 ? items(20) : items(5, 21),
          }),
        );
      }),
    );

    const { user } = renderDialog();

    expect(await screen.findByText('Episode 1')).toBeInTheDocument();
    expect(screen.getAllByText('30:00').length).toBe(20);
    expect(screen.getByText(/showing 20 of 25 episodes/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /load 20 more/i }));

    expect(await screen.findByText('Episode 25')).toBeInTheDocument();
    expect(screen.getByText(/showing 25 of 25 episodes/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /load 20 more/i })).not.toBeInTheDocument();
    expect(offsets).toEqual([0, 20]);
  });

  it('imports a row by its guid, then shows it as in the library', async () => {
    let captured;
    let imported = false;
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, () =>
        HttpResponse.json(
          makeFeedEpisodesPage({
            feedItemCount: 1,
            items: [
              makeFeedEpisodeItem({
                guid: 'guid-1',
                title: 'Episode 1',
                existingEpisodeId: imported ? 'ep-new' : null,
              }),
            ],
          }),
        ),
      ),
      http.post(`${API_BASE}/podcast/episodes/from-feed`, async ({ request }) => {
        captured = await request.json();
        imported = true;
        return HttpResponse.json(makePodcastEpisode({ id: 'ep-new', title: 'Episode 1' }), { status: 201 });
      }),
    );

    const { user, onSnackbar } = renderDialog();

    await user.click(await screen.findByRole('button', { name: /import episode 1/i }));

    expect(await screen.findByRole('button', { name: /view episode 1/i })).toBeInTheDocument();
    expect(captured).toEqual({ seriesId: SERIES_ID, guid: 'guid-1' });
    expect(onSnackbar).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('falls back to the audio URL for a feed item with no guid', async () => {
    let captured;
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, () =>
        HttpResponse.json(
          makeFeedEpisodesPage({
            feedItemCount: 1,
            items: [makeFeedEpisodeItem({ guid: null, title: 'No Guid', audioUrl: 'https://example.com/a.mp3' })],
          }),
        ),
      ),
      http.post(`${API_BASE}/podcast/episodes/from-feed`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json(makePodcastEpisode(), { status: 201 });
      }),
    );

    const { user } = renderDialog();
    await user.click(await screen.findByRole('button', { name: /import no guid/i }));

    await waitFor(() => expect(captured).toEqual({ seriesId: SERIES_ID, audioUrl: 'https://example.com/a.mp3' }));
  });

  it('offers no import for an item that cannot be identified', async () => {
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, () =>
        HttpResponse.json(
          makeFeedEpisodesPage({
            feedItemCount: 1,
            items: [makeFeedEpisodeItem({ title: 'Broken Item', importable: false })],
          }),
        ),
      ),
    );

    renderDialog();

    const row = (await screen.findByText('Broken Item')).closest('tr');
    expect(within(row).queryByRole('button')).not.toBeInTheDocument();
  });

  it('re-reads the feed with refresh=true and starts again from the first page', async () => {
    const requests = [];
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, ({ request }) => {
        const params = new URL(request.url).searchParams;
        requests.push({ offset: params.get('offset'), refresh: params.get('refresh') });
        const refreshed = requests.some((r) => r.refresh === 'true');
        return HttpResponse.json(
          makeFeedEpisodesPage({
            feedItemCount: 1,
            items: [makeFeedEpisodeItem({ guid: 'g', title: refreshed ? 'Brand New Episode' : 'Old Episode' })],
          }),
        );
      }),
    );

    const { user } = renderDialog();
    await screen.findByText('Old Episode');

    await user.click(screen.getByRole('button', { name: /refresh feed/i }));

    expect(await screen.findByText('Brand New Episode')).toBeInTheDocument();
    expect(requests.filter((r) => r.refresh === 'true')).toHaveLength(1);
  });

  it('shows the server message when the feed cannot be read (502)', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/podcast/series/:id/feed-episodes`, () =>
        HttpResponse.json({ error: 'The feed could not be read.' }, { status: 502 }),
      ),
    );

    renderDialog();

    expect(await screen.findByText('The feed could not be read.')).toBeInTheDocument();
    consoleError.mockRestore();
  });
});
