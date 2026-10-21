import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import YouTubeAccountCard from './YouTubeAccountCard';

vi.setConfig({ testTimeout: 30000 });

const status = (overrides = {}) =>
  http.get(`${API_BASE}/youtube/auth/status`, () =>
    HttpResponse.json({ configured: true, connected: false, ...overrides }));

const renderCard = (route = '/import-media') => {
  const onSnackbar = vi.fn();
  const goToUrl = vi.fn();
  const utils = renderWithProviders(<YouTubeAccountCard onSnackbar={onSnackbar} goToUrl={goToUrl} />, { route });
  return { ...utils, onSnackbar, goToUrl };
};

describe('YouTubeAccountCard', () => {
  it('renders nothing when the server has no YouTube OAuth configuration', async () => {
    // The default handler answers configured: false.
    renderCard();

    await waitFor(() => expect(screen.queryByTestId('youtube-account-card')).not.toBeInTheDocument());
    expect(screen.queryByRole('button', { name: /connect youtube/i })).not.toBeInTheDocument();
  });

  it('sends the browser to the Google consent URL when Connect is pressed', async () => {
    server.use(
      status(),
      http.post(`${API_BASE}/youtube/auth/start`, () =>
        HttpResponse.json({ authUrl: 'https://accounts.google.com/o/oauth2/v2/auth?state=abc' })),
    );
    const { user, goToUrl } = renderCard();

    await user.click(await screen.findByRole('button', { name: /connect youtube/i }));

    await waitFor(() => expect(goToUrl).toHaveBeenCalledWith('https://accounts.google.com/o/oauth2/v2/auth?state=abc'));
  });

  it('reports a successful return from Google', async () => {
    server.use(status({ connected: true, channelTitle: 'My Channel', reconnectBy: '2099-01-01T00:00:00Z' }),
      http.get(`${API_BASE}/youtube/my-playlists`, () => HttpResponse.json([])));
    const { onSnackbar } = renderCard('/import-media?section=youtube&youtube=connected');

    await waitFor(() => expect(onSnackbar).toHaveBeenCalledWith(expect.objectContaining({
      severity: 'success', message: expect.stringMatching(/connected/i),
    })));
  });

  it('lists the account playlists, links imported ones and imports the others', async () => {
    let imported = false;
    let synced = false;
    server.use(
      status({ connected: true, channelTitle: 'My Channel', reconnectBy: '2099-01-01T00:00:00Z' }),
      http.get(`${API_BASE}/youtube/my-playlists`, () => HttpResponse.json([
        { playlistId: 'PL-old', title: 'Already here', privacyStatus: 'public', itemCount: 3, importedId: 'mmv-1' },
        { playlistId: 'PL-new', title: 'Secret list', privacyStatus: 'private', itemCount: 4, importedId: null },
      ])),
      http.post(`${API_BASE}/youtubeplaylist/import/:externalId`, ({ params }) => {
        imported = params.externalId === 'PL-new';
        return HttpResponse.json({ id: 'mmv-2', title: 'Secret list' }, { status: 201 });
      }),
      http.post(`${API_BASE}/youtubeplaylist/:id/sync`, ({ params }) => {
        synced = params.id === 'mmv-2';
        return HttpResponse.json({ success: true, createdCount: 4, linkedCount: 0, unlinkedCount: 0, videoCount: 4 });
      }),
    );
    const { user, onSnackbar } = renderCard();

    expect(await screen.findByText(/connected as my channel/i)).toBeInTheDocument();
    const table = await screen.findByRole('table', { name: /my youtube playlists/i });
    const oldRow = within(table).getByText('Already here').closest('tr');
    expect(within(oldRow).getByRole('link', { name: /in library/i })).toHaveAttribute('href', '/youtube-playlist/mmv-1');

    const newRow = within(table).getByText('Secret list').closest('tr');
    expect(within(newRow).getByText('private')).toBeInTheDocument();
    await user.click(within(newRow).getByRole('button', { name: /^import$/i }));

    await waitFor(() => expect(onSnackbar).toHaveBeenCalledWith(expect.objectContaining({
      severity: 'success', message: expect.stringMatching(/4 new videos/),
    })));
    expect(imported).toBe(true);
    expect(synced).toBe(true);
  });
});
