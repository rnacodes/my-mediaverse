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
    YouTube, ExpandMore, Visibility, Add, CheckCircle
} from '@mui/icons-material';
import axios from 'axios';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import MediaInfoCard from './MediaInfoCard';
import MixlistCarousel from './MixlistCarousel';
import TopicsGenresSection from './TopicsGenresSection';
import { getYouTubePlaylistById, getYouTubePlaylistVideos, deleteYouTubePlaylist, syncYouTubePlaylist, addVideoToYouTubePlaylist } from '../api/youtubeService';
import { getAllMixlists } from '../api/mixlistService';
import {
    formatMediaType,
    formatStatus,
    getMediaTypeColor,
    getStatusColor,
    getRatingIcon,
    getRatingText
} from '../utils/formatters';

function YouTubePlaylistProfile() {
    // --- State Management ---
    const [playlist, setPlaylist] = useState(null);
    const [videos, setVideos] = useState([]);
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState(false);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [deleteConfirmDialog, setDeleteConfirmDialog] = useState(false);
    const [viewAllVideosDialog, setViewAllVideosDialog] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });

    // Pagination State for Video Browser
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
        console.log('YouTubePlaylistProfile: Loading playlist with ID:', id);
        fetchPlaylistData();
        fetchMixlists();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id, refreshKey]);

    useEffect(() => {
        const fetchCurrentMixlists = async () => {
            if (!playlist) return;
            const mixlistIds = playlist.mixlistIds || [];
            if (mixlistIds.length > 0) {
                const allMixlistsResponse = await getAllMixlists();
                const allMixlists = allMixlistsResponse.data || [];
                const playlistMixlists = mixlistIds.map(mixlistId =>
                    allMixlists.find(m => m.id === mixlistId)
                ).filter(m => m !== undefined);
                setCurrentMixlists(playlistMixlists);
            } else {
                setCurrentMixlists([]);
            }
        };
        fetchCurrentMixlists();
    }, [playlist]);

    // --- Data Fetching ---
    const fetchPlaylistData = async () => {
        try {
            console.log('Fetching playlist data for ID:', id);
            setLoading(true);
            const playlistData = await getYouTubePlaylistById(id, true);
            console.log('Playlist response:', playlistData);
            setPlaylist(playlistData);

            // Extract videos from the playlist response or fetch separately
            if (playlistData.videos && playlistData.videos.length > 0) {
                setVideos(playlistData.videos);
            } else {
                const playlistVideos = await getYouTubePlaylistVideos(id);
                console.log('Videos response:', playlistVideos);
                setVideos(playlistVideos);
            }
            console.log('Playlist data loaded successfully');
        } catch (error) {
            console.error('Error fetching playlist data:', error);
            console.error('Error details:', error.response || error.message);
            setSnackbar({ open: true, message: `Failed to load playlist: ${error.response?.data?.message || error.message}`, severity: 'error' });
        } finally {
            setLoading(false);
        }
    };

    const fetchMixlists = async () => {
        try {
            const mixlists = await getAllMixlists();
            setAvailableMixlists(mixlists.data || []);
        } catch (error) {
            console.error('Error fetching mixlists:', error);
            setAvailableMixlists([]);
        }
    };

    const handleSync = async () => {
        try {
            setSyncing(true);
            await syncYouTubePlaylist(id);
            await fetchPlaylistData();
            setSnackbar({ open: true, message: 'Playlist synced successfully', severity: 'success' });
        } catch (error) {
            console.error('Error syncing playlist:', error);
            setSnackbar({ open: true, message: 'Failed to sync playlist', severity: 'error' });
        } finally {
            setSyncing(false);
        }
    };

    const handleDelete = async () => {
        try {
            await deleteYouTubePlaylist(id);
            setSnackbar({ open: true, message: 'YouTube playlist deleted', severity: 'success' });
            setTimeout(() => navigate('/'), 1500);
        } catch (error) {
            console.error('Error deleting playlist:', error);
            setSnackbar({ open: true, message: 'Failed to delete playlist', severity: 'error' });
        }
        setDeleteConfirmDialog(false);
    };

    // --- Video Browser Functions ---

    // Helper function to check if a video is deleted or private
    const isDeletedOrPrivateVideo = (video) => {
        const title = video.snippet?.title || video.title || '';
        const titleLower = title.toLowerCase();

        // Check for common deleted/private video indicators
        if (titleLower === 'deleted video' ||
            titleLower === 'private video' ||
            titleLower === '[deleted video]' ||
            titleLower === '[private video]') {
            return true;
        }

        // Check if the video has no channel info (often indicates deleted)
        const channelTitle = video.snippet?.videoOwnerChannelTitle || video.snippet?.channelTitle || '';
        if (!channelTitle && !video.snippet?.resourceId?.videoId) {
            return true;
        }

        return false;
    };

    const handleViewAllVideos = async () => {
        if (!playlist?.playlistExternalId) {
            setSnackbar({ open: true, message: 'No external ID available for this playlist', severity: 'error' });
            return;
        }

        try {
            setLoadingAllVideos(true);
            setViewAllVideosDialog(true);
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';

            let allVideos = [];
            let pageToken = null;
            let hasMore = true;

            // Fetch playlist items from YouTube API via backend
            while (hasMore) {
                const url = pageToken
                    ? `${API_URL}/YouTube/playlists/${playlist.playlistExternalId}/items?maxResults=50&pageToken=${pageToken}`
                    : `${API_URL}/YouTube/playlists/${playlist.playlistExternalId}/items?maxResults=50`;

                const response = await axios.get(url);
                const fetched = response.data.items || response.data || [];
                allVideos = [...allVideos, ...fetched];

                pageToken = response.data.nextPageToken;
                hasMore = pageToken !== null && pageToken !== undefined && allVideos.length < 200;
            }

            // Filter out deleted and private videos
            const availableVideos = allVideos.filter(video => !isDeletedOrPrivateVideo(video));
            const filteredCount = allVideos.length - availableVideos.length;

            if (filteredCount > 0) {
                console.log(`Filtered out ${filteredCount} deleted/private videos from playlist`);
            }

            setAllVideosFromApi(availableVideos);
            setDisplayedVideos(availableVideos.slice(0, 10));
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
            const dbVideos = await getYouTubePlaylistVideos(id);
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
        const videoId = video.snippet?.resourceId?.videoId || video.id?.videoId || video.id;
        if (!videoId) return;
        try {
            setImportingVideo(videoId);
            const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5033/api';
            const response = await axios.post(`${API_URL}/YouTube/import/video/${videoId}`);
            const importedVideoId = response.data.id;

            // Add the imported video to this playlist
            await addVideoToYouTubePlaylist(id, importedVideoId);

            const newImportedMap = new Map(importedVideos);
            newImportedMap.set(videoId, importedVideoId);
            setImportedVideos(newImportedMap);
            setSnackbar({ open: true, message: `Successfully imported "${video.snippet?.title || video.title}"!`, severity: 'success' });
            await fetchPlaylistData();
        } catch {
            setSnackbar({ open: true, message: 'Failed to import video', severity: 'error' });
        } finally {
            setImportingVideo(null);
        }
    };

    const formatDuration = (seconds) => {
        if (!seconds) return 'N/A';
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;

        if (hours > 0) {
            return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        }
        return `${minutes}:${secs.toString().padStart(2, '0')}`;
    };

    const getYouTubeUrl = () => {
        return playlist?.playlistExternalId
            ? `https://www.youtube.com/playlist?list=${playlist.playlistExternalId}`
            : playlist?.link || null;
    };

    if (loading) return <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh"><CircularProgress /></Box>;
    if (!playlist) return <Box p={3}><Alert severity="error">YouTube playlist not found</Alert></Box>;

    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: { xs: 2, sm: 4 }, px: { xs: 1, sm: 2 } }}>
            <Box sx={{ width: '100%', maxWidth: '900px', backgroundColor: 'background.paper', borderRadius: { xs: '8px', sm: '16px' }, p: { xs: 2, sm: 3, md: 4 }, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>
                {/* Header */}
                <Box display="flex" alignItems="center" mb={3}>
                    <IconButton onClick={() => navigate('/all-media?mediaType=Playlist')} sx={{ mr: 2 }}><ArrowBack /></IconButton>
                    <Typography variant="h4" sx={{ flexGrow: 1 }}>{playlist.title}</Typography>
                    <IconButton onClick={() => navigate(`/media/${id}/edit`)}><Edit /></IconButton>
                </Box>

                {/* Profile Card */}
                <Card sx={{ borderRadius: 2, mb: 3 }}>
                    <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
                        <MediaInfoCard
                            mediaItem={playlist}
                            formatMediaType={formatMediaType}
                            formatStatus={formatStatus}
                            getMediaTypeColor={getMediaTypeColor}
                            getStatusColor={getStatusColor}
                            getRatingIcon={getRatingIcon}
                            getRatingText={getRatingText}
                        />

                        <Divider sx={{ my: 3 }} />
                        <TopicsGenresSection
                            mediaItem={playlist}
                            setSnackbar={setSnackbar}
                            onUpdate={() => setRefreshKey(k => k + 1)}
                        />
                        <MixlistCarousel
                            mediaItem={playlist}
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
                                            <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>
                                                {video.position !== undefined && video.position !== null && (
                                                    <span style={{ color: '#888', marginRight: '8px' }}>#{video.position + 1}</span>
                                                )}
                                                {video.title}
                                            </Typography>
                                            <Chip label={formatStatus(video.status)} size="small" sx={{ bgcolor: getStatusColor(video.status), color: 'white' }} />
                                        </Box>
                                        <Typography variant="caption" color="text.secondary">
                                            {video.lengthInSeconds > 0 ? `Duration: ${formatDuration(video.lengthInSeconds)}` : ''}
                                            {video.releaseDate ? ` • Published: ${new Date(video.releaseDate).toLocaleDateString()}` : ''}
                                        </Typography>
                                    </Box>
                                </ListItemButton>
                            ))}
                            {videos.length === 0 && (
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                                    No videos imported yet. Click "All Videos" to browse and import videos from this playlist.
                                </Typography>
                            )}
                        </List>
                    </AccordionDetails>
                </Accordion>
            </Box>

            {/* --- Dialogs --- */}

            {/* Delete Dialog */}
            <Dialog open={deleteConfirmDialog} onClose={() => setDeleteConfirmDialog(false)}>
                <DialogTitle>Delete Playlist?</DialogTitle>
                <DialogContent><Typography>This will remove "{playlist?.title}" from your library. Associated videos will remain in the database.</Typography></DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteConfirmDialog(false)} sx={{ color: '#fcfafa' }}>Cancel</Button>
                    <Button onClick={handleDelete} color="error" variant="contained">Delete Forever</Button>
                </DialogActions>
            </Dialog>

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

            <Snackbar open={snackbar.open} autoHideDuration={4000} onClose={() => setSnackbar({ ...snackbar, open: false })}>
                <Alert severity={snackbar.severity}>{snackbar.message}</Alert>
            </Snackbar>
        </Box>
    );
}

export default YouTubePlaylistProfile;

