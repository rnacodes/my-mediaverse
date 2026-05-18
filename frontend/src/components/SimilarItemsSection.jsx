import React, { useState, useMemo } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Box, Typography, Card, CardContent, CardMedia, CircularProgress, Chip, Alert, IconButton, Tooltip, Collapse } from '@mui/material';
import {
  AutoAwesome as AutoAwesomeIcon,
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  AddCircle as AddCircleIcon,
  CheckCircle as CheckCircleIcon,
} from '@mui/icons-material';
import { useSimilarMedia } from '../hooks/useRecommendation';
import { useRelatedMedia, useSaveRelatedMedia } from '../hooks/useRelatedMedia';
import { formatMediaType } from '../utils/formatters';
import { getAspectRatio } from '../utils/mediaImageUtils';

const DEFAULT_SIMILARITY_THRESHOLD = 0.40;

const getSimilarityThreshold = () => {
  const stored = localStorage.getItem('similarityThreshold');
  if (stored !== null) {
    const val = parseFloat(stored);
    if (!isNaN(val) && val >= 0 && val <= 1) return val;
  }
  return DEFAULT_SIMILARITY_THRESHOLD;
};

// eslint-disable-next-line no-unused-vars -- onRelatedMediaSaved kept for API compatibility; useSaveRelatedMedia invalidates the related-media query for the SavedRelatedMediaSection automatically.
function SimilarItemsSection({ mediaItem, setSnackbar, onRelatedMediaSaved }) {
  const [expanded, setExpanded] = useState(false);
  const [savingItemId, setSavingItemId] = useState(null);

  const similarQuery = useSimilarMedia(mediaItem?.id, 20, null, {
    enabled: !!mediaItem?.id && expanded,
  });
  const relatedQuery = useRelatedMedia(mediaItem?.id, true, {
    enabled: !!mediaItem?.id && expanded,
  });
  const saveMutation = useSaveRelatedMedia();

  const loading = similarQuery.isFetching || relatedQuery.isFetching;
  const hasFetched = similarQuery.isFetched;

  const fetchError = similarQuery.error;
  const hasEmbedding = !fetchError
    || !(fetchError.response?.status === 400 || fetchError.response?.data?.message?.includes('embedding'));
  const error = fetchError && hasEmbedding
    ? (fetchError.response?.data?.message || fetchError.message || 'Failed to load similar items')
    : null;

  const savedItemIds = useMemo(
    () => new Set((relatedQuery.data ?? []).map(r => r.relatedMediaItem?.id).filter(Boolean)),
    [relatedQuery.data]
  );

  const similarItems = useMemo(() => {
    const items = similarQuery.data ?? [];
    const threshold = getSimilarityThreshold();
    return items.filter(item =>
      !savedItemIds.has(item.id) &&
      item.similarityScore >= threshold
    );
  }, [similarQuery.data, savedItemIds]);

  const handleExpandClick = () => {
    setExpanded((prev) => !prev);
  };

  const handleRegenerate = () => {
    similarQuery.refetch();
    relatedQuery.refetch();
  };

  const handleSaveAsRelated = (item, e) => {
    e.preventDefault();
    e.stopPropagation();

    if (savedItemIds.has(item.id) || savingItemId === item.id) return;

    setSavingItemId(item.id);
    saveMutation.mutate(
      {
        sourceMediaItemId: mediaItem.id,
        relatedMediaItemId: item.id,
        source: 'AiRecommended',
        similarityScore: item.similarityScore,
        note: null,
      },
      {
        onSuccess: () => {
          setSnackbar?.({ open: true, message: `"${item.title}" saved as related`, severity: 'success' });
        },
        onError: (err) => {
          console.error('Error saving related media:', err);
          const errorMsg = err.response?.data?.error || err.message || 'Failed to save';
          setSnackbar?.({ open: true, message: errorMsg, severity: 'error' });
        },
        onSettled: () => setSavingItemId(null),
      }
    );
  };

  // Don't render if no embedding (only show after we've fetched)
  if (!hasEmbedding && hasFetched) {
    return (
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <AutoAwesomeIcon color="action" />
            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
              Get Recommendations
            </Typography>
          </Box>
          <Alert severity="info">
            Generate embeddings in the AI Admin page to enable similar item recommendations.
          </Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent sx={{ pb: expanded ? 2 : '16px !important' }}>
        {/* Clickable header for expand/collapse */}
        <Box
          onClick={handleExpandClick}
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            cursor: 'pointer',
            '&:hover': { opacity: 0.8 },
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <AutoAwesomeIcon sx={{ color: expanded ? '#fcfafa' : undefined }} color={expanded ? undefined : 'action'} />
            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
              Get Recommendations
            </Typography>
            <Chip label="AI" size="small" color="secondary" sx={{ ml: 1 }} />
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            {expanded && hasFetched && (
              <Tooltip title="Re-generate recommendations">
                <IconButton
                  onClick={(e) => {
                    e.stopPropagation();
                    handleRegenerate();
                  }}
                  disabled={loading}
                  size="small"
                >
                  <RefreshIcon />
                </IconButton>
              </Tooltip>
            )}
            <IconButton size="small">
              {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          </Box>
        </Box>

        {/* Collapsible content */}
        <Collapse in={expanded}>
          <Box sx={{ mt: 2 }}>
            {loading && (
              <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', py: 3, gap: 1.5 }}>
                <CircularProgress size={32} />
                <Typography variant="body2" color="text.secondary">
                  {hasFetched ? 'Re-generating list...' : 'Generating list...'}
                </Typography>
              </Box>
            )}

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            {!loading && !error && hasFetched && similarItems.length === 0 && (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                No similar items found with high enough similarity
              </Typography>
            )}

            {!loading && !error && similarItems.length > 0 && (
              <Box
                sx={{
                  display: 'flex',
                  overflowX: 'auto',
                  gap: 2,
                  pb: 1,
                  '&::-webkit-scrollbar': {
                    height: 6,
                  },
                  '&::-webkit-scrollbar-track': {
                    bgcolor: 'action.hover',
                    borderRadius: 3,
                  },
                  '&::-webkit-scrollbar-thumb': {
                    bgcolor: 'action.selected',
                    borderRadius: 3,
                  },
                }}
              >
            {similarItems.map((item) => (
              <Card
                key={item.id}
                sx={{
                  minWidth: 160,
                  maxWidth: 160,
                  flexShrink: 0,
                  position: 'relative',
                  '&:hover': {
                    transform: 'translateY(-2px)',
                    boxShadow: 3,
                  },
                  transition: 'all 0.2s ease-in-out',
                }}
              >
                {/* Save button */}
                <Tooltip title={savedItemIds.has(item.id) ? 'Saved as related' : 'Save as related'}>
                  <IconButton
                    size="small"
                    onClick={(e) => handleSaveAsRelated(item, e)}
                    disabled={savedItemIds.has(item.id) || savingItemId === item.id}
                    sx={{
                      position: 'absolute',
                      top: 4,
                      right: 4,
                      zIndex: 2,
                      bgcolor: 'background.paper',
                      boxShadow: 1,
                      '&:hover': { bgcolor: 'background.paper' },
                    }}
                  >
                    {savedItemIds.has(item.id) ? (
                      <CheckCircleIcon fontSize="small" color="success" />
                    ) : savingItemId === item.id ? (
                      <CircularProgress size={16} />
                    ) : (
                      <AddCircleIcon fontSize="small" sx={{ color: '#fcfafa' }} />
                    )}
                  </IconButton>
                </Tooltip>
                <Box
                  component={RouterLink}
                  to={`/media/${item.id}`}
                  sx={{ textDecoration: 'none', display: 'block' }}
                >
                  <Box sx={{
                    width: '100%',
                    aspectRatio: getAspectRatio(item.mediaType),
                    overflow: 'hidden',
                    backgroundColor: 'rgba(255, 255, 255, 0.05)',
                  }}>
                    {item.thumbnail && (
                      <CardMedia
                        component="img"
                        image={item.thumbnail}
                        alt={item.title}
                        sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
                        onError={(e) => {
                          e.target.style.display = 'none';
                        }}
                      />
                    )}
                  </Box>
                  <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                    <Typography
                      variant="body2"
                      sx={{
                        fontWeight: 'bold',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        lineHeight: 1.3,
                        mb: 0.5,
                      }}
                    >
                      {item.title}
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexWrap: 'wrap' }}>
                      <Chip
                        label={formatMediaType(item.mediaType)}
                        size="small"
                        sx={{ fontSize: '0.65rem', height: 20 }}
                      />
                    </Box>
                  </CardContent>
                </Box>
              </Card>
            ))}
              </Box>
            )}
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
}

export default React.memo(SimilarItemsSection);
