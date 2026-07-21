import { Box, Grid, Typography, Button, useTheme } from '@mui/material';
import { ArrowForwardIos } from '@mui/icons-material';
import Section from '@/shared/Section';
import MixlistCard from '@/shared/MixlistCard';

// "Recent Mixlists" — shows up to 6 mixlists, an empty state with create/seed actions,
// and a "View More Mixlists" button that routes to the search page in mixlists mode.
const RecentMixlistsSection = ({ mixlists, loading, navigate, onCreateMixlist, onSeedMixlists }) => {
  const theme = useTheme();

  return (
    <>
      <Section title="Recent Mixlists">
        {loading ? (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <Typography variant="h6" color="text.secondary">Loading mixlists...</Typography>
          </Box>
        ) : mixlists.length > 6 && (
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3, textAlign: 'center' }}>
            Showing 6 of {mixlists.length} mixlists
          </Typography>
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
                            mr: { xs: 0, sm: 2 },
                            mb: { xs: 1, sm: 0 },
                            width: { xs: '100%', sm: 'auto' },
                            minHeight: '44px'
                        }}
                    >
                        Create New Mixlist
                    </Button>
                    <Button
                        variant="contained"
                        color="primary"
                        onClick={onSeedMixlists}
                        sx={{
                            mt: { xs: 1, sm: 2 },
                            width: { xs: '100%', sm: 'auto' },
                            minHeight: '44px'
                        }}
                    >
                        Seed Mixlists (Development)
                    </Button>
                </Grid>
            ) : (
                mixlists.slice(0, 6).map((item) => (
                    <Grid item key={`mixlist-${item.id || item.Id || item.name}`} xs={12} sm={6} md={4}>
                        <MixlistCard mixlist={item} onNavigate={navigate} />
                    </Grid>
                ))
            )}
        </Grid>
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
