/**
 * Shared utility for media image display configuration.
 * Single source of truth for aspect ratios and object-fit per media type.
 */

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
