import React, { useState } from 'react';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Navigation, Pagination, Autoplay, EffectCoverflow } from 'swiper/modules';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Box,
  IconButton,
  Typography,
  useTheme,
  useMediaQuery
} from '@mui/material';
import {
  ChevronLeft,
  ChevronRight,
  PlayArrow,
  Pause
} from '@mui/icons-material';
import { motion as motionDiv } from 'framer-motion';

// Import Swiper styles
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';
import 'swiper/css/effect-coverflow';

import MediaCard from './MediaCard';
import { COLORS, commonStyles } from './DesignSystem';

const MediaCarousel = ({
  mediaItems = [],
  title = 'Featured Media',
  subtitle,
  variant = 'coverflow', // 'coverflow', 'cards', 'simple'
  autoplay = true,
  autoplayDelay = 3000,
  showNavigation = true,
  showPagination = true,
  slidesPerView = 'auto',
  spaceBetween = 30,
  centeredSlides = true,
  loop = true,
  onMediaClick,
  sx = {},
  ...props
}) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [isPlaying, setIsPlaying] = useState(autoplay);
  const [swiperInstance, setSwiperInstance] = useState(null);

  // Carousel variants
  const carouselVariants = {
    coverflow: {
      effect: 'coverflow',
      grabCursor: true,
      centeredSlides: true,
      slidesPerView: isMobile ? 1.2 : 2.5,
      coverflowEffect: {
        rotate: 0,
        stretch: 0,
        depth: 200,
        modifier: 1.5,
        slideShadows: false,
      },
    },
    cards: {
      effect: 'cards',
      grabCursor: true,
      centeredSlides: true,
      slidesPerView: isMobile ? 1.2 : 2.5,
    },
    simple: {
      slidesPerView: isMobile ? 1.2 : 3,
      spaceBetween: 20,
      centeredSlides: false,
    }
  };

  const currentVariant = carouselVariants[variant];

  const handleSwiperInit = (swiper) => {
    setSwiperInstance(swiper);
  };

  const toggleAutoplay = () => {
    if (swiperInstance) {
      if (isPlaying) {
        swiperInstance.autoplay.stop();
      } else {
        swiperInstance.autoplay.start();
      }
      setIsPlaying(!isPlaying);
    }
  };

  const handleMediaClick = (media) => {
    onMediaClick?.(media);
  };

  if (!mediaItems || mediaItems.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="h6" color="text.secondary">
          No media items to display
        </Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ width: '100%', ...sx }} {...props}>
      {/* Header */}
      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h4" component="h2" gutterBottom>
            {title}
          </Typography>
          {subtitle && (
            <Typography variant="body1" color="text.secondary">
              {subtitle}
            </Typography>
          )}
        </Box>
        
        {/* Controls */}
        <Box sx={{ display: 'flex', gap: 1 }}>
          {autoplay && (
            <IconButton
              onClick={toggleAutoplay}
              sx={{
                backgroundColor: COLORS.background.paper,
                color: COLORS.primary.main,
                '&:hover': {
                  backgroundColor: COLORS.background.elevated
                }
              }}
            >
              {isPlaying ? <Pause /> : <PlayArrow />}
            </IconButton>
          )}
        </Box>
      </Box>

      {/* Carousel */}
      <Box sx={{ 
        position: 'relative',
        '& .swiper-slide': {
          zIndex: 1,
        },
        '& .swiper-slide-active': {
          zIndex: 10
        },
        '& .swiper-slide-prev, & .swiper-slide-next': {
          zIndex: 5
        }
      }}>
        <Swiper
          modules={[Navigation, Pagination, Autoplay, EffectCoverflow]}
          {...currentVariant}
          spaceBetween={spaceBetween}
          loop={loop}
          allowTouchMove={true}
          preventClicks={false}
          preventClicksPropagation={false}
          simulateTouch={true}
          touchStartPreventDefault={false}
          autoplay={autoplay ? {
            delay: autoplayDelay,
            disableOnInteraction: false,
            pauseOnMouseEnter: true
          } : false}
          navigation={showNavigation ? {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
          } : false}
          pagination={showPagination ? {
            clickable: true,
            dynamicBullets: true,
          } : false}
          onSwiper={handleSwiperInit}
          style={{
            padding: '20px 0',
            '--swiper-navigation-color': COLORS.primary.main,
            '--swiper-pagination-color': COLORS.primary.main,
            '--swiper-pagination-bullet-inactive-color': COLORS.background.elevated,
            '--swiper-pagination-bullet-inactive-opacity': 0.5,
          }}
        >
          {mediaItems.map((media, index) => (
            <SwiperSlide key={media.id || index}>
              <Box
                component={motionDiv}
                whileHover={{ 
                  scale: 1.05,
                  transition: { duration: 0.3 }
                }}
                whileTap={{ scale: 0.95 }}
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  console.log('Wrapper clicked for media:', media);
                  handleMediaClick(media);
                }}
                sx={{ 
                  cursor: 'pointer', 
                  userSelect: 'none',
                  '&:hover': {
                    '& .media-card-overlay': {
                      opacity: 1
                    }
                  }
                }}
              >
                <Box
                  sx={{
                    height: variant === 'coverflow' ? '400px' : '350px',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    position: 'relative'
                  }}
                >
                  {/* Click overlay indicator */}
                  <Box
                    className="media-card-overlay"
                    sx={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      bottom: 0,
                      backgroundColor: 'rgba(0, 0, 0, 0.1)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      borderRadius: 2,
                      opacity: 0,
                      transition: 'opacity 0.2s ease',
                      zIndex: 10,
                      pointerEvents: 'none'
                    }}
                  >
                    <Typography variant="body2" sx={{ color: 'white', fontWeight: 'bold' }}>
                      Click to view details
                    </Typography>
                  </Box>
                  <MediaCard
                    media={media}
                    variant={variant === 'coverflow' ? 'featured' : 'default'}
                    onClick={null}
                    showMediaTypeIcon={false}
                    sx={{
                      width: '100%',
                      maxWidth: variant === 'coverflow' ? '300px' : '280px',
                      height: '100%',
                      pointerEvents: 'none'
                    }}
                  />
                </Box>
              </Box>
            </SwiperSlide>
          ))}
        </Swiper>

        {/* Custom Navigation Buttons */}
        {showNavigation && (
          <>
            <IconButton
              className="swiper-button-prev"
              sx={{
                position: 'absolute',
                left: 10,
                top: '50%',
                transform: 'translateY(-50%)',
                zIndex: 10,
                backgroundColor: COLORS.background.paper,
                color: COLORS.primary.main,
                '&:hover': {
                  backgroundColor: COLORS.background.elevated
                },
                '&.swiper-button-disabled': {
                  opacity: 0.3,
                  cursor: 'not-allowed'
                }
              }}
            >
              <ChevronLeft />
            </IconButton>
            <IconButton
              className="swiper-button-next"
              sx={{
                position: 'absolute',
                right: 10,
                top: '50%',
                transform: 'translateY(-50%)',
                zIndex: 10,
                backgroundColor: COLORS.background.paper,
                color: COLORS.primary.main,
                '&:hover': {
                  backgroundColor: COLORS.background.elevated
                },
                '&.swiper-button-disabled': {
                  opacity: 0.3,
                  cursor: 'not-allowed'
                }
              }}
            >
              <ChevronRight />
            </IconButton>
          </>
        )}
      </Box>

      {/* Custom Pagination */}
      {showPagination && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            mt: 2,
            '& .swiper-pagination-bullet': {
              backgroundColor: COLORS.primary.main,
              opacity: 0.5,
              '&.swiper-pagination-bullet-active': {
                opacity: 1,
                backgroundColor: COLORS.primary.main
              }
            }
          }}
        />
      )}
    </Box>
  );
};

export default MediaCarousel;
