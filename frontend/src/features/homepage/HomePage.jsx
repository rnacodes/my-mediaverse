import { useNavigate } from 'react-router-dom';
import { Container, Box, Typography, Button, useTheme, CircularProgress } from '@mui/material';
import Section from '@/shared/Section';
import useHomepageData from './useHomepageData';
import HomePageHeader from './HomePageHeader';
import MediaTypeNav from './MediaTypeNav';
import QuickActionsBar from './QuickActionsBar';
import ActivelyExploringCarousel from './ActivelyExploringCarousel';
import RecentMixlistsSection from './RecentMixlistsSection';

// Homepage shell: composes data via useHomepageData, owns navigation handlers and the
// page-level loading/error gates, and arranges the section components. The whole page
// is gated on the mixlists query (its loading/error stand in for the page).
export default function HomePage() {
  const theme = useTheme();
  const navigate = useNavigate();

  const {
    mixlists,
    mixlistsLoading,
    mixlistsError,
    activelyExploringMedia,
    activelyExploringLoading,
    activelyExploringError,
    wakingUp,
    seedMutation,
  } = useHomepageData();

  const handleCreateMixlist = () => navigate('/create-mixlist', { state: { returnTo: '/' } });
  const handleImportMedia = () => navigate('/import-media');
  const handleSearchByTopicOrGenre = () => navigate('/search-by-topic-genre');
  const handleAddMedia = () => navigate('/add-media');
  const handleSourceDirectory = () => navigate('/sources');
  const handleSeedMixlists = () => {
    seedMutation.mutate(undefined, {
      onError: (error) => console.error('Error seeding mixlists:', error),
    });
  };

  return (
    <Box sx={{ backgroundColor: theme.palette.background.default, minHeight: '100vh', width: '100%' }}>
      {mixlistsLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
          <Box sx={{ textAlign: 'center' }}>
            <CircularProgress size={60} sx={{ mb: 2 }} />
            <Typography variant="h6" color="text.secondary">
              {wakingUp ? "Waking up the server..." : "Loading My MediaVerse..."}
            </Typography>
            {wakingUp && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1, px: 2 }}>
                This may take a moment on first visit
              </Typography>
            )}
          </Box>
        </Box>
      ) : mixlistsError ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
          <Box sx={{ textAlign: 'center' }}>
            <Typography
                variant="h6"
                color="error"
                sx={{
                    mb: 2,
                    fontSize: { xs: '1rem', sm: '1.25rem' },
                    px: 2
                }}
            >
                {mixlistsError}
            </Typography>
            <Button
                variant="contained"
                onClick={() => window.location.reload()}
                sx={{ minHeight: '44px', px: 3 }}
            >
              Retry
            </Button>
          </Box>
        </Box>
      ) : (
        <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 3, md: 4 }, mx: 'auto', px: { xs: 2, sm: 3 } }}>

          <HomePageHeader
            onSearch={(query) => navigate(`/search?q=${encodeURIComponent(query)}`)}
          />

          {/* Media Icons and Actions Section */}
          <Section title="">
            <MediaTypeNav navigate={navigate} />
            <QuickActionsBar
              onSourceDirectory={handleSourceDirectory}
              onCreateMixlist={handleCreateMixlist}
              onImportMedia={handleImportMedia}
              onSearchByTopicOrGenre={handleSearchByTopicOrGenre}
              onAddMedia={handleAddMedia}
            />
          </Section>

          <ActivelyExploringCarousel
            items={activelyExploringMedia}
            loading={activelyExploringLoading}
            error={activelyExploringError}
            navigate={navigate}
            onAddMedia={handleAddMedia}
          />

          <RecentMixlistsSection
            mixlists={mixlists}
            loading={mixlistsLoading}
            navigate={navigate}
            onCreateMixlist={handleCreateMixlist}
            onSeedMixlists={handleSeedMixlists}
          />

        </Container>
      )}
    </Box>
  );
}
