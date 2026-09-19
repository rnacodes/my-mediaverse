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
  thumbnail: 'https://example.com/podcast-thumb.jpg',
  rssFeedUrl: 'https://example.com/feed.rss',
  applePodcastsId: null,
  metadataSource: 'rss',
  enrichedAt: '2024-01-15T10:00:00Z',
  isSubscribed: false,
  lastSyncDate: null,
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
  thumbnail: 'https://example.com/episode-thumb.jpg',
  durationInSeconds: 1800,
  dateAdded: '2024-01-15T10:00:00Z',
  topics: [],
  genres: [],
  ...overrides,
});

/** One row of GET /podcast/directory/search. */
export const makeDirectoryResult = (overrides = {}) => ({
  title: 'Test Directory Show',
  publisher: 'Test Network',
  feedUrl: 'https://example.com/directory-feed.rss',
  applePodcastsId: '1000000001',
  podcastIndexId: null,
  artworkUrl: 'https://example.com/artwork.jpg',
  genres: ['technology'],
  episodeCount: 42,
  storeUrl: 'https://podcasts.apple.com/us/podcast/id1000000001',
  latestReleaseDate: '2024-01-10T00:00:00Z',
  source: 'apple',
  existingSeriesId: null,
  ...overrides,
});

/** One item of a GET /podcast/series/{id}/feed-episodes page. */
export const makeFeedEpisodeItem = (overrides = {}) => {
  const id = nextId('feed-episode');
  return {
    guid: id,
    title: `Feed Episode ${id}`,
    publishedAt: '2024-01-10T00:00:00Z',
    durationSeconds: 1800,
    audioUrl: `https://example.com/audio/${id}.mp3`,
    imageUrl: null,
    episodeNumber: null,
    seasonNumber: null,
    description: 'A feed episode.',
    importable: true,
    existingEpisodeId: null,
    ...overrides,
  };
};

export const makeFeedEpisodesPage = (overrides = {}) => ({
  seriesId: 'podcast-series-1',
  feedItemCount: 0,
  offset: 0,
  limit: 20,
  fetchedAt: '2024-01-15T10:00:00Z',
  items: [],
  ...overrides,
});

/** POST /podcast/series/{id}/sync result (reporting-contract shaped). */
export const makeSyncResult = (overrides = {}) => ({
  success: true,
  operation: 'podcast-episode-sync',
  seriesId: 'podcast-series-1',
  seriesTitle: 'Test Podcast Series',
  createdCount: 0,
  updatedCount: 0,
  skippedCount: 0,
  failedCount: 0,
  ignoredCount: 0,
  backlogCount: 0,
  feedItemCount: 0,
  errors: [],
  errorMessage: null,
  warningMessage: null,
  startedAt: '2024-01-15T10:00:00Z',
  completedAt: '2024-01-15T10:00:01Z',
  lastSyncDate: '2024-01-15T10:00:01Z',
  reindexTriggered: true,
  ...overrides,
});
