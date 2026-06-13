import React, { useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import {
    Container, Typography, Box, Grid, Card, CardContent, CardMedia,
    Chip, Button, ButtonGroup, List, ListItem, ListItemText,
    ListItemSecondaryAction, IconButton, Divider, CircularProgress,
    Checkbox, Toolbar, Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions,
    Snackbar, Alert
} from '@mui/material';
import { ViewModule, ViewList, OpenInNew, Delete, CheckBox, CheckBoxOutlineBlank } from '@mui/icons-material';
import { useAllMedia, useMediaByType, useBulkDeleteMedia } from '@/hooks/useMedia';
import { formatMediaType, formatStatus } from '@/utils/formatters';
import { getAspectRatioPadding, resolveMediaImage, getPlaceholderImage } from '@/utils/mediaImageUtils';

function AllMedia() {
  const [viewMode, setViewMode] = useState('card'); // 'card' or 'list'
  const [searchParams] = useSearchParams();
  const [selectedItems, setSelectedItems] = useState(new Set());
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });

  const mediaType = searchParams.get('mediaType');
  const allQuery = useAllMedia({ enabled: !mediaType });
  const byTypeQuery = useMediaByType(mediaType);
  const activeQuery = mediaType ? byTypeQuery : allQuery;
  const mediaItems = activeQuery.data ?? [];
  const loading = activeQuery.isLoading;
  const error = activeQuery.error
    ? `Failed to load media items: ${activeQuery.error.response?.data?.error || activeQuery.error.message}`
    : null;

  const bulkDelete = useBulkDeleteMedia();
  const deleting = bulkDelete.isPending;

  const handleViewModeChange = (mode) => {
    setViewMode(mode);
  };

  const handleSelectItem = (itemId) => {
    const newSelected = new Set(selectedItems);
    if (newSelected.has(itemId)) {
      newSelected.delete(itemId);
    } else {
      newSelected.add(itemId);
    }
    setSelectedItems(newSelected);
  };

  const handleSelectAll = () => {
    const allIds = new Set(mediaItems.map(item => item.id));
    setSelectedItems(allIds);
  };

  const handleDeselectAll = () => {
    setSelectedItems(new Set());
  };

  const handleBulkDelete = () => {
    const idsArray = Array.from(selectedItems);
    bulkDelete.mutate(idsArray, {
      onSuccess: () => {
        setSnackbar({
          open: true,
          message: `Successfully deleted ${idsArray.length} media item${idsArray.length !== 1 ? 's' : ''}!`,
          severity: 'success'
        });
        setSelectedItems(new Set());
        setDeleteDialogOpen(false);
      },
      onError: (err) => {
        setSnackbar({
          open: true,
          message: err.response?.data?.error || 'Failed to delete media items',
          severity: 'error'
        });
        setDeleteDialogOpen(false);
      },
    });
  };

  const renderCardView = () => (
    <Grid container spacing={{ xs: 2, sm: 3 }} sx={{ mt: { xs: 1, sm: 2 } }}>
      {mediaItems.map((item) => (
        <Grid item xs={12} sm={6} md={4} key={item.id}>
          <Card 
            sx={{ 
              textDecoration: 'none',
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              position: 'relative',
              '&:hover': {
                transform: { xs: 'none', sm: 'translateY(-2px)' },
                boxShadow: 4
              },
              '&:active': {
                transform: 'scale(0.98)'
              },
              transition: 'all 0.2s ease-in-out'
            }}
          >
            {/* Thumbnail/Image Container - Always rendered for consistent card heights */}
            <Box
              sx={{
                position: 'relative',
                width: '100%',
                maxWidth: '100%',
                paddingTop: getAspectRatioPadding(item.mediaType || item.MediaType),
                overflow: 'hidden',
                backgroundColor: 'rgba(255, 255, 255, 0.05)'
              }}
            >
              <CardMedia
                component="img"
                image={resolveMediaImage(item)}
                alt={item.title || item.Title}
                sx={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  width: '100%',
                  height: '100%',
                  maxWidth: '100%',
                  maxHeight: '100%',
                  objectFit: 'cover'
                }}
                onError={(e) => {
                  e.target.onerror = null;
                  e.target.src = getPlaceholderImage(item.mediaType || item.MediaType);
                }}
              />
            </Box>
            <CardContent 
              component={Link} 
              to={`/media/${item.id}`}
              sx={{ 
                flexGrow: 1,
                textDecoration: 'none',
                p: { xs: 2, sm: 2 }
              }}
            >
              <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, mb: 1 }}>
                <Checkbox
                  checked={selectedItems.has(item.id)}
                  onChange={(e) => {
                    e.stopPropagation();
                    handleSelectItem(item.id);
                  }}
                  onClick={(e) => e.preventDefault()}
                  sx={{ 
                    p: 0,
                    width: { xs: 24, sm: 24 },
                    height: { xs: 24, sm: 24 },
                    mt: 0.5
                  }}
                />
                <Typography 
                  variant="h6" 
                  component="div" 
                  sx={{ 
                    fontSize: { xs: '1rem', sm: '1.125rem', md: '1.25rem' },
                    flex: 1
                  }}
                >
                  {item.title || item.Title}
                </Typography>
              </Box>
              <Chip 
                label={formatMediaType(item.mediaType || item.MediaType)} 
                size="small" 
                sx={{ 
                  mb: 1,
                  fontSize: { xs: '0.7rem', sm: '0.75rem' },
                  height: { xs: '22px', sm: '24px' }
                }}
              />
              {(item.rating || item.Rating) && (
                <Typography 
                  variant="body2" 
                  sx={{ 
                    mb: 1,
                    fontSize: { xs: '0.8rem', sm: '0.875rem' }
                  }}
                >
                  Rating: {item.rating || item.Rating}
                </Typography>
              )}
              <Typography 
                variant="body2" 
                color="text.secondary"
                sx={{ fontSize: { xs: '0.8rem', sm: '0.875rem' } }}
              >
                {formatStatus(item.status || item.Status) || 'No status set'}
              </Typography>
              
              {/* Topics and Genres */}
              {((item.topics?.length > 0) || (item.Topics?.length > 0) || (item.genres?.length > 0) || (item.Genres?.length > 0)) && (
                <Box sx={{ mt: 1, display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                  {(item.topics || item.Topics || []).map((topic) => (
                    <Chip
                      key={`topic-${typeof topic === 'string' ? topic : topic.id || topic.Id || topic.name || topic.Name}`}
                      label={typeof topic === 'string' ? topic : topic.name || topic.Name}
                      size="small"
                      color="primary"
                      variant="outlined"
                      sx={{ 
                        fontSize: { xs: '0.65rem', sm: '0.7rem' }, 
                        height: { xs: '18px', sm: '20px' } 
                      }}
                    />
                  ))}
                  {(item.genres || item.Genres || []).map((genre) => (
                    <Chip
                      key={`genre-${typeof genre === 'string' ? genre : genre.id || genre.Id || genre.name || genre.Name}`}
                      label={typeof genre === 'string' ? genre : genre.name || genre.Name}
                      size="small"
                      color="secondary"
                      variant="outlined"
                      sx={{ 
                        fontSize: { xs: '0.65rem', sm: '0.7rem' }, 
                        height: { xs: '18px', sm: '20px' } 
                      }}
                    />
                  ))}
                </Box>
              )}
              
              {(item.notes || item.Notes) && (
                <Typography 
                  variant="body2" 
                  sx={{ 
                    mt: 1,
                    fontSize: { xs: '0.75rem', sm: '0.875rem' },
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                  }}
                >
                  {item.notes || item.Notes}
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );

  const renderListView = () => (
    <List sx={{ mt: { xs: 1, sm: 2 } }}>
      {mediaItems.map((item, index) => (
        <React.Fragment key={item.id}>
          <ListItem 
            sx={{ 
              textDecoration: 'none',
              px: { xs: 1, sm: 2 },
              py: { xs: 1.5, sm: 2 },
              flexDirection: { xs: 'column', sm: 'row' },
              alignItems: { xs: 'flex-start', sm: 'center' },
              '&:hover': {
                backgroundColor: 'action.hover'
              },
              '&:active': {
                backgroundColor: 'action.selected'
              }
            }}
          >
            <Checkbox
              checked={selectedItems.has(item.id)}
              onChange={() => handleSelectItem(item.id)}
              sx={{ 
                mr: { xs: 0, sm: 2 },
                mb: { xs: 1, sm: 0 },
                width: { xs: 40, sm: 42 },
                height: { xs: 40, sm: 42 }
              }}
            />
            <ListItemText
              sx={{ 
                flex: 1,
                width: { xs: '100%', sm: 'auto' }
              }}
              primary={
                <Box sx={{ 
                  display: 'flex', 
                  flexDirection: { xs: 'column', sm: 'row' },
                  alignItems: { xs: 'flex-start', sm: 'center' }, 
                  gap: 1, 
                  mb: 0.5 
                }}>
                  <Typography 
                    variant="h6" 
                    component={Link}
                    to={`/media/${item.id}`}
                    sx={{ 
                      fontSize: { xs: '1rem', sm: '1.125rem', md: '1.25rem' },
                      textDecoration: 'none',
                      color: 'inherit',
                      cursor: 'pointer',
                      '&:hover': {
                        textDecoration: 'underline'
                      }
                    }}
                  >
                    {item.title || item.Title}
                  </Typography>
                  <Chip 
                    label={formatMediaType(item.mediaType || item.MediaType)} 
                    size="small"
                    sx={{ 
                      fontSize: { xs: '0.7rem', sm: '0.75rem' },
                      height: { xs: '22px', sm: '24px' }
                    }}
                  />
                </Box>
              }
              secondary={
                <Box>
                  {(item.rating || item.Rating) && (
                    <Typography 
                      variant="body2" 
                      sx={{ 
                        mb: 0.5,
                        fontSize: { xs: '0.8rem', sm: '0.875rem' }
                      }}
                    >
                      Rating: {item.rating || item.Rating}
                    </Typography>
                  )}
                  <Typography 
                    variant="body2" 
                    color="text.secondary" 
                    sx={{ 
                      mb: 0.5,
                      fontSize: { xs: '0.8rem', sm: '0.875rem' }
                    }}
                  >
                    {formatStatus(item.status || item.Status) || 'No status set'}
                  </Typography>
                  
                  {/* Topics and Genres */}
                  {((item.topics?.length > 0) || (item.Topics?.length > 0) || (item.genres?.length > 0) || (item.Genres?.length > 0)) && (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 0.5 }}>
                      {(item.topics || item.Topics || []).map((topic) => (
                        <Chip
                          key={`topic-${typeof topic === 'string' ? topic : topic.id || topic.Id || topic.name || topic.Name}`}
                          label={typeof topic === 'string' ? topic : topic.name || topic.Name}
                          size="small"
                          color="primary"
                          variant="outlined"
                          sx={{ 
                            fontSize: { xs: '0.65rem', sm: '0.7rem' }, 
                            height: { xs: '18px', sm: '20px' } 
                          }}
                        />
                      ))}
                      {(item.genres || item.Genres || []).map((genre) => (
                        <Chip
                          key={`genre-${typeof genre === 'string' ? genre : genre.id || genre.Id || genre.name || genre.Name}`}
                          label={typeof genre === 'string' ? genre : genre.name || genre.Name}
                          size="small"
                          color="secondary"
                          variant="outlined"
                          sx={{ 
                            fontSize: { xs: '0.65rem', sm: '0.7rem' }, 
                            height: { xs: '18px', sm: '20px' } 
                          }}
                        />
                      ))}
                    </Box>
                  )}
                  
                  {(item.notes || item.Notes) && (
                    <Typography 
                      variant="body2" 
                      sx={{ 
                        fontSize: { xs: '0.75rem', sm: '0.875rem' },
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                      }}
                    >
                      {item.notes || item.Notes}
                    </Typography>
                  )}
                </Box>
              }
            />
            <ListItemSecondaryAction sx={{ display: { xs: 'none', sm: 'block' } }}>
              <IconButton 
                edge="end" 
                color="primary"
                sx={{
                  width: { sm: 44 },
                  height: { sm: 44 }
                }}
              >
                <OpenInNew />
              </IconButton>
            </ListItemSecondaryAction>
          </ListItem>
          {index < mediaItems.length - 1 && <Divider />}
        </React.Fragment>
      ))}
    </List>
  );

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ px: { xs: 2, sm: 3 } }}>
        <Box sx={{ 
          mt: { xs: 3, sm: 4 }, 
          display: 'flex', 
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          gap: 2
        }}>
          <CircularProgress size={60} />
          <Typography variant="h6" color="text.secondary" sx={{ fontSize: { xs: '1rem', sm: '1.25rem' } }}>
            Loading media...
          </Typography>
        </Box>
      </Container>
    );
  }

  if (error) {
    return (
      <Container maxWidth="lg" sx={{ px: { xs: 2, sm: 3 } }}>
        <Box sx={{ mt: { xs: 3, sm: 4 }, textAlign: 'center', px: 2 }}>
          <Typography 
            variant="h6" 
            color="error" 
            gutterBottom
            sx={{ fontSize: { xs: '1rem', sm: '1.25rem' } }}
          >
            {error}
          </Typography>
          <Button
            variant="contained"
            onClick={() => activeQuery.refetch()}
            sx={{ minHeight: '48px', px: 3 }}
          >
            Retry
          </Button>
        </Box>
      </Container>
    );
  }

  const pageTitle = mediaType ? `${mediaType} Media` : 'All Media';

  return (
    <Container maxWidth="lg" sx={{ px: { xs: 2, sm: 3 }, py: { xs: 2, sm: 3, md: 4 } }}>
      <Box sx={{ mt: { xs: 2, sm: 3, md: 4 } }}>
        {/* Header with View Toggle */}
        <Box sx={{ 
          mb: { xs: 2, sm: 3 }, 
          display: 'flex', 
          flexDirection: { xs: 'column', sm: 'row' },
          justifyContent: 'space-between', 
          alignItems: { xs: 'flex-start', sm: 'center' },
          gap: { xs: 2, sm: 0 }
        }}>
          <Box>
            <Typography 
              variant="h4" 
              component="h1" 
              gutterBottom
              sx={{ fontSize: { xs: '1.5rem', sm: '2rem', md: '2.125rem' } }}
            >
              {pageTitle}
            </Typography>
            <Typography 
              variant="body1" 
              color="text.secondary"
              sx={{ fontSize: { xs: '0.875rem', sm: '1rem' } }}
            >
              {mediaItems.length} media item{mediaItems.length !== 1 ? 's' : ''} found
              {selectedItems.size > 0 && ` (${selectedItems.size} selected)`}
            </Typography>
          </Box>
          
          {/* View Mode Toggle */}
          <ButtonGroup 
            variant="outlined" 
            aria-label="view mode"
            sx={{
              width: { xs: '100%', sm: 'auto' },
              '& .MuiButton-root': {
                color: 'white',
                borderColor: 'white',
                '&:hover': {
                  borderColor: 'white',
                  backgroundColor: 'rgba(255, 255, 255, 0.08)'
                },
                '&.MuiButton-contained': {
                  color: 'background.default',
                  backgroundColor: 'white',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.9)'
                  }
                }
              }
            }}
          >
            <Button
              onClick={() => handleViewModeChange('card')}
              variant={viewMode === 'card' ? 'contained' : 'outlined'}
              startIcon={<ViewModule />}
              sx={{ 
                flex: { xs: 1, sm: 'initial' },
                fontSize: { xs: '0.875rem', sm: '1rem' },
                minHeight: '44px'
              }}
            >
              Cards
            </Button>
            <Button
              onClick={() => handleViewModeChange('list')}
              variant={viewMode === 'list' ? 'contained' : 'outlined'}
              startIcon={<ViewList />}
              sx={{ 
                flex: { xs: 1, sm: 'initial' },
                fontSize: { xs: '0.875rem', sm: '1rem' },
                minHeight: '44px'
              }}
            >
              List
            </Button>
          </ButtonGroup>
        </Box>

        {/* Bulk Actions Toolbar */}
        {mediaItems.length > 0 && (
          <Toolbar 
            sx={{ 
              mb: 2, 
              bgcolor: 'background.paper',
              borderRadius: 1,
              px: { xs: 1, sm: 2 },
              py: { xs: 1, sm: 1 },
              display: 'flex',
              flexDirection: { xs: 'column', sm: 'row' },
              gap: { xs: 1, sm: 2 },
              justifyContent: 'space-between',
              alignItems: { xs: 'stretch', sm: 'center' }
            }}
          >
            <Box sx={{ 
              display: 'flex', 
              flexDirection: { xs: 'column', sm: 'row' },
              gap: 1,
              width: { xs: '100%', sm: 'auto' }
            }}>
              <Button
                variant="outlined"
                size="small"
                onClick={handleSelectAll}
                startIcon={<CheckBox />}
                sx={{
                  color: 'white',
                  borderColor: 'white',
                  minHeight: '44px',
                  fontSize: { xs: '0.8rem', sm: '0.875rem' },
                  '&:hover': {
                    borderColor: 'white',
                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                  }
                }}
              >
                Select All
              </Button>
              <Button
                variant="outlined"
                size="small"
                onClick={handleDeselectAll}
                startIcon={<CheckBoxOutlineBlank />}
                disabled={selectedItems.size === 0}
                sx={{
                  color: 'white',
                  borderColor: 'white',
                  minHeight: '44px',
                  fontSize: { xs: '0.8rem', sm: '0.875rem' },
                  '&:hover': {
                    borderColor: 'white',
                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                  },
                  '&.Mui-disabled': {
                    borderColor: 'rgba(255, 255, 255, 0.3)',
                    color: 'rgba(255, 255, 255, 0.3)'
                  }
                }}
              >
                Deselect All
              </Button>
            </Box>
            <Button
              variant="contained"
              color="error"
              size="small"
              onClick={() => setDeleteDialogOpen(true)}
              startIcon={<Delete />}
              disabled={selectedItems.size === 0}
              sx={{
                minHeight: '44px',
                fontSize: { xs: '0.8rem', sm: '0.875rem' },
                width: { xs: '100%', sm: 'auto' }
              }}
            >
              Delete Selected ({selectedItems.size})
            </Button>
          </Toolbar>
        )}

        {/* Export Button */}
        {/* <Box sx={{ mb: 3, display: 'flex', justifyContent: 'flex-end' }}>
          <Button
            onClick={() => window.open('/api/media/export', '_blank')}
            startIcon={<FileDownload />}
            variant="outlined"
            color="primary"
            sx={{ px: 3, py: 1.5 }}
          >
            Export All Media
          </Button>
        </Box> */}
        
        {mediaItems.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: { xs: 4, sm: 6 }, px: 2 }}>
            <Typography 
              variant="h6" 
              color="text.secondary" 
              gutterBottom
              sx={{ fontSize: { xs: '1rem', sm: '1.25rem' } }}
            >
              {mediaType ? `No ${mediaType} items found` : 'No media items found'}
            </Typography>
            <Typography 
              variant="body2" 
              color="text.secondary" 
              sx={{ 
                mb: { xs: 2, sm: 3 },
                fontSize: { xs: '0.875rem', sm: '0.875rem' }
              }}
            >
              {mediaType 
                ? `Try adding some ${mediaType} items to your library` 
                : 'Try adding some media items to your library'}
            </Typography>
            <Box sx={{ 
              display: 'flex', 
              flexDirection: { xs: 'column', sm: 'row' },
              gap: { xs: 1, sm: 2 },
              justifyContent: 'center',
              alignItems: 'center'
            }}>
              <Button 
                variant="contained" 
                component={Link} 
                to="/add-media"
                sx={{ 
                  width: { xs: '100%', sm: 'auto' },
                  maxWidth: { xs: '400px', sm: 'none' },
                  minHeight: '44px'
                }}
              >
                Add Media
              </Button>
              <Button 
                variant="outlined" 
                component={Link} 
                to="/import-media"
                sx={{ 
                  width: { xs: '100%', sm: 'auto' },
                  maxWidth: { xs: '400px', sm: 'none' },
                  minHeight: '44px',
                  color: 'white',
                  borderColor: 'white',
                  '&:hover': {
                    borderColor: 'white',
                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                  }
                }}
              >
                Import Media
              </Button>
            </Box>
          </Box>
        ) : (
          viewMode === 'card' ? renderCardView() : renderListView()
        )}

        {/* Bulk Delete Confirmation Dialog */}
        <Dialog
          open={deleteDialogOpen}
          onClose={() => !deleting && setDeleteDialogOpen(false)}
        >
          <DialogTitle>Confirm Bulk Delete</DialogTitle>
          <DialogContent>
            <DialogContentText>
              Are you sure you want to delete {selectedItems.size} media item{selectedItems.size !== 1 ? 's' : ''}? 
              This action cannot be undone.
            </DialogContentText>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleting}>
              Cancel
            </Button>
            <Button 
              onClick={handleBulkDelete} 
              color="error" 
              variant="contained"
              disabled={deleting}
            >
              {deleting ? 'Deleting...' : 'Delete'}
            </Button>
          </DialogActions>
        </Dialog>

        {/* Snackbar for feedback */}
        <Snackbar 
          open={snackbar.open} 
          autoHideDuration={6000} 
          onClose={() => setSnackbar({ ...snackbar, open: false })}
        >
          <Alert 
            onClose={() => setSnackbar({ ...snackbar, open: false })} 
            severity={snackbar.severity}
          >
            {snackbar.message}
          </Alert>
        </Snackbar>
      </Box>
    </Container>
  );
}

export default AllMedia;