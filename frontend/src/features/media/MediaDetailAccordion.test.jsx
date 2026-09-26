import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeBook, makeWebsite, makeMovie, makeTvShow, makeTvShowEpisode } from '@/test/factories/media';
import { makePodcastEpisode } from '@/test/factories/podcast';
import MediaDetailAccordion from './MediaDetailAccordion';

// Smoke test only - left as a future decomposition

describe('MediaDetailAccordion', () => {
  it('mounts and renders the details heading for its media type', async () => {
    renderWithProviders(<MediaDetailAccordion mediaItem={makeBook()} navigate={() => {}} />);

    expect(await screen.findByRole('heading', { name: 'Book Details' })).toBeInTheDocument();
  });

  describe('book "Fetch Description" button', () => {
    const expandDetails = async (user) => {
      await user.click(screen.getByRole('button', { name: /book details/i }));
    };

    it('is enabled for a book with an author but no ISBN', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeBook({ isbn: null, title: 'Dune', author: 'Frank Herbert' })}
          navigate={() => {}}
        />,
      );

      await expandDetails(user);

      expect(screen.getByRole('button', { name: /fetch description/i })).toBeEnabled();
      expect(screen.queryByText(/requires isbn or title \+ author/i)).not.toBeInTheDocument();
    });

    it('is disabled with a hint when the book has no ISBN and only the placeholder author', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeBook({ isbn: null, title: 'Mystery', author: 'Unknown Author' })}
          navigate={() => {}}
        />,
      );

      await expandDetails(user);

      expect(screen.getByRole('button', { name: /fetch description/i })).toBeDisabled();
      expect(screen.getByText(/requires isbn or title \+ author/i)).toBeInTheDocument();
    });
  });

  describe('website details', () => {
    const expandDetails = async (user) => {
      await user.click(screen.getByRole('button', { name: /website details/i }));
    };

    const site = (overrides = {}) => makeWebsite({
      id: 'site-1',
      title: 'Tech',
      link: 'https://theverge.com/tech',
      domain: 'theverge.com',
      lastCheckedDate: '2026-09-07T00:00:00Z',
      lastHttpStatus: 200,
      waybackUrl: null,
      ...overrides,
    });

    it('shows the link status and leads with the archived copy when the link is broken', async () => {
      server.use(http.get(`${API_BASE}/website/by-domain/:domain`, () => HttpResponse.json([])));
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={site({ lastHttpStatus: 404, waybackUrl: 'https://web.archive.org/web/2024/https://theverge.com/tech' })}
          navigate={() => {}}
        />,
      );

      await expandDetails(user);

      expect(screen.getByTestId('website-link-status')).toHaveTextContent('Broken (404)');
      expect(screen.getByRole('link', { name: /open archived copy/i })).toHaveAttribute('href', 'https://web.archive.org/web/2024/https://theverge.com/tech');
      expect(screen.getByRole('link', { name: /wayback machine/i })).toBeInTheDocument();
      expect(screen.queryByText(/coming soon/i)).not.toBeInTheDocument();
    });

    it('lists other saved pages from the same site', async () => {
      server.use(
        http.get(`${API_BASE}/website/by-domain/:domain`, () => HttpResponse.json([
          makeWebsite({ id: 'site-1', title: 'Tech', link: 'https://theverge.com/tech' }),
          makeWebsite({ id: 'site-2', title: 'Reviews', link: 'https://theverge.com/reviews' }),
        ])),
      );
      const navigate = vi.fn();
      const { user } = renderWithProviders(<MediaDetailAccordion mediaItem={site()} navigate={navigate} />);

      await expandDetails(user);

      const siblings = await screen.findByTestId('website-siblings');
      expect(within(siblings).getByText('Reviews')).toBeInTheDocument();
      expect(within(siblings).queryByText('Tech')).not.toBeInTheDocument();
      await user.click(within(siblings).getByText('Reviews'));
      expect(navigate).toHaveBeenCalledWith('/media/site-2');
    });

    it('refreshes metadata and reports the filled fields', async () => {
      let force;
      server.use(
        http.get(`${API_BASE}/website/by-domain/:domain`, () => HttpResponse.json([])),
        http.post(`${API_BASE}/website/site-1/enrich`, ({ request }) => {
          force = new URL(request.url).searchParams.get('force');
          return HttpResponse.json({ success: true, filledFields: ['description', 'waybackUrl'], lastHttpStatus: 200 });
        }),
      );
      const onWebsiteUpdated = vi.fn();
      const { user } = renderWithProviders(
        <MediaDetailAccordion mediaItem={site()} navigate={() => {}} onWebsiteUpdated={onWebsiteUpdated} />,
      );

      await expandDetails(user);
      await user.click(screen.getByRole('button', { name: /refresh metadata/i }));

      expect(await screen.findByTestId('website-action-result')).toHaveTextContent('Filled in: description, waybackUrl.');
      expect(force).toBe('true');
      expect(onWebsiteUpdated).toHaveBeenCalled();
    });

    it('regenerates the screenshot and reports the outcome', async () => {
      server.use(
        http.get(`${API_BASE}/website/by-domain/:domain`, () => HttpResponse.json([])),
        http.post(`${API_BASE}/website/site-1/screenshot`, () =>
          HttpResponse.json({ success: true, rendered: false, warningMessage: 'No usable screenshot could be rendered for this page.' })),
      );
      const { user } = renderWithProviders(<MediaDetailAccordion mediaItem={site()} navigate={() => {}} />);

      await expandDetails(user);
      await user.click(screen.getByRole('button', { name: /regenerate screenshot/i }));

      expect(await screen.findByTestId('website-action-result')).toHaveTextContent(/no usable screenshot/i);
    });
  });

  describe('podcast episode audio link', () => {
    // Details live inside a collapsed accordion; expand it first, as a visitor would.
    const expandDetails = async (user) => {
      await user.click(screen.getByRole('button', { name: /podcast details/i }));
    };

    it('renders a download link pointing at the raw audio URL when present', async () => {
      const audioLink = 'https://example.com/audio/abc123.mp3';
      const { user } = renderWithProviders(
        <MediaDetailAccordion mediaItem={makePodcastEpisode({ audioLink })} navigate={() => {}} />,
      );

      await expandDetails(user);

      const link = screen.getByRole('link', { name: /click to download audio/i });
      expect(link).toHaveAttribute('href', audioLink);
      expect(link).toHaveAttribute('target', '_blank');
    });

    it('omits the audio link when the episode has no audioLink', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion mediaItem={makePodcastEpisode()} navigate={() => {}} />,
      );

      await expandDetails(user);

      expect(screen.queryByRole('link', { name: /download audio/i })).not.toBeInTheDocument();
    });
  });

  describe('TV episode details', () => {
    it('renders the show link, identifier, air date and duration instead of the show fields', async () => {
      const navigate = vi.fn();
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={{ ...makeTvShowEpisode({ showId: 'show-9', showTitle: 'Chernobyl', airDate: '2019-05-20T00:00:00Z', durationInMinutes: 62 }), isTvEpisode: true }}
          navigate={navigate}
        />,
      );

      await user.click(screen.getByRole('button', { name: /tv episode details/i }));

      expect(screen.getByText(/S1E3 \(Season 1, Episode 3\)/)).toBeInTheDocument();
      expect(screen.getByText('62 minutes')).toBeInTheDocument();
      expect(screen.getByText(new Date('2019-05-20T00:00:00Z').toLocaleDateString())).toBeInTheDocument();
      expect(screen.queryByText(/no specific tvshow details/i)).not.toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Chernobyl' }));
      expect(navigate).toHaveBeenCalledWith('/tv-show/show-9');
    });
  });

  describe('movie and TV show external links', () => {
    it('links a movie to IMDb, TMDB, and a JustWatch search', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeMovie({ title: 'Inception', imdbId: 'tt1375666', tmdbId: 27205 })}
          navigate={() => {}}
        />,
      );

      await user.click(screen.getByRole('button', { name: /movie details/i }));

      expect(screen.getByRole('link', { name: /view on imdb/i })).toHaveAttribute(
        'href',
        'https://www.imdb.com/title/tt1375666/',
      );
      expect(screen.getByRole('link', { name: /view on tmdb/i })).toHaveAttribute(
        'href',
        'https://www.themoviedb.org/movie/27205',
      );
      expect(screen.getByRole('link', { name: /search on justwatch/i })).toHaveAttribute(
        'href',
        'https://www.justwatch.com/us/search?q=Inception',
      );
    });

    it('links a TV show to its TMDB page (shows have no IMDb id)', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeTvShow({ title: 'Chernobyl', tmdbId: 87108 })}
          navigate={() => {}}
        />,
      );

      await user.click(screen.getByRole('button', { name: /tv show details/i }));

      expect(screen.getByRole('link', { name: /view on tmdb/i })).toHaveAttribute(
        'href',
        'https://www.themoviedb.org/tv/87108',
      );
      expect(screen.queryByRole('link', { name: /view on imdb/i })).not.toBeInTheDocument();
    });

    it('omits the id links when the movie has no external ids', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeMovie({ imdbId: null, tmdbId: null })}
          navigate={() => {}}
        />,
      );

      await user.click(screen.getByRole('button', { name: /movie details/i }));

      expect(screen.queryByRole('link', { name: /view on imdb/i })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: /view on tmdb/i })).not.toBeInTheDocument();
    });
  });
});
