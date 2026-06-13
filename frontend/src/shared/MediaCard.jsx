import React from 'react';
import { Link } from 'react-router-dom';
import {
  Card,
  CardContent,
  CardMedia,
  Typography,
  Chip,
  Box,
  Rating
} from '@mui/material';
import {
  Book,
  Movie,
  Tv,
  Article,
  LibraryMusic,
  Podcasts,
  SportsEsports,
  YouTube,
  Language,
  MenuBook,
  AutoAwesome
} from '@mui/icons-material';
import { getMediaTypeColor, getStatusColor, commonStyles } from './DesignSystem';
import { formatMediaType, formatStatus } from '@/utils/formatters';

// Media type icons mapping
const mediaTypeIcons = {
  podcast: <Podcasts />,
  book: <Book />,
  movie: <Movie />,
  tv: <Tv />,
  article: <Article />,
  music: <LibraryMusic />,
  game: <SportsEsports />,
  video: <YouTube />,
  website: <Language />,
  document: <MenuBook />,
  default: <AutoAwesome />
};

const MediaCard = ({
  media,
  variant = 'default', // 'default', 'compact', 'featured'
  onClick,
  className,
  showMediaTypeIcon = true, // new prop to control media type icon visibility
  ...props
}) => {
  const getMediaIcon = (mediaType) => {
    const type = mediaType?.toLowerCase();
    return mediaTypeIcons[type] || mediaTypeIcons.default;
  };

  const handleCardClick = (e) => {
    if (onClick) {
      e.preventDefault();
      e.stopPropagation();
      onClick(media);
    }
  };

  const cardContent = (
    <Card
      sx={{
        ...commonStyles.card,
        ...(variant === 'compact' && {
          height: 'auto',
          minHeight: '120px'
        }),
        ...(variant === 'featured' && {
          height: '400px',
          background: 'linear-gradient(135deg, rgba(54, 39, 89, 0.1) 0%, rgba(71, 67, 80, 0.1) 100%)'
        })
      }}
      className={className}
      {...props}
    >
      {/* Media Image */}
      <CardMedia
        component="img"
        sx={{
          width: '100%',
          height: variant === 'compact' ? 120 : variant === 'featured' ? 200 : 180,
          maxHeight: variant === 'compact' ? 120 : variant === 'featured' ? 200 : 180,
          objectFit: 'cover',
          backgroundColor: 'rgba(0, 0, 0, 0.1)',
          position: 'relative'
        }}
        image={media.thumbnail || media.thumbnailUrl || media.imageUrl || 'https://placehold.co/600x400/474350/fcfafa?text=No+Image'}
        alt={media.title}
        onError={(e) => {
          e.target.onerror = null;
          e.target.src = 'https://placehold.co/600x400/474350/fcfafa?text=No+Image';
        }}
      />

      {/* Overlay with media type icon */}
      {showMediaTypeIcon && (
        <Box
          sx={{
            position: 'absolute',
            top: 8,
            left: 8,
            backgroundColor: 'rgba(0, 0, 0, 0.7)',
            borderRadius: '50%',
            p: 0.5,
            color: getMediaTypeColor(media.mediaType)
          }}
        >
          {getMediaIcon(media.mediaType)}
        </Box>
      )}



      {/* Card Content */}
      <CardContent
        sx={{
          flexGrow: 1,
          display: 'flex',
          flexDirection: 'column',
          p: variant === 'compact' ? 1.5 : 2
        }}
      >
        {/* Title */}
        <Typography
          variant={variant === 'compact' ? 'body2' : 'h6'}
          component="div"
          sx={{
            fontWeight: 'bold',
            mb: 1,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            display: '-webkit-box',
            WebkitLineClamp: variant === 'compact' ? 2 : 3,
            WebkitBoxOrient: 'vertical',
            lineHeight: 1.3
          }}
        >
          {media.title}
        </Typography>

        {/* Media Type and Status Chips */}
        <Box sx={{ display: 'flex', gap: 1, mb: 1, flexWrap: 'wrap' }}>
          <Chip
            label={formatMediaType(media.mediaType)}
            size="medium"
            sx={{
              backgroundColor: getMediaTypeColor(media.mediaType),
              color: 'white',
              fontSize: '0.85rem',
              height: '28px'
            }}
          />
          {media.status && (
            <Chip
              label={formatStatus(media.status)}
              size="medium"
              sx={{
                backgroundColor: getStatusColor(media.status),
                color: 'white',
                fontSize: '0.85rem',
                height: '28px'
              }}
            />
          )}
        </Box>

        {/* Rating */}
        {media.rating && (
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
            <Rating
              value={media.rating}
              readOnly
              size="small"
              sx={{ mr: 1 }}
            />
            <Typography variant="caption" color="text.secondary">
              {media.rating}/5
            </Typography>
          </Box>
        )}

        {/* Video-specific details */}
        {media.mediaType === 'Video' && (
          <Box sx={{ mb: 1 }}>
            {media.platform && (
              <Chip
                label={media.platform}
                size="small"
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  color: 'white',
                  fontSize: '0.75rem',
                  height: '24px',
                  mr: 1,
                  mb: 0.5
                }}
              />
            )}
            {media.channelName && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                {media.channelName}
              </Typography>
            )}
            {media.lengthInSeconds && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                {Math.floor(media.lengthInSeconds / 60)}:{(media.lengthInSeconds % 60).toString().padStart(2, '0')}
              </Typography>
            )}
          </Box>
        )}

        {/* Description/Notes */}
        {media.notes && variant !== 'compact' && (
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              lineHeight: 1.4
            }}
          >
            {media.notes}
          </Typography>
        )}

        {/* Date Added */}
        {media.dateAdded && (
          <Typography
            variant="caption"
            color="text.hint"
            sx={{ mt: 'auto', pt: 1 }}
          >
            Added {new Date(media.dateAdded).toLocaleDateString()}
          </Typography>
        )}
      </CardContent>
    </Card>
  );

  // Wrap in Link if onClick is not provided
  if (!onClick) {
    return (
      <Link
        to={`/media/${media.id}`}
        style={{ textDecoration: 'none' }}
        onClick={handleCardClick}
      >
        {cardContent}
      </Link>
    );
  }

  return (
    <Box onClick={handleCardClick} sx={{ cursor: 'pointer' }}>
      {cardContent}
    </Box>
  );
};

export default MediaCard;
