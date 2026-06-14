/**
 * YouTube factories: channels, playlists, videos.
 */

let seq = 0;
const nextId = (prefix) => `${prefix}-${(seq += 1)}`;

export const makeYouTubeChannel = (overrides = {}) => ({
  id: nextId('yt-channel'),
  title: 'Test Channel',
  mediaType: 'Channel',
  externalId: 'UC_test_channel',
  description: 'A test YouTube channel.',
  thumbnail: 'https://example.com/channel-thumb.jpg',
  subscriberCount: 12345,
  videoCount: 42,
  dateAdded: '2024-01-15T10:00:00Z',
  videos: [],
  ...overrides,
});

export const makeYouTubePlaylist = (overrides = {}) => ({
  id: nextId('yt-playlist'),
  title: 'Test Playlist',
  mediaType: 'Playlist',
  externalId: 'PL_test_playlist',
  description: 'A test YouTube playlist.',
  thumbnail: 'https://example.com/playlist-thumb.jpg',
  channelName: 'Test Channel',
  itemCount: 10,
  dateAdded: '2024-01-15T10:00:00Z',
  videos: [],
  ...overrides,
});

export const makeYouTubeVideo = (overrides = {}) => ({
  id: nextId('yt-video'),
  title: 'Test Video',
  mediaType: 'Video',
  platform: 'YouTube',
  externalId: 'vid_test',
  channelName: 'Test Channel',
  description: 'A test YouTube video.',
  thumbnail: 'https://example.com/video-thumb.jpg',
  lengthInSeconds: 600,
  link: 'https://youtube.com/watch?v=vid_test',
  dateAdded: '2024-01-15T10:00:00Z',
  ...overrides,
});
