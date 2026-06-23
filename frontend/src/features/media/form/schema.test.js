import { describe, it, expect } from 'vitest';
import {
  buildVideoPayload, buildEpisodePayload, buildSeriesPayload,
  buildMoviePayload, buildTvShowPayload, buildBookPayload,
  mapMediaItemToFormValues,
} from './schema';

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
  it('sends seriesId (not parentPodcastId) and the episode fields', () => {
    const payload = buildEpisodePayload({
      title: 'E',
      selectedPodcastSeries: { id: 'series-guid' },
      durationInSeconds: '1800',
      episodeNumber: '12',
      seasonNumber: '2',
      releaseDate: '2021-05-01',
      audioLink: 'https://x.mp3',
    });
    expect(payload).not.toHaveProperty('parentPodcastId');
    expect(payload).toMatchObject({
      seriesId: 'series-guid',
      durationInSeconds: 1800,
      episodeNumber: 12,
      seasonNumber: 2,
      releaseDate: '2021-05-01',
      audioLink: 'https://x.mp3',
    });
  });

  it('falls back to podcastSeriesId when no series object is selected', () => {
    const payload = buildEpisodePayload({ title: 'E', podcastSeriesId: 'fallback-guid' });
    expect(payload.seriesId).toBe('fallback-guid');
  });
});

describe('buildSeriesPayload', () => {
  it('builds a podcast series payload with publisher and no episode-only fields', () => {
    const payload = buildSeriesPayload({ title: 'S', publisher: 'NPR' });
    expect(payload).toMatchObject({ mediaType: 'Podcast', publisher: 'NPR' });
    expect(payload).not.toHaveProperty('seriesId');
  });
});

describe('buildMoviePayload', () => {
  it('includes the expanded movie fields with numeric coercion', () => {
    const payload = buildMoviePayload({
      title: 'M', director: 'D', cast: 'A, B',
      releaseYear: '1999', runtimeMinutes: '136', mpaaRating: 'R',
      tagline: 'tag', homepage: 'https://x', originalLanguage: 'en', originalTitle: 'OT',
    });
    expect(payload).toMatchObject({
      mediaType: 'Movie', director: 'D', cast: 'A, B',
      releaseYear: 1999, runtimeMinutes: 136, mpaaRating: 'R',
      tagline: 'tag', homepage: 'https://x', originalLanguage: 'en', originalTitle: 'OT',
    });
  });

  it('nulls out blank optional fields', () => {
    const payload = buildMoviePayload({ title: 'M' });
    expect(payload.director).toBeNull();
    expect(payload.releaseYear).toBeNull();
  });
});

describe('buildTvShowPayload', () => {
  it('includes the expanded tv show fields with numeric coercion', () => {
    const payload = buildTvShowPayload({
      title: 'T', creator: 'C', cast: 'A',
      firstAirYear: '2008', lastAirYear: '2013', numberOfSeasons: '5', numberOfEpisodes: '62',
      contentRating: 'TV-14', originalName: 'ON',
    });
    expect(payload).toMatchObject({
      mediaType: 'TVShow', creator: 'C', cast: 'A',
      firstAirYear: 2008, lastAirYear: 2013, numberOfSeasons: 5, numberOfEpisodes: 62,
      contentRating: 'TV-14', originalName: 'ON',
    });
  });
});

describe('buildBookPayload', () => {
  it('includes the newly mapped book fields', () => {
    const payload = buildBookPayload({
      title: 'B', author: 'Au',
      publisher: 'Pub', yearPublished: '2014', dateRead: '2020-01-01', myReview: 'great',
    });
    expect(payload).toMatchObject({
      mediaType: 'Book', author: 'Au',
      publisher: 'Pub', yearPublished: 2014, dateRead: '2020-01-01', myReview: 'great',
    });
  });
});

describe('mapMediaItemToFormValues', () => {
  it('returns blank defaults for a null item', () => {
    const values = mapMediaItemToFormValues(null);
    expect(values.title).toBe('');
    expect(values.mediaType).toBe('');
  });

  it('maps the common base fields and formats dates as yyyy-MM-dd', () => {
    const values = mapMediaItemToFormValues({
      mediaType: 'Movie',
      title: 'Inception',
      status: 'Completed',
      dateCompleted: '2021-07-04T00:00:00Z',
      rating: 'Like',
      ownershipStatus: 'Streamed',
      topics: ['dreams'],
      genres: ['sci-fi'],
    });
    expect(values).toMatchObject({
      mediaType: 'Movie',
      title: 'Inception',
      status: 'Completed',
      dateCompleted: '2021-07-04',
      rating: 'Like',
      ownershipStatus: 'Streamed',
      topics: ['dreams'],
      genres: ['sci-fi'],
    });
  });

  it('round-trips Book detail fields, coercing numbers to strings', () => {
    const values = mapMediaItemToFormValues({
      mediaType: 'Book',
      title: 'Dune',
      author: 'Herbert',
      publisher: 'Chilton',
      yearPublished: 1965,
      dateRead: '2020-01-02T00:00:00Z',
      myReview: 'classic',
      format: 'Physical',
      partOfSeries: true,
    });
    expect(values).toMatchObject({
      author: 'Herbert',
      publisher: 'Chilton',
      yearPublished: '1965',
      dateRead: '2020-01-02',
      myReview: 'classic',
      format: 'Physical',
      partOfSeries: true,
    });
  });

  it('maps a podcast episode with its parent series selection', () => {
    const values = mapMediaItemToFormValues({
      mediaType: 'Podcast',
      podcastType: 'Episode',
      title: 'Ep 1',
      seriesId: 'series-guid',
      seriesTitle: 'The Series',
      durationInSeconds: 1800,
      episodeNumber: 5,
      releaseDate: '2021-05-01T00:00:00Z',
      audioLink: 'https://x.mp3',
    });
    expect(values).toMatchObject({
      podcastType: 'Episode',
      podcastSeriesId: 'series-guid',
      durationInSeconds: '1800',
      episodeNumber: '5',
      releaseDate: '2021-05-01',
      audioLink: 'https://x.mp3',
    });
    expect(values.selectedPodcastSeries).toMatchObject({ id: 'series-guid', title: 'The Series' });
  });

  it('maps a podcast series with publisher', () => {
    const values = mapMediaItemToFormValues({
      mediaType: 'Podcast',
      podcastType: 'Series',
      title: 'My Show',
      publisher: 'NPR',
    });
    expect(values).toMatchObject({ podcastType: 'Series', publisher: 'NPR' });
  });

  it('maps Video detail fields', () => {
    const values = mapMediaItemToFormValues({
      mediaType: 'Video',
      title: 'Clip',
      platform: 'Vimeo',
      lengthInSeconds: 120,
      externalId: 'abc123',
    });
    expect(values).toMatchObject({ platform: 'Vimeo', lengthInSeconds: '120', externalId: 'abc123' });
  });
});
