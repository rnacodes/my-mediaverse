import React from 'react';
import {
  Box,
  CircularProgress,
  Typography,
  Skeleton,
  Grid
} from '@mui/material';
import { COLORS } from './DesignSystem';

const LoadingSpinner = ({
  variant = 'spinner', // 'spinner', 'skeleton', 'dots', 'pulse'
  size = 'medium', // 'small', 'medium', 'large'
  message = 'Loading...',
  showMessage = true,
  fullScreen = false,
  sx = {},
  ...props
}) => {
  const getSize = () => {
    switch (size) {
      case 'small': return 24;
      case 'large': return 60;
      default: return 40;
    }
  };

  const getMessageSize = () => {
    switch (size) {
      case 'small': return 'body2';
      case 'large': return 'h6';
      default: return 'body1';
    }
  };

  // Skeleton loader for content
  const SkeletonLoader = () => (
    <Box sx={{ width: '100%' }}>
      <Grid container spacing={2}>
        {['s1', 's2', 's3', 's4', 's5', 's6'].map((id) => (
          <Grid item xs={12} sm={6} md={4} key={id}>
            <Skeleton
              variant="rectangular"
              height={200}
              sx={{
                borderRadius: '16px',
                backgroundColor: COLORS.background.elevated
              }}
            />
            <Box sx={{ mt: 1 }}>
              <Skeleton
                variant="text"
                width="80%"
                sx={{ backgroundColor: COLORS.background.elevated }}
              />
              <Skeleton
                variant="text"
                width="60%"
                sx={{ backgroundColor: COLORS.background.elevated }}
              />
            </Box>
          </Grid>
        ))}
      </Grid>
    </Box>
  );

  // Dots loader
  const DotsLoader = () => (
    <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 1 }}>
      {[0, 1, 2].map((i) => (
        <Box
          key={`dot-${i}`}
          sx={{
            width: 8,
            height: 8,
            borderRadius: '50%',
            backgroundColor: COLORS.primary.main,
            animation: 'pulse 1.4s ease-in-out infinite both',
            animationDelay: `${i * 0.16}s`
          }}
        />
      ))}
    </Box>
  );

  // Pulse loader
  const PulseLoader = () => (
    <Box
      sx={{
        width: getSize(),
        height: getSize(),
        borderRadius: '50%',
        backgroundColor: COLORS.primary.main,
        animation: 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite'
      }}
    />
  );

  const renderLoader = () => {
    switch (variant) {
      case 'skeleton':
        return <SkeletonLoader />;
      case 'dots':
        return <DotsLoader />;
      case 'pulse':
        return <PulseLoader />;
      default:
        return (
          <CircularProgress
            size={getSize()}
            sx={{
              color: COLORS.primary.main,
              '& .MuiCircularProgress-circle': {
                strokeLinecap: 'round'
              }
            }}
          />
        );
    }
  };

  const content = (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        ...(fullScreen && {
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: COLORS.background.default,
          zIndex: 9999
        }),
        ...sx
      }}
      {...props}
    >
      {renderLoader()}
      {showMessage && message && (
        <Typography
          variant={getMessageSize()}
          color="text.secondary"
          sx={{ textAlign: 'center' }}
        >
          {message}
        </Typography>
      )}
    </Box>
  );

  return content;
};

// Add CSS animations (only once)
if (!document.getElementById('loading-spinner-keyframes')) {
  const style = document.createElement('style');
  style.id = 'loading-spinner-keyframes';
  style.textContent = `
    @keyframes pulse {
      0%, 80%, 100% {
        opacity: 1;
        transform: scale(1);
      }
      40% {
        opacity: 0.5;
        transform: scale(0.8);
      }
    }
  `;
  document.head.appendChild(style);
}

export default LoadingSpinner;
