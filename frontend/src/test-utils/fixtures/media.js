/**
 * Factory functions for creating test fixture data.
 * Each function returns a complete object with sensible defaults, overridable via spread.
 */

export const createMediaItem = (overrides = {}) => ({
  id: 'media-1',
  title: 'Test Media Item',
  mediaType: 'Book',
  status: 'Uncharted',
  dateAdded: '2024-01-15T10:00:00Z',
  description: 'A test media item',
  thumbnail: 'https://example.com/thumb.jpg',
  link: null,
  notes: null,
  rating: null,
  topics: [],
  genres: [],
  mixlistIds: [],
  ...overrides,
});

export const createBook = (overrides = {}) => createMediaItem({
  mediaType: 'Book',
  author: 'Test Author',
  isbn: '1234567890',
  pageCount: 300,
  publisher: 'Test Publisher',
  ...overrides,
});

export const createVideo = (overrides = {}) => createMediaItem({
  mediaType: 'Video',
  platform: 'YouTube',
  channelName: 'Test Channel',
  lengthInSeconds: 3600,
  videoType: 'Series',
  link: 'https://youtube.com/watch?v=test',
  thumbnail: 'https://example.com/video-thumb.jpg',
  ...overrides,
});

export const createMovie = (overrides = {}) => createMediaItem({
  mediaType: 'Movie',
  director: 'Test Director',
  releaseYear: 2023,
  lengthInMinutes: 120,
  ...overrides,
});

export const createTvShow = (overrides = {}) => createMediaItem({
  mediaType: 'TVShow',
  seasons: 3,
  episodes: 30,
  ...overrides,
});

export const createArticle = (overrides = {}) => createMediaItem({
  mediaType: 'Article',
  author: 'Test Author',
  link: 'https://example.com/article',
  ...overrides,
});

export const createMixlist = (overrides = {}) => ({
  id: 'mixlist-1',
  name: 'Test Mixlist',
  description: 'A test mixlist',
  thumbnail: 'https://example.com/mixlist-thumb.jpg',
  mediaItems: [],
  ...overrides,
});

export const createHighlight = (overrides = {}) => ({
  id: 'highlight-1',
  text: 'This is a test highlight.',
  note: null,
  highlightedAt: '2024-01-16T12:00:00Z',
  location: 100,
  tags: [],
  url: 'https://readwise.io/highlights/1',
  ...overrides,
});
