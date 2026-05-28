import { Box, Typography, Button, CircularProgress } from '@mui/material';
import Section from '../shared/Section';
import SimpleMediaCarousel from '../shared/SimpleMediaCarousel';

// "Jump back in" — a carousel of media marked as actively exploring, with loading,
// error, and empty states. Clicking an item routes to the appropriate profile.
const ActivelyExploringCarousel = ({ items, loading, error, navigate, onAddMedia }) => (
  <Section title="Jump back in">
    {loading ? (
      <Box sx={{ textAlign: 'center', py: 6 }}>
        <CircularProgress size={40} sx={{ mb: 2 }} />
        <Typography variant="h6" color="text.secondary">Loading your active explorations...</Typography>
      </Box>
    ) : error ? (
      <Box sx={{ textAlign: 'center', py: { xs: 4, sm: 6 }, px: 2 }}>
        <Typography
            variant="h6"
            color="error"
            sx={{
                mb: 2,
                fontSize: { xs: '1rem', sm: '1.25rem' }
            }}
        >
            {error}
        </Typography>
        <Button
            variant="outlined"
            onClick={() => window.location.reload()}
            sx={{ minHeight: '44px', px: 3 }}
        >
          Retry
        </Button>
      </Box>
    ) : items.length > 0 ? (
      <SimpleMediaCarousel
        mediaItems={items}
        title=""
        subtitle="Click on any item to jump to its profile"
        onMediaClick={(media) => {
          if (media.mediaType === 'Podcast' && !media.seriesId) {
            navigate(`/podcast-series/${media.id || media.Id}`);
          } else if (media.mediaType === 'Channel') {
            navigate(`/youtube-channel/${media.id || media.Id}`);
          } else {
            navigate(`/media/${media.id || media.Id}`);
          }
        }}
        cardWidth={250}
        cardHeight={350}
        showCardContent={false}
      />
    ) : (
      <Box sx={{ textAlign: 'center', py: { xs: 4, sm: 6 }, px: 2 }}>
        <Typography
            variant="h6"
            color="text.secondary"
            sx={{
                mb: 2,
                fontSize: { xs: '1rem', sm: '1.25rem' }
            }}
        >
          No active explorations found
        </Typography>
        <Typography
            variant="body2"
            color="text.secondary"
            sx={{
                mb: 3,
                fontSize: { xs: '0.875rem', sm: '0.875rem' }
            }}
        >
          Start exploring some media and mark them as &quot;Actively Exploring&quot; to see them here
        </Typography>
        <Button
          variant="contained"
          onClick={onAddMedia}
          sx={{
              mr: { xs: 0, sm: 2 },
              mb: { xs: 2, sm: 0 },
              width: { xs: '100%', sm: 'auto' },
              minHeight: '44px'
          }}
        >
          Add Media
        </Button>
        <Button
          variant="contained"
          color="secondary"
          onClick={() => navigate('/all-media')}
          sx={{
              width: { xs: '100%', sm: 'auto' },
              minHeight: '44px',
              color: 'white'
          }}
        >
          Browse All Media
        </Button>
      </Box>
    )}
  </Section>
);

export default ActivelyExploringCarousel;
