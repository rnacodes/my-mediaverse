import React, { useState } from 'react';
import {
    Button, Card, Box, Typography, Accordion, AccordionSummary, AccordionDetails, Link, Chip, IconButton, Tooltip, Alert,
    CircularProgress, Divider, Dialog, DialogTitle, DialogContent, List, ListItem, ListItemButton, ListItemText
} from '@mui/material';
import { ExpandMore, OpenInNew, Star, RssFeed, ContentCopy, Language, Schedule, Article, AutoFixHigh, Download } from '@mui/icons-material';
import { useWebsiteRssFeedItems } from '@/hooks/useWebsite';
import { useEnrichBookById } from '@/hooks/useBackgroundJobs';
import { useAllYouTubeChannels } from '@/hooks/useYoutube';
import { useUpdateVideo } from '@/hooks/useVideo';

function getJustWatchUrl(title) {
  // Simple heuristic for generating a JustWatch search URL.
  // In a real application, you'd likely use a more robust integration (e.g., an API).
  return `https://www.justwatch.com/us/search?q=${encodeURIComponent(title)}`;
}

function MediaDetailAccordion({ mediaItem, navigate, videoPlaylists = [], onBookEnriched, onVideoLinked, onFetchContent, fetchingContent }) {
  const [enrichResult, setEnrichResult] = useState(null);
  const [channelDialogOpen, setChannelDialogOpen] = useState(false);
  const [feedDialogOpen, setFeedDialogOpen] = useState(false);

  // RSS feed items for websites — auto-fetched when conditions match.
  const isWebsiteWithRss = mediaItem.mediaType === 'Website' && !!mediaItem.rssFeedUrl;
  const rssFeedQuery = useWebsiteRssFeedItems(mediaItem.id, 3, {
    enabled: isWebsiteWithRss && !!mediaItem.id,
  });
  const rssFeedItems = rssFeedQuery.data ?? [];
  const loadingRss = rssFeedQuery.isLoading;
  const rssError = rssFeedQuery.error ? 'Failed to load RSS feed' : null;

  // Full feed as JSON — fetched lazily when the feed dialog opens. Linking
  // straight to the feed URL makes browsers download the XML instead of
  // rendering it, so the dialog shows the parsed feed in-app instead.
  const fullFeedQuery = useWebsiteRssFeedItems(mediaItem.id, 20, {
    enabled: feedDialogOpen && isWebsiteWithRss && !!mediaItem.id,
  });
  const fullFeedItems = fullFeedQuery.data ?? [];
  const loadingFullFeed = fullFeedQuery.isLoading;

  // Channels — fetched lazily when the link-to-channel dialog opens.
  const channelsQuery = useAllYouTubeChannels({ enabled: channelDialogOpen });
  const channels = channelsQuery.data ?? [];
  const loadingChannels = channelsQuery.isLoading;

  // Mutations
  const enrichBookMutation = useEnrichBookById();
  const updateVideoMutation = useUpdateVideo();
  const enriching = enrichBookMutation.isPending;
  // Google Books lookup needs an ISBN, or a title plus a real author. "Unknown Author" is the
  // placeholder imports write when the source had none, so it cannot anchor a lookup.
  const hasRealAuthor = Boolean(mediaItem.author) && mediaItem.author !== 'Unknown Author';
  const canEnrichBook = Boolean(mediaItem.isbn) || (Boolean(mediaItem.title) && hasRealAuthor);
  const linkingChannel = updateVideoMutation.isPending;

  const handleEnrichBook = () => {
    setEnrichResult(null);
    enrichBookMutation.mutate(mediaItem.id, {
      onSuccess: (result) => {
        setEnrichResult(result);
        if (result?.success && onBookEnriched) {
          onBookEnriched();
        }
      },
      onError: (error) => {
        setEnrichResult({
          success: false,
          errorMessage: error.response?.data?.error || error.message || 'Failed to enrich book',
        });
      },
    });
  };

  const handleOpenChannelDialog = () => {
    setChannelDialogOpen(true);
  };

  const handleLinkToChannel = (channelId) => {
    updateVideoMutation.mutate(
      {
        id: mediaItem.id,
        videoData: {
          title: mediaItem.title,
          mediaType: mediaItem.mediaType,
          link: mediaItem.link || '',
          notes: mediaItem.notes || '',
          status: mediaItem.status,
          description: mediaItem.description || '',
          thumbnail: mediaItem.thumbnail || '',
          topics: mediaItem.topicNames || mediaItem.topics || [],
          genres: mediaItem.genreNames || mediaItem.genres || [],
          platform: mediaItem.platform || 'YouTube',
          lengthInSeconds: mediaItem.lengthInSeconds || 0,
          externalId: mediaItem.externalId || '',
          channelId,
        },
      },
      {
        onSuccess: () => {
          setChannelDialogOpen(false);
          if (onVideoLinked) onVideoLinked();
        },
        onError: (error) => {
          console.error('Failed to link video to channel:', error);
        },
      }
    );
  };

  return (
    <Card sx={{ mt: 3, overflow: 'hidden', borderRadius: 2 }}>
      <Accordion sx={{ boxShadow: 'none', '&:before': { display: 'none' } }}>
        <AccordionSummary
          expandIcon={<ExpandMore />}
          aria-controls="media-details-content"
          id="media-details-header"
          sx={{
            backgroundColor: 'rgba(255, 255, 255, 0.05)',
            px: { xs: 2, sm: 3, md: 4 },
            '&:hover': {
              backgroundColor: 'rgba(255, 255, 255, 0.1)'
            }
          }}
        >
          <Typography
            variant="h5"
            sx={{
              fontWeight: 'bold',
              fontSize: { xs: '1.25rem', sm: '1.5rem' }
            }}
          >
            {mediaItem.mediaType === 'TVShow' ? 'TV Show Details' : `${mediaItem.mediaType} Details`}
          </Typography>
        </AccordionSummary>
        <AccordionDetails sx={{ p: { xs: 2, sm: 3, md: 4 }, backgroundColor: 'rgba(255, 255, 255, 0.02)' }}>
        
        {/* Podcast-specific properties */}
        {mediaItem.mediaType === 'Podcast' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.podcastType !== undefined && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Type:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  {mediaItem.podcastType === 'Series' || mediaItem.podcastType === 0 ? 'Podcast Series' : 'Podcast Episode'}
                </Typography>
              </Box>
            )}

            {mediaItem.series && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Parent Series:</strong>
                </Typography>
                <Link
                  component="button"
                  variant="body1"
                  onClick={() => {
                    // Navigate to podcast series profile if it's a podcast series
                    if (mediaItem.mediaType === 'Podcast') {
                      navigate(`/podcast-series/${mediaItem.series.id}`);
                    } else {
                      navigate(`/media/${mediaItem.series.id}`);
                    }
                  }}
                  sx={{
                    textAlign: 'left',
                    cursor: 'pointer',
                    fontSize: '0.875rem',
                    color: '#ffffff',
                    textDecoration: 'none',
                    '&:hover': {
                      textDecoration: 'underline',
                      color: '#e3f2fd'
                    }
                  }}
                >
                  {mediaItem.series.title}
                </Link>
              </Box>
            )}
            
            {mediaItem.durationInSeconds !== undefined && mediaItem.durationInSeconds !== null && mediaItem.durationInSeconds > 0 && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Duration:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  {Math.floor(mediaItem.durationInSeconds / 60)}:{(mediaItem.durationInSeconds % 60).toString().padStart(2, '0')}
                </Typography>
              </Box>
            )}
            
            {mediaItem.publisher && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Publisher:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.publisher}</Typography>
              </Box>
            )}
            
            {mediaItem.audioLink && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 2 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Audio Link:</strong>
                </Typography>
                <Button
                  variant="contained"
                  color="primary"
                  size="small"
                  href={mediaItem.audioLink}
                  target="_blank"
                  rel="noopener noreferrer"
                  startIcon={<Download />}
                  sx={{
                    textTransform: 'none',
                    fontSize: '0.875rem'
                  }}
                >
                  Click to Download Audio
                </Button>
              </Box>
            )}
            
            {mediaItem.releaseDate && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Release Date:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  {new Date(mediaItem.releaseDate).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                  })}
                </Typography>
              </Box>
            )}
          </Box>
        )}
        
        {/* Book-specific properties */}
        {mediaItem.mediaType === 'Book' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.author && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Author:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.author}</Typography>
              </Box>
            )}

            {mediaItem.isbn && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>ISBN:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '1rem' }}>{mediaItem.isbn}</Typography>
              </Box>
            )}

            {mediaItem.asin && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>ASIN:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '1rem' }}>{mediaItem.asin}</Typography>
              </Box>
            )}

            {mediaItem.format && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Format:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.format}</Typography>
              </Box>
            )}

            {mediaItem.partOfSeries !== undefined && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Part of Series:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.partOfSeries ? 'Yes' : 'No'}</Typography>
              </Box>
            )}

            {mediaItem.goodreadsRating && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Your Rating:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.goodreadsRating} / 5</Typography>
              </Box>
            )}

            {mediaItem.averageRating && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Avg Rating:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.averageRating} / 5</Typography>
              </Box>
            )}

            {mediaItem.publisher && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Publisher:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.publisher}</Typography>
              </Box>
            )}

            {mediaItem.yearPublished && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Year Published:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.yearPublished}</Typography>
              </Box>
            )}

            {mediaItem.originalPublicationYear && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>First Published:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>{mediaItem.originalPublicationYear}</Typography>
              </Box>
            )}

            {mediaItem.dateRead && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '1rem' }}>
                  <strong>Date Read:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>
                  {new Date(mediaItem.dateRead).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                  })}
                </Typography>
              </Box>
            )}

            {mediaItem.myReview && (
              <Box sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 1
              }}>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>
                  <strong>My Review:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.9rem', pl: 2, fontStyle: 'italic' }}>
                  &quot;{mediaItem.myReview}&quot;
                </Typography>
              </Box>
            )}

            {mediaItem.goodreadsTags && mediaItem.goodreadsTags.length > 0 && (
              <Box sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 1
              }}>
                <Typography variant="body1" sx={{ fontSize: '1rem' }}>
                  <strong>Goodreads Tags:</strong>
                </Typography>
                <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', pl: 2 }}>
                  {mediaItem.goodreadsTags.map((tag) => (
                    <Chip
                      key={`gr-tag-${tag}`}
                      label={tag}
                      size="small"
                      sx={{
                        backgroundColor: 'rgba(255, 255, 255, 0.1)',
                        color: 'text.primary',
                        fontSize: '0.75rem'
                      }}
                    />
                  ))}
                </Box>
              </Box>
            )}

            {/* Enrich Book Button */}
            <Divider sx={{ my: 2 }} />
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Typography variant="body1" sx={{ fontSize: '1rem' }}>
                <strong>Enrich from Google Books:</strong>
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={enriching ? <CircularProgress size={16} /> : <AutoFixHigh />}
                  onClick={handleEnrichBook}
                  disabled={enriching || !canEnrichBook}
                  sx={{
                    borderColor: 'rgba(255, 255, 255, 0.3)',
                    color: 'text.primary',
                    '&:hover': {
                      borderColor: 'rgba(255, 255, 255, 0.5)',
                      backgroundColor: 'rgba(255, 255, 255, 0.05)'
                    },
                    '&.Mui-disabled': {
                      borderColor: 'rgba(255, 255, 255, 0.1)',
                      color: 'rgba(255, 255, 255, 0.3)'
                    }
                  }}
                >
                  {enriching ? 'Enriching...' : 'Fetch Description'}
                </Button>
                {!canEnrichBook && (
                  <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.875rem' }}>
                    (Requires ISBN or title + author)
                  </Typography>
                )}
              </Box>
              {enrichResult && (
                <Alert
                  severity={enrichResult.success ? 'success' : enrichResult.alreadyHasDescription ? 'info' : 'warning'}
                  sx={{ mt: 1 }}
                  onClose={() => setEnrichResult(null)}
                >
                  {enrichResult.success && !enrichResult.alreadyHasDescription && (
                    <>
                      Description fetched successfully! Refresh the page to see it.
                      {enrichResult.filledFields?.length > 1 && (
                        <> Also filled: {enrichResult.filledFields.filter((f) => f !== 'description').join(', ')}.</>
                      )}
                    </>
                  )}
                  {enrichResult.alreadyHasDescription && (
                    <>This book already has a description.</>
                  )}
                  {enrichResult.noLookupKey && (
                    <>This book has no ISBN or title + author to look up.</>
                  )}
                  {!enrichResult.success && !enrichResult.alreadyHasDescription && !enrichResult.noLookupKey && (
                    <>{enrichResult.errorMessage || 'No description found in Google Books for this book.'}</>
                  )}
                </Alert>
              )}
            </Box>
          </Box>
        )}

        {/* Movie-specific properties */}
        {mediaItem.mediaType === 'Movie' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.director && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Director:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.director}</Typography>
              </Box>
            )}
            
            {mediaItem.cast && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Cast:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.cast}</Typography>
              </Box>
            )}
            
            {mediaItem.releaseYear && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Release Year:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.releaseYear}</Typography>
              </Box>
            )}
            
            {mediaItem.runtimeMinutes && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Runtime:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.runtimeMinutes} minutes</Typography>
              </Box>
            )}
            
            {mediaItem.mpaaRating && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>MPAA Rating:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.mpaaRating}</Typography>
              </Box>
            )}
            
            {mediaItem.tmdbRating && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>TMDB Rating:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.tmdbRating}/10</Typography>
              </Box>
            )}
            
            {mediaItem.imdbId && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>IMDB ID:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>{mediaItem.imdbId}</Typography>
              </Box>
            )}
            
            {mediaItem.tmdbId && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>TMDB ID:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>{mediaItem.tmdbId}</Typography>
              </Box>
            )}
            
            {mediaItem.tagline && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Tagline:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontStyle: 'italic', fontSize: '0.875rem' }}>&quot;{mediaItem.tagline}&quot;</Typography>
              </Box>
            )}
            
            {mediaItem.homepage && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Homepage:</strong>
                </Typography>
                <Link
                  href={mediaItem.homepage}
                  target="_blank"
                  rel="noopener noreferrer"
                  sx={{
                    fontSize: '0.875rem',
                    color: '#ffffff',
                    '&:hover': {
                      color: '#e3f2fd'
                    }
                  }}
                >
                  {mediaItem.homepage}
                </Link>
              </Box>
            )}
            
            {/* JustWatch Link */}
            <Box sx={{
              display: 'flex',
              flexDirection: { xs: 'column', sm: 'row' },
              alignItems: { xs: 'flex-start', sm: 'center' },
              gap: { xs: 0.5, sm: 0 }
            }}>
              <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                <strong>Where to Watch:</strong>
              </Typography>
              <Link
                href={getJustWatchUrl(mediaItem.title)}
                target="_blank"
                rel="noopener noreferrer"
                sx={{
                  fontSize: '0.875rem',
                  color: '#ffffff',
                  display: 'flex',
                  alignItems: 'center',
                  '&:hover': {
                    color: '#e3f2fd'
                  }
                }}
              >
                <OpenInNew sx={{ fontSize: 16, mr: 0.5 }} />
                Search on JustWatch
              </Link>
            </Box>
            
            {mediaItem.originalLanguage && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Original Language:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.originalLanguage}</Typography>
              </Box>
            )}
            
            {mediaItem.originalTitle && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Original Title:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.originalTitle}</Typography>
              </Box>
            )}
          </Box>
        )}
        
        {/* TV Show-specific properties */}
        {mediaItem.mediaType === 'TVShow' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.creator && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Creator:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.creator}</Typography>
              </Box>
            )}
            
            {mediaItem.cast && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Cast:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.cast}</Typography>
              </Box>
            )}
            
            {mediaItem.firstAirYear && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>First Air Year:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.firstAirYear}</Typography>
              </Box>
            )}
            
            {mediaItem.lastAirYear && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Last Air Year:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.lastAirYear}</Typography>
              </Box>
            )}
            
            {mediaItem.numberOfSeasons && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Seasons:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.numberOfSeasons}</Typography>
              </Box>
            )}
            
            {mediaItem.numberOfEpisodes && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Episodes:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.numberOfEpisodes}</Typography>
              </Box>
            )}
            
            {mediaItem.contentRating && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Content Rating:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.contentRating}</Typography>
              </Box>
            )}
            
            
            {mediaItem.tmdbRating && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>TMDB Rating:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.tmdbRating}/10</Typography>
              </Box>
            )}
            
            {mediaItem.tmdbId && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>TMDB ID:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>{mediaItem.tmdbId}</Typography>
              </Box>
            )}
            
            {mediaItem.tagline && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Tagline:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontStyle: 'italic', fontSize: '0.875rem' }}>&quot;{mediaItem.tagline}&quot;</Typography>
              </Box>
            )}
            
            {mediaItem.homepage && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Homepage:</strong>
                </Typography>
                <Link
                  href={mediaItem.homepage}
                  target="_blank"
                  rel="noopener noreferrer"
                  sx={{
                    fontSize: '0.875rem',
                    color: '#ffffff',
                    '&:hover': {
                      color: '#e3f2fd'
                    }
                  }}
                >
                  {mediaItem.homepage}
                </Link>
              </Box>
            )}
            
            {/* JustWatch Link */}
            <Box sx={{
              display: 'flex',
              flexDirection: { xs: 'column', sm: 'row' },
              alignItems: { xs: 'flex-start', sm: 'center' },
              gap: { xs: 0.5, sm: 0 }
            }}>
              <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                <strong>Where to Watch:</strong>
              </Typography>
              <Link
                href={getJustWatchUrl(mediaItem.title)}
                target="_blank"
                rel="noopener noreferrer"
                sx={{
                  fontSize: '0.875rem',
                  color: '#ffffff',
                  display: 'flex',
                  alignItems: 'center',
                  '&:hover': {
                    color: '#e3f2fd'
                  }
                }}
              >
                <OpenInNew sx={{ fontSize: 16, mr: 0.5 }} />
                Search on JustWatch
              </Link>
            </Box>
            
            {mediaItem.originalLanguage && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Original Language:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.originalLanguage}</Typography>
              </Box>
            )}
            
            {mediaItem.originalName && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Original Name:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.originalName}</Typography>
              </Box>
            )}

            {mediaItem.link && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>TMDB:</strong>
                </Typography>
                <Link
                  href={mediaItem.link}
                  target="_blank"
                  rel="noopener noreferrer"
                  sx={{
                    fontSize: '0.875rem',
                    color: '#ffffff',
                    display: 'flex',
                    alignItems: 'center',
                    '&:hover': {
                      color: '#e3f2fd'
                    }
                  }}
                >
                  <OpenInNew sx={{ fontSize: 16, mr: 0.5 }} />
                  View TMDB Page
                </Link>
              </Box>
            )}
          </Box>
        )}
        
        {/* Video-specific properties */}
        {mediaItem.mediaType === 'Video' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.platform && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Platform:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.platform}</Typography>
              </Box>
            )}
            
            {mediaItem.channel && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Channel:</strong>
                </Typography>
                <Link
                  component="button"
                  variant="body1"
                  onClick={() => navigate(`/youtube-channel/${mediaItem.channel.id}`)}
                  sx={{
                    textAlign: 'left',
                    cursor: 'pointer',
                    fontSize: '0.875rem',
                    '&:hover': {
                      textDecoration: 'underline',
                      color: 'primary.light'
                    }
                  }}
                >
                  {mediaItem.channel.title}
                  {mediaItem.channel.subscriberCount && (
                    <Typography
                      component="span"
                      variant="body2"
                      color="text.secondary"
                      sx={{ ml: 1, fontSize: '0.875rem' }}
                    >
                      ({(mediaItem.channel.subscriberCount / 1000000).toFixed(1)}M subscribers)
                    </Typography>
                  )}
                </Link>
              </Box>
            )}
            
            {mediaItem.lengthInSeconds !== undefined && mediaItem.lengthInSeconds !== null && mediaItem.lengthInSeconds > 0 && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>Duration:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  {Math.floor(mediaItem.lengthInSeconds / 3600) > 0 && `${Math.floor(mediaItem.lengthInSeconds / 3600)}:`}
                  {Math.floor((mediaItem.lengthInSeconds % 3600) / 60).toString().padStart(2, '0')}:
                  {(mediaItem.lengthInSeconds % 60).toString().padStart(2, '0')}
                </Typography>
              </Box>
            )}
            
            {mediaItem.externalId && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '120px' }, fontSize: '0.875rem' }}>
                  <strong>External ID:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>{mediaItem.externalId}</Typography>
              </Box>
            )}

            {/* Link to Channel button - shown when video has no channel */}
            {!mediaItem.channel && (
              <Box sx={{ mt: 1 }}>
                <Button
                  variant="outlined"
                  size="small"
                  onClick={handleOpenChannelDialog}
                  sx={{
                    borderColor: 'rgba(255, 255, 255, 0.3)',
                    color: 'text.primary',
                    '&:hover': {
                      borderColor: 'rgba(255, 255, 255, 0.5)',
                      backgroundColor: 'rgba(255, 255, 255, 0.05)'
                    }
                  }}
                >
                  Link to Channel
                </Button>
              </Box>
            )}

            {videoPlaylists && videoPlaylists.length > 0 && (
              <Box sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: { xs: 0.5, sm: 1 }
              }}>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  <strong>Playlists:</strong>
                </Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5, pl: { sm: 2 } }}>
                  {videoPlaylists.map((playlist) => (
                    <Link
                      key={playlist.id}
                      component="button"
                      variant="body2"
                      onClick={() => navigate(`/youtube-playlist/${playlist.id}`)}
                      sx={{
                        textAlign: 'left',
                        cursor: 'pointer',
                        fontSize: '0.875rem',
                        color: '#ffffff',
                        textDecoration: 'none',
                        '&:hover': {
                          textDecoration: 'underline',
                          color: '#e3f2fd'
                        }
                      }}
                    >
                      {playlist.title}
                      {playlist.videoCount && (
                        <Typography
                          component="span"
                          variant="body2"
                          color="text.secondary"
                          sx={{ ml: 1, fontSize: '0.75rem' }}
                        >
                          ({playlist.videoCount} videos)
                        </Typography>
                      )}
                    </Link>
                  ))}
                </Box>
              </Box>
            )}
          </Box>
        )}

        {/* Article-specific properties */}
        {mediaItem.mediaType === 'Article' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.author && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Author:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.author}</Typography>
              </Box>
            )}

            {mediaItem.link && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Source:</strong>
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Link
                    href={mediaItem.link}
                    target="_blank"
                    rel="noopener noreferrer"
                    sx={{
                      fontSize: '0.875rem',
                      color: '#ffffff',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 0.5,
                      '&:hover': {
                        color: '#e3f2fd'
                      }
                    }}
                  >
                    {(() => {
                      try {
                        return new URL(mediaItem.link).hostname.replace('www.', '');
                      } catch {
                        return mediaItem.link;
                      }
                    })()}
                    <OpenInNew sx={{ fontSize: 16 }} />
                  </Link>
                </Box>
              </Box>
            )}

            {mediaItem.publication && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Publication:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.publication}</Typography>
              </Box>
            )}

            {mediaItem.publicationDate && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Publication Date:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  {new Date(mediaItem.publicationDate).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                  })}
                </Typography>
              </Box>
            )}

            {mediaItem.estimatedReadingTimeMinutes > 0 && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Est. Reading Time:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                  {mediaItem.estimatedReadingTimeMinutes} {mediaItem.estimatedReadingTimeMinutes === 1 ? 'minute' : 'minutes'}
                </Typography>
              </Box>
            )}

            {mediaItem.wordCount > 0 && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Word Count:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.wordCount.toLocaleString()}</Typography>
              </Box>
            )}

            {mediaItem.isStarred && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '180px' }, fontSize: '0.875rem' }}>
                  <strong>Starred:</strong>
                </Typography>
                <Star sx={{ color: '#FFD700', fontSize: 20 }} />
              </Box>
            )}

            {/* Fetch Content Button - show if article has Reader document ID but no content */}
            {!mediaItem.fullTextContent && mediaItem.readwiseDocumentId && onFetchContent && (
              <Box sx={{ mt: 2 }}>
                <Button
                  variant="outlined"
                  size="small"
                  onClick={onFetchContent}
                  disabled={fetchingContent}
                  startIcon={fetchingContent ? <CircularProgress size={16} /> : <Download />}
                  sx={{
                    color: 'white',
                    borderColor: 'rgba(255,255,255,0.3)',
                    '&:hover': {
                      borderColor: 'rgba(255,255,255,0.5)',
                      backgroundColor: 'rgba(255,255,255,0.05)'
                    }
                  }}
                >
                  {fetchingContent ? 'Fetching...' : 'Fetch Content from Reader'}
                </Button>
              </Box>
            )}

          </Box>
        )}

        {/* Website-specific properties */}
        {mediaItem.mediaType === 'Website' && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {mediaItem.domain && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '0.875rem' }}>
                  <strong>Domain:</strong>
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Language sx={{ fontSize: 18, color: 'text.secondary' }} />
                  <Link
                    href={mediaItem.link}
                    target="_blank"
                    rel="noopener noreferrer"
                    sx={{
                      fontSize: '0.875rem',
                      color: '#ffffff',
                      '&:hover': {
                        color: '#e3f2fd'
                      }
                    }}
                  >
                    {mediaItem.domain}
                  </Link>
                </Box>
              </Box>
            )}

            {mediaItem.author && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '0.875rem' }}>
                  <strong>Author:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.author}</Typography>
              </Box>
            )}

            {mediaItem.publication && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '0.875rem' }}>
                  <strong>Site Name:</strong>
                </Typography>
                <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>{mediaItem.publication}</Typography>
              </Box>
            )}

            {mediaItem.rssFeedUrl && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '0.875rem' }}>
                  <strong>RSS Feed:</strong>
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <RssFeed sx={{ fontSize: 18, color: '#f5a623' }} />
                  <Link
                    component="button"
                    type="button"
                    onClick={() => setFeedDialogOpen(true)}
                    sx={{
                      fontSize: '0.875rem',
                      color: '#ffffff',
                      '&:hover': {
                        color: '#e3f2fd'
                      }
                    }}
                  >
                    View Feed (JSON)
                  </Link>
                  <Tooltip title="Copy RSS URL for your feed reader">
                    <IconButton
                      size="small"
                      onClick={() => {
                        navigator.clipboard.writeText(mediaItem.rssFeedUrl);
                      }}
                      sx={{
                        color: 'text.secondary',
                        '&:hover': { color: '#ffffff' }
                      }}
                    >
                      <ContentCopy sx={{ fontSize: 16 }} />
                    </IconButton>
                  </Tooltip>
                </Box>
              </Box>
            )}

            {mediaItem.lastCheckedDate && (
              <Box sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 0.5, sm: 0 }
              }}>
                <Typography variant="body1" sx={{ mr: 1, minWidth: { sm: '140px' }, fontSize: '0.875rem' }}>
                  <strong>Last Checked:</strong>
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Schedule sx={{ fontSize: 18, color: 'text.secondary' }} />
                  <Typography variant="body1" sx={{ fontSize: '0.875rem' }}>
                    {new Date(mediaItem.lastCheckedDate).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </Typography>
                </Box>
              </Box>
            )}

            {/* Visit Website Button */}
            {mediaItem.link && (
              <Box sx={{ mt: 1 }}>
                <Button
                  variant="contained"
                  color="primary"
                  size="small"
                  href={mediaItem.link}
                  target="_blank"
                  rel="noopener noreferrer"
                  startIcon={<OpenInNew />}
                  sx={{
                    textTransform: 'none',
                    fontSize: '0.875rem'
                  }}
                >
                  Visit Website
                </Button>
              </Box>
            )}

            {/* RSS Feed Items */}
            {mediaItem.rssFeedUrl && (
              <Box sx={{ mt: 3 }}>
                <Divider sx={{ mb: 2 }} />
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                  <RssFeed sx={{ color: '#f5a623' }} />
                  Latest Posts
                </Typography>

                {loadingRss && (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, py: 2 }}>
                    <CircularProgress size={20} />
                    <Typography variant="body2" color="text.secondary">Loading feed...</Typography>
                  </Box>
                )}

                {rssError && (
                  <Typography variant="body2" color="error" sx={{ py: 1 }}>
                    {rssError}
                  </Typography>
                )}

                {!loadingRss && !rssError && rssFeedItems.length === 0 && (
                  <Typography variant="body2" color="text.secondary" sx={{ py: 1, fontStyle: 'italic' }}>
                    No recent posts available
                  </Typography>
                )}

                {!loadingRss && rssFeedItems.length > 0 && (
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    {rssFeedItems.map((item) => (
                      <Box
                        key={`rss-${item.link || item.guid || item.title}`}
                        sx={{
                          p: 2,
                          backgroundColor: 'rgba(255, 255, 255, 0.03)',
                          borderRadius: 1,
                          border: '1px solid rgba(255, 255, 255, 0.1)',
                          '&:hover': {
                            backgroundColor: 'rgba(255, 255, 255, 0.05)'
                          }
                        }}
                      >
                        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
                          <Article sx={{ color: 'text.secondary', mt: 0.5 }} />
                          <Box sx={{ flex: 1 }}>
                            <Link
                              href={item.link}
                              target="_blank"
                              rel="noopener noreferrer"
                              sx={{
                                color: 'text.primary',
                                textDecoration: 'none',
                                fontWeight: 500,
                                fontSize: '0.95rem',
                                '&:hover': {
                                  color: 'primary.main',
                                  textDecoration: 'underline'
                                }
                              }}
                            >
                              {item.title}
                            </Link>
                            {item.publishedDate && (
                              <Typography variant="caption" sx={{ display: 'block', color: 'text.secondary', mt: 0.5 }}>
                                {new Date(item.publishedDate).toLocaleDateString('en-US', {
                                  year: 'numeric',
                                  month: 'short',
                                  day: 'numeric'
                                })}
                                {item.author && ` • ${item.author}`}
                              </Typography>
                            )}
                            {item.description && (
                              <Typography
                                variant="body2"
                                sx={{
                                  color: 'text.secondary',
                                  mt: 1,
                                  display: '-webkit-box',
                                  WebkitLineClamp: 2,
                                  WebkitBoxOrient: 'vertical',
                                  overflow: 'hidden'
                                }}
                              >
                                {item.description}
                              </Typography>
                            )}
                          </Box>
                        </Box>
                      </Box>
                    ))}
                  </Box>
                )}
              </Box>
            )}

            {/* Coming Soon Message */}
            <Alert
              severity="info"
              sx={{
                mt: 2,
                backgroundColor: 'rgba(33, 150, 243, 0.1)',
                color: 'text.secondary',
                '& .MuiAlert-icon': {
                  color: 'rgba(33, 150, 243, 0.7)'
                }
              }}
            >
              Website archival and save RSS to Readwise coming soon
            </Alert>
          </Box>
        )}
        
        {/* Show message if no specific properties are available */}
        {((mediaItem.mediaType === 'Podcast' && !mediaItem.podcastType && !mediaItem.durationInSeconds && !mediaItem.publisher && !mediaItem.audioLink && !mediaItem.releaseDate) ||
          (mediaItem.mediaType === 'Book' && !mediaItem.author && !mediaItem.isbn && !mediaItem.asin && !mediaItem.format && mediaItem.partOfSeries === undefined) ||
          (mediaItem.mediaType === 'Movie' && !mediaItem.director && !mediaItem.cast && !mediaItem.releaseYear && !mediaItem.runtimeMinutes && !mediaItem.mpaaRating && !mediaItem.tmdbRating) ||
          (mediaItem.mediaType === 'TVShow' && !mediaItem.creator && !mediaItem.cast && !mediaItem.firstAirYear && !mediaItem.numberOfSeasons && !mediaItem.contentRating) ||
          (mediaItem.mediaType === 'Video' && !mediaItem.platform && !mediaItem.channel && !mediaItem.lengthInSeconds && !mediaItem.externalId) ||
          (mediaItem.mediaType === 'Article' && !mediaItem.author && !mediaItem.publication && !mediaItem.publicationDate && !mediaItem.originalUrl && !mediaItem.readingProgress && !mediaItem.estimatedReadingTimeMinutes && !mediaItem.wordCount) ||
          (mediaItem.mediaType === 'Website' && !mediaItem.domain && !mediaItem.author && !mediaItem.publication && !mediaItem.rssFeedUrl && !mediaItem.lastCheckedDate)) && (
          <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
            No specific {mediaItem.mediaType.toLowerCase()} details available
          </Typography>
        )}
        </AccordionDetails>
      </Accordion>

      {/* Channel Selection Dialog */}
      <Dialog
        open={channelDialogOpen}
        onClose={() => setChannelDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Link to YouTube Channel</DialogTitle>
        <DialogContent>
          {loadingChannels ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
              <CircularProgress />
            </Box>
          ) : channels.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              No saved YouTube channels found. Import a channel first.
            </Typography>
          ) : (
            <List>
              {channels.map((channel) => (
                <ListItem key={channel.id} disablePadding>
                  <ListItemButton
                    onClick={() => handleLinkToChannel(channel.id)}
                    disabled={linkingChannel}
                  >
                    <ListItemText
                      primary={channel.title}
                      secondary={channel.description ? channel.description.substring(0, 100) + (channel.description.length > 100 ? '...' : '') : null}
                    />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          )}
        </DialogContent>
      </Dialog>

      {/* RSS Feed JSON Dialog */}
      <Dialog
        open={feedDialogOpen}
        onClose={() => setFeedDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <RssFeed sx={{ color: '#f5a623' }} />
          RSS Feed
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2, wordBreak: 'break-all' }}>
            {mediaItem.rssFeedUrl}
          </Typography>
          {loadingFullFeed ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
              <CircularProgress />
            </Box>
          ) : fullFeedItems.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              No feed items available.
            </Typography>
          ) : (
            <Box
              component="pre"
              sx={{
                backgroundColor: 'rgba(255, 255, 255, 0.05)',
                borderRadius: 1,
                p: 2,
                overflow: 'auto',
                maxHeight: '60vh',
                fontSize: '0.8rem',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word'
              }}
            >
              {JSON.stringify(fullFeedItems, null, 2)}
            </Box>
          )}
        </DialogContent>
      </Dialog>
    </Card>
  );
}

export default React.memo(MediaDetailAccordion);