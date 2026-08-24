import { http, HttpResponse } from 'msw';
import { makeMedia, makeBook } from '../factories/media';
import { makeMixlist } from '../factories/mixlist';
import { makePodcastSeries } from '../factories/podcast';
import { makeYouTubeChannel, makeYouTubePlaylist } from '../factories/youtube';
import { makeNote } from '../factories/note';

// Must match VITE_API_URL injected by vitest.config.js. The axios client uses
// this absolute base, so handlers register against absolute URLs.
export const API_BASE = 'http://localhost:5033/api';

/**
 * Happy-path handlers seeded from factories. Error/empty paths are NOT defined
 * here — individual tests opt into them with `server.use(...)`.
 *
 * Auth note: AuthProvider POSTs /auth/refresh on mount. We return a valid
 * session by default so the 401 redirect-to-/login path never fires; tests that
 * want the unauthenticated path override /auth/refresh with a 401 via server.use.
 */
export const handlers = [
  // --- Auth ---
  http.post(`${API_BASE}/auth/refresh`, () =>
    HttpResponse.json({
      token: 'test-access-token',
      username: 'testuser',
      expiresAt: '2099-01-01T00:00:00Z',
    }),
  ),
  http.post(`${API_BASE}/auth/login`, () =>
    HttpResponse.json({
      token: 'test-access-token',
      username: 'testuser',
      expiresAt: '2099-01-01T00:00:00Z',
    }),
  ),
  http.post(`${API_BASE}/auth/logout`, () => new HttpResponse(null, { status: 204 })),

  // --- Media ---
  http.get(`${API_BASE}/media`, () =>
    HttpResponse.json([makeBook(), makeMedia({ mediaType: 'Movie', title: 'Seeded Movie' })]),
  ),
  http.get(`${API_BASE}/media/:id`, ({ params }) =>
    HttpResponse.json(makeMedia({ id: params.id })),
  ),
  http.get(`${API_BASE}/media/by-topic/:topicId`, () => HttpResponse.json([makeBook()])),
  http.get(`${API_BASE}/media/by-genre/:genreId`, () => HttpResponse.json([makeBook()])),

  // --- Mixlists ---
  http.get(`${API_BASE}/mixlist`, () => HttpResponse.json([makeMixlist()])),
  http.get(`${API_BASE}/mixlist/:id`, ({ params }) =>
    HttpResponse.json(makeMixlist({ id: params.id })),
  ),

  // --- Podcasts ---
  http.get(`${API_BASE}/podcast/series`, () => HttpResponse.json([makePodcastSeries()])),
  http.get(`${API_BASE}/podcast/series/:id`, ({ params }) =>
    HttpResponse.json(makePodcastSeries({ id: params.id })),
  ),
  http.get(`${API_BASE}/podcast/series/:seriesId/episodes`, () => HttpResponse.json([])),

  // --- YouTube ---
  http.get(`${API_BASE}/youtube/channels/:id`, ({ params }) =>
    HttpResponse.json(makeYouTubeChannel({ id: params.id })),
  ),
  http.get(`${API_BASE}/youtube/playlists/:id`, ({ params }) =>
    HttpResponse.json(makeYouTubePlaylist({ id: params.id })),
  ),

  // --- Highlights / Notes ---
  http.get(`${API_BASE}/notes`, () => HttpResponse.json([makeNote()])),
  http.get(`${API_BASE}/notes/:id`, ({ params }) =>
    HttpResponse.json(makeNote({ id: params.id })),
  ),

  // --- Topics / Genres ---
  http.get(`${API_BASE}/topics`, () => HttpResponse.json([])),
  http.get(`${API_BASE}/genres`, () => HttpResponse.json([])),

  http.get(`${API_BASE}/search`, ({ request }) => {
    const filter = new URL(request.url).searchParams.get('filter') || '';
    const doc = (id, title, mediaType) => ({
      document: {
        id,
        title,
        media_type: mediaType,
        status: 'Completed',
        rating: 'Like',
        topics: [],
        genres: [],
        date_added: 1700000000,
        description: '',
      },
    });
    const book = doc('ts-book', 'Test Book', 'Book');
    const movie = doc('ts-movie', 'Test Movie', 'Movie');
    let hits = [book, movie];
    if (filter.includes('media_type:=Book')) hits = [book];
    else if (filter.includes('media_type:=Movie')) hits = [movie];
    return HttpResponse.json({ found: hits.length, out_of: 2, page: 1, hits });
  }),
  http.get(`${API_BASE}/search/mixlists`, () =>
    HttpResponse.json({ found: 0, out_of: 0, page: 1, hits: [] }),
  ),
  http.get(`${API_BASE}/search/highlights`, () =>
    HttpResponse.json({ found: 0, out_of: 0, page: 1, hits: [] }),
  ),
  http.get(`${API_BASE}/search/notes`, () =>
    HttpResponse.json({ found: 0, out_of: 0, page: 1, hits: [] }),
  ),
];
