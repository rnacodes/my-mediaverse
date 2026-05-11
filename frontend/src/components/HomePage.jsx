import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Box, Typography, TextField, IconButton, Grid, Card, CardMedia, CardContent, Button, useTheme, CircularProgress } from '@mui/material';
import {
    Search, Book, Movie, Tv, Article, LibraryMusic, Podcasts, SportsEsports, YouTube, Language, MenuBook, AutoAwesome,
    AddCircleOutline, BookmarkAdd, Settings, Info, Help, Share, AccountCircle, ArrowForwardIos,
    NoteAlt, ImportExport, Topic, FileDownload, LocalLibrary, Apps, FormatQuote
} from '@mui/icons-material';
import { getAllMixlists, seedMixlists } from '../api/mixlistService';
import { getAllMedia } from '../api/mediaService';

// MOCK DATA
const mainMediaIcons = [
    { name: 'Articles', icon: <Article sx={{ fontSize: 40 }} />, mediaType: 'Article', supported: true },
    { name: 'Books', icon: <Book sx={{ fontSize: 40 }} />, mediaType: 'Book', supported: true },
    { name: 'Courses', icon: <LocalLibrary sx={{ fontSize: 40 }} />, mediaType: 'Course', supported: false },
    { name: 'Documents', icon: <NoteAlt sx={{ fontSize: 40 }} />, mediaType: 'Document', supported: false },
    { name: 'Highlights', icon: <FormatQuote sx={{ fontSize: 40 }} />, mediaType: 'Highlight', supported: true },
    { name: 'Movies', icon: <Movie sx={{ fontSize: 40 }} />, mediaType: 'Movie', supported: true },
    { name: 'Music', icon: <LibraryMusic sx={{ fontSize: 40 }} />, mediaType: 'Music', supported: false },
    { name: 'Online Videos', icon: <YouTube sx={{ fontSize: 40 }} />, mediaType: 'Video,Channel,Playlist', supported: true },
    { name: 'Podcasts', icon: <Podcasts sx={{ fontSize: 40 }} />, mediaType: 'Podcast', supported: true },
    { name: 'TV Shows', icon: <Tv sx={{ fontSize: 40 }} />, mediaType: 'TVShow', supported: true },
    { name: 'Video Games', icon: <SportsEsports sx={{ fontSize: 40 }} />, mediaType: 'VideoGame', supported: false },
    { name: 'Websites', icon: <Language sx={{ fontSize: 40 }} />, mediaType: 'Website', supported: true },
];

const specialMediaIcons = [
    { name: 'Online Notebook', icon: <MenuBook sx={{ fontSize: 40 }} />, key: 'zk', mediaType: 'Note', supported: true },
    { name: 'Panorama', icon: <AutoAwesome sx={{ fontSize: 40 }} />, key: 'panorama', supported: false, caption: 'For everything else - coming soon!' },
];


// COMPONENTS
import SearchBar from './shared/SearchBar';
import SimpleMediaCarousel from './shared/SimpleMediaCarousel';

    const MixlistCard = ({ mixlist, onNavigate }) => (
  <Card 
    sx={{ 
      height: '100%', 
      display: 'flex', 
      flexDirection: 'column',
      cursor: 'pointer',
      '&:hover': {
        transform: 'translateY(-4px) scale(1.02)',
        boxShadow: 8,
        '& .MuiCardMedia-root': {
          transform: 'scale(1.05)'
        }
      },
      transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
    }}
    onClick={() => onNavigate(`/mixlist/${mixlist.id || mixlist.Id}`)}
  >
    <CardMedia
      component="img"
      sx={{ 
        flexShrink: 0, 
        height: 180,
        transition: 'transform 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
      }}
              image={mixlist.thumbnail || mixlist.Thumbnail}
        alt={mixlist.name || mixlist.Name}
      onError={(e) => { e.target.onerror = null; e.target.src = 'https://placehold.co/600x400/1e1e1e/fcfafa?text=Image+Error'; }}
    />
    <CardContent sx={{ flexGrow: 1 }}>
      <Typography gutterBottom variant="h6" component="div" sx={{ fontWeight: 'bold' }}>
        {mixlist.name || mixlist.Name}
      </Typography>
      <Typography variant="body2" color="#ffffff">
        {mixlist.description || mixlist.Description}
      </Typography>
    </CardContent>
  </Card>
);

