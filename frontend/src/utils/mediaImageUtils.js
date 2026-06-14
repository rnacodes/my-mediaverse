/**
 * Shared utility for media image display configuration.
 * Single source of truth for aspect ratios, object-fit, and placeholder
 * resolution per media type.
 */

import bookPlaceholder from '@/assets/placeholders/book.svg';
import moviePlaceholder from '@/assets/placeholders/movie.svg';
import tvShowPlaceholder from '@/assets/placeholders/tvshow.svg';
import videoPlaceholder from '@/assets/placeholders/video.svg';
import channelPlaceholder from '@/assets/placeholders/channel.svg';
import playlistPlaceholder from '@/assets/placeholders/playlist.svg';
import podcastPlaceholder from '@/assets/placeholders/podcast.svg';
import articlePlaceholder from '@/assets/placeholders/article.svg';
import websitePlaceholder from '@/assets/placeholders/website.svg';
import mixlistPlaceholder from '@/assets/placeholders/mixlist.svg';
import defaultPlaceholder from '@/assets/placeholders/default.svg';

// Per-media-type placeholder images, keyed by the PascalCase mediaType enum
// values the API returns. Used when an item has no thumbnail or its thumbnail
// fails to load.
const PLACEHOLDERS = {
  Book: bookPlaceholder,
  Movie: moviePlaceholder,
  TVShow: tvShowPlaceholder,
  Video: videoPlaceholder,
  Channel: channelPlaceholder,
  Playlist: playlistPlaceholder,
  Podcast: podcastPlaceholder,
  Article: articlePlaceholder,
  Website: websitePlaceholder,
  Mixlist: mixlistPlaceholder,
};

/**
 * Returns the placeholder image URL for a given media type, falling back to a
 * generic placeholder for unknown/missing types.
 */
export const getPlaceholderImage = (mediaType) =>
  PLACEHOLDERS[mediaType] || defaultPlaceholder;

/**
 * Resolves the display image for a media item: the provider-supplied
 * `thumbnail` if present, otherwise the per-type placeholder. This is the
 * single source of truth for picking an image URL — components should not read
 * `thumbnailUrl`/`imageUrl` or build their own fallback chains.
 *
 * @param {object} item        The media item (expects a `thumbnail` field).
 * @param {string} [typeHint]  Media type to use for the placeholder when the
 *                             item itself has no `mediaType` (e.g. mixlists).
 */
export const resolveMediaImage = (item, typeHint) => {
  if (!item) return getPlaceholderImage(typeHint);
  return item.thumbnail || getPlaceholderImage(typeHint || item.mediaType);
};

/**
 * Returns the CSS aspectRatio value for a given media type.
 * Used by MediaInfoCard (hero image) and card components.
 */
export const getAspectRatio = (mediaType) => {
  switch (mediaType) {
    case 'Book':
    case 'Movie':
    case 'TVShow':
      return '2/3';
    case 'Video':
    case 'Playlist':
    case 'Article':
    case 'Website':
      return '16/9';
    case 'Channel':
    case 'Podcast':
    default:
      return '1/1';
  }
};

/**
 * Returns the paddingTop percentage for the padding-top aspect ratio hack.
 * Used by AllMedia.jsx card grid where images use absolute positioning.
 *
 * The percentage is (height / width) * 100:
 *   2:3 = 150%, 16:9 = 56.25%, 1:1 = 100%
 */
export const getAspectRatioPadding = (mediaType) => {
  switch (mediaType) {
    case 'Book':
    case 'Movie':
    case 'TVShow':
      return '150%';
    case 'Video':
    case 'Playlist':
    case 'Article':
    case 'Website':
      return '56.25%';
    case 'Channel':
    case 'Podcast':
    default:
      return '100%';
  }
};

/**
 * Returns the appropriate objectFit for a given media type.
 * 'contain' shows the full image (with background bars if needed).
 * 'cover' fills the container but may crop edges.
 */
export const getObjectFit = () => {
  return 'contain';
};
