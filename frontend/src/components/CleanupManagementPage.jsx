import React, { useState } from 'react';
import {
    Container,
    Typography,
    Card,
    CardContent,
    Button,
    Grid,
    Alert,
    Snackbar,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    Box,
    Chip
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import WarningIcon from '@mui/icons-material/Warning';
import {
    useCleanupYouTubeData, useCleanupPodcasts, useCleanupBooks, useCleanupMovies,
    useCleanupTvShows, useCleanupArticles, useCleanupHighlights, useCleanupMixlists,
    useCleanupAllTopics, useCleanupAllGenres, useCleanupOrphanedTopics, useCleanupOrphanedGenres,
    useCleanupAllMedia, useCleanupWebsites, useCleanupChannels, useCleanupPlaylists,
    useCleanupNotes, useCleanupDocuments, useCleanupVideos, useCleanupRefreshTokens
} from '../hooks/useDev';

const CleanupManagementPage = () => {
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [confirmDialog, setConfirmDialog] = useState({ open: false, action: null, title: '', description: '' });

    const cleanupYouTubeData = useCleanupYouTubeData();
    const cleanupPodcasts = useCleanupPodcasts();
    const cleanupBooks = useCleanupBooks();
    const cleanupMovies = useCleanupMovies();
    const cleanupTvShows = useCleanupTvShows();
    const cleanupArticles = useCleanupArticles();
    const cleanupHighlights = useCleanupHighlights();
    const cleanupMixlists = useCleanupMixlists();
    const cleanupAllTopics = useCleanupAllTopics();
    const cleanupAllGenres = useCleanupAllGenres();
    const cleanupOrphanedTopics = useCleanupOrphanedTopics();
    const cleanupOrphanedGenres = useCleanupOrphanedGenres();
    const cleanupAllMedia = useCleanupAllMedia();
    const cleanupWebsites = useCleanupWebsites();
    const cleanupChannels = useCleanupChannels();
    const cleanupPlaylists = useCleanupPlaylists();
    const cleanupNotes = useCleanupNotes();
    const cleanupDocuments = useCleanupDocuments();
    const cleanupVideos = useCleanupVideos();
    const cleanupRefreshTokens = useCleanupRefreshTokens();

    const allMutations = [
        cleanupYouTubeData, cleanupPodcasts, cleanupBooks, cleanupMovies,
        cleanupTvShows, cleanupArticles, cleanupHighlights, cleanupMixlists,
        cleanupAllTopics, cleanupAllGenres, cleanupOrphanedTopics, cleanupOrphanedGenres,
        cleanupAllMedia, cleanupWebsites, cleanupChannels, cleanupPlaylists,
        cleanupNotes, cleanupDocuments, cleanupVideos, cleanupRefreshTokens,
    ];
    const loading = allMutations.some(m => m.isPending);

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const handleCloseDialog = () => {
        setConfirmDialog({ open: false, action: null, title: '', description: '' });
    };

    const executeCleanup = async (mutation, successMessage) => {
        try {
            const result = await mutation.mutateAsync();
            const deletedInfo = result?.deleted
                ? `\n${Object.entries(result.deleted).map(([key, value]) => `${key}: ${value}`).join(', ')}`
                : '';
            setSnackbar({
                open: true,
                message: successMessage + deletedInfo,
                severity: 'success'
            });
        } catch (error) {
            console.error('Cleanup error:', error);
            setSnackbar({
                open: true,
                message: `Cleanup failed: ${error.message || 'Unknown error'}`,
                severity: 'error'
            });
        } finally {
            handleCloseDialog();
        }
    };

    const confirmCleanup = (action, title, description) => {
        setConfirmDialog({
            open: true,
            action,
            title,
            description
        });
    };

    const cleanupActions = [
        {
            title: 'YouTube Data',
            description: 'Delete all YouTube channels, playlists, and videos',
            action: () => executeCleanup(cleanupYouTubeData, 'YouTube data deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Podcasts',
            description: 'Delete all podcast series and episodes',
            action: () => executeCleanup(cleanupPodcasts, 'Podcasts deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Books',
            description: 'Delete all books (highlights will be unlinked)',
            action: () => executeCleanup(cleanupBooks, 'Books deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Movies',
            description: 'Delete all movies',
            action: () => executeCleanup(cleanupMovies, 'Movies deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'TV Shows',
            description: 'Delete all TV shows',
            action: () => executeCleanup(cleanupTvShows, 'TV shows deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Articles',
            description: 'Delete all articles (highlights will be unlinked)',
            action: () => executeCleanup(cleanupArticles, 'Articles deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Websites',
            description: 'Delete all websites',
            action: () => executeCleanup(cleanupWebsites, 'Websites deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Channels',
            description: 'Delete all YouTube channels',
            action: () => executeCleanup(cleanupChannels, 'Channels deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Playlists',
            description: 'Delete all YouTube playlists and playlist-video associations',
            action: () => executeCleanup(cleanupPlaylists, 'Playlists deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Videos',
            description: 'Delete all videos (standalone, does not affect channels or playlists)',
            action: () => executeCleanup(cleanupVideos, 'Videos deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Notes',
            description: 'Delete all notes and note-media links',
            action: () => executeCleanup(cleanupNotes, 'Notes deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Documents',
            description: 'Delete all documents',
            action: () => executeCleanup(cleanupDocuments, 'Documents deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Highlights',
            description: 'Delete all highlights from all sources',
            action: () => executeCleanup(cleanupHighlights, 'Highlights deleted successfully'),
            severity: 'medium',
            color: 'error'
        },
        {
            title: 'Mixlists',
            description: 'Delete all mixlists (media items will remain)',
            action: () => executeCleanup(cleanupMixlists, 'Mixlists deleted successfully'),
            severity: 'low',
            color: 'warning'
        },
        {
            title: 'All Topics',
            description: 'Delete ALL topics (media items remain, lose associations)',
            action: () => executeCleanup(cleanupAllTopics, 'All topics deleted successfully'),
            severity: 'medium',
            color: 'warning'
        },
        {
            title: 'All Genres',
            description: 'Delete ALL genres (media items remain, lose associations)',
            action: () => executeCleanup(cleanupAllGenres, 'All genres deleted successfully'),
            severity: 'medium',
            color: 'warning'
        },
        {
            title: 'Orphaned Topics',
            description: 'Delete topics not linked to any media items',
            action: () => executeCleanup(cleanupOrphanedTopics, 'Orphaned topics deleted successfully'),
            severity: 'low',
            color: 'info'
        },
        {
            title: 'Orphaned Genres',
            description: 'Delete genres not linked to any media items',
            action: () => executeCleanup(cleanupOrphanedGenres, 'Orphaned genres deleted successfully'),
            severity: 'low',
            color: 'info'
        },
        {
            title: 'Expired/Revoked Refresh Tokens',
            description: 'Delete expired and revoked authentication refresh tokens',
            action: () => executeCleanup(cleanupRefreshTokens, 'Refresh tokens cleaned up successfully'),
            severity: 'low',
            color: 'info'
        }
    ];

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Box sx={{ mb: 4 }}>
                <Typography variant="h3" component="h1" gutterBottom>
                    Database Cleanup Management
                </Typography>
                <Typography variant="body1" color="text.secondary" paragraph>
                    Use these tools to clean up test data from your database. All deletions are permanent and cannot be undone.
                </Typography>
                <Alert severity="warning" sx={{ mb: 2 }}>
                    <strong>Warning:</strong> These operations permanently delete data from your database. Use with caution, especially in production environments.
                </Alert>
            </Box>

            <Grid container spacing={3}>
                {cleanupActions.map((item) => (
                    <Grid item xs={12} sm={6} md={4} key={item.title}>
                        <Card
                            sx={{
                                height: '100%',
                                display: 'flex',
                                flexDirection: 'column',
                                borderLeft: 4,
                                borderColor: `${item.color}.main`,
                                '&:hover': { boxShadow: 'none' },
                                transition: 'none'
                            }}
                        >
                            <CardContent sx={{ flexGrow: 1 }}>
                                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 1 }}>
                                    <Typography variant="h6" component="h2">
                                        {item.title}
                                    </Typography>
                                    <Chip 
                                        label={item.severity} 
                                        size="small" 
                                        color={item.color}
                                    />
                                </Box>
                                <Typography variant="body2" color="text.secondary" paragraph>
                                    {item.description}
                                </Typography>
                                <Button
                                    variant="contained"
                                    color={item.color}
                                    startIcon={<DeleteIcon />}
                                    fullWidth
                                    disabled={loading}
                                    onClick={() => confirmCleanup(
                                        item.action,
                                        `Delete ${item.title}?`,
                                        `Are you sure you want to delete all ${item.title.toLowerCase()}? This action cannot be undone.`
                                    )}
                                    sx={{ mt: 'auto', color: 'white' }}
                                >
                                    Delete All
                                </Button>
                            </CardContent>
                        </Card>
                    </Grid>
                ))}

                {/* Nuclear Option */}
                <Grid item xs={12}>
                    <Card 
                        sx={{ 
                            borderLeft: 4,
                            borderColor: 'error.dark',
                            bgcolor: 'error.light',
                            color: 'error.contrastText'
                        }}
                    >
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                                <WarningIcon sx={{ fontSize: 40, mr: 2 }} />
                                <Box>
                                    <Typography variant="h5" component="h2" gutterBottom>
                                        Nuclear Option: Delete All Media
                                    </Typography>
                                    <Typography variant="body1">
                                        Delete ALL media items, playlists, channels, mixlists, and highlights from the entire database.
                                        Topics and Genres will be preserved but may become orphaned.
                                    </Typography>
                                </Box>
                            </Box>
                            <Alert severity="error" sx={{ mb: 2, bgcolor: 'white' }}>
                                <strong>EXTREME CAUTION:</strong> This will delete everything except Topics and Genres. 
                                This is useful for resetting your database after testing but before importing real data.
                            </Alert>
                            <Button
                                variant="contained"
                                color="error"
                                size="large"
                                startIcon={<DeleteIcon />}
                                disabled={loading}
                                onClick={() => confirmCleanup(
                                    () => executeCleanup(cleanupAllMedia, 'All media deleted successfully (NUCLEAR OPTION)'),
                                    'DELETE EVERYTHING?',
                                    'This will DELETE ALL media items, channels, playlists, mixlists, and highlights from your entire database. Only Topics and Genres will remain. This action CANNOT be undone. Are you absolutely sure?'
                                )}
                            >
                                DELETE EVERYTHING (NUCLEAR OPTION)
                            </Button>
                        </CardContent>
                    </Card>
                </Grid>
            </Grid>

            {/* Confirmation Dialog */}
            <Dialog
                open={confirmDialog.open}
                onClose={handleCloseDialog}
                aria-labelledby="alert-dialog-title"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title">
                    {confirmDialog.title}
                </DialogTitle>
                <DialogContent>
                    <DialogContentText id="alert-dialog-description">
                        {confirmDialog.description}
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    {/* eslint-disable-next-line jsx-a11y/no-autofocus -- focuses Cancel on confirm-dialog open */}
                    <Button onClick={handleCloseDialog} color="primary" autoFocus>
                        Cancel
                    </Button>
                    <Button 
                        onClick={confirmDialog.action} 
                        color="error" 
                        variant="contained"
                        disabled={loading}
                    >
                        Confirm Delete
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Snackbar for notifications */}
            <Snackbar
                open={snackbar.open}
                autoHideDuration={6000}
                onClose={handleCloseSnackbar}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} sx={{ width: '100%' }}>
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Container>
    );
};

export default CleanupManagementPage;

