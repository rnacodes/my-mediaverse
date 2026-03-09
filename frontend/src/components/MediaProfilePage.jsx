import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import {
    Box, Card, CardContent,
    Divider, Link, Fab,
    Dialog, DialogTitle, DialogContent, DialogActions,
    List, ListItem, ListItemText, Collapse, Snackbar, Alert,
    CircularProgress,
    TextField, InputAdornment,
    IconButton, Typography, Button
} from '@mui/material';
import {
    PlaylistAdd,
    Search, Close,
} from '@mui/icons-material';
import MediaHeader from './MediaHeader';
import MixlistCarousel from './MixlistCarousel';
import MediaInfoCard from './MediaInfoCard';
import MediaDetailAccordion from './MediaDetailAccordion';
import HighlightsSection from './HighlightsSection';
import TopicsGenresSection from './TopicsGenresSection';
import RelatedNotesSection from './RelatedNotesSection';
import SimilarItemsSection from './SimilarItemsSection';
import SavedRelatedMediaSection from './SavedRelatedMediaSection';
import { formatMediaType, formatStatus, getMediaTypeColor, getStatusColor, getRatingIcon, getRatingText } from '../utils/formatters';
import { getMediaById } from '../api/mediaService';
import { getAllMixlists, addMediaToMixlist } from '../api/mixlistService';
import { getBookById } from '../api/bookService';
import { getPodcastSeriesById, getPodcastEpisodeById, getEpisodesBySeriesId } from '../api/podcastService';
import { getMovieById } from '../api/movieService';
import { getTvShowById } from '../api/tvShowService';
import { getVideoById, getPlaylistsForVideo } from '../api/videoService';
import { getArticleById, fetchArticleContent } from '../api/articleService';
import { getHighlightsByArticle, getHighlightsByBook } from '../api/highlightService';

