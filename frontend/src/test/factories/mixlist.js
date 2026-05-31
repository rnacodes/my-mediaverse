/**
 * Mixlist factory. Returns a plausible default mixlist merged with `overrides`.
 */

let seq = 0;
const nextId = () => `mixlist-${(seq += 1)}`;

export const makeMixlist = (overrides = {}) => ({
  id: nextId(),
  name: 'Test Mixlist',
  description: 'A test mixlist.',
  thumbnail: 'https://example.com/mixlist-thumb.jpg',
  dateCreated: '2024-01-15T10:00:00Z',
  mediaItems: [],
  ...overrides,
});
