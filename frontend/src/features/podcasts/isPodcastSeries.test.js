import { describe, it, expect } from 'vitest';
import { isPodcastSeries } from './isPodcastSeries';

describe('isPodcastSeries', () => {
  it('goes by podcastType when the item carries one', () => {
    expect(isPodcastSeries({ podcastType: 'Series', seriesId: 'x' })).toBe(true);
    expect(isPodcastSeries({ podcastType: 'Episode' })).toBe(false);
  });

  it('falls back to the absence of a parent series id', () => {
    expect(isPodcastSeries({})).toBe(true);
    expect(isPodcastSeries({ seriesId: 'series-1' })).toBe(false);
  });
});
