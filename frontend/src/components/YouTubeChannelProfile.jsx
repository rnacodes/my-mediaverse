import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent,
    Chip, Divider, IconButton, CircularProgress, Alert,
    Accordion, AccordionSummary, AccordionDetails, List, ListItemText,
    Dialog, DialogTitle, DialogContent, DialogActions, Snackbar, ListItemButton,
    Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper
} from '@mui/material';
import {
    ArrowBack, Edit, Sync, Delete,
    ExpandMore, Visibility, Add, CheckCircle, YouTube
} from '@mui/icons-material';
import axios from 'axios';
import MediaInfoCard from './MediaInfoCard';
import MediaDetailAccordion from './MediaDetailAccordion';
import MixlistCarousel from './MixlistCarousel';
import TopicsGenresSection from './TopicsGenresSection';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { getYouTubeChannelById, getYouTubeChannelVideos, deleteYouTubeChannel, syncYouTubeChannelMetadata } from '../api/youtubeService';
import { getAllMixlists } from '../api/mixlistService';
import {
    formatMediaType,
    formatStatus,
    getMediaTypeColor,
    getStatusColor,
    getRatingIcon,
    getRatingText
} from '../utils/formatters';

function YouTubeChannelProfile() {
    // --- State Management ---
    const [channel, setChannel] = useState(null);
    const [videos, setVideos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState(false);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [deleteConfirmDialog, setDeleteConfirmDialog] = useState(false);
    const [viewAllVideosDialog, setViewAllVideosDialog] = useState(false);

    // Pagination State
    const [allVideosFromApi, setAllVideosFromApi] = useState([]);
    const [displayedVideos, setDisplayedVideos] = useState([]);
    const [loadingAllVideos, setLoadingAllVideos] = useState(false);

    const [importedVideos, setImportedVideos] = useState(new Map());
    const [importingVideo, setImportingVideo] = useState(null);
    const [refreshKey, setRefreshKey] = useState(0);

    const { id } = useParams();
    const navigate = useNavigate();
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

    // --- Effects ---
    useEffect(() => {
        console.log('YouTubeChannelProfile: Loading channel with ID:', id);
        fetchChannelData();
        fetchMixlists();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id, refreshKey]);

    useEffect(() => {
        const fetchCurrentMixlists = async () => {
            if (!channel) return;
            const mixlistIds = channel.mixlistIds || [];
            if (mixlistIds.length > 0) {
                const allMixlistsResponse = await getAllMixlists();
                const allMixlists = allMixlistsResponse.data || [];
                const channelMixlists = mixlistIds.map(mixlistId =>
                    allMixlists.find(m => m.id === mixlistId)
                ).filter(m => m !== undefined);
                setCurrentMixlists(channelMixlists);
            } else {
                setCurrentMixlists([]);
            }
        };
        fetchCurrentMixlists();
    }, [channel]);

    // --- Data Fetching ---
    const fetchChannelData = async () => {
        try {
            console.log('Fetching channel data for ID:', id);
            setLoading(true);

            const channelData = await getYouTubeChannelById(id);
            console.log('Channel response:', channelData);
            setChannel(channelData);

            const channelVideos = await getYouTubeChannelVideos(id);
            console.log('Videos response:', channelVideos);
            const sortedVideos = (channelVideos || []).sort((a, b) => {
                if (a.releaseDate && b.releaseDate) return new Date(b.releaseDate) - new Date(a.releaseDate);
                return new Date(b.dateAdded) - new Date(a.dateAdded);
            });
            setVideos(sortedVideos);
            console.log('Channel data loaded successfully');
            setLoading(false);
        } catch (error) {
            console.error('Error fetching YouTube channel:', error);
            console.error('Error details:', error.response || error.message);
            setSnackbar({ open: true, message: `Failed to load YouTube channel: ${error.response?.data?.message || error.message}`, severity: 'error' });
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
    const handleViewAllVideos = async () => {
        if (!channel?.channelExternalId) {
            setSnackbar({ open: true, message: 'No external ID available for this channel', severity: 'error' });
            return;
        }

        try {
            setLoadingAllVideos(true);
            setViewAllVideosDialog(true);
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';

            let allVideos = [];
            let pageToken = null;
            let hasMore = true;

            // Fetch videos from YouTube API via backend using the uploads endpoint
            while (hasMore) {
                const url = pageToken
                    ? `${API_URL}/YouTube/channels/${channel.channelExternalId}/uploads?maxResults=50&pageToken=${pageToken}`
                    : `${API_URL}/YouTube/channels/${channel.channelExternalId}/uploads?maxResults=50`;

                const response = await axios.get(url);
                const fetched = response.data.items || response.data || [];
                allVideos = [...allVideos, ...fetched];

                pageToken = response.data.nextPageToken;
                hasMore = pageToken !== null && pageToken !== undefined && allVideos.length < 200; // Limit to 200 for performance
            }

            setAllVideosFromApi(allVideos);
            setDisplayedVideos(allVideos.slice(0, 10)); // Start by showing first 10
            await checkImportedVideos();
        } catch (error) {
            console.error('Error fetching all videos:', error);
            setSnackbar({ open: true, message: 'Failed to fetch videos from YouTube API', severity: 'error' });
            setViewAllVideosDialog(false);
        } finally {
            setLoadingAllVideos(false);
        }
    };

    const loadMoreLocal = () => {
        const currentCount = displayedVideos.length;
        const nextBatch = allVideosFromApi.slice(0, currentCount + 10);
        setDisplayedVideos(nextBatch);
    };

    const checkImportedVideos = async () => {
        try {
            const dbVideos = await getYouTubeChannelVideos(id);
            const importedMap = new Map();
            (dbVideos || []).forEach(video => {
                if (video.externalId) importedMap.set(video.externalId, video.id);
            });
            setImportedVideos(importedMap);
        } catch (error) {
            console.error('Error checking imported videos:', error);
        }
    };

    const handleImportVideo = async (video) => {
        // Handle the nested id structure from YouTube API playlist items
        const videoId = video.snippet?.resourceId?.videoId || video.id?.videoId || video.id;
        if (!videoId) return;
        try {
            setImportingVideo(videoId);
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            const response = await axios.post(`${API_URL}/YouTube/import/video/${videoId}`);
            const newImportedMap = new Map(importedVideos);
            newImportedMap.set(videoId, response.data.id);
            setImportedVideos(newImportedMap);
            setSnackbar({ open: true, message: `Successfully imported "${video.snippet?.title || video.title}"!`, severity: 'success' });
            await fetchChannelData();
        } catch {
            setSnackbar({ open: true, message: 'Failed to import video', severity: 'error' });
        } finally {
            setImportingVideo(null);
        }
    };

    const handleSync = async () => {
        try {
            setSyncing(true);
            await syncYouTubeChannelMetadata(id);
            setSnackbar({
                open: true,
                message: 'Channel metadata synced successfully!',
                severity: 'success'
            });
            await fetchChannelData();
        } catch {
            setSnackbar({ open: true, message: 'Failed to sync channel', severity: 'error' });
        } finally {
            setSyncing(false);
        }
    };

    const handleDelete = async () => {
        try {
            await deleteYouTubeChannel(id);
            setSnackbar({ open: true, message: 'YouTube channel deleted', severity: 'success' });
            setTimeout(() => navigate('/youtube-channels'), 1500);
        } catch {
            setSnackbar({ open: true, message: 'Failed to delete channel', severity: 'error' });
        }
        setDeleteConfirmDialog(false);
    };

    const getYouTubeUrl = () => {
        return channel?.channelExternalId ? `https://www.youtube.com/channel/${channel.channelExternalId}` : channel?.link || null;
    };

    if (loading) return <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh"><CircularProgress /></Box>;
    if (!channel) return <Box p={3}><Alert severity="error">YouTube channel not found</Alert></Box>;

    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: { xs: 2, sm: 4 }, px: { xs: 1, sm: 2 } }}>
            <Box sx={{ width: '100%', maxWidth: '900px', backgroundColor: 'background.paper', borderRadius: { xs: '8px', sm: '16px' }, p: { xs: 2, sm: 3, md: 4 }, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>
                {/* Header */}
                <Box display="flex" alignItems="center" mb={3}>
                    <IconButton onClick={() => navigate('/youtube-channels')} sx={{ mr: 2 }}><ArrowBack /></IconButton>
                    <Typography variant="h4" sx={{ flexGrow: 1 }}>{channel.title}</Typography>
                    <IconButton onClick={() => navigate(`/media/${id}/edit`)}><Edit /></IconButton>
                </Box>

                {/* Profile Card */}
                <Card sx={{ borderRadius: 2, mb: 3 }}>
                    <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
                        <MediaInfoCard
                            mediaItem={channel}
                            formatMediaType={formatMediaType}
                            formatStatus={formatStatus}
                            getMediaTypeColor={getMediaTypeColor}
                            getStatusColor={getStatusColor}
                            getRatingIcon={getRatingIcon}
                            getRatingText={getRatingText}
                        />

                        <Divider sx={{ my: 3 }} />
                        {/* Hide MediaDetailAccordion when empty - YouTube channels don't have specific details yet */}
                        {/* Keeping the component here for future use when channel-specific details are added */}
                        {(channel.mediaType === 'Podcast' || channel.mediaType === 'Book' ||
                          channel.mediaType === 'Movie' || channel.mediaType === 'TVShow' ||
                          channel.mediaType === 'Video' || channel.mediaType === 'Article') && (
                            <MediaDetailAccordion mediaItem={channel} navigate={navigate} />
                        )}
                        <TopicsGenresSection
                            mediaItem={channel}
                            setSnackbar={setSnackbar}
                            onUpdate={() => setRefreshKey(k => k + 1)}
                        />
                        <MixlistCarousel
                            mediaItem={channel}
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
                    {getYouTubeUrl() && <Button variant="contained" size="small" startIcon={<YouTube />} href={getYouTubeUrl()} target="_blank">YouTube</Button>}
                    <Button variant="contained" size="small" startIcon={<Sync />} onClick={handleSync} disabled={syncing}>{syncing ? <CircularProgress size={20} /> : 'Sync'}</Button>
                    <Button variant="contained" size="small" startIcon={<Visibility />} onClick={handleViewAllVideos}>All Videos</Button>
                    <Button variant="contained" size="small" startIcon={<Delete />} onClick={() => setDeleteConfirmDialog(true)} color="error">Delete</Button>
                </Box>

                {/* Local Videos (Already Imported) */}
                <Accordion defaultExpanded sx={{ borderRadius: 2 }}>
                    <AccordionSummary expandIcon={<ExpandMore />}><Typography variant="h6">My Videos ({videos.length})</Typography></AccordionSummary>
                    <AccordionDetails>
                        <List>
                            {videos.map((video) => (
                                <ListItemButton key={video.id} onClick={() => navigate(`/media/${video.id}`)} sx={{ mb: 1, border: '1px solid #eee', borderRadius: 2 }}>
                                    <Box sx={{ width: '100%' }}>
                                        <Box display="flex" justifyContent="space-between">
                                            <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>{video.title}</Typography>
                                            <Chip label={formatStatus(video.status)} size="small" sx={{ bgcolor: getStatusColor(video.status), color: 'white' }} />
                                        </Box>
                                        <Typography variant="caption" color="text.secondary">
                                            {video.releaseDate ? `Published: ${new Date(video.releaseDate).toLocaleDateString()}` : `Added: ${new Date(video.dateAdded).toLocaleDateString()}`}
                                        </Typography>
                                    </Box>
                                </ListItemButton>
                            ))}
                            {videos.length === 0 && (
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                                    No videos imported yet. Click &quot;All Videos&quot; to browse and import videos from this channel.
                                </Typography>
                            )}
                        </List>
                    </AccordionDetails>
                </Accordion>
            </Box>

            {/* --- Dialogs --- */}

            {/* View All Videos (API Browser) */}
            <Dialog open={viewAllVideosDialog} onClose={() => setViewAllVideosDialog(false)} maxWidth="md" fullWidth>
                <DialogTitle>YouTube Video Browser</DialogTitle>
                <DialogContent dividers>
                    {loadingAllVideos ? (
                        <Box textAlign="center" py={4}><CircularProgress /><Typography sx={{ mt: 2 }}>Fetching videos from YouTube...</Typography></Box>
                    ) : (
                        <>
                            <TableContainer component={Paper} sx={{ maxHeight: 400 }}>
                                <Table stickyHeader size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Status</TableCell>
                                            <TableCell>Video Title</TableCell>
                                            <TableCell>Published</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {displayedVideos.map((video) => {
                                            // Handle YouTube playlist item structure
                                            const videoId = video.snippet?.resourceId?.videoId || video.id?.videoId || video.id;
                                            const title = video.snippet?.title || video.title;
                                            const publishedAt = video.snippet?.publishedAt || video.contentDetails?.videoPublishedAt || video.publishedAt;
                                            return (
                                                <TableRow key={videoId} hover>
                                                    <TableCell>
                                                        {importedVideos.has(videoId) ? (
                                                            <CheckCircle color="success" />
                                                        ) : (
                                                            <IconButton onClick={() => handleImportVideo(video)} disabled={importingVideo === videoId}>
                                                                {importingVideo === videoId ? <CircularProgress size={20} /> : <Add />}
                                                            </IconButton>
                                                        )}
                                                    </TableCell>
                                                    <TableCell sx={{ fontWeight: 500 }}>{title}</TableCell>
                                                    <TableCell>{publishedAt ? new Date(publishedAt).toLocaleDateString() : 'N/A'}</TableCell>
                                                </TableRow>
                                            );
                                        })}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                            <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                <Typography variant="caption" color="text.secondary">
                                    Showing {displayedVideos.length} of {allVideosFromApi.length} available videos
                                </Typography>
                                {displayedVideos.length < allVideosFromApi.length && (
                                    <Button size="small" variant="contained" onClick={loadMoreLocal}>Load 10 More</Button>
                                )}
                            </Box>
                        </>
                    )}
                </DialogContent>
                <DialogActions><Button onClick={() => setViewAllVideosDialog(false)} sx={{ color: '#fcfafa' }}>Close</Button></DialogActions>
            </Dialog>

            {/* Delete Dialog */}
            <Dialog open={deleteConfirmDialog} onClose={() => setDeleteConfirmDialog(false)}>
                <DialogTitle>Delete Channel?</DialogTitle>
                <DialogContent><Typography>This will remove &quot;{channel?.title}&quot; from your library. Associated videos will remain in the database.</Typography></DialogContent>
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

export default YouTubeChannelProfile;
