/**
 * Note + highlight factories.
 */

let seq = 0;
const nextId = (prefix) => `${prefix}-${(seq += 1)}`;

export const makeNote = (overrides = {}) => ({
  id: nextId('note'),
  title: 'Test Note',
  content: 'This is a test note body.',
  dateCreated: '2024-01-15T10:00:00Z',
  dateModified: '2024-01-15T10:00:00Z',
  topics: [],
  tags: [],
  linkedMediaIds: [],
  ...overrides,
});

export const makeHighlight = (overrides = {}) => ({
  id: nextId('highlight'),
  text: 'This is a test highlight.',
  note: null,
  highlightedAt: '2024-01-16T12:00:00Z',
  location: 100,
  tags: [],
  highlightUrl: 'https://readwise.io/open/1',
  linkedMediaId: null,
  linkedMediaTitle: null,
  ...overrides,
});
