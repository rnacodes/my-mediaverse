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
  Grid,
  Collapse,
} from '@mui/material';
import {
  AutoAwesome as AutoAwesomeIcon,
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import { getMediaForNote as getRecommendedMediaForNote } from '../api/recommendationService';
import { formatMediaType } from '../utils/formatters';

function RelatedMediaByEmbeddingSection({ note }) {
  const [relatedMedia, setRelatedMedia] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [hasEmbedding, setHasEmbedding] = useState(true);
  const [expanded, setExpanded] = useState(false);
  const [hasFetched, setHasFetched] = useState(false);

  const fetchRelatedMedia = useCallback(async () => {
    if (!note?.id) return;

    setLoading(true);
    setError(null);

    try {
      const media = await getRecommendedMediaForNote(note.id, 8);
      setRelatedMedia(media || []);
      setHasEmbedding(true);
      setHasFetched(true);
    } catch (err) {
      console.error('Error fetching related media:', err);
      // Check if error is due to missing embedding
      if (err.response?.status === 400 || err.response?.data?.message?.includes('embedding')) {
        setHasEmbedding(false);
        setRelatedMedia([]);
      } else {
        setError(err.response?.data?.message || err.message || 'Failed to load related media');
      }
      setHasFetched(true);
    } finally {
      setLoading(false);
    }
  }, [note?.id]);

  const handleExpandClick = () => {
    const newExpanded = !expanded;
    setExpanded(newExpanded);
    // Fetch on first expand
    if (newExpanded && !hasFetched) {
      fetchRelatedMedia();
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
              Related Media
            </Typography>
          </Box>
          <Alert severity="info">
            Generate embeddings in the AI Admin page to enable related media recommendations.
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
              Related Media
            </Typography>
            <Chip label="AI" size="small" color="secondary" sx={{ ml: 1 }} />
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            {expanded && hasFetched && (
              <Tooltip title="Refresh recommendations">
                <IconButton
                  onClick={(e) => {
                    e.stopPropagation();
                    fetchRelatedMedia();
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
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                <CircularProgress size={32} />
              </Box>
            )}

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            {!loading && !error && hasFetched && relatedMedia.length === 0 && (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                No related media found
              </Typography>
            )}

            {!loading && !error && relatedMedia.length > 0 && (
              <Grid container spacing={2}>
                {relatedMedia.map((item) => (
                  <Grid item xs={6} sm={4} md={3} key={item.id}>
                    <Card
                      component={RouterLink}
                      to={`/media/${item.id}`}
                      sx={{
                        height: '100%',
                        textDecoration: 'none',
                        '&:hover': {
                          transform: 'translateY(-2px)',
                          boxShadow: 3,
                        },
                        transition: 'all 0.2s ease-in-out',
                      }}
                    >
                      {item.thumbnail && (
                        <CardMedia
                          component="img"
                          height="100"
                          image={item.thumbnail}
                          alt={item.title}
                          sx={{ objectFit: 'cover' }}
                          onError={(e) => {
                            e.target.style.display = 'none';
                          }}
                        />
                      )}
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
                    </Card>
                  </Grid>
                ))}
              </Grid>
            )}
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
}

export default React.memo(RelatedMediaByEmbeddingSection);
