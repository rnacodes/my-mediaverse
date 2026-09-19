import { describe, it, expect, vi, beforeEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeDirectoryResult, makePodcastSeries } from '@/test/factories/podcast';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import PodcastImportSection from './PodcastImportSection';
import { parseFeedInput } from './parseFeedInput';

// PodcastImportSection finds shows through the podcast directory
// (GET /podcast/directory/search) and imports them from their feed
// (POST /podcast/series/from-feed — 201 created, 200 already in the library).
// Nothing fires on mount. Import success reports through onSnackbar and then
// navigates after a 1.5s timer (the snackbar is asserted; the timed nav is not).
// The MUI-heavy accordion renders slowly on CI-class hardware, so the per-test
// timeout is raised.
vi.setConfig({ testTimeout: 30000 });

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});

const renderExpanded = (onSnackbar = vi.fn()) => ({
  onSnackbar,
  ...renderWithProviders(
    <PodcastImportSection expanded="podcasts" onAccordionChange={() => () => {}} onSnackbar={onSnackbar} />,
    { route: '/import-media' },
  ),
});

const searchFor = async (user, term) => {
  await user.type(screen.getByRole('textbox', { name: /search podcasts/i }), term);
  await user.click(screen.getByRole('button', { name: /^search$/i }));
};

const importResult = (overrides = {}) => ({
  series: makePodcastSeries({ id: 'series-new', title: 'Darknet Diaries' }),
  created: true,
  feedRead: true,
  warningMessage: null,
  ...overrides,
});

