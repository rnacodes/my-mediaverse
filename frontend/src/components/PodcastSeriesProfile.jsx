import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent, 
    Chip, Divider, IconButton, CircularProgress, Alert,
    Accordion, AccordionSummary, AccordionDetails, List, ListItem, ListItemText,
    Dialog, DialogTitle, DialogContent, DialogActions, Snackbar, ListItemButton,
    Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper
} from '@mui/material';
import {
    ArrowBack, Edit, OpenInNew, Sync, Delete,
    ExpandMore, Visibility, Add, CheckCircle, Close as CloseIcon
} from '@mui/icons-material';
import axios from 'axios';
import MediaInfoCard from './MediaInfoCard';
import MediaDetailAccordion from './MediaDetailAccordion';
import MixlistCarousel from './MixlistCarousel';
import TopicsGenresSection from './TopicsGenresSection';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { getPodcastSeriesById, getEpisodesBySeriesId, syncPodcastSeriesEpisodes, deletePodcastSeries, importPodcastEpisodeFromApi } from '../api/podcastService';
import { getAllMixlists } from '../api/mixlistService';
import { 
    formatMediaType, 
    formatStatus, 
    getMediaTypeColor, 
    getStatusColor, 
    getRatingIcon, 
    getRatingText 
} from '../utils/formatters';