function MediaProfilePage() {
  const [mediaItem, setMediaItem] = useState(null);
  const [loading, setLoading] = useState(true);
  const [availableMixlists, setAvailableMixlists] = useState([]);
  const [currentMixlists, setCurrentMixlists] = useState([]);
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
  const [addToMixlistDialog, setAddToMixlistDialog] = useState(false);
  const [selectedMixlistId, setSelectedMixlistId] = useState(null);
  const [mixlistSearchQuery, setMixlistSearchQuery] = useState('');
  const [highlights, setHighlights] = useState([]);
  const [highlightsLoading, setHighlightsLoading] = useState(false);
  const [videoPlaylists, setVideoPlaylists] = useState([]);
  const [refreshKey, setRefreshKey] = useState(0);
  const [fetchingContent, setFetchingContent] = useState(false);
  const [relatedMediaRefreshTrigger, setRelatedMediaRefreshTrigger] = useState(0);

  const { id } = useParams();
  const navigate = useNavigate();
  const theme = useTheme();
  const isTablet = useMediaQuery(theme.breakpoints.down('md'));

  useEffect(() => {
    const fetchData = async () => {
      try {
        // First get the basic media info to determine the type
        const mediaResponse = await getMediaById(id);
        const basicMedia = mediaResponse.data;

        // Redirect to specialized profile pages for certain media types
        const mediaType = basicMedia.mediaType;

        if (mediaType === 'Playlist' || mediaType === 7) {
          navigate(`/youtube-playlist/${id}`, { replace: true });
          return;
        }
        if (mediaType === 'Channel' || mediaType === 2) {
          navigate(`/youtube-channel/${id}`, { replace: true });
          return;
        }

        let detailedMedia = basicMedia;
        
        // If it's a book, podcast, movie, TV show, or video, fetch the detailed information
        if (basicMedia.mediaType === 'Book') {
          try {
            const bookResponse = await getBookById(id);
            detailedMedia = { ...basicMedia, ...bookResponse.data };
          } catch (bookError) {
            console.warn('Could not fetch detailed book data, using basic data:', bookError);
          }
        } else if (basicMedia.mediaType === 'Podcast') {
          try {
            // Try to fetch as podcast series first
            try {
              const seriesResponse = await getPodcastSeriesById(id);
              // Redirect to dedicated podcast series profile page
              navigate(`/podcast-series/${id}`, { replace: true });
              return;
            } catch (seriesError) {
              // If series fetch fails, try as episode
              try {
                const episodeResponse = await getPodcastEpisodeById(id);
                detailedMedia = { ...basicMedia, ...episodeResponse.data };
                
                // If it's an episode, try to fetch the parent series info
                if (detailedMedia.seriesId) {
                  try {
                    const parentSeriesResponse = await getPodcastSeriesById(detailedMedia.seriesId);
                    detailedMedia.series = parentSeriesResponse.data;
                  } catch (parentSeriesError) {
                    console.warn('Could not fetch parent series data:', parentSeriesError);
                  }
                }
              } catch (episodeError) {
                console.warn('Could not fetch detailed podcast data, using basic data:', episodeError);
              }
            }
          } catch (podcastError) {
            console.warn('Could not fetch detailed podcast data, using basic data:', podcastError);
          }
        } else if (basicMedia.mediaType === 'Movie') {
          try {
            const movieResponse = await getMovieById(id);
            detailedMedia = { ...basicMedia, ...movieResponse.data };
          } catch (movieError) {
            console.warn('Could not fetch detailed movie data, using basic data:', movieError);
          }
        } else if (basicMedia.mediaType === 'TVShow') {
          // Check if this is a TV show (not an episode) and redirect to dedicated profile
          try {
            const tvShowResponse = await getTvShowById(id);
            if (tvShowResponse.data) {
              navigate(`/tv-show/${id}`, { replace: true });
              return;
            }
          } catch (tvShowError) {
            // Not a TV show parent, might be an episode - continue with generic profile
            console.warn('Could not fetch as TV show, using generic profile:', tvShowError);
          }
        } else if (basicMedia.mediaType === 'Video') {
          try {
            const videoResponse = await getVideoById(id);
            detailedMedia = { ...basicMedia, ...videoResponse.data };
          } catch (videoError) {
            console.warn('Could not fetch detailed video data, using basic data:', videoError);
          }
        } else if (basicMedia.mediaType === 'Article') {
          try {
            const articleResponse = await getArticleById(id);
            // Check if response has .data property or if it's the data itself
            const articleData = articleResponse.data || articleResponse;
            detailedMedia = { ...basicMedia, ...articleData };
          } catch (articleError) {
            console.warn('Could not fetch detailed article data, using basic data:', articleError);
          }
        }
        
        setMediaItem(detailedMedia);

        // Fetch all mixlists once and derive both current and available lists
        const mixlistsResponse = await getAllMixlists();
        const allMixlists = mixlistsResponse.data || [];
        setAvailableMixlists(allMixlists);

        const mixlistIds = detailedMedia.mixlistIds || [];
        if (mixlistIds.length > 0) {
          const mixlistIdSet = new Set(mixlistIds);
          setCurrentMixlists(allMixlists.filter(m => mixlistIdSet.has(m.id)));
        } else {
          setCurrentMixlists([]);
        }

      } catch (error) {
        console.error('Failed to fetch data:', error);
        setSnackbar({ open: true, message: 'Failed to load media item', severity: 'error' });
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchData();
    } else {
      setLoading(false);
    }
  }, [id, refreshKey]);

  // Fetch highlights when media item is loaded
  useEffect(() => {
    const fetchHighlights = async () => {
      if (!mediaItem) return;

      if (mediaItem.mediaType === 'Article' || mediaItem.mediaType === 'Book') {
        setHighlightsLoading(true);
        try {
          let highlightsData = [];
          if (mediaItem.mediaType === 'Article') {
            highlightsData = await getHighlightsByArticle(mediaItem.id);
          } else if (mediaItem.mediaType === 'Book') {
            highlightsData = await getHighlightsByBook(mediaItem.id);
          }
          setHighlights(highlightsData || []);
        } catch (error) {
          console.error('Failed to fetch highlights:', error);
          // Don't show error snackbar for highlights, just log it
          setHighlights([]);
        } finally {
          setHighlightsLoading(false);
        }
      }
    };

    fetchHighlights();
  }, [mediaItem]);

  // Fetch playlists for videos
  useEffect(() => {
    const fetchVideoPlaylists = async () => {
      if (!mediaItem) return;

      if (mediaItem.mediaType === 'Video') {
        try {
          const playlists = await getPlaylistsForVideo(mediaItem.id);
          setVideoPlaylists(playlists || []);
        } catch (error) {
          console.error('Failed to fetch playlists for video:', error);
          setVideoPlaylists([]);
        }
      }
    };

    fetchVideoPlaylists();
  }, [mediaItem]);

  // Handle fetching content for articles from Readwise Reader
  const handleFetchContent = async () => {
    if (!mediaItem?.id) return;

    setFetchingContent(true);
    try {
      await fetchArticleContent(mediaItem.id);
      setSnackbar({ open: true, message: 'Content fetched successfully!', severity: 'success' });
      setRefreshKey(prev => prev + 1); // Refresh to show updated content
    } catch (error) {
      console.error('Failed to fetch article content:', error);
      setSnackbar({ open: true, message: 'Failed to fetch content', severity: 'error' });
    } finally {
      setFetchingContent(false);
    }
  };

  const handleAddToMixlist = async () => {
    if (!selectedMixlistId) {
      setSnackbar({ open: true, message: 'Please select a mixlist first', severity: 'warning' });
      return;
    }
    
    try {
      await addMediaToMixlist(selectedMixlistId, id);
      setSnackbar({ open: true, message: 'Media added to mixlist successfully!', severity: 'success' });
      setAddToMixlistDialog(false);
      setSelectedMixlistId(null);
      setMixlistSearchQuery('');

      // Refresh the current mixlists
      const updatedMediaResponse = await getMediaById(id);
      const updatedMedia = updatedMediaResponse.data;
      const mixlistIds = updatedMedia.mixlistIds || [];

      const mixlistsResponse = await getAllMixlists();
      const allMixlists = mixlistsResponse.data || [];
      setAvailableMixlists(allMixlists);

      if (mixlistIds.length > 0) {
        const mixlistIdSet = new Set(mixlistIds);
        setCurrentMixlists(allMixlists.filter(m => mixlistIdSet.has(m.id)));
      } else {
        setCurrentMixlists([]);
      }
    } catch (error) {
      console.error('Failed to add media to mixlist:', error);
      console.error('Error details:', error.response || error);
      setSnackbar({ 
        open: true, 
        message: `Failed to add media to mixlist: ${error.response?.data?.message || error.message || 'Unknown error'}`, 
        severity: 'error' 
      });
    }
  };

  const handleCloseMixlistDialog = () => {
    setAddToMixlistDialog(false);
    setSelectedMixlistId(null);
    setMixlistSearchQuery('');
  };

  const filteredAvailableMixlists = availableMixlists
    .filter(mixlist => !currentMixlists.some(current => current.id === mixlist.id))
    .filter(mixlist => 
      mixlist.name?.toLowerCase().includes(mixlistSearchQuery.toLowerCase()) ||
      mixlist.description?.toLowerCase().includes(mixlistSearchQuery.toLowerCase())
    );

  const handleCreateNewMixlist = () => {
    navigate('/create-mixlist', { state: { returnTo: `/media/${id}` } });
  };

  if (loading) {
    return (
      <Box sx={{ 
        minHeight: '100vh', 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center',
        py: 4,
        px: 2
      }}>
        <Box sx={{ 
          width: '100%',
          maxWidth: '600px',
          backgroundColor: 'background.paper',
          borderRadius: '16px',
          p: 4,
          boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
          textAlign: 'center'
        }}>
          <CircularProgress sx={{ mb: 2 }} />
          <Typography variant="h6">Loading media item...</Typography>
          <Typography variant="body2" color="text.secondary">ID: {id}</Typography>
        </Box>
      </Box>
    );
  }

  if (!mediaItem) {
    return (
      <Box sx={{ 
        minHeight: '100vh', 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'flex-start',
        py: 4,
        px: 2
      }}>
        <Box sx={{ 
          width: '100%',
          maxWidth: '600px',
          backgroundColor: 'background.paper',
          borderRadius: '16px',
          p: 4,
          boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
          textAlign: 'center'
        }}>
          <Typography variant="h6">Media item not found.</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            The media item you're looking for doesn't exist or couldn't be loaded.
          </Typography>
          <Button 
            onClick={() => navigate('/all-media')} 
            variant="contained" 
            sx={{ mt: 2 }}
          >
            Back to All Media
          </Button>
        </Box>
      </Box>
    );
  }

  return (
    <Box sx={{ 
      minHeight: '100vh', 
      display: 'flex', 
      justifyContent: 'center', 
      alignItems: 'flex-start',
      py: { xs: 2, sm: 4 },
      px: { xs: 1, sm: 2 }
    }}>
      <Box sx={{ 
        width: '100%',
        maxWidth: '900px',
        backgroundColor: 'background.paper',
        borderRadius: { xs: '8px', sm: '16px' },
        p: { xs: 2, sm: 3, md: 4 },
        boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
      }}>
        {/* Header with back button and edit button */}
        <MediaHeader title={mediaItem?.title} mediaId={id} />

        <Card sx={{ overflow: 'hidden', borderRadius: 2 }}>
          <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
            {/* Main content with responsive layout */}
            <MediaInfoCard 
              mediaItem={mediaItem}
              formatMediaType={formatMediaType}
              formatStatus={formatStatus}
              getMediaTypeColor={getMediaTypeColor}
              getStatusColor={getStatusColor}
              getRatingIcon={getRatingIcon}
              getRatingText={getRatingText}
            />


        <MediaDetailAccordion
          mediaItem={mediaItem}
          navigate={navigate}
          videoPlaylists={videoPlaylists}
          onBookEnriched={() => setRefreshKey(k => k + 1)}
          onVideoLinked={() => setRefreshKey(k => k + 1)}
          onFetchContent={handleFetchContent}
          fetchingContent={fetchingContent}
        />

        <HighlightsSection mediaItem={mediaItem} highlights={highlights} highlightsLoading={highlightsLoading} />

        <TopicsGenresSection
          mediaItem={mediaItem}
          setSnackbar={setSnackbar}
          onUpdate={() => setRefreshKey(k => k + 1)}
        />

        <RelatedNotesSection
          mediaItem={mediaItem}
          setSnackbar={setSnackbar}
          onUpdate={() => setRefreshKey(k => k + 1)}
        />

        <SavedRelatedMediaSection
          mediaItem={mediaItem}
          setSnackbar={setSnackbar}
          refreshTrigger={relatedMediaRefreshTrigger}
        />

        <SimilarItemsSection
          mediaItem={mediaItem}
          setSnackbar={setSnackbar}
          onRelatedMediaSaved={() => setRelatedMediaRefreshTrigger(prev => prev + 1)}
        />

        <MixlistCarousel 
          mediaItem={mediaItem}
          currentMixlists={currentMixlists}
          availableMixlists={availableMixlists}
          setCurrentMixlists={setCurrentMixlists}
          setAvailableMixlists={setAvailableMixlists}
          setSnackbar={setSnackbar}
          isMobile={isTablet}
        />
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
}

export default MediaProfilePage;
