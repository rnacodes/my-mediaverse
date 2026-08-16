import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
    Box, Card, CardContent, Typography, Button, Chip, CircularProgress,
    IconButton, Tooltip, Grid, Divider, Snackbar, Alert, TextField
} from '@mui/material';
import {
    ArrowBack as ArrowBackIcon,
    OpenInNew as OpenInNewIcon,
    Edit as EditIcon,
    Save as SaveIcon,
    Close as CloseIcon,
    Article as NoteIcon,
    AutoAwesome as AutoAwesomeIcon
} from '@mui/icons-material';
import { useNote, useMediaForNote, useUpdateNote } from '@/hooks/useNote';
import { useGenerateNoteDescription } from '@/hooks/useAi';
import { formatMediaType } from '@/utils/formatters';
import { getMediaTypeColor } from '@/shared/DesignSystem';
import { resolveMediaImage, getPlaceholderImage } from '@/utils/mediaImageUtils';
import SimilarNotesSection from '../SimilarNotesSection';
import RelatedMediaByEmbeddingSection from '../RelatedMediaByEmbeddingSection';

function NoteProfilePage() {
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [editingDescription, setEditingDescription] = useState(false);
    const [editedDescription, setEditedDescription] = useState('');

    const { id } = useParams();
    const navigate = useNavigate();

    const noteQuery = useNote(id);
    const note = noteQuery.data ?? null;
    const loading = noteQuery.isLoading;

    const linkedMediaQuery = useMediaForNote(id);
    const linkedMedia = linkedMediaQuery.data ?? [];

    const updateMutation = useUpdateNote();
    const saving = updateMutation.isPending;

    const generateMutation = useGenerateNoteDescription();
    const generatingDescription = generateMutation.isPending;

    // Seed the edit field once the note has loaded.
    useEffect(() => {
        if (note) setEditedDescription(note.description ?? '');
    }, [note]);

    // Surface fetch failures via snackbar.
    useEffect(() => {
        if (noteQuery.error) {
            setSnackbar({ open: true, message: 'Failed to load note', severity: 'error' });
        }
    }, [noteQuery.error]);

    // Get vault color
    const getVaultColor = (vaultName) => {
        switch (vaultName?.toLowerCase()) {
            case 'general':
                return '#4caf50'; // Green
            case 'programming':
                return '#2196f3'; // Blue
            default:
                return '#9e9e9e'; // Gray
        }
    };

    // Format date for display
    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        try {
            return new Date(dateString).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });
        } catch {
            return 'N/A';
        }
    };

    const handleSaveDescription = () => {
        updateMutation.mutate({ id, noteData: { description: editedDescription } }, {
            onSuccess: () => {
                setEditingDescription(false);
                setSnackbar({ open: true, message: 'Description updated', severity: 'success' });
            },
            onError: () => setSnackbar({ open: true, message: 'Failed to update description', severity: 'error' }),
        });
    };

    const handleCancelEdit = () => {
        setEditedDescription(note?.description ?? '');
        setEditingDescription(false);
    };

    const handleGenerateDescription = () => {
        generateMutation.mutate(id, {
            onSuccess: (result) => {
                if (result?.generatedDescription) {
                    setEditedDescription(result.generatedDescription);
                    setSnackbar({ open: true, message: 'Description generated successfully', severity: 'success' });
                } else if (result?.success === false) {
                    setSnackbar({ open: true, message: result.errorMessage || 'Failed to generate description', severity: 'error' });
                } else {
                    setSnackbar({ open: true, message: 'No description was generated', severity: 'warning' });
                }
            },
            onError: (error) => {
                let errorMessage = 'Failed to generate description';
                if (error.response?.status === 503) {
                    errorMessage = 'AI service is not configured or unavailable';
                } else if (error.response?.status === 404) {
                    errorMessage = error.response?.data?.message || 'Note not found or has no content';
                } else if (error.response?.data?.message) {
                    errorMessage = error.response.data.message;
                }
                setSnackbar({ open: true, message: errorMessage, severity: 'error' });
            },
        });
    };

    if (loading) {
        return (
            <Box sx={{
                minHeight: '100vh',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                py: 4,
                px: 2
            }}>
                <Box sx={{
                    width: '100%',
                    maxWidth: '600px',
                    backgroundColor: 'background.paper',
                    borderRadius: '16px',
                    p: 4,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                    textAlign: 'center'
                }}>
                    <CircularProgress sx={{ mb: 2 }} />
                    <Typography variant="h6">Loading note...</Typography>
                </Box>
            </Box>
        );
    }

    if (!note) {
        return (
            <Box sx={{
                minHeight: '100vh',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'flex-start',
                py: 4,
                px: 2
            }}>
                <Box sx={{
                    width: '100%',
                    maxWidth: '600px',
                    backgroundColor: 'background.paper',
                    borderRadius: '16px',
                    p: 4,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                    textAlign: 'center'
                }}>
                    <Typography variant="h6">Note not found.</Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        The note you&apos;re looking for doesn&apos;t exist or couldn&apos;t be loaded.
                    </Typography>
                    <Button
                        onClick={() => navigate(-1)}
                        variant="contained"
                        sx={{ mt: 2 }}
                    >
                        Go Back
                    </Button>
                </Box>
            </Box>
        );
    }

    return (
        <Box sx={{
            minHeight: '100vh',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'flex-start',
            py: { xs: 2, sm: 4 },
            px: { xs: 1, sm: 2 }
        }}>
            <Box sx={{
                width: '100%',
                maxWidth: '900px',
                backgroundColor: 'background.paper',
                borderRadius: { xs: '8px', sm: '16px' },
                p: { xs: 2, sm: 3, md: 4 },
                boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
            }}>
                {/* Header with back button */}
                <Box sx={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    mb: 3
                }}>
                    <Button
                        startIcon={<ArrowBackIcon />}
                        onClick={() => navigate(-1)}
                        sx={{ color: 'white' }}
                    >
                        Back
                    </Button>
                </Box>

                {/* Main Card */}
                <Card sx={{ overflow: 'hidden', borderRadius: 2 }}>
                    <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
                        {/* Title and Vault Badge */}
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3, flexWrap: 'wrap' }}>
                            <NoteIcon sx={{ fontSize: 32, color: 'white' }} />
                            <Typography variant="h4" sx={{ fontWeight: 'bold', flex: 1 }}>
                                {note.title}
                            </Typography>
                            <Chip
                                label={note.vaultName}
                                sx={{
                                    backgroundColor: getVaultColor(note.vaultName),
                                    color: 'white',
                                    fontWeight: 'bold',
                                    fontSize: '0.85rem'
                                }}
                            />
                        </Box>

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* Description Section */}
                        <Box sx={{ mb: 3 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                                <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                                    Description
                                </Typography>
                                {!editingDescription && (
                                    <Box sx={{ display: 'flex', gap: 0.5 }}>
                                        <Tooltip title="Generate AI description">
                                            <IconButton
                                                onClick={handleGenerateDescription}
                                                size="small"
                                                disabled={generatingDescription}
                                                sx={{ color: 'rgba(255, 255, 255, 0.7)' }}
                                            >
                                                {generatingDescription ? (
                                                    <CircularProgress size={18} sx={{ color: 'rgba(255, 255, 255, 0.7)' }} />
                                                ) : (
                                                    <AutoAwesomeIcon fontSize="small" />
                                                )}
                                            </IconButton>
                                        </Tooltip>
                                        <Tooltip title="Edit description">
                                            <IconButton
                                                onClick={() => setEditingDescription(true)}
                                                size="small"
                                                sx={{ color: 'rgba(255, 255, 255, 0.7)' }}
                                            >
                                                <EditIcon fontSize="small" />
                                            </IconButton>
                                        </Tooltip>
                                    </Box>
                                )}
                            </Box>

                            {editingDescription ? (
                                <Box>
                                    <TextField
                                        fullWidth
                                        multiline
                                        rows={4}
                                        value={editedDescription}
                                        onChange={(e) => setEditedDescription(e.target.value)}
                                        placeholder="Enter a description for this note..."
                                        variant="outlined"
                                        sx={{
                                            mb: 1,
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
                                    <Box sx={{ display: 'flex', gap: 1 }}>
                                        <Button
                                            variant="contained"
                                            size="small"
                                            startIcon={<SaveIcon />}
                                            onClick={handleSaveDescription}
                                            disabled={saving}
                                        >
                                            {saving ? 'Saving...' : 'Save'}
                                        </Button>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            startIcon={<CloseIcon />}
                                            onClick={handleCancelEdit}
                                            disabled={saving}
                                            sx={{
                                                borderColor: 'rgba(255, 255, 255, 0.3)',
                                                color: 'white'
                                            }}
                                        >
                                            Cancel
                                        </Button>
                                    </Box>
                                </Box>
                            ) : (
                                <Typography
                                    variant="body1"
                                    sx={{
                                        color: note.description ? 'rgba(255, 255, 255, 0.9)' : 'rgba(255, 255, 255, 0.5)',
                                        fontStyle: note.description ? 'normal' : 'italic',
                                        lineHeight: 1.7
                                    }}
                                >
                                    {note.description || 'No description yet. Click the edit button to add one.'}
                                </Typography>
                            )}
                        </Box>

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* Tags Section */}
                        {note.tags && note.tags.length > 0 && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
                                    Tags
                                </Typography>
                                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                    {note.tags.map((tag) => (
                                        <Chip
                                            key={`tag-${tag}`}
                                            label={tag}
                                            size="small"
                                            sx={{
                                                backgroundColor: 'rgba(255, 255, 255, 0.1)',
                                                color: 'rgba(255, 255, 255, 0.8)',
                                                fontSize: '0.8rem'
                                            }}
                                        />
                                    ))}
                                </Box>
                            </Box>
                        )}

                        {/* Metadata Section */}
                        <Box sx={{ mb: 3 }}>
                            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
                                Details
                            </Typography>
                            <Grid container spacing={2}>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Note Date
                                    </Typography>
                                    <Typography variant="body1">
                                        {formatDate(note.noteDate)}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Imported
                                    </Typography>
                                    <Typography variant="body1">
                                        {formatDate(note.dateImported)}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Last Synced
                                    </Typography>
                                    <Typography variant="body1">
                                        {formatDate(note.lastSyncedAt)}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Slug
                                    </Typography>
                                    <Typography variant="body1" sx={{ fontFamily: 'monospace', fontSize: '0.9rem' }}>
                                        {note.slug}
                                    </Typography>
                                </Grid>
                            </Grid>
                        </Box>

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* View in Quartz Button */}
                        {note.sourceUrl && (
                            <Box sx={{ mb: 3 }}>
                                <Button
                                    variant="contained"
                                    startIcon={<OpenInNewIcon />}
                                    href={note.sourceUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    sx={{
                                        backgroundColor: getVaultColor(note.vaultName),
                                        '&:hover': {
                                            backgroundColor: getVaultColor(note.vaultName),
                                            filter: 'brightness(1.1)'
                                        }
                                    }}
                                >
                                    View Full Note in Quartz
                                </Button>
                            </Box>
                        )}

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* Linked Media Items Section */}
                        <Box>
                            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 2 }}>
                                Linked Media Items ({linkedMedia.length})
                            </Typography>

                            {linkedMedia.length > 0 ? (
                                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                                    {linkedMedia.map((item) => (
                                        <Card
                                            key={item.id}
                                            component={RouterLink}
                                            to={`/media/${item.id}`}
                                            sx={{
                                                display: 'flex',
                                                backgroundColor: 'rgba(255, 255, 255, 0.05)',
                                                border: '1px solid rgba(255, 255, 255, 0.1)',
                                                borderRadius: 2,
                                                overflow: 'hidden',
                                                textDecoration: 'none',
                                                color: 'inherit',
                                                transition: 'all 0.2s ease-in-out',
                                                '&:hover': {
                                                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                                                    transform: 'translateY(-2px)',
                                                    boxShadow: '0 4px 12px rgba(0,0,0,0.2)'
                                                }
                                            }}
                                        >
                                            {/* Thumbnail */}
                                            <Box
                                                component="img"
                                                src={resolveMediaImage(item)}
                                                alt={item.title}
                                                onError={(e) => { e.target.onerror = null; e.target.src = getPlaceholderImage(item.mediaType); }}
                                                sx={{
                                                    width: { xs: 80, sm: 120 },
                                                    height: { xs: 80, sm: 120 },
                                                    objectFit: 'cover',
                                                    flexShrink: 0
                                                }}
                                            />

                                            {/* Content */}
                                            <Box sx={{ p: 2, flex: 1, minWidth: 0 }}>
                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                                                    <Chip
                                                        label={formatMediaType(item.mediaType)}
                                                        size="small"
                                                        sx={{
                                                            backgroundColor: getMediaTypeColor(item.mediaType),
                                                            color: 'white',
                                                            fontWeight: 'bold',
                                                            fontSize: '0.65rem',
                                                            height: '20px'
                                                        }}
                                                    />
                                                </Box>
                                                <Typography
                                                    variant="subtitle1"
                                                    sx={{
                                                        fontWeight: 'bold',
                                                        overflow: 'hidden',
                                                        textOverflow: 'ellipsis',
                                                        whiteSpace: 'nowrap'
                                                    }}
                                                >
                                                    {item.title}
                                                </Typography>
                                                {item.linkDescription && (
                                                    <Typography
                                                        variant="body2"
                                                        sx={{
                                                            color: 'rgba(255, 255, 255, 0.6)',
                                                            fontStyle: 'italic',
                                                            mt: 0.5,
                                                            overflow: 'hidden',
                                                            textOverflow: 'ellipsis',
                                                            whiteSpace: 'nowrap'
                                                        }}
                                                    >
                                                        &quot;{item.linkDescription}&quot;
                                                    </Typography>
                                                )}
                                            </Box>
                                        </Card>
                                    ))}
                                </Box>
                            ) : (
                                <Typography
                                    variant="body1"
                                    color="text.secondary"
                                    sx={{ fontStyle: 'italic' }}
                                >
                                    This note isn&apos;t linked to any media items yet. Link it from a media item&apos;s profile page.
                                </Typography>
                            )}
                        </Box>
                    </CardContent>
                </Card>

                {/* AI Recommendation Sections */}
                <Box sx={{ mt: 3 }}>
                    <SimilarNotesSection
                        note={note}
                        setSnackbar={setSnackbar}
                    />

                    <RelatedMediaByEmbeddingSection
                        note={note}
                        setSnackbar={setSnackbar}
                    />
                </Box>
            </Box>

            {/* Snackbar for notifications */}
            <Snackbar
                open={snackbar.open}
                autoHideDuration={4000}
                onClose={() => setSnackbar({ ...snackbar, open: false })}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert
                    onClose={() => setSnackbar({ ...snackbar, open: false })}
                    severity={snackbar.severity}
                    sx={{ width: '100%' }}
                >
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Box>
    );
}

export default NoteProfilePage;
