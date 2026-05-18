import React, { useState, useCallback } from 'react';
import {
    Box, Card, CardContent, Typography, Button, Dialog,
    DialogTitle, DialogContent, DialogActions, TextField, InputAdornment,
    List, ListItem, ListItemText, IconButton, Chip, Checkbox
} from '@mui/material';
import { PlaylistAdd, Search, Close } from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAddMediaToMixlist } from '../hooks/useMixlist';

function MixlistCarousel({
  mediaItem,
  currentMixlists,
  availableMixlists,
  setCurrentMixlists,
  setAvailableMixlists,
  setSnackbar,
  isMobile
}) {
  const [addToMixlistDialog, setAddToMixlistDialog] = useState(false);
  const [selectedMixlistIds, setSelectedMixlistIds] = useState(new Set());
  const [mixlistSearchQuery, setMixlistSearchQuery] = useState('');
  const navigate = useNavigate();
  const location = useLocation();
  const addMediaMutation = useAddMediaToMixlist();

  const toggleMixlistSelection = useCallback((mixlistId) => {
    setSelectedMixlistIds(prev => {
      const newSet = new Set(prev);
      if (newSet.has(mixlistId)) {
        newSet.delete(mixlistId);
      } else {
        newSet.add(mixlistId);
      }
      return newSet;
    });
  }, []);

  const handleAddToMixlist = useCallback(async () => {
    if (selectedMixlistIds.size === 0) {
      setSnackbar({ open: true, message: 'Please select at least one mixlist', severity: 'warning' });
      return;
    }

    try {
      let successCount = 0;
      let errorCount = 0;
      const addedMixlists = [];

      for (const mixlistId of selectedMixlistIds) {
        try {
          await addMediaMutation.mutateAsync({ mixlistId, mediaItemId: mediaItem.id });
          successCount++;
          const addedMixlist = availableMixlists.find(m => m.id === mixlistId);
          if (addedMixlist) addedMixlists.push(addedMixlist);
        } catch (err) {
          console.error(`Failed to add to mixlist ${mixlistId}:`, err);
          errorCount++;
        }
      }

      if (successCount > 0) {
        setSnackbar({
          open: true,
          message: `Added to ${successCount} mixlist${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
          severity: errorCount > 0 ? 'warning' : 'success'
        });
        setCurrentMixlists(prev => [...prev, ...addedMixlists]);
        setAvailableMixlists(prev => prev.filter(m => !selectedMixlistIds.has(m.id)));
      } else {
        setSnackbar({
          open: true,
          message: 'Failed to add to mixlists',
          severity: 'error'
        });
      }

      setAddToMixlistDialog(false);
      setSelectedMixlistIds(new Set());
      setMixlistSearchQuery('');
    } catch (error) {
      console.error('Failed to add media to mixlists:', error);
      setSnackbar({
        open: true,
        message: `Failed to add media to mixlist: ${error.response?.data?.message || error.message || 'Unknown error'}`,
        severity: 'error'
      });
    }
  }, [selectedMixlistIds, mediaItem.id, setSnackbar, availableMixlists, setCurrentMixlists, setAvailableMixlists, addMediaMutation]);

  const handleCloseMixlistDialog = useCallback(() => {
    setAddToMixlistDialog(false);
    setSelectedMixlistIds(new Set());
    setMixlistSearchQuery('');
  }, []);

  const filteredAvailableMixlists = availableMixlists
    .filter(mixlist => !currentMixlists.some(current => current.id === mixlist.id))
    .filter(mixlist => 
      mixlist.name?.toLowerCase().includes(mixlistSearchQuery.toLowerCase()) ||
      mixlist.description?.toLowerCase().includes(mixlistSearchQuery.toLowerCase())
    );

  const handleCreateNewMixlist = useCallback(() => {
    navigate('/create-mixlist', { state: { returnTo: location.pathname } });
  }, [navigate, location.pathname]);

  return (
    <Card sx={{ mt: 3, overflow: 'hidden', borderRadius: 2 }}>
      <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
        <Box sx={{ 
          display: 'flex', 
          flexDirection: { xs: 'column', sm: 'row' },
          justifyContent: 'space-between', 
          alignItems: { xs: 'flex-start', sm: 'center' },
          gap: { xs: 2, sm: 0 },
          mb: 3 
        }}>
          <Typography 
            variant="h5" 
            sx={{ 
              fontWeight: 'bold',
              fontSize: { xs: '1.25rem', sm: '1.5rem' }
            }}
          >
            Mixlists
          </Typography>
          <Box sx={{ 
            display: 'flex', 
            flexDirection: { xs: 'column', sm: 'row' },
            gap: 1,
            width: { xs: '100%', sm: 'auto' }
          }}>
            <Button
              variant="outlined"
              size="small"
              startIcon={<PlaylistAdd />}
              onClick={() => setAddToMixlistDialog(true)}
              fullWidth={isMobile}
              sx={{ 
                borderColor: 'white',
                color: 'white',
                '&:hover': {
                  borderColor: 'white',
                  backgroundColor: 'rgba(255,255,255,0.1)'
                }
              }}
            >
              Add to Mixlist
            </Button>
            <Button
              variant="contained"
              size="small"
              onClick={handleCreateNewMixlist}
              fullWidth={isMobile}
              sx={{ 
                backgroundColor: 'white',
                color: 'black',
                '&:hover': {
                  backgroundColor: 'rgba(255,255,255,0.9)'
                }
              }}
            >
              Create New
            </Button>
          </Box>
        </Box>
        
        {currentMixlists.length > 0 ? (
          <Box sx={{ position: 'relative' }}>
            {/* Carousel Container */}
            <Box 
              className="mixlist-carousel"
              sx={{ 
                display: 'flex', 
                gap: 2, 
                overflowX: 'auto',
                overflowY: 'hidden',
                scrollBehavior: 'smooth',
                pb: 1,
                '&::-webkit-scrollbar': {
                  height: '8px'
                },
                '&::-webkit-scrollbar-thumb': {
                  backgroundColor: 'rgba(255,255,255,0.3)',
                  borderRadius: '4px'
                }
              }}
            >
              {currentMixlists.map((mixlist) => (
                <Card 
                  key={mixlist.id} 
                  sx={{
                    minWidth: { xs: '85%', sm: 280 },
                    maxWidth: { xs: '85%', sm: 'none' },
                    flexShrink: 0,
                    cursor: 'pointer',
                    transition: 'transform 0.2s ease-in-out',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: '0 8px 25px rgba(0,0,0,0.15)'
                    }
                  }}
                  onClick={() => navigate(`/mixlist/${mixlist.id}`)}
                >
                  <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
                    <Typography 
                      variant="h6" 
                      sx={{ 
                        fontWeight: 'bold', 
                        mb: 1,
                        fontSize: { xs: '1rem', sm: '1.25rem' }
                      }}
                    >
                      {mixlist.name || `Mixlist ${mixlist.id}`}
                    </Typography>
                    {mixlist.description && (
                      <Typography 
                        variant="body2" 
                        color="text.secondary" 
                        sx={{ 
                          mb: 2,
                          fontSize: '0.875rem'
                        }}
                      >
                        {mixlist.description}
                      </Typography>
                    )}
                    <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                      <Chip 
                        label={`${mixlist.mediaItems?.length || 0} items`} 
                        size="small" 
                        variant="outlined"
                      />
                    </Box>
                  </CardContent>
                </Card>
              ))}
            </Box>
          </Box>
        ) : (
          <Box sx={{ textAlign: 'center', py: 3 }}>
            <Typography variant="body1" color="text.secondary">
              This media item is not part of any mixlists yet. Use the buttons above to add it to an existing mixlist or create a new one.
            </Typography>
          </Box>
        )}
      </CardContent>

      {/* Add to Mixlist Dialog */}
      <Dialog 
        open={addToMixlistDialog} 
        onClose={handleCloseMixlistDialog}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="h6">Add to Mixlist</Typography>
            <IconButton
              onClick={handleCloseMixlistDialog}
              size="small"
              sx={{
                color: 'rgba(255, 255, 255, 0.7)',
                '&:hover': {
                  color: 'white',
                  backgroundColor: 'rgba(255, 255, 255, 0.1)'
                }
              }}
            >
              <Close fontSize="small" />
            </IconButton>
          </Box>
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Select mixlists to add &quot;{mediaItem?.title}&quot; to:
            {selectedMixlistIds.size > 0 && (
              <Chip
                label={`${selectedMixlistIds.size} selected`}
                size="small"
                color="success"
                sx={{ ml: 1 }}
              />
            )}
          </Typography>
          
          {/* Search Bar */}
          <Box sx={{ mb: 2 }}>
            <TextField
              fullWidth
              placeholder="Search mixlists..."
              value={mixlistSearchQuery}
              onChange={(e) => setMixlistSearchQuery(e.target.value)}
              variant="outlined"
              size="small"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Search sx={{ color: 'rgba(255, 255, 255, 0.5)' }} />
                  </InputAdornment>
                ),
              }}
              sx={{
                '& .MuiOutlinedInput-root': {
                  color: 'white',
                  '& fieldset': {
                    borderColor: 'rgba(255, 255, 255, 0.3)',
                  },
                  '&:hover fieldset': {
                    borderColor: 'rgba(255, 255, 255, 0.5)',
                  },
                  '&.Mui-focused fieldset': {
                    borderColor: 'rgba(255, 255, 255, 0.7)',
                  },
                },
                '& .MuiInputBase-input::placeholder': {
                  color: 'rgba(255, 255, 255, 0.5)',
                  opacity: 1,
                },
              }}
            />
          </Box>

          {/* Mixlist List */}
          <List sx={{ maxHeight: '300px', overflowY: 'auto' }}>
            {filteredAvailableMixlists.length > 0 ? (
              filteredAvailableMixlists.map((mixlist) => (
                <ListItem
                  key={mixlist.id}
                  onClick={() => toggleMixlistSelection(mixlist.id)}
                  sx={{
                    borderRadius: 1,
                    mb: 1,
                    cursor: 'pointer',
                    backgroundColor: selectedMixlistIds.has(mixlist.id)
                      ? 'rgba(25, 118, 210, 0.3)'
                      : 'transparent',
                    border: selectedMixlistIds.has(mixlist.id)
                      ? '2px solid rgba(25, 118, 210, 0.8)'
                      : '1px solid rgba(255, 255, 255, 0.1)',
                    '&:hover': {
                      backgroundColor: selectedMixlistIds.has(mixlist.id)
                        ? 'rgba(25, 118, 210, 0.4)'
                        : 'rgba(255, 255, 255, 0.05)'
                    }
                  }}
                >
                  <Checkbox
                    checked={selectedMixlistIds.has(mixlist.id)}
                    onClick={(e) => e.stopPropagation()}
                    onChange={() => toggleMixlistSelection(mixlist.id)}
                    sx={{ mr: 1 }}
                  />
                  <ListItemText
                    primary={mixlist.name}
                    secondary={mixlist.description || `${mixlist.mediaItems?.length || 0} items`}
                  />
                </ListItem>
              ))
            ) : (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                {mixlistSearchQuery 
                  ? 'No mixlists match your search.' 
                  : 'No available mixlists to add to. Create a new mixlist first.'}
              </Typography>
            )}
          </List>
        </DialogContent>
        <DialogActions>
          <Button 
            onClick={handleCloseMixlistDialog}
            sx={{ color: 'white' }}
          >
            Cancel
          </Button>
          <Button
            onClick={handleAddToMixlist}
            sx={{ color: 'white' }}
            disabled={selectedMixlistIds.size === 0}
          >
            {`Add${selectedMixlistIds.size > 1 ? ` (${selectedMixlistIds.size})` : ''}`}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
}

export default React.memo(MixlistCarousel);
