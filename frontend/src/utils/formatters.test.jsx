import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  formatEnumValue,
  formatMediaType,
  formatStatus,
  getMediaTypeColor,
  getStatusColor,
  getRatingIcon,
  getRatingText,
} from './formatters';

describe('formatEnumValue', () => {
  it('inserts a space at camelCase boundaries', () => {
    expect(formatEnumValue('activelyExploring')).toBe('actively Exploring');
  });

  it('splits PascalCase acronym boundaries', () => {
    expect(formatEnumValue('TVShow')).toBe('TV Show');
    expect(formatEnumValue('VideoGame')).toBe('Video Game');
    expect(formatEnumValue('ActivelyExploring')).toBe('Actively Exploring');
  });

  it('leaves single-word values unchanged', () => {
    expect(formatEnumValue('Uncharted')).toBe('Uncharted');
    expect(formatEnumValue('Book')).toBe('Book');
  });

  it('returns falsy non-zero values unchanged', () => {
    expect(formatEnumValue(null)).toBeNull();
    expect(formatEnumValue(undefined)).toBeUndefined();
    expect(formatEnumValue('')).toBe('');
  });

  it('stringifies non-string values, including 0', () => {
    expect(formatEnumValue(0)).toBe('0');
    expect(formatEnumValue(42)).toBe('42');
  });
});

describe('formatMediaType', () => {
  it('delegates to formatEnumValue', () => {
    expect(formatMediaType('TVShow')).toBe('TV Show');
    expect(formatMediaType('VideoGame')).toBe('Video Game');
    expect(formatMediaType(null)).toBeNull();
  });
});

describe('formatStatus', () => {
  it('delegates to formatEnumValue', () => {
    expect(formatStatus('ActivelyExploring')).toBe('Actively Exploring');
    expect(formatStatus('Uncharted')).toBe('Uncharted');
    expect(formatStatus(undefined)).toBeUndefined();
  });
});

describe('getMediaTypeColor', () => {
  it.each([
    ['Book', 'purple.500'],
    ['Podcast', 'green.500'],
    ['Movie', 'red.500'],
    ['TVShow', 'blue.500'],
    ['Video', 'orange.500'],
    ['Article', 'teal.500'],
    ['Website', 'cyan.500'],
    ['VideoGame', 'pink.500'],
  ])('maps %s to %s', (mediaType, color) => {
    expect(getMediaTypeColor(mediaType)).toBe(color);
  });

  it('falls back to gray.500 for unknown / missing types', () => {
    expect(getMediaTypeColor('Unknown')).toBe('gray.500');
    expect(getMediaTypeColor(undefined)).toBe('gray.500');
  });
});

describe('getStatusColor', () => {
  it.each([
    ['Completed', '#4caf50'],
    ['ActivelyExploring', '#2196f3'],
    ['Uncharted', '#9c27b0'],
    ['Abandoned', '#f44336'],
  ])('maps %s to %s', (status, color) => {
    expect(getStatusColor(status)).toBe(color);
  });

  it('falls back to #9e9e9e for unknown / missing statuses', () => {
    expect(getStatusColor('Whatever')).toBe('#9e9e9e');
    expect(getStatusColor(undefined)).toBe('#9e9e9e');
  });
});

describe('getRatingIcon', () => {
  it.each([
    ['superlike', 'FavoriteIcon'],
    [0, 'FavoriteIcon'],
    ['like', 'ThumbUpIcon'],
    [1, 'ThumbUpIcon'],
    ['neutral', 'RemoveIcon'],
    [2, 'RemoveIcon'],
    ['dislike', 'ThumbDownIcon'],
    [3, 'ThumbDownIcon'],
  ])('renders rating %s as the %s', (rating, testId) => {
    render(getRatingIcon(rating));
    expect(screen.getByTestId(testId)).toBeInTheDocument();
  });

  it('normalizes string casing before matching', () => {
    render(getRatingIcon('LIKE'));
    expect(screen.getByTestId('ThumbUpIcon')).toBeInTheDocument();
  });

  it.each([null, undefined, 'bogus', 99])('returns null for %s', (rating) => {
    expect(getRatingIcon(rating)).toBeNull();
  });
});

describe('getRatingText', () => {
  it.each([
    ['superlike', 'Super Like'],
    [0, 'Super Like'],
    ['like', 'Like'],
    [1, 'Like'],
    ['neutral', 'Neutral'],
    [2, 'Neutral'],
    ['dislike', 'Dislike'],
    [3, 'Dislike'],
  ])('maps rating %s to "%s"', (rating, text) => {
    expect(getRatingText(rating)).toBe(text);
  });

  it('normalizes string casing before matching', () => {
    expect(getRatingText('SuperLike')).toBe('Super Like');
  });

  it('formats unmatched numeric ratings as N/5 Stars', () => {
    expect(getRatingText(4)).toBe('4/5 Stars');
    expect(getRatingText(4.5)).toBe('4.5/5 Stars');
  });

  it('returns "Not Rated" for null, undefined, and unknown strings', () => {
    expect(getRatingText(null)).toBe('Not Rated');
    expect(getRatingText(undefined)).toBe('Not Rated');
    expect(getRatingText('bogus')).toBe('Not Rated');
  });
});
