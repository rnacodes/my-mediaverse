import { describe, it, expect } from 'vitest';
import { getAspectRatio, getAspectRatioPadding, getObjectFit } from './mediaImageUtils';

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
