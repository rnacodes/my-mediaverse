import { z } from 'zod';

export const SUPPORTED_TYPES = ['Podcast', 'Book', 'Movie', 'TVShow', 'Video'];

export const defaultValues = {
  title: '',
  mediaType: '',
  link: '',
  description: '',
  notes: '',
  status: 'Uncharted',
  dateCompleted: '',
  rating: '',
  ownershipStatus: '',
  thumbnail: '',
  genres: [],
  topics: [],
  selectedMixlists: [],
  // Book
  author: '',
  isbn: '',
  asin: '',
  goodreadsRating: '',
  format: 'Digital',
  partOfSeries: false,
  yearPublished: '',
  dateRead: '',
  myReview: '',
  // Podcast
  podcastType: '',
  podcastSeriesId: '',
  selectedPodcastSeries: null,
  durationInSeconds: '',
  episodeNumber: '',
  seasonNumber: '',
  releaseDate: '',
  audioLink: '',
  // Movie
  director: '',
  releaseYear: '',
  runtimeMinutes: '',
  mpaaRating: '',
  originalTitle: '',
  // TV Show
  creator: '',
  firstAirYear: '',
  lastAirYear: '',
  numberOfSeasons: '',
  numberOfEpisodes: '',
  contentRating: '',
  originalName: '',
  // Shared by Movie + TV Show
  cast: '',
  tagline: '',
  homepage: '',
  originalLanguage: '',
  // Shared by Book + Podcast Series
  publisher: '',
  // Video
  platform: 'YouTube',
  lengthInSeconds: '',
  externalId: '',
};

export const mediaSchema = z
  .object({
    title: z.string().trim().min(1, 'Title is required'),
    mediaType: z.string().min(1, 'Media Type is required'),
    link: z.string().optional(),
    description: z.string().optional(),
    notes: z.string().optional(),
    status: z.string(),
    dateCompleted: z.string().optional(),
    rating: z.string().optional(),
    ownershipStatus: z.string().optional(),
    thumbnail: z.string().optional(),
    genres: z.array(z.string()),
    topics: z.array(z.string()),
    selectedMixlists: z.array(z.any()),
    // Book
    author: z.string().optional(),
    isbn: z.string().optional(),
    asin: z.string().optional(),
    goodreadsRating: z.string().optional(),
    format: z.string(),
    partOfSeries: z.boolean(),
    yearPublished: z.string().optional(),
    dateRead: z.string().optional(),
    myReview: z.string().optional(),
    // Podcast
    podcastType: z.string().optional(),
    podcastSeriesId: z.string().optional(),
    selectedPodcastSeries: z.any().nullable(),
    durationInSeconds: z.string().optional(),
    episodeNumber: z.string().optional(),
    seasonNumber: z.string().optional(),
    releaseDate: z.string().optional(),
    audioLink: z.string().optional(),
    // Movie
    director: z.string().optional(),
    releaseYear: z.string().optional(),
    runtimeMinutes: z.string().optional(),
    mpaaRating: z.string().optional(),
    originalTitle: z.string().optional(),
    // TV Show
    creator: z.string().optional(),
    firstAirYear: z.string().optional(),
    lastAirYear: z.string().optional(),
    numberOfSeasons: z.string().optional(),
    numberOfEpisodes: z.string().optional(),
    contentRating: z.string().optional(),
    originalName: z.string().optional(),
    // Shared by Movie + TV Show
    cast: z.string().optional(),
    tagline: z.string().optional(),
    homepage: z.string().optional(),
    originalLanguage: z.string().optional(),
    // Shared by Book + Podcast Series
    publisher: z.string().optional(),
    // Video
    platform: z.string(),
    lengthInSeconds: z.string().optional(),
    externalId: z.string().optional(),
  })
  .superRefine((data, ctx) => {
    if (data.mediaType === 'Book' && !data.author?.trim()) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['author'], message: 'Author is required' });
    }
  });

// Parse an optional numeric text input to an integer, or null when blank.
const toIntOrNull = (v) => (v ? parseInt(v, 10) : null);

