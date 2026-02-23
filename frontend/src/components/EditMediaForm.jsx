import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
    Container, Typography, TextField, Button, Box, MenuItem,
    Card, CardContent, Snackbar, Alert, CircularProgress,
    Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions,
    List, ListItem, ListItemText, IconButton, Chip, InputAdornment, Tooltip
} from '@mui/material';
import { Save, Cancel, ArrowBack, Delete, Add as AddIcon, Search, Close, Delete as DeleteIcon, OpenInNew as OpenInNewIcon, Article as NoteIcon, PlaylistAdd } from '@mui/icons-material';
import { getMediaById, updateMedia, deleteMedia } from '../api/mediaService';
import { uploadThumbnail } from '../api/uploadService';
import { getNotesForMedia, getAllNotes, searchNotes, linkNoteToMedia, unlinkNoteFromMedia } from '../api/noteService';
import { getAllMixlists, addMediaToMixlist, removeMediaFromMixlist } from '../api/mixlistService';
import { formatStatus, formatMediaType } from '../utils/formatters';
import TopicsGenresSection from './TopicsGenresSection';

function EditMediaForm() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [thumbnailFile, setThumbnailFile] = useState(null);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [mediaItem, setMediaItem] = useState(null);
    const [refreshKey, setRefreshKey] = useState(0);

    // Notes linking state
    const [linkedNotes, setLinkedNotes] = useState([]);
    const [loadingNotes, setLoadingNotes] = useState(false);
    const [linkNoteDialog, setLinkNoteDialog] = useState(false);
    const [noteSearchQuery, setNoteSearchQuery] = useState('');
    const [availableNotes, setAvailableNotes] = useState([]);
    const [loadingAvailableNotes, setLoadingAvailableNotes] = useState(false);
    const [selectedNoteId, setSelectedNoteId] = useState(null);
    const [linkDescription, setLinkDescription] = useState('');
    const [savingNote, setSavingNote] = useState(false);

    // Mixlist management state
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    const [addMixlistDialog, setAddMixlistDialog] = useState(false);
    const [mixlistSearchQuery, setMixlistSearchQuery] = useState('');
    const [selectedMixlistId, setSelectedMixlistId] = useState(null);
    const [savingMixlist, setSavingMixlist] = useState(false);

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

    // Media type options (not editable)
    const mediaTypes = [
        'Article', 'Book', 'Document', 'Movie', 'Music', 'Other',
        'Podcast', 'TVShow', 'Video', 'VideoGame', 'Website'
    ];

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

    useEffect(() => {
        const fetchMedia = async () => {
            try {
                const response = await getMediaById(id);
                const media = response.data;

                setMediaItem(media);
                setFormData({
                    title: media.title || media.Title || '',
                    mediaType: media.mediaType || media.MediaType || 'Other',
                    status: media.status || media.Status || 'Uncharted',
                    rating: media.rating || media.Rating || '',
                    ownershipStatus: media.ownershipStatus || media.OwnershipStatus || '',
                    link: media.link || media.Link || '',
                    description: media.description || media.Description || '',
                    notes: media.notes || media.Notes || '',
                    thumbnail: media.thumbnail || media.Thumbnail || '',
                    genre: media.genre || media.Genre || '',
                    dateCompleted: media.dateCompleted || media.DateCompleted ?
                        new Date(media.dateCompleted || media.DateCompleted).toISOString().split('T')[0] : ''
                });
            } catch (error) {
                console.error('Failed to fetch media:', error);
                setSnackbar({ open: true, message: 'Failed to load media item', severity: 'error' });
            } finally {
                setLoading(false);
            }
        };

        if (id) {
            fetchMedia();
        }
    }, [id, refreshKey]);

    // Fetch linked notes
    const fetchLinkedNotes = useCallback(async () => {
        if (!id) return;
        setLoadingNotes(true);
        try {
            const notes = await getNotesForMedia(id);
            setLinkedNotes(notes || []);
        } catch (error) {
            console.error('Error fetching linked notes:', error);
            setLinkedNotes([]);
        } finally {
            setLoadingNotes(false);
        }
    }, [id]);

    useEffect(() => {
        if (id) {
            fetchLinkedNotes();
        }
    }, [id, fetchLinkedNotes]);

    // Fetch mixlists for this media item
    const fetchMixlists = useCallback(async () => {
        if (!mediaItem) return;
        try {
            const response = await getAllMixlists();
            const allMixlists = response.data || [];
            const mixlistIds = mediaItem.mixlistIds || [];
            if (mixlistIds.length > 0) {
                const mixlistIdSet = new Set(mixlistIds);
                setCurrentMixlists(allMixlists.filter(m => mixlistIdSet.has(m.id)));
                setAvailableMixlists(allMixlists.filter(m => !mixlistIdSet.has(m.id)));
            } else {
                setCurrentMixlists([]);
                setAvailableMixlists(allMixlists);
            }
        } catch (error) {
            console.error('Error fetching mixlists:', error);
        }
    }, [mediaItem]);

    useEffect(() => {
        if (mediaItem) {
            fetchMixlists();
        }
    }, [mediaItem, fetchMixlists]);

    // Mixlist handlers
    const handleOpenMixlistDialog = () => {
        setAddMixlistDialog(true);
        setMixlistSearchQuery('');
        setSelectedMixlistId(null);
    };

    const handleCloseMixlistDialog = () => {
        setAddMixlistDialog(false);
        setSelectedMixlistId(null);
        setMixlistSearchQuery('');
    };

    const handleAddToMixlist = async () => {
        if (!selectedMixlistId) {
            setSnackbar({ open: true, message: 'Please select a mixlist first', severity: 'warning' });
            return;
        }
        setSavingMixlist(true);
        try {
            await addMediaToMixlist(selectedMixlistId, id);
            const addedMixlist = availableMixlists.find(m => m.id === selectedMixlistId);
            if (addedMixlist) {
                setCurrentMixlists(prev => [...prev, addedMixlist]);
                setAvailableMixlists(prev => prev.filter(m => m.id !== selectedMixlistId));
            }
            setSnackbar({ open: true, message: 'Added to mixlist', severity: 'success' });
            handleCloseMixlistDialog();
        } catch (error) {
            console.error('Error adding to mixlist:', error);
            setSnackbar({ open: true, message: 'Failed to add to mixlist', severity: 'error' });
        } finally {
            setSavingMixlist(false);
        }
    };

    const handleRemoveFromMixlist = async (mixlistId, mixlistName) => {
        setSavingMixlist(true);
        try {
            await removeMediaFromMixlist(mixlistId, id);
            const removedMixlist = currentMixlists.find(m => m.id === mixlistId);
            if (removedMixlist) {
                setAvailableMixlists(prev => [...prev, removedMixlist]);
                setCurrentMixlists(prev => prev.filter(m => m.id !== mixlistId));
            }
            setSnackbar({ open: true, message: `Removed from "${mixlistName}"`, severity: 'success' });
        } catch (error) {
            console.error('Error removing from mixlist:', error);
            setSnackbar({ open: true, message: 'Failed to remove from mixlist', severity: 'error' });
        } finally {
            setSavingMixlist(false);
        }
    };

    const filteredAvailableMixlistsForDialog = availableMixlists.filter(m =>
        (m.name || '').toLowerCase().includes(mixlistSearchQuery.toLowerCase()) ||
        (m.description || '').toLowerCase().includes(mixlistSearchQuery.toLowerCase())
    );

    // Fetch available notes when dialog opens
    const fetchAvailableNotes = useCallback(async () => {
        setLoadingAvailableNotes(true);
        try {
            const notes = await getAllNotes();
            setAvailableNotes(notes || []);
        } catch (error) {
            console.error('Error fetching available notes:', error);
            setAvailableNotes([]);
        } finally {
            setLoadingAvailableNotes(false);
        }
    }, []);

    // Search notes
    const handleNoteSearch = useCallback(async (query) => {
        if (!query || query.length < 2) {
            fetchAvailableNotes();
            return;
        }
        setLoadingAvailableNotes(true);
        try {
            const results = await searchNotes(query);
            const hits = results?.hits?.map(hit => hit.document) || [];
            setAvailableNotes(hits);
        } catch (error) {
            console.error('Error searching notes:', error);
            fetchAvailableNotes();
        } finally {
            setLoadingAvailableNotes(false);
        }
    }, [fetchAvailableNotes]);

    // Open link note dialog
    const handleOpenLinkNoteDialog = () => {
        setLinkNoteDialog(true);
        setNoteSearchQuery('');
        setSelectedNoteId(null);
        setLinkDescription('');
        fetchAvailableNotes();
    };

    // Close link note dialog
    const handleCloseLinkNoteDialog = () => {
        setLinkNoteDialog(false);
        setSelectedNoteId(null);
        setNoteSearchQuery('');
        setLinkDescription('');
    };

    // Link note to media
    const handleLinkNote = async () => {
        if (!selectedNoteId) {
            setSnackbar({ open: true, message: 'Please select a note first', severity: 'warning' });
            return;
        }
        setSavingNote(true);
        try {
            await linkNoteToMedia(selectedNoteId, id, linkDescription || null);
            const selectedNote = availableNotes.find(n => n.id === selectedNoteId);
            setSnackbar({ open: true, message: `Linked note "${selectedNote?.title}"`, severity: 'success' });
            handleCloseLinkNoteDialog();
            fetchLinkedNotes();
        } catch (error) {
            console.error('Error linking note:', error);
            setSnackbar({ open: true, message: 'Failed to link note', severity: 'error' });
        } finally {
            setSavingNote(false);
        }
    };

    // Unlink note from media
    const handleUnlinkNote = async (noteId, noteTitle) => {
        setSavingNote(true);
        try {
            await unlinkNoteFromMedia(noteId, id);
            setSnackbar({ open: true, message: `Unlinked note "${noteTitle}"`, severity: 'success' });
            fetchLinkedNotes();
        } catch (error) {
            console.error('Error unlinking note:', error);
            setSnackbar({ open: true, message: 'Failed to unlink note', severity: 'error' });
        } finally {
            setSavingNote(false);
        }
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
    const handleThumbnailUpload = async (event) => {
        const file = event.target.files[0];
        if (file) {
            setThumbnailFile(file);
            console.log('Thumbnail file selected:', file.name);
            
            try {
                // Upload thumbnail to DigitalOcean Spaces
                console.log('Uploading thumbnail to DigitalOcean Spaces...');
                const response = await uploadThumbnail(file);
                const thumbnailUrl = response.data.url;
                
                // Set the thumbnail URL from the upload response
                handleInputChange('thumbnail', thumbnailUrl);
                console.log('Thumbnail uploaded successfully:', thumbnailUrl);
                
                setSnackbar({ 
                    open: true, 
                    message: 'Thumbnail uploaded successfully!', 
                    severity: 'success' 
                });
            } catch (error) {
                console.error('Error uploading thumbnail:', error);
                setSnackbar({ 
                    open: true, 
                    message: 'Failed to upload thumbnail. Please try again.', 
                    severity: 'error' 
                });
                setThumbnailFile(null);
            }
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setSaving(true);

        try {
            // Prepare update data according to CreateMediaItemDto
            const updateData = {
                title: formData.title,
                mediaType: formData.mediaType,
                status: formData.status,
                rating: formData.rating || null, // Convert empty string to null for enum
                ownershipStatus: formData.ownershipStatus || null, // Convert empty string to null for enum
                link: formData.link || null,
                description: formData.description || null,
                notes: formData.notes || null,
                thumbnail: formData.thumbnail || null,
                genre: formData.genre || null,
                dateCompleted: formData.dateCompleted ? new Date(formData.dateCompleted).toISOString() : null,
                topics: mediaItem?.topics || mediaItem?.topicNames || [],
                genres: mediaItem?.genres || mediaItem?.genreNames || []
            };

            await updateMedia(id, updateData);
            
            setSnackbar({ 
                open: true, 
                message: 'Media item updated successfully!', 
                severity: 'success' 
            });

            // Redirect back to media profile after a short delay
            setTimeout(() => {
                navigate(`/media/${id}`);
            }, 1500);

        } catch (error) {
            console.error('Failed to update media:', error);
            setSnackbar({ 
                open: true, 
                message: error.response?.data?.message || 'Failed to update media item', 
                severity: 'error' 
            });
        } finally {
            setSaving(false);
        }
    };

    const handleCancel = () => {
        navigate(`/media/${id}`);
    };

    const handleDelete = async () => {
        try {
            await deleteMedia(id);
            setSnackbar({ 
                open: true, 
                message: 'Media item deleted successfully!', 
                severity: 'success' 
            });
            
            // Navigate to all media page after a short delay
            setTimeout(() => {
                navigate('/all-media');
            }, 1500);
        } catch (error) {
            console.error('Failed to delete media:', error);
            setSnackbar({ 
                open: true, 
                message: error.response?.data?.error || 'Failed to delete media item', 
                severity: 'error' 
            });
        } finally {
            setDeleteDialogOpen(false);
        }
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
                                            Not part of any mixlists. Click "Add to Mixlist" to assign.
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
                                                                "{note.linkDescription}"
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
                                            No linked notes. Click "Link Note" to connect Obsidian notes.
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
                        Are you sure you want to delete "{formData.title}"? This action cannot be undone.
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
                        Select a note to link to "{formData.title}":
                    </Typography>

                    {/* Search Bar */}
                    <Box sx={{ mb: 2 }}>
                        <TextField
                            fullWidth
                            placeholder="Search notes..."
                            value={noteSearchQuery}
                            onChange={(e) => {
                                setNoteSearchQuery(e.target.value);
                                handleNoteSearch(e.target.value);
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
                                        onClick={() => setSelectedNoteId(note.id)}
                                        sx={{
                                            borderRadius: 1,
                                            mb: 1,
                                            cursor: 'pointer',
                                            backgroundColor: selectedNoteId === note.id
                                                ? 'rgba(25, 118, 210, 0.3)'
                                                : 'transparent',
                                            border: selectedNoteId === note.id
                                                ? '2px solid rgba(25, 118, 210, 0.8)'
                                                : '1px solid rgba(255, 255, 255, 0.1)',
                                            '&:hover': {
                                                backgroundColor: selectedNoteId === note.id
                                                    ? 'rgba(25, 118, 210, 0.4)'
                                                    : 'rgba(255, 255, 255, 0.05)'
                                            }
                                        }}
                                    >
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
                    {selectedNoteId && (
                        <TextField
                            fullWidth
                            placeholder="Optional: Describe how this note relates to this media..."
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
                        disabled={!selectedNoteId || savingNote}
                    >
                        {savingNote ? 'Linking...' : 'Link Note'}
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
                        Select a mixlist to add "{formData.title}" to:
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
                        disabled={!selectedMixlistId || savingMixlist}
                    >
                        {savingMixlist ? 'Adding...' : 'Add'}
                    </Button>
                </DialogActions>
            </Dialog>
        </Container>
    );
}

export default EditMediaForm;
