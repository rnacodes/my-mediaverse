import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeYouTubePlaylist, makeYouTubeVideo } from '@/test/factories/youtube';
import YouTubePlaylistProfile from './YouTubePlaylistProfile';

// YouTubePlaylistProfile is mounted under /youtube-playlist/:id so useParams() resolves.
// On the success path it fetches: GET /youtubeplaylist/:id?includeVideos=true (the
// managed playlist entity) and GET /mixlist (default handler). When the playlist
// response carries no embedded videos, it falls back to GET /youtubeplaylist/:id/videos
// — so the seed always registers that endpoint too. Like the other profile pages, a
// failed GET /youtubeplaylist/:id leaves `playlist` null and renders the "YouTube
// playlist not found" Alert.
const PLAYLIST_ID = 'yt-playlist-1';
const render = () =>
  renderWithProviders(<YouTubePlaylistProfile />, {
    route: `/youtube-playlist/${PLAYLIST_ID}`,
    path: '/youtube-playlist/:id',
  });

const VIDEOS = [
  makeYouTubeVideo({ id: 'vid-1', title: 'Playlist Video One', position: 0 }),
  makeYouTubeVideo({ id: 'vid-2', title: 'Playlist Video Two', position: 1 }),
];

const seedPlaylist = (overrides = {}, videos = VIDEOS) => {
  server.use(
    http.get(`${API_BASE}/youtubeplaylist/:id/videos`, () => HttpResponse.json(videos)),
    http.get(`${API_BASE}/youtubeplaylist/:id`, ({ params }) =>
      HttpResponse.json(
        makeYouTubePlaylist({
          id: params.id,
          title: 'The Test Playlist',
          playlistExternalId: 'PL_test_playlist',
          videos,
          ...overrides,
        }),
      ),
    ),
  );
};

describe('YouTubePlaylistProfile', () => {
  it('shows the loading spinner before the playlist resolves', async () => {
    seedPlaylist();
    render();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Playlist' })).toBeInTheDocument(),
    );
  });

  it('renders the not-found alert when the playlist request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/youtubeplaylist/:id`, () => new HttpResponse(null, { status: 500 })),
    );

    render();

    await waitFor(() =>
      expect(screen.getByText(/youtube playlist not found/i)).toBeInTheDocument(),
    );

    consoleError.mockRestore();
  });

  it('renders the playlist with its metadata, videos, and YouTube link', async () => {
    seedPlaylist();
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Playlist' })).toBeInTheDocument(),
    );

    // Media-type chip and description come from MediaInfoCard.
    expect(screen.getByText('Playlist')).toBeInTheDocument();
    expect(screen.getByText('A test YouTube playlist.')).toBeInTheDocument();

    // Videos list with its count and each video title.
    expect(screen.getByText(/my videos \(2\)/i)).toBeInTheDocument();
    expect(screen.getByText('Playlist Video One')).toBeInTheDocument();
    expect(screen.getByText('Playlist Video Two')).toBeInTheDocument();

    // Action bar, including the external YouTube link built from playlistExternalId.
    expect(screen.getByRole('button', { name: /^sync$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /all videos/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^delete$/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /youtube/i })).toHaveAttribute(
      'href',
      'https://www.youtube.com/playlist?list=PL_test_playlist',
    );
  });

  it('shows the empty-state message when the playlist has no videos', async () => {
    seedPlaylist({}, []);
    render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Playlist' })).toBeInTheDocument(),
    );

    expect(screen.getByText(/my videos \(0\)/i)).toBeInTheDocument();
    expect(screen.getByText(/no videos imported yet/i)).toBeInTheDocument();
    expect(screen.queryByText('Playlist Video One')).not.toBeInTheDocument();
  });

  it('opens the delete confirmation dialog and cancels it', async () => {
    seedPlaylist();
    const { user } = render();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'The Test Playlist' })).toBeInTheDocument(),
    );

    await user.click(screen.getByRole('button', { name: /^delete$/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/delete playlist\?/i)).toBeInTheDocument();
    expect(within(dialog).getByText(/this will remove "the test playlist"/i)).toBeInTheDocument();

    await user.click(within(dialog).getByRole('button', { name: /cancel/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });
});
