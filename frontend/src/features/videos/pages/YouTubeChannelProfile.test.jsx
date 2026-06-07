import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeYouTubeChannel, makeYouTubeVideo } from '@/test/factories/youtube';
import YouTubeChannelProfile from './YouTubeChannelProfile';

// YouTubeChannelProfile is mounted under /youtube-channel/:id so useParams() resolves.
// On the success path it fetches: GET /youtubechannel/:id (the managed channel entity),
// GET /youtubechannel/:id/videos (locally imported videos), and GET /mixlist (default
// handler, for the MixlistCarousel). The real children render but fire no requests on
// mount. NOTE: the default handlers.js entries register /youtube/channels/:id — that's
// the *external* YouTube Data API, a different path from the managed /youtubechannel
// endpoints this page uses, so every test seeds its own. Error and not-found collapse:
// a failed GET /youtubechannel/:id leaves `channel` null and renders the "YouTube
// channel not found" Alert.
const CHANNEL_ID = 'yt-channel-1';
const render = () =>
  renderWithProviders(<YouTubeChannelProfile />, {
    route: `/youtube-channel/${CHANNEL_ID}`,
    path: '/youtube-channel/:id',
  });

const VIDEOS = [
  makeYouTubeVideo({ id: 'vid-1', title: 'My First Upload' }),
  makeYouTubeVideo({ id: 'vid-2', title: 'My Second Upload' }),
];

const seedChannel = (overrides = {}, videos = VIDEOS) => {
  server.use(
    http.get(`${API_BASE}/youtubechannel/:id/videos`, () => HttpResponse.json(videos)),
    http.get(`${API_BASE}/youtubechannel/:id`, ({ params }) =>
      HttpResponse.json(
        makeYouTubeChannel({
          id: params.id,
          title: 'The Test Channel',
          channelExternalId: 'UC_test_channel',
          ...overrides,
        }),
      ),
    ),
  );
};

describe('YouTubeChannelProfile', () => {
  it('shows the loading spinner before the channel resolves', async () => {
    seedChannel();
    render();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Channel' })).toBeInTheDocument(),
    );
  });

  it('renders the not-found alert when the channel request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/youtubechannel/:id`, () => new HttpResponse(null, { status: 500 })),
      http.get(`${API_BASE}/youtubechannel/:id/videos`, () => HttpResponse.json([])),
    );

    render();

    await waitFor(() =>
      expect(screen.getByText(/youtube channel not found/i)).toBeInTheDocument(),
    );

    consoleError.mockRestore();
  });

  it('renders the channel with its metadata, imported videos, and YouTube link', async () => {
    seedChannel();
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Channel' })).toBeInTheDocument(),
    );

    // Media-type chip and description come from MediaInfoCard.
    expect(screen.getByText('Channel')).toBeInTheDocument();
    expect(screen.getByText('A test YouTube channel.')).toBeInTheDocument();

    // Imported-videos list with its count and each video title.
    expect(screen.getByText(/my videos \(2\)/i)).toBeInTheDocument();
    expect(screen.getByText('My First Upload')).toBeInTheDocument();
    expect(screen.getByText('My Second Upload')).toBeInTheDocument();

    // Action bar, including the external YouTube link built from channelExternalId.
    expect(screen.getByRole('button', { name: /^sync$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /all videos/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^delete$/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /youtube/i })).toHaveAttribute(
      'href',
      'https://www.youtube.com/channel/UC_test_channel',
    );
  });

  it('shows the empty-state message when no videos are imported', async () => {
    seedChannel({}, []);
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Channel' })).toBeInTheDocument(),
    );

    expect(screen.getByText(/my videos \(0\)/i)).toBeInTheDocument();
    expect(screen.getByText(/no videos imported yet/i)).toBeInTheDocument();
    expect(screen.queryByText('My First Upload')).not.toBeInTheDocument();
  });

  it('opens the delete confirmation dialog and cancels it', async () => {
    seedChannel();
    const { user } = render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Channel' })).toBeInTheDocument(),
    );

    await user.click(screen.getByRole('button', { name: /^delete$/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/delete channel\?/i)).toBeInTheDocument();
    expect(within(dialog).getByText(/this will remove "the test channel"/i)).toBeInTheDocument();

    await user.click(within(dialog).getByRole('button', { name: /cancel/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });
});
