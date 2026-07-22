import React, { useState } from 'react';
import {
  Box, Typography, Card, CardContent, CardMedia, IconButton, Chip
} from '@mui/material';
import { ChevronLeft, ChevronRight } from '@mui/icons-material';
import { formatMediaType } from '@/utils/formatters';
import { resolveMediaImage, getPlaceholderImage } from '@/utils/mediaImageUtils';

const VISIBLE_ITEMS = 5;
const MAX_DOTS = 6;

const SimpleMediaCarousel = ({
  mediaItems = [],
  title = 'Featured Media',
  subtitle,
  onMediaClick,
  cardWidth = 320,
  cardHeight = 380,
  imageHeight,
  showCardContent = true,
  // Nav arrows appear only when item count exceeds this threshold. Defaults to 1
  // (arrows whenever there's more than one item); callers can raise it to hide the
  // arrows for small sets that already fit on screen.
  arrowThreshold = 1,
  sx = {},
  ...props
}) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const effectiveImageHeight = imageHeight ?? (showCardContent ? Math.round(cardHeight * 0.58) : cardHeight);
  
  if (!mediaItems || mediaItems.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="h6" color="text.secondary">
          No media items to display
        </Typography>
      </Box>
    );
  }

  const handlePrevious = () => {
    setCurrentIndex((prev) => (prev - 1 + mediaItems.length) % mediaItems.length);
  };

  const handleNext = () => {
    setCurrentIndex((prev) => (prev + 1) % mediaItems.length);
  };

  const handleMediaClick = (media) => {
    if (onMediaClick) {
      onMediaClick(media);
    }
  };

  const getMediaTypeColor = (mediaType) => {
    const colors = {
      'Podcast': '#9C27B0', 'Book': '#2196F3', 'Movie': '#FF5722',
      'Article': '#4CAF50', 'Video': '#FF9800', 'Music': '#E91E63',
      'VideoGame': '#673AB7', 'TVShow': '#795548', 'Website': '#607D8B',
      'Document': '#3F51B5', 'Other': '#9E9E9E'
    };
    return colors[mediaType] || colors['Other'];
  };

  // Show current item and 2 items on each side (5 total)
  const getVisibleItems = () => {
    if (mediaItems.length <= VISIBLE_ITEMS) {
      return mediaItems.map((item, index) => ({ ...item, offset: index - currentIndex }));
    }

    const visible = [];
    for (let i = -2; i <= 2; i++) {
      const index = (currentIndex + i + mediaItems.length) % mediaItems.length;
      visible.push({ ...mediaItems[index], offset: i });
    }
    return visible;
  };

  const visibleItems = getVisibleItems();

  const showDots = mediaItems.length > VISIBLE_ITEMS;
  const dotCount = Math.min(mediaItems.length, MAX_DOTS);
  const activeDotIndex = dotCount >= mediaItems.length
    ? currentIndex
    : Math.min(dotCount - 1, Math.floor((currentIndex / mediaItems.length) * dotCount));
  const handleDotClick = (dotIdx) => {
    const target = dotCount >= mediaItems.length
      ? dotIdx
      : Math.round((dotIdx / dotCount) * mediaItems.length);
    setCurrentIndex(target % mediaItems.length);
  };

  return (
    <Box sx={{ width: '100%', ...sx }} {...props}>
      {/* Header */}
      <Box sx={{ mb: 3, textAlign: 'center' }}>
        <Typography variant="h4" component="h2" gutterBottom sx={{ fontSize: '1.8rem', fontWeight: 'bold' }}>
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body1" color="text.secondary" sx={{ fontSize: '1.1rem' }}>
            {subtitle}
          </Typography>
        )}
      </Box>

      {/* Carousel */}
      <Box sx={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
        {/* Previous Button */}
        {mediaItems.length > arrowThreshold && (
          <IconButton
            onClick={handlePrevious}
            sx={{
              position: 'absolute',
              left: -20,
              zIndex: 2,
              backgroundColor: 'background.paper',
              boxShadow: 2,
              '&:hover': { backgroundColor: 'background.default' }
            }}
          >
            <ChevronLeft />
          </IconButton>
        )}

        {/* Media Items */}
        <Box sx={{ 
          display: 'flex', 
          justifyContent: 'center', 
          alignItems: 'center',
          gap: 1, // Reduced from 2 to 1 for tighter spacing
          overflow: 'hidden',
          px: 6
        }}>
          {visibleItems.map((media) => {
            const isCenter = media.offset === 0;

            return (
              <Card
                key={`carousel-${media.id || media.Id}-${media.offset}`}
                sx={{
                  minWidth: cardWidth,
                  maxWidth: cardWidth,
                  height: cardHeight,
                  transition: 'all 0.3s ease',
                  cursor: 'pointer',
                  '&:hover': {
                    transform: 'scale(1.05)',
                    zIndex: 1
                  }
                }}
                onClick={() => handleMediaClick(media)}
              >
                <CardMedia
                  component="img"
                  height={effectiveImageHeight}
                  image={resolveMediaImage(media)}
                  alt={media.title || media.Title}
                  onError={(e) => { e.target.onerror = null; e.target.src = getPlaceholderImage(media.mediaType || media.MediaType); }}
                  sx={{
                    width: '100%',
                    height: effectiveImageHeight,
                    objectFit: 'contain',
                    backgroundColor: 'rgba(0, 0, 0, 0.35)',
                    p: 1
                  }}
                />
                {showCardContent && (
                  <CardContent sx={{ p: 2.5, textAlign: 'center' }}>
                    <Typography 
                      variant="body2" 
                      sx={{ 
                        fontWeight: 'bold',
                        fontSize: isCenter ? '1rem' : '0.9rem',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        lineHeight: 1.3
                      }}
                    >
                      {media.title || media.Title}
                    </Typography>
                    <Chip
                      label={formatMediaType(media.mediaType || media.MediaType)}
                      size="small"
                      sx={{
                        backgroundColor: getMediaTypeColor(media.mediaType || media.MediaType),
                        color: 'white',
                        fontSize: '0.8rem',
                        mt: 1.5,
                        height: '24px'
                      }}
                    />
                  </CardContent>
                )}
              </Card>
            );
          })}
        </Box>

        {/* Next Button */}
        {mediaItems.length > arrowThreshold && (
          <IconButton
            onClick={handleNext}
            sx={{
              position: 'absolute',
              right: -20,
              zIndex: 2,
              backgroundColor: 'background.paper',
              boxShadow: 2,
              '&:hover': { backgroundColor: 'background.default' }
            }}
          >
            <ChevronRight />
          </IconButton>
        )}
      </Box>

      {/* Pagination Dots — only when there's more than fits on screen, capped at MAX_DOTS */}
      {showDots && (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 3 }}>
          {Array.from({ length: dotCount }, (_, index) => index).map((index) => (
            <Box
              key={`dot-of-${dotCount}-${index}`}
              onClick={() => handleDotClick(index)}
              sx={{
                width: 10,
                height: 10,
                borderRadius: '50%',
                backgroundColor: index === activeDotIndex ? 'primary.main' : 'grey.300',
                cursor: 'pointer',
                transition: 'background-color 0.3s ease',
                '&:hover': {
                  backgroundColor: index === activeDotIndex ? 'primary.dark' : 'grey.400'
                }
              }}
            />
          ))}
        </Box>
      )}
    </Box>
  );
};

export default SimpleMediaCarousel;
