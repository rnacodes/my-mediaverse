/**
 * Podcast factories. Series and episodes are distinct shapes; both are
 * media-typed 'Podcast' but differ by `podcastType`.
 */

let seq = 0;
const nextId = (prefix) => `${prefix}-${(seq += 1)}`;

export const makePodcastSeries = (overrides = {}) => ({
  id: nextId('podcast-series'),
  title: 'Test Podcast Series',
  mediaType: 'Podcast',
  podcastType: 'Series',
  status: 'Uncharted',
  publisher: 'Test Network',
  description: 'A test podcast series.',
  thumbnailUrl: 'https://example.com/podcast-thumb.jpg',
  rssUrl: 'https://example.com/feed.rss',
  dateAdded: '2024-01-15T10:00:00Z',
  topics: [],
  genres: [],
  episodes: [],
  ...overrides,
});

export const makePodcastEpisode = (overrides = {}) => ({
  id: nextId('podcast-episode'),
  title: 'Test Podcast Episode',
  mediaType: 'Podcast',
  podcastType: 'Episode',
  status: 'Uncharted',
  seriesId: 'podcast-series-1',
  publisher: 'Test Network',
  description: 'A test podcast episode.',
  thumbnailUrl: 'https://example.com/episode-thumb.jpg',
  durationInSeconds: 1800,
  dateAdded: '2024-01-15T10:00:00Z',
  topics: [],
  genres: [],
  ...overrides,
});
