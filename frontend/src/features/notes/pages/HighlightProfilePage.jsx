import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
    Box, Card, CardContent, Typography, Button, Chip, CircularProgress,
    Grid, Divider, Snackbar, Alert, Dialog, DialogTitle, DialogContent,
    DialogContentText, DialogActions
} from '@mui/material';
import {
    ArrowBack as ArrowBackIcon,
    OpenInNew as OpenInNewIcon,
    FormatQuote as QuoteIcon,
    Article as ArticleIcon,
    Book as BookIcon,
    Delete as DeleteIcon
} from '@mui/icons-material';
import { useHighlight, useDeleteHighlight } from '@/hooks/useHighlight';
import { getMediaTypeColor } from '@/shared/DesignSystem';

function HighlightProfilePage() {
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

    const { id } = useParams();
    const navigate = useNavigate();

    const highlightQuery = useHighlight(id);
    const highlight = highlightQuery.data ?? null;
    const loading = highlightQuery.isLoading;

    const deleteMutation = useDeleteHighlight();
    const deleting = deleteMutation.isPending;

    // Surface fetch failures via snackbar (preserves prior UX).
    useEffect(() => {
        if (highlightQuery.error) {
            setSnackbar({ open: true, message: 'Failed to load highlight', severity: 'error' });
        }
    }, [highlightQuery.error]);

    const handleDelete = () => {
        deleteMutation.mutate(id, {
            onSuccess: () => {
                setSnackbar({ open: true, message: 'Highlight deleted successfully', severity: 'success' });
                setDeleteDialogOpen(false);
                setTimeout(() => navigate(-1), 1000);
            },
            onError: () => {
                setSnackbar({ open: true, message: 'Failed to delete highlight', severity: 'error' });
                setDeleteDialogOpen(false);
            },
        });
    };

    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        try {
            return new Date(dateString).toLocaleDateString('en-US', {
                year: 'numeric', month: 'long', day: 'numeric'
            });
        } catch {
            return 'N/A';
        }
    };

    // Readwise categories are plural ("books"); media type colors are keyed by singular type.
    const getCategoryColor = (category) => getMediaTypeColor(category?.replace(/s$/i, ''));

    // Parse tags - handles both array (from API) and string (legacy) formats
    const parseTags = (tags) => {
        if (!tags) return [];
        // If already an array, just filter empty values
        if (Array.isArray(tags)) {
            return tags.filter(t => t && t.trim());
        }
        // If string (legacy), split by comma
        if (typeof tags === 'string') {
            return tags.split(',').map(t => t.trim()).filter(t => t);
        }
        return [];
    };

    // Check if a GUID is valid (not null, undefined, or empty GUID)
    const isValidGuid = (guid) => {
        if (!guid) return false;
        const emptyGuid = '00000000-0000-0000-0000-000000000000';
        return guid !== emptyGuid;
    };

    if (loading) {
        return (
            <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center', py: 4 }}>
                <Box sx={{ width: '100%', maxWidth: '600px', backgroundColor: 'background.paper', borderRadius: '16px', p: 4, textAlign: 'center' }}>
                    <CircularProgress sx={{ mb: 2 }} />
                    <Typography variant="h6">Loading highlight...</Typography>
                </Box>
            </Box>
        );
    }

    if (!highlight) {
        return (
            <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: 4 }}>
                <Box sx={{ width: '100%', maxWidth: '600px', backgroundColor: 'background.paper', borderRadius: '16px', p: 4, textAlign: 'center' }}>
                    <Typography variant="h6">Highlight not found.</Typography>
                    <Button onClick={() => navigate(-1)} variant="contained" sx={{ mt: 2 }}>Go Back</Button>
                </Box>
            </Box>
        );
    }

    const tags = parseTags(highlight.tags);

    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: { xs: 2, sm: 4 }, px: { xs: 1, sm: 2 } }}>
            <Box sx={{ width: '100%', maxWidth: '900px', backgroundColor: 'background.paper', borderRadius: { xs: '8px', sm: '16px' }, p: { xs: 2, sm: 3, md: 4 }, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>

                {/* Header with back button */}
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                    <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(-1)} sx={{ color: 'white' }}>Back</Button>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Button
                            startIcon={<DeleteIcon />}
                            onClick={() => setDeleteDialogOpen(true)}
                            variant="contained"
                            size="small"
                            sx={{ color: '#fcfafa' }}
                        >
                            Delete
                        </Button>
                    </Box>
                </Box>

                {/* Main Card */}
                <Card sx={{ overflow: 'hidden', borderRadius: 2 }}>
                    <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>

                        {/* Category Badge and Title */}
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3, flexWrap: 'wrap' }}>
                            <QuoteIcon sx={{ fontSize: 32, color: 'white' }} />
                            <Typography variant="h5" sx={{ fontWeight: 'bold', flex: 1 }}>{highlight.title || 'Untitled'}</Typography>
                            {highlight.category && (
                                <Chip label={highlight.category} sx={{ backgroundColor: getCategoryColor(highlight.category), color: 'white', fontWeight: 'bold' }} />
                            )}
                        </Box>

                        {highlight.author && (
                            <Typography variant="subtitle1" color="text.secondary" sx={{ mb: 2 }}>
                                by {highlight.author}
                            </Typography>
                        )}

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* Highlight Text - Quote Style */}
                        <Box sx={{ mb: 3, pl: 3, borderLeft: '4px solid rgba(255,255,255,0.3)', py: 1 }}>
                            <Typography variant="body1" sx={{ fontStyle: 'italic', fontSize: '1.1rem', lineHeight: 1.8, color: 'rgba(255,255,255,0.95)' }}>
                                &quot;{highlight.text}&quot;
                            </Typography>
                        </Box>

                        {/* Note/Annotation */}
                        {highlight.note && (
                            <Box sx={{ mb: 3, p: 2, backgroundColor: 'rgba(255,255,255,0.05)', borderRadius: 1 }}>
                                <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1 }}>Note</Typography>
                                <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>{highlight.note}</Typography>
                            </Box>
                        )}

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* Tags */}
                        {tags.length > 0 && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>Tags</Typography>
                                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                    {tags.map((tag) => (
                                        <Chip key={`tag-${tag}`} label={tag} size="small" sx={{ backgroundColor: 'rgba(255,255,255,0.1)', color: 'rgba(255,255,255,0.8)' }} />
                                    ))}
                                </Box>
                            </Box>
                        )}

                        {/* Metadata */}
                        <Box sx={{ mb: 3 }}>
                            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>Details</Typography>
                            <Grid container spacing={2}>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">Highlighted</Typography>
                                    <Typography variant="body1">{formatDate(highlight.highlightedAt)}</Typography>
                                </Grid>
                                {highlight.location && (
                                    <Grid item xs={12} sm={6}>
                                        <Typography variant="body2" color="text.secondary">
                                            Location ({highlight.locationType || 'position'})
                                        </Typography>
                                        <Typography variant="body1">{highlight.location}</Typography>
                                    </Grid>
                                )}
                                {highlight.color && (
                                    <Grid item xs={12} sm={6}>
                                        <Typography variant="body2" color="text.secondary">Color</Typography>
                                        <Chip label={highlight.color} size="small" sx={{ backgroundColor: highlight.color, color: 'white' }} />
                                    </Grid>
                                )}
                            </Grid>
                        </Box>

                        <Divider sx={{ mb: 3, borderColor: 'rgba(255, 255, 255, 0.1)' }} />

                        {/* Link to Source Article/Book */}
                        {(isValidGuid(highlight.articleId) || isValidGuid(highlight.bookId)) && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 2 }}>Source</Typography>
                                {isValidGuid(highlight.articleId) && (
                                    <Card
                                        component={RouterLink}
                                        to={`/media/${highlight.articleId}`}
                                        sx={{
                                            display: 'flex',
                                            alignItems: 'center',
                                            p: 2,
                                            textDecoration: 'none',
                                            backgroundColor: 'rgba(255,255,255,0.05)',
                                            '&:hover': { backgroundColor: 'rgba(255,255,255,0.08)' }
                                        }}
                                    >
                                        <ArticleIcon sx={{ mr: 2, color: getMediaTypeColor('article') }} />
                                        <Typography sx={{ color: 'white' }}>{highlight.articleTitle || highlight.title || 'View Article'}</Typography>
                                    </Card>
                                )}
                                {isValidGuid(highlight.bookId) && (
                                    <Card
                                        component={RouterLink}
                                        to={`/media/${highlight.bookId}`}
                                        sx={{
                                            display: 'flex',
                                            alignItems: 'center',
                                            p: 2,
                                            textDecoration: 'none',
                                            backgroundColor: 'rgba(255,255,255,0.05)',
                                            '&:hover': { backgroundColor: 'rgba(255,255,255,0.08)' }
                                        }}
                                    >
                                        <BookIcon sx={{ mr: 2, color: getMediaTypeColor('book') }} />
                                        <Typography sx={{ color: 'white' }}>{highlight.bookTitle || highlight.title || 'View Book'}</Typography>
                                    </Card>
                                )}
                            </Box>
                        )}

                        {/* External Source Link */}
                        {highlight.sourceUrl && (
                            <Box>
                                <Button
                                    variant="contained"
                                    startIcon={<OpenInNewIcon />}
                                    href={highlight.sourceUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                >
                                    View Original Source
                                </Button>
                            </Box>
                        )}
                    </CardContent>
                </Card>
            </Box>

            <Dialog open={deleteDialogOpen} onClose={() => !deleting && setDeleteDialogOpen(false)}>
                <DialogTitle>Delete Highlight</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Are you sure you want to delete this highlight? This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleting}>Cancel</Button>
                    <Button onClick={handleDelete} color="error" variant="contained" disabled={deleting}>
                        {deleting ? 'Deleting...' : 'Delete'}
                    </Button>
                </DialogActions>
            </Dialog>

            <Snackbar open={snackbar.open} autoHideDuration={4000} onClose={() => setSnackbar({ ...snackbar, open: false })}>
                <Alert onClose={() => setSnackbar({ ...snackbar, open: false })} severity={snackbar.severity}>{snackbar.message}</Alert>
            </Snackbar>
        </Box>
    );
}

export default HighlightProfilePage;
