import { z } from 'zod';

// Media types the backend can actually create. Anything else is "Coming Soon"
// in the dropdown and is blocked at submit time (see AddMediaForm shell).
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
  // Podcast
  podcastType: '',
  podcastSeriesId: '',
  selectedPodcastSeries: null,
  durationInSeconds: '',
  // Movie
  director: '',
  // TV Show
  creator: '',
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
    // Podcast
    podcastType: z.string().optional(),
    podcastSeriesId: z.string().optional(),
    selectedPodcastSeries: z.any().nullable(),
    durationInSeconds: z.string().optional(),
    // Movie
    director: z.string().optional(),
    // TV Show
    creator: z.string().optional(),
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

function typedBase(d, mediaType) {
  return {
    title: d.title,
    mediaType,
    link: d.link,
    notes: d.notes,
    description: d.description,
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
  };
}

export function buildEpisodePayload(d) {
  return {
    ...typedBase(d, 'Podcast'),
    seriesId: d.selectedPodcastSeries?.id || d.selectedPodcastSeries?.Id || d.podcastSeriesId,
    audioLink: null,
    releaseDate: null,
    durationInSeconds: d.durationInSeconds ? parseInt(d.durationInSeconds, 10) : 0,
  };
}

export function buildMoviePayload(d) {
  return {
    ...typedBase(d, 'Movie'),
    director: d.director || null,
  };
}

export function buildTvShowPayload(d) {
  return {
    ...typedBase(d, 'TVShow'),
    creator: d.creator || null,
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
