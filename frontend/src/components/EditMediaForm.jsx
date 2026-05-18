import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
    Container, Typography, TextField, Button, Box, MenuItem,
    Card, CardContent, Snackbar, Alert, CircularProgress,
    Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions,
    List, ListItem, ListItemText, IconButton, Chip, InputAdornment, Tooltip, Checkbox
} from '@mui/material';
import { Save, Cancel, ArrowBack, Delete, Add as AddIcon, Search, Close, Delete as DeleteIcon, OpenInNew as OpenInNewIcon, Article as NoteIcon, PlaylistAdd } from '@mui/icons-material';
import { useMediaItem, useUpdateMedia, useDeleteMedia } from '../hooks/useMedia';
import { useUploadThumbnail } from '../hooks/useUpload';
import {
    useNotesForMedia,
    useAllNotes,
    useNoteSearch,
    useLinkNoteToMedia,
    useUnlinkNoteFromMedia,
} from '../hooks/useNote';
import {
    useAllMixlists,
    useAddMediaToMixlist,
    useRemoveMediaFromMixlist,
} from '../hooks/useMixlist';
import { formatStatus } from '../utils/formatters';
import TopicsGenresSection from './TopicsGenresSection';

function EditMediaForm() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [thumbnailFile, setThumbnailFile] = useState(null);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [refreshKey, setRefreshKey] = useState(0);

    // Notes linking state
    const [linkNoteDialog, setLinkNoteDialog] = useState(false);
    const [noteSearchQuery, setNoteSearchQuery] = useState('');
    const [selectedNoteIds, setSelectedNoteIds] = useState(new Set());
    const [linkDescription, setLinkDescription] = useState('');

    // Mixlist management state
    const [addMixlistDialog, setAddMixlistDialog] = useState(false);
    const [mixlistSearchQuery, setMixlistSearchQuery] = useState('');
    const [selectedMixlistIds, setSelectedMixlistIds] = useState(new Set());

    // Queries
    const mediaQuery = useMediaItem(id);
    const mediaItem = mediaQuery.data ?? null;
    const loading = mediaQuery.isLoading;

    const linkedNotesQuery = useNotesForMedia(id);
    const linkedNotes = linkedNotesQuery.data ?? [];
    const loadingNotes = linkedNotesQuery.isLoading;

    const allNotesQuery = useAllNotes(null, {
        enabled: linkNoteDialog && noteSearchQuery.length < 2,
    });
    const noteSearchActive = linkNoteDialog && noteSearchQuery.length >= 2;
    const notesSearchQuery = useNoteSearch(noteSearchActive ? noteSearchQuery : '');
    const availableNotes = useMemo(() => {
        if (noteSearchActive) {
            if (notesSearchQuery.error) return allNotesQuery.data ?? [];
            return notesSearchQuery.data?.hits?.map(hit => hit.document) ?? [];
        }
        return allNotesQuery.data ?? [];
    }, [noteSearchActive, notesSearchQuery.data, notesSearchQuery.error, allNotesQuery.data]);
    const loadingAvailableNotes = noteSearchActive ? notesSearchQuery.isLoading : allNotesQuery.isLoading;

    const mixlistsQuery = useAllMixlists();
    const { currentMixlists, availableMixlists } = useMemo(() => {
        const all = mixlistsQuery.data ?? [];
        const mixlistIds = mediaItem?.mixlistIds || [];
        if (mixlistIds.length === 0) {
            return { currentMixlists: [], availableMixlists: all };
        }
        const set = new Set(mixlistIds);
        return {
            currentMixlists: all.filter(m => set.has(m.id)),
            availableMixlists: all.filter(m => !set.has(m.id)),
        };
    }, [mixlistsQuery.data, mediaItem]);

    // Mutations
    const updateMediaMutation = useUpdateMedia();
    const deleteMediaMutation = useDeleteMedia();
    const uploadThumbnailMutation = useUploadThumbnail();
    const linkNoteMutation = useLinkNoteToMedia();
    const unlinkNoteMutation = useUnlinkNoteFromMedia();
    const addToMixlistMutation = useAddMediaToMixlist();
    const removeFromMixlistMutation = useRemoveMediaFromMixlist();

    const saving = updateMediaMutation.isPending;
    const savingNote = linkNoteMutation.isPending || unlinkNoteMutation.isPending;
    const savingMixlist = addToMixlistMutation.isPending || removeFromMixlistMutation.isPending;

    const [formData, setFormData] = useState({
        title: '',
        mediaType: 'Other',
        status: 'Uncharted',
        rating: '',
        ownershipStatus: '',
        link: '',
        description: '',
        notes: '',
        thumbnail: '',
        genre: '',
        dateCompleted: ''
    });

    // Status options
    const statusOptions = [
        'Uncharted', 'ActivelyExploring', 'Completed', 'Abandoned'
    ];

    // Rating options
    const ratingOptions = [
        'SuperLike', 'Like', 'Neutral', 'Dislike'
    ];

    // Ownership status options
    const ownershipStatusOptions = [
        'Own', 'Rented', 'Streamed'
    ];

    // Sync form state when the media item loads (or refreshKey bumps).
    useEffect(() => {
        if (!mediaItem) return;
        setFormData({
            title: mediaItem.title || mediaItem.Title || '',
            mediaType: mediaItem.mediaType || mediaItem.MediaType || 'Other',
            status: mediaItem.status || mediaItem.Status || 'Uncharted',
            rating: mediaItem.rating || mediaItem.Rating || '',
            ownershipStatus: mediaItem.ownershipStatus || mediaItem.OwnershipStatus || '',
            link: mediaItem.link || mediaItem.Link || '',
            description: mediaItem.description || mediaItem.Description || '',
            notes: mediaItem.notes || mediaItem.Notes || '',
            thumbnail: mediaItem.thumbnail || mediaItem.Thumbnail || '',
            genre: mediaItem.genre || mediaItem.Genre || '',
            dateCompleted: mediaItem.dateCompleted || mediaItem.DateCompleted ?
                new Date(mediaItem.dateCompleted || mediaItem.DateCompleted).toISOString().split('T')[0] : ''
        });
    }, [mediaItem, refreshKey]);

    useEffect(() => {
        if (mediaQuery.error) {
            console.error('Failed to fetch media:', mediaQuery.error);
            setSnackbar({ open: true, message: 'Failed to load media item', severity: 'error' });
        }
    }, [mediaQuery.error]);

    // Toggle mixlist selection
    const toggleMixlistSelection = (mixlistId) => {
        setSelectedMixlistIds(prev => {
            const newSet = new Set(prev);
            if (newSet.has(mixlistId)) {
                newSet.delete(mixlistId);
            } else {
                newSet.add(mixlistId);
            }
            return newSet;
        });
    };

    // Mixlist handlers
    const handleOpenMixlistDialog = () => {
        setAddMixlistDialog(true);
        setMixlistSearchQuery('');
        setSelectedMixlistIds(new Set());
    };

    const handleCloseMixlistDialog = () => {
        setAddMixlistDialog(false);
        setSelectedMixlistIds(new Set());
        setMixlistSearchQuery('');
    };

    const handleAddToMixlist = async () => {
        if (selectedMixlistIds.size === 0) {
            setSnackbar({ open: true, message: 'Please select at least one mixlist', severity: 'warning' });
            return;
        }
        let successCount = 0;
        let errorCount = 0;
        for (const mixlistId of selectedMixlistIds) {
            try {
                await addToMixlistMutation.mutateAsync({ mixlistId, mediaItemId: id });
                successCount++;
            } catch (err) {
                console.error(`Failed to add to mixlist ${mixlistId}:`, err);
                errorCount++;
            }
        }
        if (successCount > 0) {
            // Bump refreshKey so mediaQuery refetches and currentMixlists/availableMixlists re-derive.
            setRefreshKey(k => k + 1);
            mediaQuery.refetch();
            setSnackbar({
                open: true,
                message: `Added to ${successCount} mixlist${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
                severity: errorCount > 0 ? 'warning' : 'success'
            });
        } else {
            setSnackbar({ open: true, message: 'Failed to add to mixlists', severity: 'error' });
        }
        handleCloseMixlistDialog();
    };

    const handleRemoveFromMixlist = (mixlistId, mixlistName) => {
        removeFromMixlistMutation.mutate(
            { mixlistId, mediaItemId: id },
            {
                onSuccess: () => {
                    setRefreshKey(k => k + 1);
                    mediaQuery.refetch();
                    setSnackbar({ open: true, message: `Removed from "${mixlistName}"`, severity: 'success' });
                },
                onError: (error) => {
                    console.error('Error removing from mixlist:', error);
                    setSnackbar({ open: true, message: 'Failed to remove from mixlist', severity: 'error' });
                },
            }
        );
    };

    const filteredAvailableMixlistsForDialog = availableMixlists.filter(m =>
        (m.name || '').toLowerCase().includes(mixlistSearchQuery.toLowerCase()) ||
        (m.description || '').toLowerCase().includes(mixlistSearchQuery.toLowerCase())
    );

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

    // Open link note dialog
    const handleOpenLinkNoteDialog = () => {
        setLinkNoteDialog(true);
        setNoteSearchQuery('');
        setSelectedNoteIds(new Set());
        setLinkDescription('');
    };

    // Close link note dialog
    const handleCloseLinkNoteDialog = () => {
        setLinkNoteDialog(false);
        setSelectedNoteIds(new Set());
        setNoteSearchQuery('');
        setLinkDescription('');
    };

    // Link notes to media
    const handleLinkNote = async () => {
        if (selectedNoteIds.size === 0) {
            setSnackbar({ open: true, message: 'Please select at least one note', severity: 'warning' });
            return;
        }
        let successCount = 0;
        let errorCount = 0;
        for (const noteId of selectedNoteIds) {
            try {
                await linkNoteMutation.mutateAsync({
                    noteId,
                    mediaItemId: id,
                    linkDescription: linkDescription || null,
                });
                successCount++;
            } catch (err) {
                console.error(`Error linking note ${noteId}:`, err);
                errorCount++;
            }
        }
        if (successCount > 0) {
            setSnackbar({
                open: true,
                message: `Linked ${successCount} note${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
                severity: errorCount > 0 ? 'warning' : 'success'
            });
        } else {
            setSnackbar({ open: true, message: 'Failed to link notes', severity: 'error' });
        }
        handleCloseLinkNoteDialog();
    };

    // Unlink note from media
    const handleUnlinkNote = (noteId, noteTitle) => {
        unlinkNoteMutation.mutate(
            { noteId, mediaItemId: id },
            {
                onSuccess: () => {
                    setSnackbar({ open: true, message: `Unlinked note "${noteTitle}"`, severity: 'success' });
                },
                onError: (error) => {
                    console.error('Error unlinking note:', error);
                    setSnackbar({ open: true, message: 'Failed to unlink note', severity: 'error' });
                },
            }
        );
    };

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

    // Filter available notes (exclude already linked)
    const linkedNoteIds = linkedNotes.map(n => n.id);
    const filteredAvailableNotes = availableNotes.filter(note => !linkedNoteIds.includes(note.id));

    const handleInputChange = (field, value) => {
        setFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    // Handle thumbnail file upload
    const handleThumbnailUpload = (event) => {
        const file = event.target.files[0];
        if (!file) return;
        setThumbnailFile(file);
        uploadThumbnailMutation.mutate(file, {
            onSuccess: (data) => {
                handleInputChange('thumbnail', data.url);
                setSnackbar({
                    open: true,
                    message: 'Thumbnail uploaded successfully!',
                    severity: 'success'
                });
            },
            onError: (error) => {
                console.error('Error uploading thumbnail:', error);
                setSnackbar({
                    open: true,
                    message: 'Failed to upload thumbnail. Please try again.',
                    severity: 'error'
                });
                setThumbnailFile(null);
            },
        });
    };

    const handleSubmit = (e) => {
        e.preventDefault();

        const updateData = {
            title: formData.title,
            mediaType: formData.mediaType,
            status: formData.status,
            rating: formData.rating || null,
            ownershipStatus: formData.ownershipStatus || null,
            link: formData.link || null,
            description: formData.description || null,
            notes: formData.notes || null,
            thumbnail: formData.thumbnail || null,
            genre: formData.genre || null,
            dateCompleted: formData.dateCompleted ? new Date(formData.dateCompleted).toISOString() : null,
            topics: mediaItem?.topics || mediaItem?.topicNames || [],
            genres: mediaItem?.genres || mediaItem?.genreNames || []
        };

        updateMediaMutation.mutate(
            { id, mediaData: updateData },
            {
                onSuccess: () => {
                    setSnackbar({
                        open: true,
                        message: 'Media item updated successfully!',
                        severity: 'success'
                    });
                    setTimeout(() => navigate(`/media/${id}`), 1500);
                },
                onError: (error) => {
                    console.error('Failed to update media:', error);
                    setSnackbar({
                        open: true,
                        message: error.response?.data?.message || 'Failed to update media item',
                        severity: 'error'
                    });
                },
            }
        );
    };

    const handleCancel = () => {
        navigate(`/media/${id}`);
    };

    const handleDelete = () => {
        deleteMediaMutation.mutate(id, {
            onSuccess: () => {
                setSnackbar({
                    open: true,
                    message: 'Media item deleted successfully!',
                    severity: 'success'
                });
                setTimeout(() => navigate('/'), 1500);
            },
            onError: (error) => {
                console.error('Failed to delete media:', error);
                setSnackbar({
                    open: true,
                    message: error.response?.data?.error || 'Failed to delete media item',
                    severity: 'error'
                });
            },
            onSettled: () => setDeleteDialogOpen(false),
        });
    };

    if (loading) {
        return (
            <Container maxWidth="md" sx={{ px: { xs: 2, sm: 3 } }}>
                <Box sx={{ 
                    display: 'flex', 
                    flexDirection: 'column',
                    justifyContent: 'center', 
                    alignItems: 'center', 
                    minHeight: '50vh',
                    gap: 2
                }}>
                    <CircularProgress size={60} />
                    <Typography variant="h6" color="text.secondary" sx={{ fontSize: { xs: '1rem', sm: '1.25rem' } }}>
                        Loading media item...
                    </Typography>
                </Box>
            </Container>
        );
    }

    return (
        <Container maxWidth="md" sx={{ px: { xs: 2, sm: 3 }, py: { xs: 2, sm: 3, md: 4 } }}>
            <Box sx={{ mt: { xs: 2, sm: 3, md: 4 } }}>
                {/* Header */}
                <Box sx={{ 
                    display: 'flex', 
                    flexDirection: { xs: 'column', sm: 'row' },
                    alignItems: { xs: 'flex-start', sm: 'center' },
                    mb: { xs: 3, sm: 4 },
                    gap: { xs: 2, sm: 0 }
                }}>
                    <Button
                        onClick={handleCancel}
                        startIcon={<ArrowBack />}
                        variant="outlined"
                        sx={{ 
                            mr: { xs: 0, sm: 2 },
                            minHeight: '44px',
                            color: 'white',
                            borderColor: 'white',
                            fontSize: { xs: '0.875rem', sm: '1rem' },
                            '&:hover': {
                                borderColor: 'white',
                                backgroundColor: 'rgba(255, 255, 255, 0.08)'
                            }
                        }}
                    >
                        Back
                    </Button>
                    <Typography 
                        variant="h4" 
                        component="h1" 
                        sx={{ 
                            fontWeight: 'bold',
                            fontSize: { xs: '1.5rem', sm: '2rem', md: '2.125rem' }
                        }}
                    >
                        Edit Media Item
                    </Typography>
                </Box>

                <Card>
                    <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
                        <form onSubmit={handleSubmit}>
                            <Box sx={{ display: 'flex', flexDirection: 'column', gap: { xs: 2, sm: 3 } }}>
                                {/* Title */}
                                <TextField
                                    fullWidth
                                    label="Title *"
                                    value={formData.title}
                                    onChange={(e) => handleInputChange('title', e.target.value)}
                                    required
                                />

                                {/* Media Type (Display only - not editable) */}
                                <TextField
                                    fullWidth
                                    label="Media Type"
                                    value={formData.mediaType}
                                    disabled
                                    helperText="Media type cannot be changed after creation"
                                />

                                {/* Status */}
                                <TextField
                                    fullWidth
                                    select
                                    label="Status"
                                    value={formData.status}
                                    onChange={(e) => handleInputChange('status', e.target.value)}
                                >
                                    {statusOptions.map((option) => (
                                        <MenuItem key={option} value={option}>
                                            {formatStatus(option)}
                                        </MenuItem>
                                    ))}
                                </TextField>

                                {/* Rating */}
                                <TextField
                                    fullWidth
                                    select
                                    label="Rating"
                                    value={formData.rating}
                                    onChange={(e) => handleInputChange('rating', e.target.value)}
                                >
                                    <MenuItem value="">None</MenuItem>
                                    {ratingOptions.map((option) => (
                                        <MenuItem key={option} value={option}>
                                            {option}
                                        </MenuItem>
                                    ))}
                                </TextField>

                                {/* Ownership Status */}
                                <TextField
                                    fullWidth
                                    select
                                    label="Ownership Status"
                                    value={formData.ownershipStatus}
                                    onChange={(e) => handleInputChange('ownershipStatus', e.target.value)}
                                >
                                    <MenuItem value="">None</MenuItem>
                                    {ownershipStatusOptions.map((option) => (
                                        <MenuItem key={option} value={option}>
                                            {option}
                                        </MenuItem>
                                    ))}
                                </TextField>

                                {/* Link */}
                                <TextField
                                    fullWidth
                                    label="Link/URL"
                                    value={formData.link}
                                    onChange={(e) => handleInputChange('link', e.target.value)}
                                    placeholder="https://example.com"
                                />

                                {/* Topics & Genres */}
                                {mediaItem && (
                                    <TopicsGenresSection
                                        mediaItem={mediaItem}
                                        setSnackbar={setSnackbar}
                                        onUpdate={() => setRefreshKey(k => k + 1)}
                                    />
                                )}

                                {/* Thumbnail URL */}
                                <TextField
                                    fullWidth
                                    label="Thumbnail URL"
                                    value={formData.thumbnail}
                                    onChange={(e) => handleInputChange('thumbnail', e.target.value)}
                                    placeholder="https://example.com/image.jpg"
                                />

                                {/* Thumbnail Upload */}
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="body1" sx={{ 
                                        mb: 2, 
                                        fontSize: { xs: '0.875rem', sm: '1rem' },
                                        fontWeight: 'bold'
                                    }}>
                                        Upload New Thumbnail
                                    </Typography>
                                    <Button
                                        variant="contained"
                                        color="primary"
                                        component="label"
                                        sx={{ 
                                            fontSize: { xs: '0.875rem', sm: '1rem' },
                                            fontWeight: 'bold',
                                            textTransform: 'none',
                                            py: 1.5,
                                            px: 3,
                                            minHeight: '48px',
                                            width: { xs: '100%', sm: 'auto' },
                                            borderRadius: '8px'
                                        }}
                                    >
                                        Choose File
                                        <input
                                            type="file"
                                            accept="image/*"
                                            hidden
                                            onChange={handleThumbnailUpload}
                                        />
                                    </Button>
                                    {thumbnailFile && (
                                        <Typography variant="body2" sx={{ 
                                            mt: 1, 
                                            fontSize: { xs: '0.75rem', sm: '0.875rem' },
                                            color: 'text.secondary'
                                        }}>
                                            Selected: {thumbnailFile.name}
                                        </Typography>
                                    )}
                                </Box>

                                {/* Date Completed */}
                                <TextField
                                    fullWidth
                                    label="Date Completed"
                                    type="date"
                                    value={formData.dateCompleted}
                                    onChange={(e) => handleInputChange('dateCompleted', e.target.value)}
                                    InputLabelProps={{ shrink: true }}
                                />

                                {/* Description */}
                                <TextField
                                    fullWidth
                                    label="Description"
                                    multiline
                                    rows={4}
                                    value={formData.description}
                                    onChange={(e) => handleInputChange('description', e.target.value)}
                                />

                                {/* Notes */}
                                <TextField
                                    fullWidth
                                    label="Notes"
                                    multiline
                                    rows={4}
                                    value={formData.notes}
                                    onChange={(e) => handleInputChange('notes', e.target.value)}
                                />

                                {/* Mixlists Section */}
                                <Box sx={{
                                    border: '1px solid rgba(255, 255, 255, 0.23)',
                                    borderRadius: 1,
                                    p: 2
                                }}>
                                    <Box sx={{
                                        display: 'flex',
                                        justifyContent: 'space-between',
                                        alignItems: 'center',
                                        mb: 2
                                    }}>
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                            <PlaylistAdd sx={{ fontSize: 20, color: 'rgba(255, 255, 255, 0.7)' }} />
                                            <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                                                Mixlists ({currentMixlists.length})
                                            </Typography>
                                        </Box>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            startIcon={<AddIcon />}
                                            onClick={handleOpenMixlistDialog}
                                            disabled={savingMixlist}
                                            sx={{
                                                borderColor: 'rgba(255, 255, 255, 0.5)',
                                                color: 'white',
                                                '&:hover': {
                                                    borderColor: 'white',
                                                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                                }
                                            }}
                                        >
                                            Add to Mixlist
                                        </Button>
                                    </Box>

                                    {currentMixlists.length > 0 ? (
                                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                            {currentMixlists.map((mixlist) => (
                                                <Chip
                                                    key={mixlist.id}
                                                    label={mixlist.name}
                                                    onDelete={() => handleRemoveFromMixlist(mixlist.id, mixlist.name)}
                                                    deleteIcon={<Close sx={{ fontSize: 16, color: 'white !important' }} />}
                                                    disabled={savingMixlist}
                                                    onClick={() => navigate(`/mixlist/${mixlist.id}`)}
                                                    sx={{
                                                        cursor: 'pointer',
                                                        backgroundColor: '#362759',
                                                        color: 'white',
                                                        fontWeight: 'bold',
                                                        '&:hover': {
                                                            backgroundColor: '#2a1e47'
                                                        }
                                                    }}
                                                />
                                            ))}
                                        </Box>
                                    ) : (
                                        <Typography
                                            variant="body2"
                                            color="text.secondary"
                                            sx={{ fontStyle: 'italic', textAlign: 'center', py: 1 }}
                                        >
                                            Not part of any mixlists. Click &quot;Add to Mixlist&quot; to assign.
                                        </Typography>
                                    )}
                                </Box>

                                {/* Linked Notes Section */}
                                <Box sx={{
                                    border: '1px solid rgba(255, 255, 255, 0.23)',
                                    borderRadius: 1,
                                    p: 2
                                }}>
                                    <Box sx={{
                                        display: 'flex',
                                        justifyContent: 'space-between',
                                        alignItems: 'center',
                                        mb: 2
                                    }}>
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                            <NoteIcon sx={{ fontSize: 20, color: 'rgba(255, 255, 255, 0.7)' }} />
                                            <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                                                Linked Notes ({linkedNotes.length})
                                            </Typography>
                                        </Box>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            startIcon={<AddIcon />}
                                            onClick={handleOpenLinkNoteDialog}
                                            disabled={savingNote}
                                            sx={{
                                                borderColor: 'rgba(255, 255, 255, 0.5)',
                                                color: 'white',
                                                '&:hover': {
                                                    borderColor: 'white',
                                                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                                }
                                            }}
                                        >
                                            Link Note
                                        </Button>
                                    </Box>

                                    {loadingNotes ? (
                                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
                                            <CircularProgress size={24} />
                                        </Box>
                                    ) : linkedNotes.length > 0 ? (
                                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                                            {linkedNotes.map((note) => (
                                                <Box
                                                    key={note.id}
                                                    sx={{
                                                        display: 'flex',
                                                        justifyContent: 'space-between',
                                                        alignItems: 'center',
                                                        p: 1.5,
                                                        borderRadius: 1,
                                                        backgroundColor: 'rgba(255, 255, 255, 0.05)',
                                                        border: '1px solid rgba(255, 255, 255, 0.1)',
                                                        '&:hover': {
                                                            backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                                        }
                                                    }}
                                                >
                                                    <Box sx={{ flex: 1, minWidth: 0 }}>
                                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                                                            <Typography
                                                                component={RouterLink}
                                                                to={`/note/${note.id}`}
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
                                                                    fontSize: '0.65rem',
                                                                    height: '18px'
                                                                }}
                                                            />
                                                        </Box>
                                                        {note.linkDescription && (
                                                            <Typography
                                                                variant="caption"
                                                                sx={{
                                                                    color: 'rgba(255, 255, 255, 0.5)',
                                                                    fontStyle: 'italic'
                                                                }}
                                                            >
                                                                &quot;{note.linkDescription}&quot;
                                                            </Typography>
                                                        )}
                                                    </Box>
                                                    <Box sx={{ display: 'flex', gap: 0.5 }}>
                                                        {note.sourceUrl && (
                                                            <Tooltip title="View in Quartz">
                                                                <IconButton
                                                                    href={note.sourceUrl}
                                                                    target="_blank"
                                                                    rel="noopener noreferrer"
                                                                    size="small"
                                                                    sx={{
                                                                        color: 'rgba(255, 255, 255, 0.5)',
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
                                                                disabled={savingNote}
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
                                            ))}
                                        </Box>
                                    ) : (
                                        <Typography
                                            variant="body2"
                                            color="text.secondary"
                                            sx={{ fontStyle: 'italic', textAlign: 'center', py: 1 }}
                                        >
                                            No linked notes. Click &quot;Link Note&quot; to connect Obsidian notes.
                                        </Typography>
                                    )}
                                </Box>

                                {/* Action Buttons */}
                                <Box sx={{ 
                                    display: 'flex', 
                                    flexDirection: { xs: 'column', sm: 'row' },
                                    gap: 2, 
                                    justifyContent: 'space-between', 
                                    mt: { xs: 3, sm: 4 }
                                }}>
                                    <Button
                                        variant="outlined"
                                        startIcon={<Delete />}
                                        onClick={() => setDeleteDialogOpen(true)}
                                        disabled={saving}
                                        size="large"
                                        sx={{
                                            width: { xs: '100%', sm: 'auto' },
                                            minHeight: '48px',
                                            color: 'white',
                                            borderColor: 'white',
                                            fontSize: { xs: '0.875rem', sm: '1rem' },
                                            '&:hover': {
                                                borderColor: 'white',
                                                backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                            }
                                        }}
                                    >
                                        Delete Media
                                    </Button>
                                    <Box sx={{ 
                                        display: 'flex', 
                                        flexDirection: { xs: 'column', sm: 'row' },
                                        gap: 2,
                                        width: { xs: '100%', sm: 'auto' }
                                    }}>
                                        <Button
                                            variant="outlined"
                                            startIcon={<Cancel />}
                                            onClick={handleCancel}
                                            disabled={saving}
                                            size="large"
                                            sx={{
                                                width: { xs: '100%', sm: 'auto' },
                                                minHeight: '48px',
                                                color: 'white',
                                                borderColor: 'white',
                                                fontSize: { xs: '0.875rem', sm: '1rem' },
                                                '&:hover': {
                                                    borderColor: 'white',
                                                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                                }
                                            }}
                                        >
                                            Cancel
                                        </Button>
                                        <Button
                                            type="submit"
                                            variant="contained"
                                            startIcon={<Save />}
                                            disabled={saving}
                                            size="large"
                                            sx={{
                                                width: { xs: '100%', sm: 'auto' },
                                                minHeight: '48px',
                                                fontSize: { xs: '0.875rem', sm: '1rem' }
                                            }}
                                        >
                                            {saving ? 'Saving...' : 'Save Changes'}
                                        </Button>
                                    </Box>
                                </Box>
                            </Box>
                        </form>
                    </CardContent>
                </Card>
            </Box>

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

            {/* Delete Confirmation Dialog */}
            <Dialog
                open={deleteDialogOpen}
                onClose={() => setDeleteDialogOpen(false)}
            >
                <DialogTitle>Confirm Delete</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Are you sure you want to delete &quot;{formData.title}&quot;? This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteDialogOpen(false)}>
                        Cancel
                    </Button>
                    <Button onClick={handleDelete} color="error" variant="contained">
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Link Note Dialog */}
            <Dialog
                open={linkNoteDialog}
                onClose={handleCloseLinkNoteDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="h6">Link Note</Typography>
                        <IconButton
                            onClick={handleCloseLinkNoteDialog}
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
                        Select notes to link to &quot;{formData.title}&quot;:
                        {selectedNoteIds.size > 0 && (
                            <Chip label={`${selectedNoteIds.size} selected`} size="small" color="success" sx={{ ml: 1 }} />
                        )}
                    </Typography>

                    {/* Search Bar */}
                    <Box sx={{ mb: 2 }}>
                        <TextField
                            fullWidth
                            placeholder="Search notes..."
                            value={noteSearchQuery}
                            onChange={(e) => setNoteSearchQuery(e.target.value)}
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
                    {loadingAvailableNotes ? (
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
                                    {noteSearchQuery
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
                            placeholder="Optional: Describe how these notes relate to this media..."
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
                    <Button onClick={handleCloseLinkNoteDialog} sx={{ color: 'white' }}>
                        Cancel
                    </Button>
                    <Button
                        onClick={handleLinkNote}
                        variant="contained"
                        disabled={selectedNoteIds.size === 0 || savingNote}
                    >
                        {savingNote ? 'Linking...' : `Link${selectedNoteIds.size > 1 ? ` (${selectedNoteIds.size})` : ' Note'}`}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Add to Mixlist Dialog */}
            <Dialog
                open={addMixlistDialog}
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
                        Select mixlists to add &quot;{formData.title}&quot; to:
                        {selectedMixlistIds.size > 0 && (
                            <Chip label={`${selectedMixlistIds.size} selected`} size="small" color="success" sx={{ ml: 1 }} />
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

                    {/* Mixlist List */}
                    <List sx={{ maxHeight: '300px', overflowY: 'auto' }}>
                        {filteredAvailableMixlistsForDialog.length > 0 ? (
                            filteredAvailableMixlistsForDialog.map((mixlist) => (
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
                                    : 'No available mixlists. Create a new mixlist first.'}
                            </Typography>
                        )}
                    </List>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseMixlistDialog} sx={{ color: 'white' }}>
                        Cancel
                    </Button>
                    <Button
                        onClick={handleAddToMixlist}
                        sx={{ color: 'white' }}
                        disabled={selectedMixlistIds.size === 0 || savingMixlist}
                    >
                        {savingMixlist ? 'Adding...' : `Add${selectedMixlistIds.size > 1 ? ` (${selectedMixlistIds.size})` : ''}`}
                    </Button>
                </DialogActions>
            </Dialog>
        </Container>
    );
}

export default EditMediaForm;
