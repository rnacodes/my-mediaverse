/**
 * Media factories. Each returns a plausible default media item merged with
 * `overrides`. Property names are camelCase (API convention); `mediaType` /
 * `status` values are the PascalCase enum strings the components switch on.
 */

let seq = 0;
const nextId = (prefix) => `${prefix}-${(seq += 1)}`;

export const makeMedia = (overrides = {}) => ({
  id: nextId('media'),
  title: 'Test Media Item',
  mediaType: 'Book',
  status: 'Uncharted',
  dateAdded: '2024-01-15T10:00:00Z',
  description: 'A test media item.',
  thumbnail: 'https://example.com/thumb.jpg',
  link: null,
  notes: null,
  rating: null,
  topics: [],
  genres: [],
  mixlistIds: [],
  ...overrides,
});

export const makeBook = (overrides = {}) =>
  makeMedia({
    mediaType: 'Book',
    title: 'Test Book',
    author: 'Test Author',
    isbn: '1234567890',
    pageCount: 300,
    publisher: 'Test Publisher',
    goodreadsRating: 4.2,
    ...overrides,
  });

export const makeMovie = (overrides = {}) =>
  makeMedia({
    mediaType: 'Movie',
    title: 'Test Movie',
    director: 'Test Director',
    releaseYear: 2023,
    runtimeMinutes: 120,
    tmdbRating: 7.8,
    imdbId: 'tt1375666',
    tmdbId: 27205,
    cast: ['Test Lead', 'Test Support'],
    mpaaRating: 'PG-13',
    tagline: 'Your mind is the scene of the crime.',
    ...overrides,
  });

/** A TV show as GET /tvshow/:id returns it (TvShowResponseDto field names). */
export const makeTvShow = (overrides = {}) =>
  makeMedia({
    mediaType: 'TVShow',
    title: 'Test TV Show',
    creator: 'Test Creator',
    cast: ['Test Lead', 'Test Support'],
    firstAirYear: 2019,
    lastAirYear: 2019,
    airYears: '2019',
    numberOfSeasons: 3,
    numberOfEpisodes: 30,
    episodeCount: 30,
    contentRating: 'TV-MA',
    tmdbId: 87108,
    tmdbRating: 8.1,
    tmdbPosterPath: '/poster.jpg',
    tmdbPosterUrl: 'https://image.tmdb.org/t/p/w500/poster.jpg',
    ...overrides,
  });

/** A TV episode as GET /tvshow/episodes/:id returns it (shares the TVShow media type). */
export const makeTvShowEpisode = (overrides = {}) =>
  makeMedia({
    mediaType: 'TVShow',
    title: 'Test Episode',
    showId: 'tvshow-1',
    showTitle: 'Test TV Show',
    seasonNumber: 1,
    episodeNumber: 3,
    episodeIdentifier: 'S1E3',
    airDate: '2019-05-20T00:00:00Z',
    durationInMinutes: 62,
    tmdbEpisodeId: 1700000,
    stillPath: '/still.jpg',
    traktPlays: null,
    traktLastWatchedAt: null,
    ...overrides,
  });

export const makeVideo = (overrides = {}) =>
  makeMedia({
    mediaType: 'Video',
    title: 'Test Video',
    platform: 'YouTube',
    channelName: 'Test Channel',
    lengthInSeconds: 3600,
    link: 'https://youtube.com/watch?v=test',
    ...overrides,
  });

export const makeArticle = (overrides = {}) =>
  makeMedia({
    mediaType: 'Article',
    title: 'Test Article',
    author: 'Test Author',
    publication: 'Test Publication',
    estimatedReadingTimeMinutes: 8,
    wordCount: 1600,
    link: 'https://example.com/article',
    ...overrides,
  });

export const makeWebsite = (overrides = {}) =>
  makeMedia({
    mediaType: 'Website',
    title: 'Test Website',
    link: 'https://example.com',
    ...overrides,
  });
