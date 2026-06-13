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
    ...overrides,
  });

export const makeTvShow = (overrides = {}) =>
  makeMedia({
    mediaType: 'TVShow',
    title: 'Test TV Show',
    creator: 'Test Creator',
    seasons: 3,
    episodes: 30,
    tmdbRating: 8.1,
    ...overrides,
  });

export const makeVideo = (overrides = {}) =>
  makeMedia({
    mediaType: 'Video',
    title: 'Test Video',
    platform: 'YouTube',
    channelName: 'Test Channel',
    lengthInSeconds: 3600,
    videoType: 'Series',
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
