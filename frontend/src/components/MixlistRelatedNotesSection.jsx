import React, { useState, useEffect, useCallback } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent, Dialog,
    DialogTitle, DialogContent, DialogActions, TextField, InputAdornment,
    List, ListItem, ListItemText, IconButton, Checkbox,
    CircularProgress, Chip, Link, Tooltip, useMediaQuery, useTheme
} from '@mui/material';
import {
    Article as NoteIcon, Add as AddIcon, Search, Close,
    OpenInNew as OpenInNewIcon, Delete as DeleteIcon
} from '@mui/icons-material';
import { getNotesForMixlist, linkNoteToMixlist, unlinkNoteFromMixlist } from '../api/mixlistService';
import { getAllNotes, searchNotes } from '../api/noteService';

function MixlistRelatedNotesSection({ mixlistId, mixlistName, setSnackbar }) {
    // State for linked notes
    const [linkedNotes, setLinkedNotes] = useState([]);
    const [loading, setLoading] = useState(true);

    // State for dialog
    const [linkDialog, setLinkDialog] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [availableNotes, setAvailableNotes] = useState([]);
    const [loadingAvailable, setLoadingAvailable] = useState(false);
    const [selectedNoteIds, setSelectedNoteIds] = useState(new Set());
    const [linkDescription, setLinkDescription] = useState('');
    const [saving, setSaving] = useState(false);

    // Fetch linked notes
    const fetchLinkedNotes = useCallback(async () => {
        if (!mixlistId) return;

        setLoading(true);
        try {
            const response = await getNotesForMixlist(mixlistId);
            setLinkedNotes(response?.data || []);
        } catch (error) {
            console.error('Error fetching linked notes:', error);
            setLinkedNotes([]);
        } finally {
            setLoading(false);
        }
    }, [mixlistId]);

    useEffect(() => {
        fetchLinkedNotes();
    }, [fetchLinkedNotes]);

    // Fetch available notes when dialog opens
    const fetchAvailableNotes = useCallback(async () => {
        setLoadingAvailable(true);
        try {
            const notes = await getAllNotes();
            setAvailableNotes(notes || []);
        } catch (error) {
            console.error('Error fetching available notes:', error);
            setAvailableNotes([]);
        } finally {
            setLoadingAvailable(false);
        }
    }, []);

    // Search notes
    const handleSearch = useCallback(async (query) => {
        if (!query || query.length < 2) {
            fetchAvailableNotes();
            return;
        }

        setLoadingAvailable(true);
        try {
            const results = await searchNotes(query);
            const hits = results?.hits?.map(hit => hit.document) || [];
            setAvailableNotes(hits);
        } catch (error) {
            console.error('Error searching notes:', error);
            fetchAvailableNotes();
        } finally {
            setLoadingAvailable(false);
        }
    }, [fetchAvailableNotes]);

    // Toggle note selection
    const toggleNoteSelection = (noteId) => {
        setSelectedNoteIds(prev => {
            const newSet = new Set(prev);
            if (newSet.has(noteId)) {
                newSet.delete(noteId);
            } else {
                newSet.add(noteId);
            }
            return newSet;
        });
    };

    // Open dialog
    const handleOpenDialog = () => {
        setLinkDialog(true);
        setSearchQuery('');
        setSelectedNoteIds(new Set());
        setLinkDescription('');
        fetchAvailableNotes();
    };

    // Close dialog
    const handleCloseDialog = () => {
        setLinkDialog(false);
        setSelectedNoteIds(new Set());
        setSearchQuery('');
        setLinkDescription('');
    };

    // Link notes to mixlist
    const handleLinkNote = async () => {
        if (selectedNoteIds.size === 0) {
            setSnackbar?.({ open: true, message: 'Please select at least one note', severity: 'warning' });
            return;
        }

        setSaving(true);
        try {
            let successCount = 0;
            let errorCount = 0;
            for (const noteId of selectedNoteIds) {
                try {
                    await linkNoteToMixlist(mixlistId, noteId, linkDescription || null);
                    successCount++;
                } catch (err) {
                    console.error(`Error linking note ${noteId}:`, err);
                    errorCount++;
                }
            }
            if (successCount > 0) {
                setSnackbar?.({
                    open: true,
                    message: `Linked ${successCount} note${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
                    severity: errorCount > 0 ? 'warning' : 'success'
                });
            } else {
                setSnackbar?.({ open: true, message: 'Failed to link notes', severity: 'error' });
            }
            handleCloseDialog();
            fetchLinkedNotes();
        } catch (error) {
            console.error('Error linking notes:', error);
            setSnackbar?.({ open: true, message: 'Failed to link notes', severity: 'error' });
        } finally {
            setSaving(false);
        }
    };

    // Unlink note from mixlist
    const handleUnlinkNote = async (noteId, noteTitle) => {
        setSaving(true);
        try {
            await unlinkNoteFromMixlist(mixlistId, noteId);
            setSnackbar?.({ open: true, message: `Unlinked note "${noteTitle}"`, severity: 'success' });
            fetchLinkedNotes();
        } catch (error) {
            console.error('Error unlinking note:', error);
            setSnackbar?.({ open: true, message: 'Failed to unlink note', severity: 'error' });
        } finally {
            setSaving(false);
        }
    };

    // Filter available notes (exclude already linked)
    const linkedNoteIds = linkedNotes.map(n => n.id);
    const filteredAvailableNotes = availableNotes.filter(note => !linkedNoteIds.includes(note.id));

    // Get vault color
    const getVaultColor = (vaultName) => {
        switch (vaultName?.toLowerCase()) {
            case 'general':
                return '#4caf50';
            case 'programming':
                return '#2196f3';
            default:
                return '#9e9e9e';
        }
    };

    const muiTheme = useTheme();
    const isMobile = useMediaQuery(muiTheme.breakpoints.down('sm'));

    return (
        <Card sx={{ mt: 3, overflow: 'hidden', borderRadius: 2 }}>
            <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
                {/* Header */}
                <Box sx={{
                    display: 'flex',
                    flexDirection: { xs: 'column', sm: 'row' },
                    justifyContent: 'space-between',
                    alignItems: { xs: 'flex-start', sm: 'center' },
                    gap: { xs: 2, sm: 0 },
                    mb: 3
                }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <NoteIcon sx={{ fontSize: 28, color: 'white' }} />
                        <Typography
                            variant="h5"
                            sx={{
                                fontWeight: 'bold',
                                fontSize: { xs: '1.25rem', sm: '1.5rem' }
                            }}
                        >
                            Related Notes
                        </Typography>
                    </Box>
                    <Button
                        variant="outlined"
                        size="small"
                        startIcon={<AddIcon sx={{ color: 'white' }} />}
                        onClick={handleOpenDialog}
                        fullWidth={isMobile}
                        disabled={saving}
                        sx={{
                            borderColor: 'white',
                            color: 'white',
                            '&:hover': {
                                borderColor: 'white',
                                backgroundColor: 'rgba(255,255,255,0.1)'
                            }
                        }}
                    >
                        Link Note
                    </Button>
                </Box>

                {/* Notes List */}
                {loading ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                        <CircularProgress size={30} />
                    </Box>
                ) : linkedNotes.length > 0 ? (
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                        {linkedNotes.map((note) => (
                            <Box
                                key={note.id}
                                sx={{
                                    p: 2,
                                    borderRadius: 2,
                                    backgroundColor: 'rgba(255, 255, 255, 0.05)',
                                    border: '1px solid rgba(255, 255, 255, 0.1)',
                                    '&:hover': {
                                        backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                    }
                                }}
                            >
                                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                                    <Box sx={{ flex: 1 }}>
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                                            <Typography
                                                component={RouterLink}
                                                to={`/note/${note.id}`}
                                                variant="subtitle1"
                                                sx={{
                                                    fontWeight: 'bold',
                                                    color: 'white',
                                                    textDecoration: 'none',
                                                    '&:hover': {
                                                        textDecoration: 'underline',
                                                        color: '#90caf9'
                                                    }
                                                }}
                                            >
                                                {note.title}
                                            </Typography>
                                            <Chip
                                                label={note.vaultName}
                                                size="small"
                                                sx={{
                                                    backgroundColor: getVaultColor(note.vaultName),
                                                    color: 'white',
                                                    fontWeight: 'bold',
                                                    fontSize: '0.7rem',
                                                    height: '20px'
                                                }}
                                            />
                                        </Box>
                                        {note.description && (
                                            <Typography
                                                variant="body2"
                                                sx={{ color: 'rgba(255, 255, 255, 0.7)', mb: 1 }}
                                            >
                                                {note.description}
                                            </Typography>
                                        )}
                                        {note.linkDescription && (
                                            <Typography
                                                variant="caption"
                                                sx={{
                                                    color: 'rgba(255, 255, 255, 0.5)',
                                                    fontStyle: 'italic',
                                                    display: 'block',
                                                    mb: 1
                                                }}
                                            >
                                                "{note.linkDescription}"
                                            </Typography>
                                        )}
                                        {note.tags && note.tags.length > 0 && (
                                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                                                {note.tags.slice(0, 5).map((tag, idx) => (
                                                    <Chip
                                                        key={idx}
                                                        label={tag}
                                                        size="small"
                                                        sx={{
                                                            backgroundColor: 'rgba(255, 255, 255, 0.1)',
                                                            color: 'rgba(255, 255, 255, 0.7)',
                                                            fontSize: '0.7rem',
                                                            height: '18px'
                                                        }}
                                                    />
                                                ))}
                                                {note.tags.length > 5 && (
                                                    <Chip
                                                        label={`+${note.tags.length - 5}`}
                                                        size="small"
                                                        sx={{
                                                            backgroundColor: 'rgba(255, 255, 255, 0.1)',
                                                            color: 'rgba(255, 255, 255, 0.5)',
                                                            fontSize: '0.7rem',
                                                            height: '18px'
                                                        }}
                                                    />
                                                )}
                                            </Box>
                                        )}
                                    </Box>
                                    <Box sx={{ display: 'flex', gap: 0.5, ml: 1 }}>
                                        {note.sourceUrl && (
                                            <Tooltip title="View in Quartz">
                                                <IconButton
                                                    component={Link}
                                                    href={note.sourceUrl}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    size="small"
                                                    sx={{
                                                        color: 'rgba(255, 255, 255, 0.7)',
                                                        '&:hover': {
                                                            color: 'white',
                                                            backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                                        }
                                                    }}
                                                >
                                                    <OpenInNewIcon fontSize="small" />
                                                </IconButton>
                                            </Tooltip>
                                        )}
                                        <Tooltip title="Unlink note">
                                            <IconButton
                                                onClick={() => handleUnlinkNote(note.id, note.title)}
                                                size="small"
                                                disabled={saving}
                                                sx={{
                                                    color: 'rgba(255, 255, 255, 0.5)',
                                                    '&:hover': {
                                                        color: '#f44336',
                                                        backgroundColor: 'rgba(244, 67, 54, 0.1)'
                                                    }
                                                }}
                                            >
                                                <DeleteIcon fontSize="small" />
                                            </IconButton>
                                        </Tooltip>
                                    </Box>
                                </Box>
                            </Box>
                        ))}
                    </Box>
                ) : (
                    <Typography variant="body1" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                        No related notes. Click "Link Note" to connect Obsidian notes to this mixlist.
                    </Typography>
                )}
            </CardContent>

            {/* Link Note Dialog */}
            <Dialog
                open={linkDialog}
                onClose={handleCloseDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="h6">Link Note</Typography>
                        <IconButton
                            onClick={handleCloseDialog}
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
                        Select notes to link to "{mixlistName}":
                        {selectedNoteIds.size > 0 && (
                            <Chip label={`${selectedNoteIds.size} selected`} size="small" color="success" sx={{ ml: 1 }} />
                        )}
                    </Typography>

                    {/* Search Bar */}
                    <Box sx={{ mb: 2 }}>
                        <TextField
                            fullWidth
                            placeholder="Search notes..."
                            value={searchQuery}
                            onChange={(e) => {
                                setSearchQuery(e.target.value);
                                handleSearch(e.target.value);
                            }}
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
                                '& .MuiInputBase-input::placeholder': {
                                    color: 'rgba(255, 255, 255, 0.5)',
                                    opacity: 1,
                                },
                            }}
                        />
                    </Box>

                    {/* Note List */}
                    {loadingAvailable ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                            <CircularProgress size={30} />
                        </Box>
                    ) : (
                        <List sx={{ maxHeight: '250px', overflowY: 'auto', mb: 2 }}>
                            {filteredAvailableNotes.length > 0 ? (
                                filteredAvailableNotes.map((note) => (
                                    <ListItem
                                        key={note.id}
                                        onClick={() => toggleNoteSelection(note.id)}
                                        sx={{
                                            borderRadius: 1,
                                            mb: 1,
                                            cursor: 'pointer',
                                            backgroundColor: selectedNoteIds.has(note.id)
                                                ? 'rgba(25, 118, 210, 0.3)'
                                                : 'transparent',
                                            border: selectedNoteIds.has(note.id)
                                                ? '2px solid rgba(25, 118, 210, 0.8)'
                                                : '1px solid rgba(255, 255, 255, 0.1)',
                                            '&:hover': {
                                                backgroundColor: selectedNoteIds.has(note.id)
                                                    ? 'rgba(25, 118, 210, 0.4)'
                                                    : 'rgba(255, 255, 255, 0.05)'
                                            }
                                        }}
                                    >
                                        <Checkbox
                                            checked={selectedNoteIds.has(note.id)}
                                            onClick={(e) => e.stopPropagation()}
                                            onChange={() => toggleNoteSelection(note.id)}
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
                                                            height: '18px'
                                                        }}
                                                    />
                                                </Box>
                                            }
                                            secondary={note.description}
                                            secondaryTypographyProps={{
                                                sx: {
                                                    color: 'rgba(255, 255, 255, 0.5)',
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    whiteSpace: 'nowrap'
                                                }
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

                    {/* Link Description */}
                    {selectedNoteIds.size > 0 && (
                        <TextField
                            fullWidth
                            placeholder="Optional: Describe how these notes relate to this mixlist..."
                            value={linkDescription}
                            onChange={(e) => setLinkDescription(e.target.value)}
                            variant="outlined"
                            size="small"
                            multiline
                            rows={2}
                            sx={{
                                '& .MuiOutlinedInput-root': {
                                    color: 'white',
                                    '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                                    '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                                    '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
                                },
                                '& .MuiInputBase-input::placeholder': {
                                    color: 'rgba(255, 255, 255, 0.5)',
                                    opacity: 1,
                                },
                            }}
                        />
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseDialog} sx={{ color: 'white' }}>
                        Cancel
                    </Button>
                    <Button
                        onClick={handleLinkNote}
                        sx={{ color: 'white' }}
                        disabled={selectedNoteIds.size === 0 || saving}
                    >
                        {saving ? 'Linking...' : `Link${selectedNoteIds.size > 1 ? ` (${selectedNoteIds.size})` : ''}`}
                    </Button>
                </DialogActions>
            </Dialog>
        </Card>
    );
}

export default React.memo(MixlistRelatedNotesSection);