beforeEach(() => {
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

describe('parseFeedInput', () => {
  it('reads a bare number as an Apple Podcasts id', () => {
    expect(parseFeedInput(' 1296350485 ')).toEqual({ applePodcastsId: '1296350485' });
  });

  it('pulls the id out of an Apple Podcasts link', () => {
    expect(
      parseFeedInput('https://podcasts.apple.com/us/podcast/darknet-diaries/id1296350485?uo=4'),
    ).toEqual({ applePodcastsId: '1296350485' });
  });

  it('treats any other http(s) URL as a feed URL', () => {
    expect(parseFeedInput('https://podcast.darknetdiaries.com/')).toEqual({
      feedUrl: 'https://podcast.darknetdiaries.com/',
    });
  });

  it('rejects empty text and text that is not a URL or an id', () => {
    expect(parseFeedInput('')).toBeNull();
    expect(parseFeedInput('darknet diaries')).toBeNull();
  });
});

describe('PodcastImportSection', () => {
  it('searches the directory and renders the result with its Apple link and caption', async () => {
    let term;
    server.use(
      http.get(`${API_BASE}/podcast/directory/search`, ({ request }) => {
        term = new URL(request.url).searchParams.get('term');
        return HttpResponse.json([
          makeDirectoryResult({ title: 'Darknet Diaries', publisher: 'Jack Rhysider', episodeCount: 180 }),
        ]);
      }),
    );

    const { user } = renderExpanded();
    await searchFor(user, 'darknet');

    expect(await screen.findByText('Darknet Diaries')).toBeInTheDocument();
    expect(term).toBe('darknet');
    expect(screen.getByText('Jack Rhysider')).toBeInTheDocument();
    expect(screen.getByText('180 episodes')).toBeInTheDocument();
    expect(screen.getByText(/search results from apple podcasts/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /view on apple podcasts/i })).toHaveAttribute(
      'href',
      'https://podcasts.apple.com/us/podcast/id1000000001',
    );
  });

  it('asks for a search term instead of calling the directory with none', async () => {
    const { user } = renderExpanded();

    await user.click(screen.getByRole('button', { name: /^search$/i }));

    expect(await screen.findByText(/please enter a search term/i)).toBeInTheDocument();
  });

  it('offers "View in library" instead of Import for a show that is already stored', async () => {
    server.use(
      http.get(`${API_BASE}/podcast/directory/search`, () =>
        HttpResponse.json([makeDirectoryResult({ existingSeriesId: 'series-7' })]),
      ),
    );

    const { user } = renderExpanded();
    await searchFor(user, 'test');

    expect(await screen.findByText(/in your library/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /view in library/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^import$/i })).not.toBeInTheDocument();
  });

  it('credits Podcast Index on a row it resolved', async () => {
    server.use(
      http.get(`${API_BASE}/podcast/directory/search`, () =>
        HttpResponse.json([makeDirectoryResult({ source: 'podcastindex', podcastIndexId: 743080 })]),
      ),
    );

    const { user } = renderExpanded();
    await searchFor(user, 'test');

    expect(await screen.findByRole('link', { name: /podcast index/i })).toHaveAttribute(
      'href',
      'https://podcastindex.org',
    );
  });

  it('imports a search result from its feed and reports a new import on 201', async () => {
    let captured;
    server.use(
      http.get(`${API_BASE}/podcast/directory/search`, () =>
        HttpResponse.json([
          makeDirectoryResult({
            title: 'Darknet Diaries',
            feedUrl: 'https://podcast.darknetdiaries.com/',
            applePodcastsId: '1296350485',
          }),
        ]),
      ),
      http.post(`${API_BASE}/podcast/series/from-feed`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json(importResult(), { status: 201 });
      }),
    );

    const { user, onSnackbar } = renderExpanded();
    await searchFor(user, 'darknet');
    await screen.findByText('Darknet Diaries');
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    await waitFor(() =>
      expect(onSnackbar).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'success', message: expect.stringMatching(/imported successfully/i) }),
      ),
    );
    expect(captured).toEqual({
      feedUrl: 'https://podcast.darknetdiaries.com/',
      applePodcastsId: '1296350485',
    });
  });

  it('reports "already in your library" when from-feed answers 200', async () => {
    server.use(
      http.post(`${API_BASE}/podcast/series/from-feed`, () =>
        HttpResponse.json(importResult({ created: false }), { status: 200 }),
      ),
    );

    const { user, onSnackbar } = renderExpanded();
    await searchFor(user, 'test');
    await screen.findByText('Test Directory Show');
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    await waitFor(() =>
      expect(onSnackbar).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'info', message: expect.stringMatching(/already in your library/i) }),
      ),
    );
  });

  it('surfaces the warning when the series was created but its feed could not be read', async () => {
    server.use(
      http.post(`${API_BASE}/podcast/series/from-feed`, () =>
        HttpResponse.json(
          importResult({ feedRead: false, warningMessage: 'The feed could not be read; it will be retried later.' }),
          { status: 201 },
        ),
      ),
    );

    const { user, onSnackbar } = renderExpanded();
    await searchFor(user, 'test');
    await screen.findByText('Test Directory Show');
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    await waitFor(() =>
      expect(onSnackbar).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'warning', message: expect.stringMatching(/could not be read/i) }),
      ),
    );
  });

  it('imports from a pasted Apple Podcasts link by its id', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/podcast/series/from-feed`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json(importResult(), { status: 201 });
      }),
    );

    const { user, onSnackbar } = renderExpanded();
    await user.click(screen.getByRole('tab', { name: /paste a feed url/i }));
    await user.type(
      screen.getByRole('textbox', { name: /feed url or apple podcasts link/i }),
      'https://podcasts.apple.com/us/podcast/darknet-diaries/id1296350485',
    );
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    await waitFor(() => expect(onSnackbar).toHaveBeenCalled());
    expect(captured).toEqual({ applePodcastsId: '1296350485' });
  });

  it('rejects pasted text that is neither a URL nor an id without calling the API', async () => {
    const { user } = renderExpanded();
    await user.click(screen.getByRole('tab', { name: /paste a feed url/i }));
    await user.type(screen.getByRole('textbox', { name: /feed url or apple podcasts link/i }), 'not a feed');
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    expect(await screen.findByText(/enter a feed url, an apple podcasts link/i)).toBeInTheDocument();
  });

  it('shows the server message when the directory is rate limited (503)', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.get(`${API_BASE}/podcast/directory/search`, () =>
        HttpResponse.json({ error: 'The podcast directory is busy; try again in a minute.' }, { status: 503 }),
      ),
    );

    const { user } = renderExpanded();
    await searchFor(user, 'test');

    expect(await screen.findByText(/directory is busy; try again in a minute/i)).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('disables importing while the demo blocks writes', async () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);

    const { user } = renderExpanded();
    await searchFor(user, 'test');
    await screen.findByText('Test Directory Show');

    expect(screen.getByRole('button', { name: /^import$/i })).toBeDisabled();
  });
});
