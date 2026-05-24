import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { Box, Card, CardContent, CircularProgress, Typography, Button } from '@mui/material';

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
import { useMediaItem } from '../hooks/useMedia';
import { useAllMixlists } from '../hooks/useMixlist';
import { useBook } from '../hooks/useBook';
import { usePodcastSeries, usePodcastEpisode } from '../hooks/usePodcast';
import { useMovie } from '../hooks/useMovie';
import { useTvShow } from '../hooks/useTvShow';
import { useVideo, usePlaylistsForVideo } from '../hooks/useVideo';
import { useArticle, useFetchArticleContent } from '../hooks/useArticle';
import { useHighlightsByArticle, useHighlightsByBook } from '../hooks/useHighlight';
import { useReindexMediaItem } from '../hooks/useTypesense';

function MediaProfilePage() {
  const [currentMixlists, setCurrentMixlists] = useState([]);
  const [availableMixlists, setAvailableMixlists] = useState([]);
  const [_snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
  const [refreshKey, setRefreshKey] = useState(0);
  const [relatedMediaRefreshTrigger, setRelatedMediaRefreshTrigger] = useState(0);

  const { id } = useParams();
  const navigate = useNavigate();
  const theme = useTheme();
  const isTablet = useMediaQuery(theme.breakpoints.down('md'));

  const basicQuery = useMediaItem(id);
  const basicMedia = basicQuery.data ?? null;
  const basicType = basicMedia?.mediaType;

  // Redirect side-effect for media types that have their own profile page.
  useEffect(() => {
    if (!basicMedia) return;
    if (basicType === 'Playlist' || basicType === 7) {
      navigate(`/youtube-playlist/${id}`, { replace: true });
    } else if (basicType === 'Channel' || basicType === 2) {
      navigate(`/youtube-channel/${id}`, { replace: true });
    }
  }, [basicType, basicMedia, id, navigate]);

  // Conditional secondary fetches, each gated by the basic mediaType.
  const bookQuery = useBook(id, { enabled: basicType === 'Book' });
  const movieQuery = useMovie(id, { enabled: basicType === 'Movie' });
  const videoQuery = useVideo(id, { enabled: basicType === 'Video' });
  const articleQuery = useArticle(id, { enabled: basicType === 'Article' });

  // Podcast: try series first; if it returns data, redirect. Otherwise try episode.
  const podcastSeriesProbe = usePodcastSeries(id, { enabled: basicType === 'Podcast', retry: false });
  const isSeries = basicType === 'Podcast' && !!podcastSeriesProbe.data;
  useEffect(() => {
    if (isSeries) {
      navigate(`/podcast-series/${id}`, { replace: true });
    }
  }, [isSeries, id, navigate]);
  const podcastEpisodeQuery = usePodcastEpisode(id, {
    enabled: basicType === 'Podcast' && podcastSeriesProbe.isError,
  });
  const parentSeriesId = podcastEpisodeQuery.data?.seriesId;
  const parentSeriesQuery = usePodcastSeries(parentSeriesId, { enabled: !!parentSeriesId });

  // TVShow: probe; if a row exists, redirect to dedicated profile. Otherwise treat as episode.
  const tvShowProbe = useTvShow(id, { enabled: basicType === 'TVShow', retry: false });
  useEffect(() => {
    if (basicType === 'TVShow' && tvShowProbe.data) {
      navigate(`/tv-show/${id}`, { replace: true });
    }
  }, [basicType, tvShowProbe.data, id, navigate]);

  // Derive the merged mediaItem from basic + the active secondary query.
  const mediaItem = useMemo(() => {
    if (!basicMedia) return null;
    if (basicType === 'Book' && bookQuery.data) return { ...basicMedia, ...bookQuery.data };
    if (basicType === 'Movie' && movieQuery.data) return { ...basicMedia, ...movieQuery.data };
    if (basicType === 'Video' && videoQuery.data) return { ...basicMedia, ...videoQuery.data };
    if (basicType === 'Article' && articleQuery.data) return { ...basicMedia, ...articleQuery.data };
    if (basicType === 'Podcast' && podcastEpisodeQuery.data) {
      const merged = { ...basicMedia, ...podcastEpisodeQuery.data };
      if (parentSeriesQuery.data) merged.series = parentSeriesQuery.data;
      return merged;
    }
    return basicMedia;
  }, [basicMedia, basicType, bookQuery.data, movieQuery.data, videoQuery.data, articleQuery.data, podcastEpisodeQuery.data, parentSeriesQuery.data]);

  // Highlights (for Article / Book only).
  const articleHighlightsQuery = useHighlightsByArticle(mediaItem?.id, { enabled: mediaItem?.mediaType === 'Article' });
  const bookHighlightsQuery = useHighlightsByBook(mediaItem?.id, { enabled: mediaItem?.mediaType === 'Book' });
  const highlights = mediaItem?.mediaType === 'Article'
    ? (articleHighlightsQuery.data ?? [])
    : mediaItem?.mediaType === 'Book'
      ? (bookHighlightsQuery.data ?? [])
      : [];
  const highlightsLoading = articleHighlightsQuery.isLoading || bookHighlightsQuery.isLoading;

  // Playlists for videos.
  const videoPlaylistsQuery = usePlaylistsForVideo(mediaItem?.id, { enabled: mediaItem?.mediaType === 'Video' });
  const videoPlaylists = videoPlaylistsQuery.data ?? [];

  // Mixlists.
  const mixlistsQuery = useAllMixlists();
  const allMixlistsFromQuery = useMemo(() => mixlistsQuery.data ?? [], [mixlistsQuery.data]);
  useEffect(() => { setAvailableMixlists(allMixlistsFromQuery); }, [allMixlistsFromQuery]);
  useEffect(() => {
    if (!mediaItem) return;
    const mixlistIds = mediaItem.mixlistIds || [];
    if (mixlistIds.length > 0 && allMixlistsFromQuery.length > 0) {
      const mixlistIdSet = new Set(mixlistIds);
      setCurrentMixlists(allMixlistsFromQuery.filter(m => mixlistIdSet.has(m.id)));
    } else {
      setCurrentMixlists([]);
    }
  }, [mediaItem, allMixlistsFromQuery]);

  // Force refetch of the basic media on refreshKey bump (used by child sections after mutations).
  useEffect(() => {
    if (refreshKey > 0) basicQuery.refetch();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshKey]);

  // Surface basic-load failures.
  useEffect(() => {
    if (basicQuery.error) {
      setSnackbar({ open: true, message: 'Failed to load media item', severity: 'error' });
    }
  }, [basicQuery.error]);

  const loading = basicQuery.isLoading;

  const reindexMutation = useReindexMediaItem();
  const reindexing = reindexMutation.isPending;
  const fetchContentMutation = useFetchArticleContent();
  const fetchingContent = fetchContentMutation.isPending;

  const handleReindex = () => {
    reindexMutation.mutate(id, {
      onSuccess: () => setSnackbar({ open: true, message: 'Media item re-indexed in search.', severity: 'success' }),
      onError: (error) => {
        if (error.response?.status !== 403) {
          setSnackbar({ open: true, message: 'Failed to re-index media item.', severity: 'error' });
        }
      },
    });
  };

  const handleFetchContent = () => {
    if (!mediaItem?.id) return;
    fetchContentMutation.mutate(mediaItem.id, {
      onSuccess: () => {
        setSnackbar({ open: true, message: 'Content fetched successfully!', severity: 'success' });
        setRefreshKey(prev => prev + 1);
      },
      onError: () => setSnackbar({ open: true, message: 'Failed to fetch content', severity: 'error' }),
    });
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
            The media item you&apos;re looking for doesn&apos;t exist or couldn&apos;t be loaded.
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
        <MediaHeader
          title={mediaItem?.title}
          mediaId={id}
          onReindex={handleReindex}
          reindexing={reindexing}
        />

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
