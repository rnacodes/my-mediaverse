import React, { useState, useMemo } from 'react';
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  Avatar,
} from '@mui/material';
import {
  Link as LinkIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  AutoAwesome as AutoAwesomeIcon,
  Search as SearchIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import {
  useRelatedMedia,
  useSaveRelatedMedia,
  useRemoveRelatedMedia,
} from '@/hooks/useRelatedMedia';
import { useMediaSearch } from '@/hooks/useMedia';
import { formatMediaType } from '@/utils/formatters';
import { getAspectRatio } from '@/utils/mediaImageUtils';

// eslint-disable-next-line no-unused-vars -- refreshTrigger kept for API compatibility; mutation hooks now invalidate the related-media query automatically.
function SavedRelatedMediaSection({ mediaItem, setSnackbar, refreshTrigger }) {
  const [expanded, setExpanded] = useState(false);
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [submittedSearch, setSubmittedSearch] = useState('');
  const [removingId, setRemovingId] = useState(null);

  const relatedQuery = useRelatedMedia(mediaItem?.id, true, {
    enabled: !!mediaItem?.id && expanded,
  });
  const relatedItems = relatedQuery.data ?? [];
  const loading = relatedQuery.isFetching;
  const hasFetched = relatedQuery.isFetched;
  const error = relatedQuery.error
    ? (relatedQuery.error.response?.data?.error || relatedQuery.error.message || 'Failed to load related items')
    : null;

  const searchQueryResult = useMediaSearch(submittedSearch);
  const searching = searchQueryResult.isFetching;
  const searchResults = useMemo(() => {
    const items = searchQueryResult.data ?? [];
    const sourceItems = relatedQuery.data ?? [];
    const relatedIds = new Set(sourceItems.map(r => r.relatedMediaItem?.id));
    return items
      .filter(item => item.id !== mediaItem?.id && !relatedIds.has(item.id))
      .slice(0, 10);
  }, [searchQueryResult.data, relatedQuery.data, mediaItem?.id]);

  const saveMutation = useSaveRelatedMedia();
  const removeMutation = useRemoveRelatedMedia();

  const handleExpandClick = () => {
    setExpanded((prev) => !prev);
  };

  const handleRemoveRelated = (relation) => {
    const relatedItemId = relation.relatedMediaItem?.id;
    if (!relatedItemId || removingId) return;

    setRemovingId(relatedItemId);
    removeMutation.mutate(
      {
        sourceMediaItemId: relation.sourceMediaItemId,
        relatedMediaItemId: relation.relatedMediaItemId,
      },
      {
        onSuccess: () => {
          setSnackbar?.({ open: true, message: 'Related item removed', severity: 'success' });
        },
        onError: (err) => {
          console.error('Error removing related item:', err);
          setSnackbar?.({ open: true, message: 'Failed to remove related item', severity: 'error' });
        },
        onSettled: () => setRemovingId(null),
      }
    );
  };

  const handleSearch = () => {
    if (!searchQuery.trim()) return;
    setSubmittedSearch(searchQuery.trim());
  };

  const handleAddFromSearch = (item) => {
    saveMutation.mutate(
      { sourceMediaItemId: mediaItem.id, relatedMediaItemId: item.id, source: 'ManuallyAdded' },
      {
        onSuccess: () => {
          setAddDialogOpen(false);
          setSearchQuery('');
          setSubmittedSearch('');
          setSnackbar?.({ open: true, message: `Added "${item.title}" as related`, severity: 'success' });
        },
        onError: (err) => {
          console.error('Error adding related item:', err);
          const errorMsg = err.response?.data?.error || err.message || 'Failed to add';
          setSnackbar?.({ open: true, message: errorMsg, severity: 'error' });
        },
      }
    );
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent sx={{ pb: expanded ? 2 : '16px !important' }}>
        {/* Header */}
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
            <LinkIcon sx={{ color: '#fcfafa' }} />
            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
              Related Media
            </Typography>
            {relatedItems.length > 0 && (
              <Chip label={relatedItems.length} size="small" color="primary" />
            )}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            {expanded && (
              <>
                <Tooltip title="Refresh">
                  <IconButton
                    onClick={(e) => {
                      e.stopPropagation();
                      relatedQuery.refetch();
                    }}
                    size="small"
                    disabled={loading}
                  >
                    <RefreshIcon />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Add related item">
                  <IconButton
                    onClick={(e) => {
                      e.stopPropagation();
                      setAddDialogOpen(true);
                    }}
                    size="small"
                  >
                    <AddIcon sx={{ color: '#fcfafa' }} />
                  </IconButton>
                </Tooltip>
              </>
            )}
            <IconButton size="small">
              {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          </Box>
        </Box>

        {/* Content */}
        <Collapse in={expanded}>
          <Box sx={{ mt: 2 }}>
            {loading && (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                <CircularProgress size={32} />
              </Box>
            )}

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>
            )}

            {!loading && !error && hasFetched && relatedItems.length === 0 && (
              <Box sx={{ textAlign: 'center', py: 2 }}>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  No saved related items yet
                </Typography>
                <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 2 }}>
                  Save items from &quot;Similar Items&quot; section or add manually
                </Typography>
                <Button
                  startIcon={<AddIcon />}
                  onClick={() => setAddDialogOpen(true)}
                  variant="outlined"
                  size="small"
                  sx={{ color: '#fcfafa', borderColor: '#fcfafa', '&:hover': { borderColor: '#fcfafa', bgcolor: 'rgba(252,250,250,0.08)' } }}
                >
                  Add Related Item
                </Button>
              </Box>
            )}

            {!loading && !error && relatedItems.length > 0 && (
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
                {relatedItems.map((relation) => (
                  <Card
                    key={`${relation.sourceMediaItemId}-${relation.relatedMediaItemId}`}
                    sx={{
                      minWidth: 160,
                      maxWidth: 160,
                      position: 'relative',
                      flexShrink: 0,
                      '&:hover': {
                        transform: 'translateY(-2px)',
                        boxShadow: 3,
                      },
                      transition: 'all 0.2s ease-in-out',
                    }}
                  >
                    {/* Delete button */}
                    <Tooltip title="Remove">
                      <IconButton
                        size="small"
                        onClick={() => handleRemoveRelated(relation)}
                        disabled={removingId === relation.relatedMediaItem?.id}
                        sx={{
                          position: 'absolute',
                          top: 4,
                          right: 4,
                          zIndex: 2,
                          bgcolor: 'background.paper',
                          boxShadow: 1,
                          '&:hover': { bgcolor: 'error.light', color: 'white' },
                        }}
                      >
                        {removingId === relation.relatedMediaItem?.id ? (
                          <CircularProgress size={16} />
                        ) : (
                          <DeleteIcon fontSize="small" />
                        )}
                      </IconButton>
                    </Tooltip>
                    <Box
                      component={RouterLink}
                      to={`/media/${relation.relatedMediaItem?.id}`}
                      sx={{ textDecoration: 'none', display: 'block' }}
                    >
                      <Box sx={{
                        width: '100%',
                        aspectRatio: getAspectRatio(relation.relatedMediaItem?.mediaType),
                        overflow: 'hidden',
                        backgroundColor: 'rgba(255, 255, 255, 0.05)',
                      }}>
                        {relation.relatedMediaItem?.thumbnail && (
                          <CardMedia
                            component="img"
                            image={relation.relatedMediaItem.thumbnail}
                            alt={relation.relatedMediaItem.title}
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
                          {relation.relatedMediaItem?.title}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                          <Chip
                            label={formatMediaType(relation.relatedMediaItem?.mediaType)}
                            size="small"
                            sx={{ fontSize: '0.65rem', height: 20 }}
                          />
                          {relation.source === 'AiRecommended' && (
                            <Chip
                              icon={<AutoAwesomeIcon sx={{ fontSize: '12px !important' }} />}
                              label="AI"
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

      {/* Add Dialog */}
      <Dialog
        open={addDialogOpen}
        onClose={() => {
          setAddDialogOpen(false);
          setSearchQuery('');
          setSubmittedSearch('');
        }}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Add Related Media</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', gap: 1, mb: 2, mt: 1 }}>
            <TextField
              fullWidth
              placeholder="Search media items..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={handleKeyPress}
              size="small"
              // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses search field on add-related-media dialog open
              autoFocus
            />
            <Button
              onClick={handleSearch}
              disabled={searching || !searchQuery.trim()}
              variant="contained"
            >
              {searching ? <CircularProgress size={20} /> : <SearchIcon />}
            </Button>
          </Box>
          {searchResults.length > 0 && (
            <List sx={{ maxHeight: 300, overflow: 'auto' }}>
              {searchResults.map((item) => (
                <ListItem
                  key={item.id}
                  button
                  onClick={() => handleAddFromSearch(item)}
                  sx={{
                    borderRadius: 1,
                    mb: 0.5,
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                >
                  <ListItemAvatar>
                    <Avatar
                      src={item.thumbnail}
                      variant="rounded"
                      sx={{ width: 48, height: 48 }}
                    >
                      {item.title?.[0]}
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={item.title}
                    secondary={formatMediaType(item.mediaType)}
                    primaryTypographyProps={{ noWrap: true }}
                  />
                </ListItem>
              ))}
            </List>
          )}
          {!searching && searchQuery && searchResults.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
              No results found. Try a different search term.
            </Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setAddDialogOpen(false);
              setSearchQuery('');
              setSubmittedSearch('');
            }}
            sx={{ color: '#fcfafa' }}
          >
            Cancel
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
}

export default React.memo(SavedRelatedMediaSection);