function PodcastSeriesProfile() {
    // --- State Management ---
    const [series, setSeries] = useState(null);
    const [episodes, setEpisodes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState(false);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [deleteConfirmDialog, setDeleteConfirmDialog] = useState(false);
    const [viewAllEpisodesDialog, setViewAllEpisodesDialog] = useState(false);
    
    // Pagination State
    const [allEpisodesFromApi, setAllEpisodesFromApi] = useState([]);
    const [displayedEpisodes, setDisplayedEpisodes] = useState([]);
    const [loadingAllEpisodes, setLoadingAllEpisodes] = useState(false);
    
    const [importedEpisodes, setImportedEpisodes] = useState(new Map());
    const [importingEpisode, setImportingEpisode] = useState(null);
    const [refreshKey, setRefreshKey] = useState(0);

    const { id } = useParams();
    const navigate = useNavigate();
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

    // --- Effects ---
    useEffect(() => {
        console.log('PodcastSeriesProfile: Loading series with ID:', id);
        fetchSeriesData();
        fetchMixlists();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id, refreshKey]);

    useEffect(() => {
        const fetchCurrentMixlists = async () => {
            if (!series) return;
            const mixlistIds = series.mixlistIds || [];
            if (mixlistIds.length > 0) {
                const allMixlistsResponse = await getAllMixlists();
                const allMixlists = allMixlistsResponse.data || [];
                const seriesMixlists = mixlistIds.map(mixlistId => 
                    allMixlists.find(m => m.id === mixlistId)
                ).filter(m => m !== undefined);
                setCurrentMixlists(seriesMixlists);
            } else {
                setCurrentMixlists([]);
            }
        };
        fetchCurrentMixlists();
    }, [series]);

    // --- Data Fetching ---
    const fetchSeriesData = async () => {
        try {
            console.log('Fetching series data for ID:', id);
            setLoading(true);
            
            console.log('Making API calls...');
            const [seriesResponse, episodesResponse] = await Promise.all([
                getPodcastSeriesById(id),
                getEpisodesBySeriesId(id)
            ]);

            console.log('Series response:', seriesResponse);
            console.log('Episodes response:', episodesResponse);

            setSeries(seriesResponse.data);
            const sortedEpisodes = (episodesResponse.data || []).sort((a, b) => {
                if (a.episodeNumber && b.episodeNumber) return b.episodeNumber - a.episodeNumber;
                if (a.releaseDate && b.releaseDate) return new Date(b.releaseDate) - new Date(a.releaseDate);
                return new Date(b.dateAdded) - new Date(a.dateAdded);
            });
            setEpisodes(sortedEpisodes);
            console.log('Series data loaded successfully');
            setLoading(false);
        } catch (error) {
            console.error('Error fetching podcast series:', error);
            console.error('Error details:', error.response || error.message);
            setSnackbar({ open: true, message: `Failed to load podcast series: ${error.response?.data?.message || error.message}`, severity: 'error' });
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

    // --- Pagination & API Logic ---
    const handleViewAllEpisodes = async () => {
        if (!series?.externalId) {
            setSnackbar({ open: true, message: 'No external ID available for this series', severity: 'error' });
            return;
        }

        try {
            setLoadingAllEpisodes(true);
            setViewAllEpisodesDialog(true);
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            
            let allEpisodes = [];
            let nextDate = null;
            let hasMore = true;
            
            // Fetch episodes until the ListenNotes API has no more pages
            while (hasMore) {
                const url = nextDate 
                    ? `${API_URL}/ListenNotes/podcasts/${series.externalId}?next_episode_pub_date=${nextDate}`
                    : `${API_URL}/ListenNotes/podcasts/${series.externalId}`;
                    
                const response = await axios.get(url);
                const fetched = response.data.episodes || [];
                allEpisodes = [...allEpisodes, ...fetched];
                
                // Documentation: ListenNotes uses next_episode_pub_date for pagination
                nextDate = response.data.next_episode_pub_date || response.data.nextEpisodePubDate;
                hasMore = nextDate !== null && nextDate !== undefined && allEpisodes.length < 500; // Limit to 500 for performance
            }
            
            setAllEpisodesFromApi(allEpisodes);
            setDisplayedEpisodes(allEpisodes.slice(0, 10)); // Start by showing first 10
            await checkImportedEpisodes();
        } catch (error) {
            console.error('Error fetching all episodes:', error);
            setSnackbar({ open: true, message: 'Failed to fetch episodes from ListenNotes', severity: 'error' });
            setViewAllEpisodesDialog(false);
        } finally {
            setLoadingAllEpisodes(false);
        }
    };

    const loadMoreLocal = () => {
        const currentCount = displayedEpisodes.length;
        const nextBatch = allEpisodesFromApi.slice(0, currentCount + 10);
        setDisplayedEpisodes(nextBatch);
    };

    const checkImportedEpisodes = async () => {
        try {
            const dbEpisodesResponse = await getEpisodesBySeriesId(id);
            const dbEpisodes = dbEpisodesResponse.data || [];
            const importedMap = new Map();
            dbEpisodes.forEach(ep => {
                if (ep.externalId) importedMap.set(ep.externalId, ep.id);
            });
            setImportedEpisodes(importedMap);
        } catch (error) {
            console.error('Error checking imported episodes:', error);
        }
    };

    const handleImportEpisode = async (episode) => {
        if (!episode.id) return;
        try {
            setImportingEpisode(episode.id);
            const importedEp = await importPodcastEpisodeFromApi(episode.id, id);
            const newImportedMap = new Map(importedEpisodes);
            newImportedMap.set(episode.id, importedEp.id);
            setImportedEpisodes(newImportedMap);
            setSnackbar({ open: true, message: `Successfully imported "${episode.title}"!`, severity: 'success' });
            await fetchSeriesData();
        } catch {
            setSnackbar({ open: true, message: 'Failed to import episode', severity: 'error' });
        } finally {
            setImportingEpisode(null);
        }
    };

    const handleSync = async () => {
        try {
            setSyncing(true);
            const response = await syncPodcastSeriesEpisodes(id);
            setSnackbar({ 
                open: true, 
                message: `Synced! ${response.data.newEpisodesCount || 0} new episodes found.`, 
                severity: 'success' 
            });
            await fetchSeriesData();
        } catch {
            setSnackbar({ open: true, message: 'Failed to sync episodes', severity: 'error' });
        } finally {
            setSyncing(false);
        }
    };

    const handleDelete = async () => {
        try {
            await deletePodcastSeries(id);
            setSnackbar({ open: true, message: 'Podcast series deleted', severity: 'success' });
            setTimeout(() => navigate('/'), 1500);
        } catch {
            setSnackbar({ open: true, message: 'Failed to delete podcast series', severity: 'error' });
        }
        setDeleteConfirmDialog(false);
    };

    const formatDuration = (seconds) => {
        if (!seconds) return 'N/A';
        const h = Math.floor(seconds / 3600);
        const m = Math.floor((seconds % 3600) / 60);
        const s = seconds % 60;
        return h > 0 ? `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}` : `${m}:${s.toString().padStart(2, '0')}`;
    };

    const getListenNotesUrl = () => {
        return series?.externalId ? `https://www.listennotes.com/podcasts/${series.externalId}/` : series?.link || null;
    };

    if (loading) return <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh"><CircularProgress /></Box>;
    if (!series) return <Box p={3}><Alert severity="error">Podcast series not found</Alert></Box>;

    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: { xs: 2, sm: 4 }, px: { xs: 1, sm: 2 } }}>
            <Box sx={{ width: '100%', maxWidth: '900px', backgroundColor: 'background.paper', borderRadius: { xs: '8px', sm: '16px' }, p: { xs: 2, sm: 3, md: 4 }, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>
                {/* Header */}
                <Box display="flex" alignItems="center" mb={3}>
                    <IconButton onClick={() => navigate('/all-media?mediaType=Podcast')} sx={{ mr: 2 }}><ArrowBack /></IconButton>
                    <Typography variant="h4" sx={{ flexGrow: 1 }}>{series.title}</Typography>
                    <IconButton onClick={() => navigate(`/media/${id}/edit`)}><Edit /></IconButton>
                </Box>

                {/* Profile Card */}
                <Card sx={{ borderRadius: 2, mb: 3 }}>
                    <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
                        <MediaInfoCard
                            mediaItem={series}
                            formatMediaType={formatMediaType}
                            formatStatus={formatStatus}
                            getMediaTypeColor={getMediaTypeColor}
                            getStatusColor={getStatusColor}
                            getRatingIcon={getRatingIcon}
                            getRatingText={getRatingText}
                        />

                        <Divider sx={{ my: 3 }} />
                        <MediaDetailAccordion mediaItem={series} navigate={navigate} />
                        <TopicsGenresSection
                            mediaItem={series}
                            setSnackbar={setSnackbar}
                            onUpdate={() => setRefreshKey(k => k + 1)}
                        />
                        <MixlistCarousel 
                            mediaItem={series} 
                            currentMixlists={currentMixlists} 
                            availableMixlists={availableMixlists} 
                            setCurrentMixlists={setCurrentMixlists}
                            setAvailableMixlists={setAvailableMixlists}
                            setSnackbar={setSnackbar} 
                            isMobile={isMobile} 
                        />
                    </CardContent>
                </Card>

                {/* Main Action Bar */}
                <Box display="flex" gap={1} flexWrap="wrap" my={3}>
                    {getListenNotesUrl() && <Button variant="contained" size="small" startIcon={<OpenInNew />} href={getListenNotesUrl()} target="_blank">ListenNotes</Button>}
                    <Button variant="contained" size="small" startIcon={<Sync />} onClick={handleSync} disabled={syncing}>{syncing ? <CircularProgress size={20} /> : 'Sync'}</Button>
                    <Button variant="contained" size="small" startIcon={<Visibility />} onClick={handleViewAllEpisodes}>All Episodes</Button>
                    <Button variant="contained" size="small" startIcon={<Delete />} onClick={() => setDeleteConfirmDialog(true)} color="error">Delete</Button>
                </Box>

                {/* Local Episodes (Already Imported) */}
                <Accordion defaultExpanded sx={{ borderRadius: 2 }}>
                    <AccordionSummary expandIcon={<ExpandMore />}><Typography variant="h6">My Episodes ({episodes.length})</Typography></AccordionSummary>
                    <AccordionDetails>
                        <List>
                            {episodes.map((ep) => (
                                <ListItemButton key={ep.id} onClick={() => navigate(`/podcast-episode/${ep.id}`)} sx={{ mb: 1, border: '1px solid #eee', borderRadius: 2 }}>
                                    <Box sx={{ width: '100%' }}>
                                        <Box display="flex" justifyContent="space-between">
                                            <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>{ep.title}</Typography>
                                            <Chip label={formatStatus(ep.status)} size="small" sx={{ bgcolor: getStatusColor(ep.status), color: 'white' }} />
                                        </Box>
                                        <Typography variant="caption" color="text.secondary">Released: {ep.releaseDate ? new Date(ep.releaseDate).toLocaleDateString() : 'N/A'}</Typography>
                                    </Box>
                                </ListItemButton>
                            ))}
                        </List>
                    </AccordionDetails>
                </Accordion>
            </Box>

            {/* --- Dialogs --- */}

            {/* View All Episodes (API Browser) */}
            <Dialog open={viewAllEpisodesDialog} onClose={() => setViewAllEpisodesDialog(false)} maxWidth="md" fullWidth>
                <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    ListenNotes Episode Browser
                    <IconButton onClick={() => setViewAllEpisodesDialog(false)} size="small"><CloseIcon /></IconButton>
                </DialogTitle>
                <DialogContent dividers>
                    {loadingAllEpisodes ? (
                        <Box textAlign="center" py={4}><CircularProgress /><Typography sx={{ mt: 2 }}>Fetching full catalog...</Typography></Box>
                    ) : (
                        <>
                            <TableContainer component={Paper} sx={{ maxHeight: 400 }}>
                                <Table stickyHeader size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Status</TableCell>
                                            <TableCell>Episode Title</TableCell>
                                            <TableCell>Length</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {displayedEpisodes.map((ep) => (
                                            <TableRow key={ep.id} hover>
                                                <TableCell>
                                                    {importedEpisodes.has(ep.id) ? (
                                                        <CheckCircle color="success" />
                                                    ) : (
                                                        <IconButton onClick={() => handleImportEpisode(ep)} disabled={importingEpisode === ep.id}>
                                                            {importingEpisode === ep.id ? <CircularProgress size={20} /> : <Add />}
                                                        </IconButton>
                                                    )}
                                                </TableCell>
                                                <TableCell sx={{ fontWeight: 500 }}>{ep.title}</TableCell>
                                                <TableCell>{formatDuration(ep.audio_length_sec)}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                            <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                <Typography variant="caption" color="text.secondary">
                                    Showing {displayedEpisodes.length} of {allEpisodesFromApi.length} available episodes
                                </Typography>
                                {displayedEpisodes.length < allEpisodesFromApi.length && (
                                    <Button size="small" variant="contained" onClick={loadMoreLocal}>Load 10 More</Button>
                                )}
                            </Box>
                        </>
                    )}
                </DialogContent>
                <DialogActions><Button onClick={() => setViewAllEpisodesDialog(false)} sx={{ color: '#fcfafa' }}>Close</Button></DialogActions>
            </Dialog>

            {/* Delete Dialog */}
            <Dialog open={deleteConfirmDialog} onClose={() => setDeleteConfirmDialog(false)}>
                <DialogTitle>Delete Series?</DialogTitle>
                <DialogContent><Typography>This will remove "{series?.title}" and all its imported episodes.</Typography></DialogContent>
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

export default PodcastSeriesProfile;