import React, { useState, useCallback } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardMedia,
  CircularProgress,
  Chip,
  Alert,
  IconButton,
  Tooltip,
  Button,
  Collapse,
} from '@mui/material';
import {
  AutoAwesome as AutoAwesomeIcon,
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  AddCircle as AddCircleIcon,
  CheckCircle as CheckCircleIcon,
} from '@mui/icons-material';
import { getSimilarMedia } from '../api/recommendationService';
import { saveRelatedMedia, getRelatedMedia } from '../api/relatedMediaService';
import { formatMediaType } from '../utils/formatters';
import { getAspectRatio } from '../utils/mediaImageUtils';

const SIMILARITY_THRESHOLD = 0.40;

function SimilarItemsSection({ mediaItem, setSnackbar, onRelatedMediaSaved }) {
  const [similarItems, setSimilarItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [hasEmbedding, setHasEmbedding] = useState(true);
  const [expanded, setExpanded] = useState(false);
  const [hasFetched, setHasFetched] = useState(false);
  const [savedItemIds, setSavedItemIds] = useState(new Set());
  const [savingItemId, setSavingItemId] = useState(null);

  const generateSimilarItems = useCallback(async () => {
    if (!mediaItem?.id) return;

    setLoading(true);
    setError(null);

    try {
      // Fetch existing related media to exclude from results and initialize saved IDs
      let relatedIds = new Set();
      try {
        const relatedItems = await getRelatedMedia(mediaItem.id);
        relatedIds = new Set(
          (relatedItems || []).map(r => r.relatedMediaItem?.id).filter(Boolean)
        );
        setSavedItemIds(relatedIds);
      } catch {
        // If fetching related media fails, continue without exclusion
      }

      // Request more items to account for filtering
      const items = await getSimilarMedia(mediaItem.id, 20);

      // Filter: exclude already-saved related items and items below threshold
      const filtered = (items || []).filter(item =>
        !relatedIds.has(item.id) &&
        item.similarityScore >= SIMILARITY_THRESHOLD
      );

      setSimilarItems(filtered);
      setHasEmbedding(true);
      setHasFetched(true);
    } catch (err) {
      console.error('Error fetching similar items:', err);
      if (err.response?.status === 400 || err.response?.data?.message?.includes('embedding')) {
        setHasEmbedding(false);
        setSimilarItems([]);
      } else {
        setError(err.response?.data?.message || err.message || 'Failed to load similar items');
      }
      setHasFetched(true);
    } finally {
      setLoading(false);
    }
  }, [mediaItem?.id]);

  const handleExpandClick = () => {
    const newExpanded = !expanded;
    setExpanded(newExpanded);
    // Fetch on first expand
    if (newExpanded && !hasFetched) {
      generateSimilarItems();
    }
  };

  const handleSaveAsRelated = async (item, e) => {
    e.preventDefault();
    e.stopPropagation();

    if (savedItemIds.has(item.id) || savingItemId === item.id) return;

    setSavingItemId(item.id);
    try {
      await saveRelatedMedia(
        mediaItem.id,
        item.id,
        'AiRecommended',
        item.similarityScore,
        null
      );
      setSavedItemIds(prev => new Set([...prev, item.id]));
      setSnackbar?.({ open: true, message: `"${item.title}" saved as related`, severity: 'success' });
      onRelatedMediaSaved?.();
    } catch (err) {
      console.error('Error saving related media:', err);
      const errorMsg = err.response?.data?.error || err.message || 'Failed to save';
      setSnackbar?.({ open: true, message: errorMsg, severity: 'error' });
    } finally {
      setSavingItemId(null);
    }
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
            <AutoAwesomeIcon color={expanded ? 'primary' : 'action'} />
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
                    generateSimilarItems();
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
                      <AddCircleIcon fontSize="small" color="primary" />
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
                      {item.similarityScore && (
                        <Chip
                          label={`${Math.round(item.similarityScore * 100)}%`}
                          size="small"
                          color="secondary"
                          sx={{ fontSize: '0.65rem', height: 20 }}
                        />
                      )}
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
