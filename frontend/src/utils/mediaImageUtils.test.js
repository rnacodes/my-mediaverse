import { describe, it, expect } from 'vitest';
import {
  getAspectRatio,
  getAspectRatioPadding,
  getObjectFit,
  getPlaceholderImage,
  resolveMediaImage,
} from './mediaImageUtils';

describe('getAspectRatio', () => {
  it('returns 2/3 for portrait media types', () => {
    expect(getAspectRatio('Book')).toBe('2/3');
    expect(getAspectRatio('Movie')).toBe('2/3');
    expect(getAspectRatio('TVShow')).toBe('2/3');
  });

  it('returns 16/9 for landscape media types', () => {
    expect(getAspectRatio('Video')).toBe('16/9');
    expect(getAspectRatio('Playlist')).toBe('16/9');
    expect(getAspectRatio('Article')).toBe('16/9');
    expect(getAspectRatio('Website')).toBe('16/9');
  });

  it('returns 1/1 for square media types', () => {
    expect(getAspectRatio('Podcast')).toBe('1/1');
    expect(getAspectRatio('Channel')).toBe('1/1');
  });

  it('returns 1/1 for unknown types', () => {
    expect(getAspectRatio('Unknown')).toBe('1/1');
    expect(getAspectRatio(undefined)).toBe('1/1');
    expect(getAspectRatio(null)).toBe('1/1');
  });
});

describe('getAspectRatioPadding', () => {
  it('returns 150% for portrait media types', () => {
    expect(getAspectRatioPadding('Book')).toBe('150%');
    expect(getAspectRatioPadding('Movie')).toBe('150%');
    expect(getAspectRatioPadding('TVShow')).toBe('150%');
  });

  it('returns 56.25% for landscape media types', () => {
    expect(getAspectRatioPadding('Video')).toBe('56.25%');
    expect(getAspectRatioPadding('Playlist')).toBe('56.25%');
    expect(getAspectRatioPadding('Article')).toBe('56.25%');
    expect(getAspectRatioPadding('Website')).toBe('56.25%');
  });

  it('returns 100% for square media types', () => {
    expect(getAspectRatioPadding('Podcast')).toBe('100%');
    expect(getAspectRatioPadding('Channel')).toBe('100%');
  });

  it('returns 100% for unknown types', () => {
    expect(getAspectRatioPadding('Unknown')).toBe('100%');
    expect(getAspectRatioPadding(undefined)).toBe('100%');
  });
});

describe('getObjectFit', () => {
  it('returns contain for all media types', () => {
    expect(getObjectFit('Book')).toBe('contain');
    expect(getObjectFit('Movie')).toBe('contain');
    expect(getObjectFit('Video')).toBe('contain');
    expect(getObjectFit('Podcast')).toBe('contain');
    expect(getObjectFit(undefined)).toBe('contain');
  });
});

// In the test environment Vite resolves SVG imports to their asset path, so we
// assert on the resolved filename rather than a data URI.
describe('getPlaceholderImage', () => {
  it.each([
    ['Book', 'book.svg'],
    ['Movie', 'movie.svg'],
    ['TVShow', 'tvshow.svg'],
    ['Video', 'video.svg'],
    ['Channel', 'channel.svg'],
    ['Playlist', 'playlist.svg'],
    ['Podcast', 'podcast.svg'],
    ['Article', 'article.svg'],
    ['Website', 'website.svg'],
    ['Mixlist', 'mixlist.svg'],
  ])('returns the %s placeholder', (mediaType, expectedAsset) => {
    expect(getPlaceholderImage(mediaType)).toContain(expectedAsset);
  });

  it('falls back to the generic placeholder for an unknown type', () => {
    expect(getPlaceholderImage('Sculpture')).toContain('default.svg');
  });

  it('falls back to the generic placeholder when type is missing', () => {
    expect(getPlaceholderImage(undefined)).toContain('default.svg');
  });
});

describe('resolveMediaImage', () => {
  it('returns the stored thumbnail when present', () => {
    const item = { mediaType: 'Book', thumbnail: 'https://example.com/cover.jpg' };
    expect(resolveMediaImage(item)).toBe('https://example.com/cover.jpg');
  });

  it('returns the per-type placeholder when the thumbnail is missing', () => {
    expect(resolveMediaImage({ mediaType: 'Movie', thumbnail: null })).toContain('movie.svg');
    expect(resolveMediaImage({ mediaType: 'Podcast' })).toContain('podcast.svg');
  });

  it('ignores legacy thumbnailUrl/imageUrl fields (canonical `thumbnail` only)', () => {
    const item = {
      mediaType: 'Book',
      thumbnailUrl: 'https://example.com/legacy.jpg',
      imageUrl: 'https://example.com/legacy2.jpg',
    };
    expect(resolveMediaImage(item)).toContain('book.svg');
  });

  it('uses the type hint for items without a mediaType (e.g. mixlists)', () => {
    expect(resolveMediaImage({ thumbnail: null }, 'Mixlist')).toContain('mixlist.svg');
  });

  it('prefers the stored thumbnail over the type hint', () => {
    const mixlist = { thumbnail: 'https://example.com/mix.jpg' };
    expect(resolveMediaImage(mixlist, 'Mixlist')).toBe('https://example.com/mix.jpg');
  });

  it('returns a placeholder (no throw) for a null item', () => {
    expect(resolveMediaImage(null)).toContain('default.svg');
    expect(resolveMediaImage(null, 'Article')).toContain('article.svg');
  });
});