const Section = ({ title, children }) => (
    <Box sx={{ my: { xs: 3, sm: 4, md: 6 } }}>
        {title && (
            <Typography 
                variant="h4" 
                sx={{ 
                    fontSize: { xs: '1.5rem', sm: '1.8rem', md: '2.125rem' },
                    mb: { xs: 2, sm: 3 }
                }}
            >
                {title}
            </Typography>
        )}
        {children}
    </Box>
);

// MAIN HOMEPAGE COMPONENT
export default function HomePage() {
  const theme = useTheme();
  const navigate = useNavigate();
  const [mixlists, setMixlists] = useState([]);
  const [mixlistsLoading, setMixlistsLoading] = useState(true);
  const [mixlistsError, setMixlistsError] = useState(null);
  const [activelyExploringMedia, setActivelyExploringMedia] = useState([]);
  const [activelyExploringLoading, setActivelyExploringLoading] = useState(true);
  const [activelyExploringError, setActivelyExploringError] = useState(null);
  const [wakingUp, setWakingUp] = useState(false);

  useEffect(() => {
    let cancelled = false;
    const TIMEOUT_MS = 30000;
    const MAX_RETRIES = 2;

    const fetchWithRetry = async (apiFn, onSuccess, onError, attempt = 0) => {
      try {
        const timeoutPromise = new Promise((_, reject) =>
          setTimeout(() => reject(new Error('TIMEOUT')), TIMEOUT_MS)
        );
        const response = await Promise.race([apiFn(), timeoutPromise]);
        if (!cancelled) onSuccess(response);
      } catch (error) {
        if (cancelled) return;
        const isRetryable = error.message === 'TIMEOUT' ||
          error.code === 'ERR_NETWORK' || error.message === 'Network Error';

        if (isRetryable && attempt < MAX_RETRIES) {
          setWakingUp(true);
          return fetchWithRetry(apiFn, onSuccess, onError, attempt + 1);
        }
        onError(error);
      }
    };

    const getErrorMessage = (error, context) => {
      if (error.message === 'TIMEOUT') {
        return `Request timed out. The server may still be starting up — please try again.`;
      } else if (error.code === 'ERR_NETWORK' || error.message === 'Network Error') {
        return "Unable to connect to the server. Please make sure the backend API is running.";
      } else if (error.response?.status === 404) {
        return "API endpoint not found. Please check the backend configuration.";
      } else if (error.response?.status >= 500) {
        return "Server error occurred. Please try again later.";
      }
      return `Failed to load ${context}. Please check your connection.`;
    };

    fetchWithRetry(
      getAllMixlists,
      (response) => {
        setMixlists(response.data);
        setMixlistsLoading(false);
        setWakingUp(false);
      },
      (error) => {
        console.error("Error fetching mixlists:", error);
        setWakingUp(false);
        setMixlistsError(getErrorMessage(error, "mixlists"));
        setMixlistsLoading(false);
      }
    );

    fetchWithRetry(
      getAllMedia,
      (response) => {
        const activelyExploring = response.data.filter(item => {
          const status = item.status || item.Status;
          return status && (
            status.toLowerCase() === 'actively exploring' ||
            status.toLowerCase() === 'activelyexploring' ||
            status.toLowerCase() === 'inprogress'
          );
        });
        setActivelyExploringMedia(activelyExploring);
        setActivelyExploringLoading(false);
      },
      (error) => {
        console.error("Error fetching actively exploring media:", error);
        setActivelyExploringError(getErrorMessage(error, "actively exploring media"));
        setActivelyExploringLoading(false);
      }
    );

    return () => { cancelled = true; };
  }, []);

  const handleCreateMixlist = () => {
    navigate('/create-mixlist', { state: { returnTo: '/' } });
  };

  const handleImportMedia = () => {
    navigate('/import-media');
  };

  const handleSearchByTopicOrGenre = () => {
    navigate('/search-by-topic-genre');
  };

  const handleAddMedia = () => {
    navigate('/add-media');
  };

  const handleSourceDirectory = () => {
    navigate('/sources');
  };

  const handleSeedMixlists = async () => {
    try {
      setMixlistsLoading(true);
      await seedMixlists();
      // Refresh the mixlists after seeding
      const response = await getAllMixlists();
      setMixlists(response.data);
    } catch (error) {
      console.error("Error seeding mixlists:", error);
    } finally {
      setMixlistsLoading(false);
    }
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
        
        {/* Header and Search Section */}
        <Box sx={{ textAlign: 'center', my: { xs: 2, sm: 3, md: 4 }, px: { xs: 1, sm: 2 } }}>
          <Typography 
            variant="h1" 
            sx={{ 
              fontSize: { xs: '2.5rem', sm: '3.5rem', md: '4rem' },
              mb: { xs: 2, sm: 3 }
            }}
          >
            My MediaVerse
          </Typography>
          <SearchBar 
            placeholder="Your next adventure awaits..."
            onSearch={(query) => {
              navigate(`/search?q=${encodeURIComponent(query)}`);
            }}
            showSuggestions={true}
          />
        </Box>

        {/* Media Icons and Actions Section */}
        <Section title="">
            {/* Alphabetized Icons */}
            <Box sx={{ display: 'flex', justifyContent: 'center', width: '100%' }}>
              <Grid container spacing={{ xs: 1, sm: 2 }} justifyContent="center" sx={{ mt: { xs: 2, sm: 3, md: 4 }, mb: 2, maxWidth: '900px' }}>
                {mainMediaIcons.map((item, index) => (
                    <Grid item xs={4} sm={3} md={2} key={`media-${index}`} sx={{ display: 'flex', justifyContent: 'center' }}>
                        <Box
                            onClick={() => {
                                if (item.supported) {
                                    navigate(`/search?mediaType=${item.mediaType}`);
                                }
                            }}
                            sx={{ 
                                display: 'flex', 
                                flexDirection: 'column', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                cursor: item.supported ? 'pointer' : 'default', 
                                color: item.supported ? 'text.secondary' : 'text.disabled',
                                minHeight: { xs: '60px', sm: '70px' },
                                minWidth: { xs: '60px', sm: '70px' },
                                p: { xs: 1, sm: 1.5 },
                                borderRadius: '12px',
                                transition: 'all 0.2s ease',
                                opacity: item.supported ? 1 : 0.5,
                                '&:hover': item.supported ? { 
                                    color: 'text.primary',
                                    transform: 'scale(1.05)',
                                    backgroundColor: 'rgba(255, 255, 255, 0.05)'
                                } : {},
                                '&:active': item.supported ? {
                                    transform: 'scale(0.98)'
                                } : {}
                            }}
                        >
                            {React.cloneElement(item.icon, { 
                                sx: { fontSize: { xs: 32, sm: 40 } } 
                            })}
                            <Typography 
                                variant="caption" 
                                sx={{ 
                                    mt: 0.5,
                                    fontSize: { xs: '0.65rem', sm: '0.75rem' },
                                    lineHeight: 1.2,
                                    textAlign: 'center'
                                }}
                            >
                                {item.name}
                            </Typography>
                            {!item.supported && (
                                <Typography 
                                    variant="caption" 
                                    sx={{ 
                                        fontSize: { xs: '0.55rem', sm: '0.65rem' },
                                        color: 'text.disabled',
                                        fontStyle: 'italic',
                                        textAlign: 'center'
                                    }}
                                >
                                    Coming Soon
                                </Typography>
                            )}
                        </Box>
                    </Grid>
                ))}
              </Grid>
            </Box>
            {/* Special, larger icons */}
            <Box sx={{ display: 'flex', justifyContent: 'center', width: '100%' }}>
              <Grid container spacing={{ xs: 1, sm: 2 }} justifyContent="center" sx={{ mb: { xs: 2, sm: 3, md: 4 }, maxWidth: '900px' }}>
                  {specialMediaIcons.map((item) => (
                      <Grid item xs={6} sm={4} md={3} key={item.key} sx={{ display: 'flex', justifyContent: 'center' }}>
                          <Box
                              onClick={() => {
                                  if (item.supported && item.mediaType) {
                                      navigate(`/search?mediaType=${item.mediaType}`);
                                  }
                              }}
                              sx={{
                                  display: 'flex',
                                  flexDirection: 'column',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  cursor: item.supported ? 'pointer' : 'default',
                                  color: item.supported ? 'text.secondary' : 'text.disabled',
                                  minHeight: { xs: '70px', sm: '80px' },
                                  p: { xs: 1.5, sm: 2 },
                                  borderRadius: '12px',
                                  transition: 'all 0.2s ease',
                                  opacity: item.supported ? 1 : 0.5,
                                  '&:hover': item.supported ? {
                                      color: 'text.primary',
                                      backgroundColor: 'rgba(255, 255, 255, 0.05)'
                                  } : {},
                                  '&:active': item.supported ? {
                                      transform: 'scale(0.98)'
                                  } : {}
                              }}>
                              {React.cloneElement(item.icon, { sx: { fontSize: { xs: 40, sm: 50 } } })}
                              <Typography
                                  variant="body2"
                                  sx={{
                                      mt: 0.5,
                                      fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                      textAlign: 'center'
                                  }}
                              >
                                  {item.name}
                              </Typography>
                              {item.caption && (
                                  <Typography
                                      variant="caption"
                                      sx={{
                                          fontSize: { xs: '0.55rem', sm: '0.65rem' },
                                          color: 'text.disabled',
                                          fontStyle: 'italic',
                                          textAlign: 'center'
                                      }}
                                  >
                                      {item.caption}
                                  </Typography>
                              )}
                          </Box>
                      </Grid>
                  ))}
              </Grid>
            </Box>

            <Box sx={{ 
                mt: { xs: 2, sm: 3, md: 4 }, 
                p: { xs: 2, sm: 3 }, 
                backgroundColor: 'background.paper', 
                borderRadius: '16px', 
                boxShadow: '0 4px 12px rgba(0,0,0,0.3)' 
            }}>
                <Grid container spacing={{ xs: 2, sm: 3 }} alignItems="center" justifyContent="center">
                    <Grid item xs={6} sm={6} md={2.4} sx={{ textAlign: 'center' }}>
                        <Box 
                            onClick={handleSourceDirectory}
                            sx={{ 
                                display: 'flex', 
                                flexDirection: 'column', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                cursor: 'pointer', 
                                color: 'text.primary', 
                                p: { xs: 1.5, sm: 2 },
                                minHeight: { xs: '100px', sm: '120px' },
                                borderRadius: '12px',
                                transition: 'all 0.2s ease',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    backgroundColor: 'rgba(156, 39, 176, 0.1)'
                                },
                                '&:active': {
                                    transform: 'scale(0.98)'
                                }
                            }}
                        >
                            <Apps sx={{ fontSize: { xs: 50, sm: 60, md: 70 }, color: '#695a8c' }} />
                            <Typography 
                                variant="h5" 
                                sx={{ 
                                    mt: 1,
                                    fontSize: { xs: '0.9rem', sm: '1.1rem', md: '1.25rem' },
                                    fontWeight: 'bold'
                                }}
                            >
                                Source Directory
                            </Typography>
                        </Box>
                    </Grid>
                    <Grid item xs={6} sm={6} md={2.4} sx={{ textAlign: 'center' }}>
                        <Box 
                            onClick={handleCreateMixlist}
                            sx={{ 
                                display: 'flex', 
                                flexDirection: 'column', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                cursor: 'pointer', 
                                color: 'text.primary', 
                                p: { xs: 1.5, sm: 2 },
                                minHeight: { xs: '100px', sm: '120px' },
                                borderRadius: '12px',
                                transition: 'all 0.2s ease',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    backgroundColor: 'rgba(105, 90, 140, 0.1)'
                                },
                                '&:active': {
                                    transform: 'scale(0.98)'
                                }
                            }}
                        >
                            <AddCircleOutline sx={{ fontSize: { xs: 50, sm: 60, md: 70 }, color: '#695a8c' }} />
                            <Typography 
                                variant="h5" 
                                sx={{ 
                                    mt: 1,
                                    fontSize: { xs: '0.9rem', sm: '1.1rem', md: '1.25rem' },
                                    fontWeight: 'bold'
                                }}
                            >
                                Create a Mixlist
                            </Typography>
                        </Box>
                    </Grid>
                    <Grid item xs={6} sm={6} md={2.4} sx={{ textAlign: 'center' }}>
                        <Box 
                            onClick={handleImportMedia}
                            sx={{ 
                                display: 'flex', 
                                flexDirection: 'column', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                cursor: 'pointer', 
                                color: 'text.primary', 
                                p: { xs: 1.5, sm: 2 },
                                minHeight: { xs: '100px', sm: '120px' },
                                borderRadius: '12px',
                                transition: 'all 0.2s ease',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    backgroundColor: 'rgba(105, 90, 140, 0.1)'
                                },
                                '&:active': {
                                    transform: 'scale(0.98)'
                                }
                            }}
                        >
                            <ImportExport sx={{ fontSize: { xs: 50, sm: 60, md: 70 }, color: '#695a8c' }} />
                            <Typography 
                                variant="h5" 
                                sx={{ 
                                    mt: 1,
                                    fontSize: { xs: '0.9rem', sm: '1.1rem', md: '1.25rem' },
                                    fontWeight: 'bold'
                                }}
                            >
                                Import Media
                            </Typography>
                        </Box>
                    </Grid>
                    <Grid item xs={6} sm={6} md={2.4} sx={{ textAlign: 'center' }}>
                        <Box 
                            onClick={handleSearchByTopicOrGenre}
                            sx={{ 
                                display: 'flex', 
                                flexDirection: 'column', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                cursor: 'pointer', 
                                color: 'text.primary', 
                                p: { xs: 1.5, sm: 2 },
                                minHeight: { xs: '100px', sm: '120px' },
                                borderRadius: '12px',
                                transition: 'all 0.2s ease',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    backgroundColor: 'rgba(105, 90, 140, 0.1)'
                                },
                                '&:active': {
                                    transform: 'scale(0.98)'
                                }
                            }}
                        >
                            <Topic sx={{ fontSize: { xs: 50, sm: 60, md: 70 }, color: '#695a8c' }} />
                            <Typography 
                                variant="h5" 
                                sx={{ 
                                    mt: 1,
                                    fontSize: { xs: '0.9rem', sm: '1.1rem', md: '1.25rem' },
                                    fontWeight: 'bold'
                                }}
                            >
                                Browse Topics/Genres
                            </Typography>
                        </Box>
                    </Grid>
                    <Grid item xs={6} sm={6} md={2.4} sx={{ textAlign: 'center' }}>
                        <Box 
                            onClick={handleAddMedia}
                            sx={{ 
                                display: 'flex', 
                                flexDirection: 'column', 
                                alignItems: 'center', 
                                justifyContent: 'center',
                                cursor: 'pointer', 
                                color: 'text.primary', 
                                p: { xs: 1.5, sm: 2 },
                                minHeight: { xs: '100px', sm: '120px' },
                                borderRadius: '12px',
                                transition: 'all 0.2s ease',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    backgroundColor: 'rgba(105, 90, 140, 0.1)'
                                },
                                '&:active': {
                                    transform: 'scale(0.98)'
                                }
                            }}
                        >
                            <BookmarkAdd sx={{ fontSize: { xs: 50, sm: 60, md: 70 }, color: '#695a8c' }} />
                            <Typography 
                                variant="h5" 
                                sx={{ 
                                    mt: 1,
                                    fontSize: { xs: '0.9rem', sm: '1.1rem', md: '1.25rem' },
                                    fontWeight: 'bold'
                                }}
                            >
                                Add Media
                            </Typography>
                        </Box>
                    </Grid>
                </Grid>
            </Box>
        </Section>

        {/* Jump back in Section - Actively Exploring Media */}
        <Section title="Jump back in">
          {activelyExploringLoading ? (
            <Box sx={{ textAlign: 'center', py: 6 }}>
              <CircularProgress size={40} sx={{ mb: 2 }} />
              <Typography variant="h6" color="text.secondary">Loading your active explorations...</Typography>
            </Box>
          ) : activelyExploringError ? (
            <Box sx={{ textAlign: 'center', py: { xs: 4, sm: 6 }, px: 2 }}>
              <Typography 
                  variant="h6" 
                  color="error" 
                  sx={{ 
                      mb: 2,
                      fontSize: { xs: '1rem', sm: '1.25rem' }
                  }}
              >
                  {activelyExploringError}
              </Typography>
              <Button 
                  variant="outlined" 
                  onClick={() => window.location.reload()}
                  sx={{ minHeight: '44px', px: 3 }}
              >
                Retry
              </Button>
            </Box>
          ) : activelyExploringMedia.length > 0 ? (
            <SimpleMediaCarousel
              mediaItems={activelyExploringMedia}
              title=""
              subtitle="Continue exploring these items"
              onMediaClick={(media) => {
                // Navigate to appropriate profile page based on media type
                // Podcast episodes have seriesId, series don't
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
              showCardContent={true}
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
                Start exploring some media and mark them as "Actively Exploring" to see them here
              </Typography>
              <Button 
                variant="contained" 
                onClick={handleAddMedia}
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

        {/* Mixlists Section */}
        <Section title="Recent Mixlists">
          {mixlistsLoading ? (
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
                          onClick={handleCreateMixlist}
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
                          onClick={handleSeedMixlists}
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
                  mixlists.slice(0, 6).map((item, index) => (
                      <Grid item key={index} xs={12} sm={6} md={4}>
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
                onClick={() => navigate('/mixlists')}
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


      </Container>
      )}
    </Box>
  );
}