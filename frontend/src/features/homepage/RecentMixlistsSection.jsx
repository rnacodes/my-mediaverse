import { useState } from 'react';
import { Box, Grid, Typography, Button, IconButton, useTheme } from '@mui/material';
import { ArrowForwardIos, ChevronLeft, ChevronRight } from '@mui/icons-material';
import Section from '@/shared/Section';
import MixlistCard from '@/shared/MixlistCard';

const VISIBLE_MIXLISTS = 3;

// "Recent Mixlists" — shows three mixlists at a time, an empty state with a create
// action, and a "View More Mixlists" button that routes to the search page in mixlists
// mode. With more than three, arrows step through the list one card at a time, wrapping
// at either end.
const RecentMixlistsSection = ({ mixlists, loading, navigate, onCreateMixlist }) => {
  const theme = useTheme();
  const [startIndex, setStartIndex] = useState(0);

  const scrollable = mixlists.length > VISIBLE_MIXLISTS;
  const visibleMixlists = scrollable
    ? Array.from({ length: VISIBLE_MIXLISTS }, (_, i) => mixlists[(startIndex + i) % mixlists.length])
    : mixlists;

  const handlePrevious = () => setStartIndex((prev) => (prev - 1 + mixlists.length) % mixlists.length);
  const handleNext = () => setStartIndex((prev) => (prev + 1) % mixlists.length);

  const arrowSx = {
    position: 'absolute',
    top: '50%',
    transform: 'translateY(-50%)',
    zIndex: 2,
    backgroundColor: 'background.paper',
    boxShadow: 2,
    '&:hover': { backgroundColor: 'background.default' }
  };

  return (
    <>
      <Section title="Recent Mixlists">
        {loading ? (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <Typography variant="h6" color="text.secondary">Loading mixlists...</Typography>
          </Box>
        ) : scrollable && (
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3, textAlign: 'center' }}>
            Showing {VISIBLE_MIXLISTS} of {mixlists.length} mixlists
          </Typography>
        )}
        <Box sx={{ position: 'relative', px: scrollable ? { xs: 5, sm: 6 } : 0 }}>
          {scrollable && (
            <IconButton onClick={handlePrevious} aria-label="Previous mixlists" sx={{ ...arrowSx, left: { xs: 0, sm: -20 } }}>
              <ChevronLeft />
            </IconButton>
          )}
          <Grid container spacing={4}>
              {mixlists.length === 0 ? (
                  <Grid item xs={12} sx={{ textAlign: 'center' }}>
                      <Typography variant="h6" color="text.secondary">No mixlists found. Create one to get started!</Typography>
                      <Button
                          variant="contained"
                          color="primary"
                          onClick={onCreateMixlist}
                          sx={{
                              mt: 2,
                              width: { xs: '100%', sm: 'auto' },
                              minHeight: '44px'
                          }}
                      >
                          Create New Mixlist
                      </Button>
                  </Grid>
              ) : (
                  visibleMixlists.map((item) => (
                      <Grid item key={`mixlist-${item.id || item.Id || item.name}`} xs={12} sm={6} md={4}>
                          <MixlistCard mixlist={item} onNavigate={navigate} />
                      </Grid>
                  ))
              )}
          </Grid>
          {scrollable && (
            <IconButton onClick={handleNext} aria-label="Next mixlists" sx={{ ...arrowSx, right: { xs: 0, sm: -20 } }}>
              <ChevronRight />
            </IconButton>
          )}
        </Box>
      </Section>

      {/* View More Button */}
      <Box sx={{ display: 'flex', justifyContent: 'center', my: { xs: 3, sm: 4, md: 6 }, px: { xs: 2, sm: 0 } }}>
          <Button
              variant="contained"
              color="secondary"
              size="large"
              endIcon={<ArrowForwardIos />}
              onClick={() => navigate('/search?searchMode=mixlists')}
              sx={{
                  fontSize: { xs: '1rem', sm: '1.1rem', md: '1.2rem' },
                  padding: { xs: '10px 20px', sm: '12px 30px' },
                  mb: { xs: 2, sm: 3, md: 4 },
                  minWidth: { xs: '250px', sm: '300px' },
                  width: { xs: '100%', sm: 'auto' },
                  maxWidth: { xs: '400px', sm: 'none' },
                  color: theme.palette.background.default,
                  backgroundColor: theme.palette.text.primary,
                  minHeight: '48px'
              }}
          >
              View More Mixlists
          </Button>
      </Box>
    </>
  );
};

export default RecentMixlistsSection;
