import { useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Box, Typography, Chip,
  TextField, InputAdornment, List, ListItem, ListItemText, Checkbox, IconButton, Button,
} from '@mui/material';
import { Search, Close } from '@mui/icons-material';
import { useAddMediaToMixlist } from '../../hooks/useMixlist';

// Dialog for adding a media item to one or more mixlists. Owns its own
// search/selection state; reports outcomes via onResult and asks the parent to
// refetch the media item via onChanged (mixlist mutations don't invalidate the
// media detail query, which is where mixlistIds live).
function AddToMixlistDialog({ open, onClose, mediaId, mediaTitle, availableMixlists, onResult, onChanged }) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedIds, setSelectedIds] = useState(new Set());

  const addToMixlistMutation = useAddMediaToMixlist();
  const saving = addToMixlistMutation.isPending;

  const toggleSelection = (mixlistId) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(mixlistId)) next.delete(mixlistId);
      else next.add(mixlistId);
      return next;
    });
  };

  const handleClose = () => {
    setSelectedIds(new Set());
    setSearchQuery('');
    onClose();
  };

  const handleAdd = async () => {
    if (selectedIds.size === 0) {
      onResult('Please select at least one mixlist', 'warning');
      return;
    }
    let successCount = 0;
    let errorCount = 0;
    for (const mixlistId of selectedIds) {
      try {
        await addToMixlistMutation.mutateAsync({ mixlistId, mediaItemId: mediaId });
        successCount++;
      } catch (err) {
        console.error(`Failed to add to mixlist ${mixlistId}:`, err);
        errorCount++;
      }
    }
    if (successCount > 0) {
      onChanged();
      onResult(
        `Added to ${successCount} mixlist${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
        errorCount > 0 ? 'warning' : 'success'
      );
    } else {
      onResult('Failed to add to mixlists', 'error');
    }
    handleClose();
  };

  const filtered = availableMixlists.filter(
    (m) =>
      (m.name || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
      (m.description || '').toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Add to Mixlist</Typography>
          <IconButton
            onClick={handleClose}
            size="small"
            sx={{ color: 'rgba(255, 255, 255, 0.7)', '&:hover': { color: 'white', backgroundColor: 'rgba(255, 255, 255, 0.1)' } }}
          >
            <Close fontSize="small" />
          </IconButton>
        </Box>
      </DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Select mixlists to add &quot;{mediaTitle}&quot; to:
          {selectedIds.size > 0 && (
            <Chip label={`${selectedIds.size} selected`} size="small" color="success" sx={{ ml: 1 }} />
          )}
        </Typography>

        <Box sx={{ mb: 2 }}>
          <TextField
            fullWidth
            placeholder="Search mixlists..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
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
                '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
              },
              '& .MuiInputBase-input::placeholder': { color: 'rgba(255, 255, 255, 0.5)', opacity: 1 },
            }}
          />
        </Box>

        <List sx={{ maxHeight: '300px', overflowY: 'auto' }}>
          {filtered.length > 0 ? (
            filtered.map((mixlist) => (
              <ListItem
                key={mixlist.id}
                onClick={() => toggleSelection(mixlist.id)}
                sx={{
                  borderRadius: 1,
                  mb: 1,
                  cursor: 'pointer',
                  backgroundColor: selectedIds.has(mixlist.id) ? 'rgba(25, 118, 210, 0.3)' : 'transparent',
                  border: selectedIds.has(mixlist.id) ? '2px solid rgba(25, 118, 210, 0.8)' : '1px solid rgba(255, 255, 255, 0.1)',
                  '&:hover': {
                    backgroundColor: selectedIds.has(mixlist.id) ? 'rgba(25, 118, 210, 0.4)' : 'rgba(255, 255, 255, 0.05)',
                  },
                }}
              >
                <Checkbox
                  checked={selectedIds.has(mixlist.id)}
                  onClick={(e) => e.stopPropagation()}
                  onChange={() => toggleSelection(mixlist.id)}
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
              {searchQuery ? 'No mixlists match your search.' : 'No available mixlists. Create a new mixlist first.'}
            </Typography>
          )}
        </List>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} sx={{ color: 'white' }}>
          Cancel
        </Button>
        <Button onClick={handleAdd} sx={{ color: 'white' }} disabled={selectedIds.size === 0 || saving}>
          {saving ? 'Adding...' : `Add${selectedIds.size > 1 ? ` (${selectedIds.size})` : ''}`}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default AddToMixlistDialog;
