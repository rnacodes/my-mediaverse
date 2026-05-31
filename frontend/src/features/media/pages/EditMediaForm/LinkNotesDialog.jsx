import { useState, useMemo } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Box, Typography, Chip,
  TextField, InputAdornment, List, ListItem, ListItemText, Checkbox, IconButton,
  CircularProgress, Button,
} from '@mui/material';
import { Search, Close } from '@mui/icons-material';
import { useAllNotes, useNoteSearch, useLinkNoteToMedia } from '@/hooks/useNote';
import { getVaultColor } from './schema';

// Dialog for searching Obsidian notes and linking one or more to a media item.
// Owns its own search/selection state; reports outcomes via onResult.
function LinkNotesDialog({ open, onClose, mediaId, mediaTitle, linkedNotes, onResult }) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedNoteIds, setSelectedNoteIds] = useState(new Set());
  const [linkDescription, setLinkDescription] = useState('');

  const linkNoteMutation = useLinkNoteToMedia();
  const saving = linkNoteMutation.isPending;

  const searchActive = open && searchQuery.length >= 2;
  const allNotesQuery = useAllNotes(null, { enabled: open && searchQuery.length < 2 });
  const searchResults = useNoteSearch(searchActive ? searchQuery : '');

  const availableNotes = useMemo(() => {
    if (searchActive) {
      if (searchResults.error) return allNotesQuery.data ?? [];
      return searchResults.data?.hits?.map((hit) => hit.document) ?? [];
    }
    return allNotesQuery.data ?? [];
  }, [searchActive, searchResults.data, searchResults.error, allNotesQuery.data]);

  const loadingNotes = searchActive ? searchResults.isLoading : allNotesQuery.isLoading;

  const linkedNoteIds = linkedNotes.map((n) => n.id);
  const filteredNotes = availableNotes.filter((note) => !linkedNoteIds.includes(note.id));

  const toggleSelection = (noteId) => {
    setSelectedNoteIds((prev) => {
      const next = new Set(prev);
      if (next.has(noteId)) next.delete(noteId);
      else next.add(noteId);
      return next;
    });
  };

  const handleClose = () => {
    setSelectedNoteIds(new Set());
    setSearchQuery('');
    setLinkDescription('');
    onClose();
  };

  const handleLink = async () => {
    if (selectedNoteIds.size === 0) {
      onResult('Please select at least one note', 'warning');
      return;
    }
    let successCount = 0;
    let errorCount = 0;
    for (const noteId of selectedNoteIds) {
      try {
        await linkNoteMutation.mutateAsync({ noteId, mediaItemId: mediaId, linkDescription: linkDescription || null });
        successCount++;
      } catch (err) {
        console.error(`Error linking note ${noteId}:`, err);
        errorCount++;
      }
    }
    if (successCount > 0) {
      onResult(
        `Linked ${successCount} note${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
        errorCount > 0 ? 'warning' : 'success'
      );
    } else {
      onResult('Failed to link notes', 'error');
    }
    handleClose();
  };

  const searchFieldSx = {
    '& .MuiOutlinedInput-root': {
      color: 'white',
      '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
      '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
      '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
    },
    '& .MuiInputBase-input::placeholder': { color: 'rgba(255, 255, 255, 0.5)', opacity: 1 },
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Link Note</Typography>
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
          Select notes to link to &quot;{mediaTitle}&quot;:
          {selectedNoteIds.size > 0 && (
            <Chip label={`${selectedNoteIds.size} selected`} size="small" color="success" sx={{ ml: 1 }} />
          )}
        </Typography>

        <Box sx={{ mb: 2 }}>
          <TextField
            fullWidth
            placeholder="Search notes..."
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
            sx={searchFieldSx}
          />
        </Box>

        {loadingNotes ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress size={30} />
          </Box>
        ) : (
          <List sx={{ maxHeight: '250px', overflowY: 'auto', mb: 2 }}>
            {filteredNotes.length > 0 ? (
              filteredNotes.map((note) => (
                <ListItem
                  key={note.id}
                  onClick={() => toggleSelection(note.id)}
                  sx={{
                    borderRadius: 1,
                    mb: 1,
                    cursor: 'pointer',
                    backgroundColor: selectedNoteIds.has(note.id) ? 'rgba(25, 118, 210, 0.3)' : 'transparent',
                    border: selectedNoteIds.has(note.id) ? '2px solid rgba(25, 118, 210, 0.8)' : '1px solid rgba(255, 255, 255, 0.1)',
                    '&:hover': {
                      backgroundColor: selectedNoteIds.has(note.id) ? 'rgba(25, 118, 210, 0.4)' : 'rgba(255, 255, 255, 0.05)',
                    },
                  }}
                >
                  <Checkbox
                    checked={selectedNoteIds.has(note.id)}
                    onClick={(e) => e.stopPropagation()}
                    onChange={() => toggleSelection(note.id)}
                    sx={{ mr: 1 }}
                  />
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        {note.title}
                        <Chip
                          label={note.vaultName || note.vault_name}
                          size="small"
                          sx={{
                            backgroundColor: getVaultColor(note.vaultName || note.vault_name),
                            color: 'white',
                            fontWeight: 'bold',
                            fontSize: '0.65rem',
                            height: '18px',
                          }}
                        />
                      </Box>
                    }
                    secondary={note.description}
                    secondaryTypographyProps={{
                      sx: { color: 'rgba(255, 255, 255, 0.5)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
                    }}
                  />
                </ListItem>
              ))
            ) : (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                {searchQuery
                  ? 'No notes match your search.'
                  : 'No available notes to link. Create notes by syncing from your Quartz vaults.'}
              </Typography>
            )}
          </List>
        )}

        {selectedNoteIds.size > 0 && (
          <TextField
            fullWidth
            placeholder="Optional: Describe how these notes relate to this media..."
            value={linkDescription}
            onChange={(e) => setLinkDescription(e.target.value)}
            variant="outlined"
            size="small"
            multiline
            rows={2}
            sx={searchFieldSx}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} sx={{ color: 'white' }}>
          Cancel
        </Button>
        <Button onClick={handleLink} variant="contained" disabled={selectedNoteIds.size === 0 || saving}>
          {saving ? 'Linking...' : `Link${selectedNoteIds.size > 1 ? ` (${selectedNoteIds.size})` : ' Note'}`}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default LinkNotesDialog;
