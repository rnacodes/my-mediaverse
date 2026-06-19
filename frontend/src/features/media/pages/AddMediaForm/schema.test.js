import { describe, it, expect } from 'vitest';
import { buildVideoPayload, buildEpisodePayload } from './schema';

describe('buildVideoPayload', () => {
  it('does not send the removed channelName field', () => {
    const payload = buildVideoPayload({ title: 'V', platform: 'YouTube', channelName: 'ignored' });
    expect(payload).not.toHaveProperty('channelName');
  });

  it('sends the video fields the backend DTO accepts', () => {
    const payload = buildVideoPayload({
      title: 'V',
      platform: 'Vimeo',
      lengthInSeconds: '120',
      externalId: 'abc123',
    });
    expect(payload).toMatchObject({
      mediaType: 'Video',
      platform: 'Vimeo',
      lengthInSeconds: 120,
      externalId: 'abc123',
    });
  });
});

describe('buildEpisodePayload', () => {
  it('sends seriesId (not parentPodcastId) from the selected series', () => {
    const payload = buildEpisodePayload({
      title: 'E',
      selectedPodcastSeries: { id: 'series-guid' },
      durationInSeconds: '1800',
    });
    expect(payload).not.toHaveProperty('parentPodcastId');
    expect(payload.seriesId).toBe('series-guid');
    expect(payload.durationInSeconds).toBe(1800);
  });

  it('falls back to podcastSeriesId when no series object is selected', () => {
    const payload = buildEpisodePayload({ title: 'E', podcastSeriesId: 'fallback-guid' });
    expect(payload.seriesId).toBe('fallback-guid');
  });
});