const toDateInput = (v) => (v ? new Date(v).toISOString().split('T')[0] : '');

const toFieldStr = (v) => (v === null || v === undefined ? '' : String(v));

export function mapMediaItemToFormValues(mediaItem) {
  if (!mediaItem) return { ...defaultValues };
  const m = mediaItem;
  const mediaType = m.mediaType || '';

  const base = {
    ...defaultValues,
    title: m.title ?? '',
    mediaType,
    link: m.link ?? '',
    description: m.description ?? '',
    notes: m.notes ?? '',
    status: m.status || 'Uncharted',
    dateCompleted: toDateInput(m.dateCompleted),
    rating: m.rating ?? '',
    ownershipStatus: m.ownershipStatus ?? '',
    thumbnail: m.thumbnail ?? '',
    topics: Array.isArray(m.topics) ? m.topics : [],
    genres: Array.isArray(m.genres) ? m.genres : [],
  };

  switch (mediaType) {
    case 'Book':
      return {
        ...base,
        author: m.author ?? '',
        isbn: m.isbn ?? m.ISBN ?? '',
        asin: m.asin ?? m.ASIN ?? '',
        goodreadsRating: toFieldStr(m.goodreadsRating),
        format: m.format || 'Digital',
        partOfSeries: !!m.partOfSeries,
        publisher: m.publisher ?? '',
        yearPublished: toFieldStr(m.yearPublished),
        dateRead: toDateInput(m.dateRead),
        myReview: m.myReview ?? '',
      };
    case 'Movie':
      return {
        ...base,
        director: m.director ?? '',
        cast: m.cast ?? '',
        releaseYear: toFieldStr(m.releaseYear),
        runtimeMinutes: toFieldStr(m.runtimeMinutes),
        mpaaRating: m.mpaaRating ?? '',
        tagline: m.tagline ?? '',
        homepage: m.homepage ?? '',
        originalLanguage: m.originalLanguage ?? '',
        originalTitle: m.originalTitle ?? '',
      };
    case 'TVShow':
      return {
        ...base,
        creator: m.creator ?? '',
        cast: m.cast ?? '',
        firstAirYear: toFieldStr(m.firstAirYear),
        lastAirYear: toFieldStr(m.lastAirYear),
        numberOfSeasons: toFieldStr(m.numberOfSeasons),
        numberOfEpisodes: toFieldStr(m.numberOfEpisodes),
        contentRating: m.contentRating ?? '',
        tagline: m.tagline ?? '',
        homepage: m.homepage ?? '',
        originalLanguage: m.originalLanguage ?? '',
        originalName: m.originalName ?? '',
      };
    case 'Video':
      return {
        ...base,
        platform: m.platform || 'YouTube',
        lengthInSeconds: toFieldStr(m.lengthInSeconds),
        externalId: m.externalId ?? '',
      };
    case 'Podcast': {
      const podcastType = m.podcastType || (m.seriesId ? 'Episode' : 'Series');
      if (podcastType === 'Episode') {
        const series = m.series || (m.seriesId ? { id: m.seriesId, title: m.seriesTitle } : null);
        return {
          ...base,
          podcastType: 'Episode',
          podcastSeriesId: m.seriesId ? String(m.seriesId) : '',
          selectedPodcastSeries: series,
          durationInSeconds: toFieldStr(m.durationInSeconds),
          episodeNumber: toFieldStr(m.episodeNumber),
          seasonNumber: toFieldStr(m.seasonNumber),
          releaseDate: toDateInput(m.releaseDate),
          audioLink: m.audioLink ?? '',
          publisher: m.publisher ?? '',
        };
      }
      return {
        ...base,
        podcastType: 'Series',
        publisher: m.publisher ?? '',
      };
    }
    default:
      return base;
  }
}

