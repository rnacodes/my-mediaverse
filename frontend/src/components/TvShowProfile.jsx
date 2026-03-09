import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent,
    Chip, Divider, IconButton, CircularProgress, Alert,
    Accordion, AccordionSummary, AccordionDetails, List, ListItemButton,
    Dialog, DialogTitle, DialogContent, DialogActions, Snackbar,
    LinearProgress
} from '@mui/material';
import {
    ArrowBack, Edit, Delete, ExpandMore, Visibility
} from '@mui/icons-material';
import MediaInfoCard from './MediaInfoCard';
import MediaDetailAccordion from './MediaDetailAccordion';
import MixlistCarousel from './MixlistCarousel';
import TopicsGenresSection from './TopicsGenresSection';
import RelatedNotesSection from './RelatedNotesSection';
import SavedRelatedMediaSection from './SavedRelatedMediaSection';
import SimilarItemsSection from './SimilarItemsSection';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { getTvShowById, getEpisodesByShowId, deleteTvShow } from '../api/tvShowService';
import { getAllMixlists } from '../api/mixlistService';
import {
    formatMediaType,
    formatStatus,
    getMediaTypeColor,
    getStatusColor,
    getRatingIcon,
    getRatingText
} from '../utils/formatters';

function TvShowProfile() {
    const [show, setShow] = useState(null);
    const [episodes, setEpisodes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [deleteConfirmDialog, setDeleteConfirmDialog] = useState(false);
    const [refreshKey, setRefreshKey] = useState(0);
    const [relatedMediaRefreshTrigger, setRelatedMediaRefreshTrigger] = useState(0);

    const { id } = useParams();
    const navigate = useNavigate();
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

    useEffect(() => {
        fetchShowData();
        fetchMixlists();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id, refreshKey]);

    useEffect(() => {
        const fetchCurrentMixlists = async () => {
            if (!show) return;
            const mixlistIds = show.mixlistIds || [];
            if (mixlistIds.length > 0) {
                const allMixlistsResponse = await getAllMixlists();
                const allMixlists = allMixlistsResponse.data || [];
                const showMixlists = mixlistIds.map(mixlistId =>
                    allMixlists.find(m => m.id === mixlistId)
                ).filter(m => m !== undefined);
                setCurrentMixlists(showMixlists);
            } else {
                setCurrentMixlists([]);
            }
        };
        fetchCurrentMixlists();
    }, [show]);

    const fetchShowData = async () => {
        try {
            setLoading(true);
            const [showResponse, episodesResponse] = await Promise.all([
                getTvShowById(id),
                getEpisodesByShowId(id)
            ]);

            setShow(showResponse.data);
            setEpisodes(episodesResponse.data || []);
            setLoading(false);
        } catch (error) {
            console.error('Error fetching TV show:', error);
            setSnackbar({ open: true, message: `Failed to load TV show: ${error.response?.data?.message || error.message}`, severity: 'error' });
            setLoading(false);
        }
    };

    const fetchMixlists = async () => {
        try {
            const response = await getAllMixlists();
            setAvailableMixlists(response.data || []);
        } catch (error) {
            console.error('Error fetching mixlists:', error);
        }
    };

    const handleDelete = async () => {
        try {
            await deleteTvShow(id);
            setSnackbar({ open: true, message: 'TV show deleted', severity: 'success' });
            setTimeout(() => navigate('/all-media?mediaType=TVShow'), 1500);
        } catch (error) {
            setSnackbar({ open: true, message: 'Failed to delete TV show', severity: 'error' });
        }
        setDeleteConfirmDialog(false);
    };

    // Group episodes by season, sorted by season desc then episode desc
    const groupedEpisodes = React.useMemo(() => {
        if (!episodes.length) return [];

        const grouped = {};
        episodes.forEach(ep => {
            const season = ep.seasonNumber ?? 0;
            if (!grouped[season]) grouped[season] = [];
            grouped[season].push(ep);
        });

        // Sort episodes within each season by episode number descending
        Object.keys(grouped).forEach(season => {
            grouped[season].sort((a, b) => (b.episodeNumber || 0) - (a.episodeNumber || 0));
        });

        // Return seasons sorted descending
        return Object.keys(grouped)
            .map(Number)
            .sort((a, b) => b - a)
            .map(season => ({
                season,
                episodes: grouped[season]
            }));
    }, [episodes]);

    // Calculate watch progress
    const watchProgress = React.useMemo(() => {
        if (!episodes.length) return null;
        const watched = episodes.filter(ep => ep.status === 'Completed').length;
        const total = episodes.length;
        const percentage = Math.round((watched / total) * 100);
        return { watched, total, percentage };
    }, [episodes]);

    if (loading) return <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh"><CircularProgress /></Box>;
    if (!show) return <Box p={3}><Alert severity="error">TV show not found</Alert></Box>;

    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: { xs: 2, sm: 4 }, px: { xs: 1, sm: 2 } }}>
            <Box sx={{ width: '100%', maxWidth: '900px', backgroundColor: 'background.paper', borderRadius: { xs: '8px', sm: '16px' }, p: { xs: 2, sm: 3, md: 4 }, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>
                {/* Header */}
                <Box display="flex" alignItems="center" mb={3}>
                    <IconButton onClick={() => navigate('/all-media?mediaType=TVShow')} sx={{ mr: 2 }}><ArrowBack /></IconButton>
                    <Typography variant="h4" sx={{ flexGrow: 1 }}>{show.title}</Typography>
                    <IconButton onClick={() => navigate(`/media/${id}/edit`)}><Edit /></IconButton>
                </Box>

                {/* Profile Card */}
                <Card sx={{ borderRadius: 2, mb: 3 }}>
                    <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
                        <MediaInfoCard
                            mediaItem={show}
                            formatMediaType={formatMediaType}
                            formatStatus={formatStatus}
                            getMediaTypeColor={getMediaTypeColor}
                            getStatusColor={getStatusColor}
                            getRatingIcon={getRatingIcon}
                            getRatingText={getRatingText}
                        />

                        <Divider sx={{ my: 3 }} />
                        <MediaDetailAccordion
                            mediaItem={show}
                            navigate={navigate}
                            onBookEnriched={() => setRefreshKey(k => k + 1)}
                        />
                        <TopicsGenresSection
                            mediaItem={show}
                            setSnackbar={setSnackbar}
                            onUpdate={() => setRefreshKey(k => k + 1)}
                        />

                        <RelatedNotesSection
                            mediaItem={show}
                            setSnackbar={setSnackbar}
                            onUpdate={() => setRefreshKey(k => k + 1)}
                        />

                        <SavedRelatedMediaSection
                            mediaItem={show}
                            setSnackbar={setSnackbar}
                            refreshTrigger={relatedMediaRefreshTrigger}
                        />

                        <SimilarItemsSection
                            mediaItem={show}
                            setSnackbar={setSnackbar}
                            onRelatedMediaSaved={() => setRelatedMediaRefreshTrigger(prev => prev + 1)}
                        />

                        <MixlistCarousel
                            mediaItem={show}
                            currentMixlists={currentMixlists}
                            availableMixlists={availableMixlists}
                            setCurrentMixlists={setCurrentMixlists}
                            setAvailableMixlists={setAvailableMixlists}
                            setSnackbar={setSnackbar}
                            isMobile={isMobile}
                        />
                    </CardContent>
                </Card>

                {/* Action Bar */}
                <Box display="flex" gap={1} flexWrap="wrap" my={3}>
                    {show.link && (
                        <Button variant="contained" size="small" startIcon={<Visibility />} href={show.link} target="_blank">
                            View
                        </Button>
                    )}
                    <Button variant="contained" size="small" startIcon={<Delete />} onClick={() => setDeleteConfirmDialog(true)} color="error">
                        Delete
                    </Button>
                </Box>

                {/* Watch Progress */}
                {watchProgress && (
                    <Box sx={{ mb: 3 }}>
                        <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                            <Typography variant="subtitle2">
                                Watch Progress
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                                {watchProgress.watched} / {watchProgress.total} episodes ({watchProgress.percentage}%)
                            </Typography>
                        </Box>
                        <LinearProgress
                            variant="determinate"
                            value={watchProgress.percentage}
                            sx={{
                                height: 8,
                                borderRadius: 4,
                                backgroundColor: 'rgba(255,255,255,0.1)',
                                '& .MuiLinearProgress-bar': {
                                    borderRadius: 4,
                                    backgroundColor: watchProgress.percentage === 100 ? '#4caf50' : '#2196f3'
                                }
                            }}
                        />
                    </Box>
                )}

                {/* Episodes grouped by season */}
                {groupedEpisodes.length > 0 ? (
                    groupedEpisodes.map(({ season, episodes: seasonEpisodes }) => (
                        <Accordion key={season} defaultExpanded={groupedEpisodes.length <= 3} sx={{ borderRadius: 2, mb: 1 }}>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography variant="h6">
                                    {season === 0 ? 'Specials' : `Season ${season}`} ({seasonEpisodes.length})
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails>
                                <List disablePadding>
                                    {seasonEpisodes.map((ep) => (
                                        <ListItemButton
                                            key={ep.id}
                                            onClick={() => navigate(`/media/${ep.id}`)}
                                            sx={{ mb: 1, border: '1px solid rgba(255,255,255,0.1)', borderRadius: 2 }}
                                        >
                                            <Box sx={{ width: '100%' }}>
                                                <Box display="flex" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                                                    <Box display="flex" alignItems="center" gap={1} sx={{ minWidth: 0, flex: 1 }}>
                                                        <Chip
                                                            label={ep.episodeIdentifier || `E${ep.episodeNumber || '?'}`}
                                                            size="small"
                                                            sx={{ fontWeight: 600, minWidth: 60 }}
                                                        />
                                                        <Typography variant="subtitle1" sx={{ fontWeight: 500 }} noWrap>
                                                            {ep.title}
                                                        </Typography>
                                                    </Box>
                                                    <Box display="flex" alignItems="center" gap={1}>
                                                        {ep.traktPlays > 0 && (
                                                            <Chip
                                                                label={`${ep.traktPlays} play${ep.traktPlays !== 1 ? 's' : ''}`}
                                                                size="small"
                                                                variant="outlined"
                                                                sx={{ fontSize: '0.7rem' }}
                                                            />
                                                        )}
                                                        <Chip
                                                            label={formatStatus(ep.status)}
                                                            size="small"
                                                            sx={{ bgcolor: getStatusColor(ep.status), color: 'white' }}
                                                        />
                                                    </Box>
                                                </Box>
                                                <Box display="flex" gap={2} mt={0.5}>
                                                    {ep.airDate && (
                                                        <Typography variant="caption" color="text.secondary">
                                                            Aired: {new Date(ep.airDate).toLocaleDateString()}
                                                        </Typography>
                                                    )}
                                                    {ep.traktLastWatchedAt && (
                                                        <Typography variant="caption" color="text.secondary">
                                                            Last watched: {new Date(ep.traktLastWatchedAt).toLocaleDateString()}
                                                        </Typography>
                                                    )}
                                                </Box>
                                            </Box>
                                        </ListItemButton>
                                    ))}
                                </List>
                            </AccordionDetails>
                        </Accordion>
                    ))
                ) : (
                    <Box sx={{ textAlign: 'center', py: 4 }}>
                        <Typography variant="body1" color="text.secondary">
                            No episodes tracked yet. Sync from Trakt to import your watch history.
                        </Typography>
                        <Button
                            variant="contained"
                            size="small"
                            onClick={() => navigate('/trakt-sync')}
                            sx={{ mt: 2 }}
                        >
                            Go to Trakt Sync
                        </Button>
                    </Box>
                )}
            </Box>

            {/* Delete Dialog */}
            <Dialog open={deleteConfirmDialog} onClose={() => setDeleteConfirmDialog(false)}>
                <DialogTitle>Delete TV Show?</DialogTitle>
                <DialogContent>
                    <Typography>This will remove "{show?.title}" and all its tracked episodes.</Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteConfirmDialog(false)} sx={{ color: '#fcfafa' }}>Cancel</Button>
                    <Button onClick={handleDelete} color="error" variant="contained">Delete Forever</Button>
                </DialogActions>
            </Dialog>

            <Snackbar open={snackbar.open} autoHideDuration={4000} onClose={() => setSnackbar({ ...snackbar, open: false })}>
                <Alert severity={snackbar.severity}>{snackbar.message}</Alert>
            </Snackbar>
        </Box>
    );
}

export default TvShowProfile;
