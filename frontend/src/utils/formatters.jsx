import React from 'react';
import { Favorite, ThumbUp, Remove, ThumbDown } from '@mui/icons-material';
import { getMediaTypeColor as getDesignSystemMediaTypeColor } from '@/shared/DesignSystem';

/**
 * Formats enum values by adding spaces between words
 * Examples:
 * - "TVShow" -> "TV Show"
 * - "VideoGame" -> "Video Game"
 * - "ActivelyExploring" -> "Actively Exploring"
 * - "Uncharted" -> "Uncharted" (no change needed)
 */
export const formatEnumValue = (value) => {
  if (!value && value !== 0) return value;
  if (typeof value !== 'string') return String(value);

  // Add space before capital letters (except the first one)
  // and before sequences of capitals followed by a lowercase letter
  return value
    .replace(/([a-z])([A-Z])/g, '$1 $2')  // camelCase: "activelyExploring" -> "actively Exploring"
    .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2'); // PascalCase: "TVShow" -> "TV Show"
};

/**
 * Formats media type enum values for display
 */
export const formatMediaType = (mediaType) => {
  return formatEnumValue(mediaType);
};

/**
 * Formats status enum values for display
 */
export const formatStatus = (status) => {
  return formatEnumValue(status);
};

// Same palette as the design system so the media-type chip on a profile page
// matches the cards everywhere else. Returns a hex color MUI can use directly.
export const getMediaTypeColor = (mediaType) => {
  return getDesignSystemMediaTypeColor(mediaType === 'VideoGame' ? 'game' : mediaType);
};

export const getStatusColor = (status) => {
  switch (status) {
    case 'Completed': return '#4caf50';
    case 'ActivelyExploring': return '#2196f3';
    case 'Uncharted': return '#9c27b0';
    case 'Abandoned': return '#f44336';
    default: return '#9e9e9e';
  }
};

export const getRatingIcon = (rating) => {
  if (rating === undefined || rating === null) return null;
  
  // Normalize rating value for consistent comparison
  const r = typeof rating === 'string' ? rating.toLowerCase() : rating;

  switch (r) {
    case 'superlike':
    case 0:
      return <Favorite sx={{ fontSize: 18, color: '#e91e63' }} />;
    case 'like':
    case 1:
      return <ThumbUp sx={{ fontSize: 18, color: '#4caf50' }} />;
    case 'neutral':
    case 2:
      return <Remove sx={{ fontSize: 18, color: '#9e9e9e' }} />;
    case 'dislike':
    case 3:
      return <ThumbDown sx={{ fontSize: 18, color: '#f44336' }} />;
    default:
      return null;
  }
};

export const getRatingText = (rating) => {
  if (rating === undefined || rating === null) return 'Not Rated';
  
  const r = typeof rating === 'string' ? rating.toLowerCase() : rating;

  switch (r) {
    case 'superlike':
    case 0:
      return 'Super Like';
    case 'like':
    case 1:
      return 'Like';
    case 'neutral':
    case 2:
      return 'Neutral';
    case 'dislike':
    case 3:
      return 'Dislike';
    default:
      if (typeof rating === 'number') return `${rating}/5 Stars`;
      return 'Not Rated';
  }
};