function typedBase(d, mediaType) {
  return {
    title: d.title,
    mediaType,
    // Optional URL field: send null (not '') when blank — the backend's [Url]
    // validator rejects an empty string.
    link: d.link?.trim() ? d.link : null,
    notes: d.notes?.trim() ? d.notes : null,
    description: d.description?.trim() ? d.description : null,
    status: d.status,
    dateCompleted: d.status === 'Completed' && d.dateCompleted ? d.dateCompleted : null,
    rating: d.status === 'Completed' && d.rating ? d.rating : null,
    ownershipStatus: d.ownershipStatus || null,
    topics: d.topics?.length ? d.topics : [],
    genres: d.genres?.length ? d.genres : [],
    thumbnail: d.thumbnail || null,
  };
}

export function buildMediaPayload(d) {
  const payload = {
    title: d.title,
    mediaType: d.mediaType,
    status: d.status,
    topics: d.topics?.length ? d.topics : [],
    genres: d.genres?.length ? d.genres : [],
  };
  if (d.link?.trim()) payload.link = d.link;
  if (d.notes?.trim()) payload.notes = d.notes;
  if (d.status === 'Completed' && d.dateCompleted) payload.dateCompleted = d.dateCompleted;
  if (d.status === 'Completed' && d.rating) payload.rating = d.rating;
  if (d.ownershipStatus) payload.ownershipStatus = d.ownershipStatus;
  if (d.description?.trim()) payload.description = d.description;
  payload.thumbnail = d.thumbnail?.trim() ? d.thumbnail : null;
  return payload;
}

export function buildBookPayload(d) {
  return {
    ...typedBase(d, 'Book'),
    author: d.author,
    isbn: d.isbn || null,
    asin: d.asin || null,
    format: d.format,
    partOfSeries: d.partOfSeries,
    goodreadsRating: d.goodreadsRating ? parseFloat(d.goodreadsRating) : null,
    publisher: d.publisher || null,
    yearPublished: toIntOrNull(d.yearPublished),
    dateRead: d.dateRead || null,
    myReview: d.myReview || null,
  };
}

export function buildEpisodePayload(d) {
  return {
    ...typedBase(d, 'Podcast'),
    seriesId: d.selectedPodcastSeries?.id || d.selectedPodcastSeries?.Id || d.podcastSeriesId,
    audioLink: d.audioLink || null,
    releaseDate: d.releaseDate || null,
    durationInSeconds: d.durationInSeconds ? parseInt(d.durationInSeconds, 10) : 0,
    episodeNumber: toIntOrNull(d.episodeNumber),
    seasonNumber: toIntOrNull(d.seasonNumber),
  };
}

export function buildSeriesPayload(d) {
  return {
    ...typedBase(d, 'Podcast'),
    publisher: d.publisher || null,
  };
}

export function buildMoviePayload(d) {
  return {
    ...typedBase(d, 'Movie'),
    director: d.director || null,
    cast: d.cast || null,
    releaseYear: toIntOrNull(d.releaseYear),
    runtimeMinutes: toIntOrNull(d.runtimeMinutes),
    mpaaRating: d.mpaaRating || null,
    tagline: d.tagline || null,
    homepage: d.homepage || null,
    originalLanguage: d.originalLanguage || null,
    originalTitle: d.originalTitle || null,
  };
}

export function buildTvShowPayload(d) {
  return {
    ...typedBase(d, 'TVShow'),
    creator: d.creator || null,
    cast: d.cast || null,
    firstAirYear: toIntOrNull(d.firstAirYear),
    lastAirYear: toIntOrNull(d.lastAirYear),
    numberOfSeasons: toIntOrNull(d.numberOfSeasons),
    numberOfEpisodes: toIntOrNull(d.numberOfEpisodes),
    contentRating: d.contentRating || null,
    tagline: d.tagline || null,
    homepage: d.homepage || null,
    originalLanguage: d.originalLanguage || null,
    originalName: d.originalName || null,
  };
}

export function buildVideoPayload(d) {
  return {
    ...typedBase(d, 'Video'),
    platform: d.platform || 'YouTube',
    lengthInSeconds: d.lengthInSeconds ? parseInt(d.lengthInSeconds, 10) : 0,
    externalId: d.externalId || null,
  };
}
