import React, { useState, useCallback } from 'react';
import {
    Box, Card, CardContent, Typography, Button, Dialog,
    DialogTitle, DialogContent, DialogActions, TextField, InputAdornment,
    List, ListItem, ListItemText, IconButton, Chip
} from '@mui/material';
import { PlaylistAdd, Search, Close } from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { addMediaToMixlist } from '../api/mixlistService';

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
  const [selectedMixlistId, setSelectedMixlistId] = useState(null);
  const [mixlistSearchQuery, setMixlistSearchQuery] = useState('');
  const navigate = useNavigate();
  const location = useLocation();

  const handleAddToMixlist = useCallback(async () => {
    if (!selectedMixlistId) {
      setSnackbar({ open: true, message: 'Please select a mixlist first', severity: 'warning' });
      return;
    }
    
    try {
      console.log('Adding media to mixlist:', { mixlistId: selectedMixlistId, mediaId: mediaItem.id });
      await addMediaToMixlist(selectedMixlistId, mediaItem.id);
      setSnackbar({ open: true, message: 'Media added to mixlist successfully!', severity: 'success' });
      setAddToMixlistDialog(false);
      setSelectedMixlistId(null);
      setMixlistSearchQuery('');
      
      // To avoid circular dependency and simplify, we will just refetch all mixlists
      // in the parent component after this action, or update the state here more directly.
      // For now, we will just clear the dialog and let the parent handle the refresh.
      // A more robust solution might involve passing a refresh function from the parent.

      // For simplicity, directly update currentMixlists and availableMixlists if the data is available.
      const newlyAddedMixlist = availableMixlists.find(m => m.id === selectedMixlistId);
      if (newlyAddedMixlist) {
        setCurrentMixlists(prev => [...prev, newlyAddedMixlist]);
        setAvailableMixlists(prev => prev.filter(m => m.id !== selectedMixlistId));
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
  }, [selectedMixlistId, mediaItem.id, setSnackbar, availableMixlists, setCurrentMixlists, setAvailableMixlists]);

  const handleCloseMixlistDialog = useCallback(() => {
    setAddToMixlistDialog(false);
    setSelectedMixlistId(null);
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
              {currentMixlists.map((mixlist, index) => (
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
            Select a mixlist to add "{mediaItem?.title}" to:
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
                  onClick={() => setSelectedMixlistId(mixlist.id)}
                  sx={{
                    borderRadius: 1,
                    mb: 1,
                    cursor: 'pointer',
                    backgroundColor: selectedMixlistId === mixlist.id 
                      ? 'rgba(25, 118, 210, 0.3)' 
                      : 'transparent',
                    border: selectedMixlistId === mixlist.id 
                      ? '2px solid rgba(25, 118, 210, 0.8)' 
                      : '1px solid rgba(255, 255, 255, 0.1)',
                    '&:hover': {
                      backgroundColor: selectedMixlistId === mixlist.id 
                        ? 'rgba(25, 118, 210, 0.4)' 
                        : 'rgba(255, 255, 255, 0.05)'
                    }
                  }}
                >
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
            disabled={!selectedMixlistId}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
}

export default React.memo(MixlistCarousel);
