import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Typography, Button, Card, CardContent, Chip, Divider, IconButton, CircularProgress, Alert, Accordion, AccordionSummary, AccordionDetails, List, Dialog, DialogTitle, DialogContent, DialogActions, Snackbar, ListItemButton, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper } from '@mui/material';
import {
    Sync, Delete,
    YouTube, ExpandMore, Visibility, Add, CheckCircle
} from '@mui/icons-material';
import { getYouTubePlaylistItems, importYouTubeVideo } from '@/api/youtubeService';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import MediaHeader from '@/features/media/MediaHeader';
import MediaInfoCard from '@/features/media/MediaInfoCard';
import MixlistCarousel from '@/features/mixlists/MixlistCarousel';
import TopicsGenresSection from '@/features/media/TopicsGenresSection';
import {
    useYouTubePlaylist,
    useYouTubePlaylistVideos,
    useDeleteYouTubePlaylist,
    useSyncYouTubePlaylist,
    useAddVideoToYouTubePlaylist,
} from '@/hooks/useYoutube';
import { useAllMixlists } from '@/hooks/useMixlist';
import { useReindexMediaItem } from '@/hooks/useTypesense';
import {
    formatMediaType,
    formatStatus,
    getMediaTypeColor,
    getStatusColor,
    getRatingIcon,
    getRatingText
} from '@/utils/formatters';

function YouTubePlaylistProfile() {
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [deleteConfirmDialog, setDeleteConfirmDialog] = useState(false);
    const [viewAllVideosDialog, setViewAllVideosDialog] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });

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

    const playlistQuery = useYouTubePlaylist(id, true);
    const playlist = playlistQuery.data ?? null;
    const playlistEmbeddedVideos = playlist?.videos ?? null;

    // Only fetch separate videos endpoint if playlist response doesn't include them.
    const fallbackVideosQuery = useYouTubePlaylistVideos(id, {
        enabled: !!playlist && (!playlistEmbeddedVideos || playlistEmbeddedVideos.length === 0),
    });
    const videos = playlistEmbeddedVideos?.length
        ? playlistEmbeddedVideos
        : (fallbackVideosQuery.data ?? []);

    const mixlistsQuery = useAllMixlists();
    const availableMixlistsFromQuery = useMemo(() => mixlistsQuery.data ?? [], [mixlistsQuery.data]);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    useEffect(() => { setAvailableMixlists(availableMixlistsFromQuery); }, [availableMixlistsFromQuery]);

    const loading = playlistQuery.isLoading;

    const syncMutation = useSyncYouTubePlaylist();
    const syncing = syncMutation.isPending;
    const deleteMutation = useDeleteYouTubePlaylist();
    const addVideoMutation = useAddVideoToYouTubePlaylist();

    const reindexMutation = useReindexMediaItem();
    const reindexing = reindexMutation.isPending;

    const handleReindex = () => {
        reindexMutation.mutate(id, {
            onSuccess: () => setSnackbar({ open: true, message: 'Media item re-indexed in search.', severity: 'success' }),
            onError: (error) => {
                if (error.response?.status !== 403) {
                    setSnackbar({ open: true, message: 'Failed to re-index media item.', severity: 'error' });
                }
            },
        });
    };

    // Force refetch when refreshKey changes (used by child sections).
    useEffect(() => {
        if (refreshKey > 0) {
            playlistQuery.refetch();
            fallbackVideosQuery.refetch();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [refreshKey]);

    // Surface load errors.
    useEffect(() => {
        if (playlistQuery.error) {
            setSnackbar({ open: true, message: `Failed to load playlist: ${playlistQuery.error.response?.data?.message || playlistQuery.error.message}`, severity: 'error' });
        }
    }, [playlistQuery.error]);

    // Derive currentMixlists.
    useEffect(() => {
        if (!playlist) return;
        const mixlistIds = playlist.mixlistIds || [];
        if (mixlistIds.length > 0 && availableMixlistsFromQuery.length > 0) {
            const playlistMixlists = mixlistIds
                .map(mixlistId => availableMixlistsFromQuery.find(m => m.id === mixlistId))
                .filter(Boolean);
            setCurrentMixlists(playlistMixlists);
        } else {
            setCurrentMixlists([]);
        }
    }, [playlist, availableMixlistsFromQuery]);

    const handleSync = () => {
        syncMutation.mutate(id, {
            onSuccess: () => setSnackbar({ open: true, message: 'Playlist synced successfully', severity: 'success' }),
            onError: () => setSnackbar({ open: true, message: 'Failed to sync playlist', severity: 'error' }),
        });
    };

    const handleDelete = () => {
        deleteMutation.mutate(id, {
            onSuccess: () => {
                setSnackbar({ open: true, message: 'YouTube playlist deleted', severity: 'success' });
                setTimeout(() => navigate('/'), 1500);
            },
            onError: () => setSnackbar({ open: true, message: 'Failed to delete playlist', severity: 'error' }),
        });
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

            let allVideos = [];
            let pageToken = null;
            let hasMore = true;

            // Fetch playlist items from YouTube API via backend
            while (hasMore) {
                const data = await getYouTubePlaylistItems(playlist.playlistExternalId, 50, pageToken);
                const fetched = data.items || data || [];
                allVideos = [...allVideos, ...fetched];

                pageToken = data.nextPageToken;
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
            checkImportedVideos();
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

    const checkImportedVideos = () => {
        const importedMap = new Map();
        (videos || []).forEach(video => {
            if (video.externalId) importedMap.set(video.externalId, video.id);
        });
        setImportedVideos(importedMap);
    };

    const handleImportVideo = async (video) => {
        const videoId = video.snippet?.resourceId?.videoId || video.id?.videoId || video.id;
        if (!videoId) return;
        try {
            setImportingVideo(videoId);
            const imported = await importYouTubeVideo(videoId);
            const importedVideoId = imported.id;

            // Add the imported video to this playlist (await direct call — sequential with import).
            await addVideoMutation.mutateAsync({ playlistId: id, videoId: importedVideoId });

            const newImportedMap = new Map(importedVideos);
            newImportedMap.set(videoId, importedVideoId);
            setImportedVideos(newImportedMap);
            setSnackbar({ open: true, message: `Successfully imported "${video.snippet?.title || video.title}"!`, severity: 'success' });
            playlistQuery.refetch();
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
                {/* Header with back button, reindex, and edit buttons */}
                <MediaHeader
                    title={playlist.title}
                    mediaId={id}
                    onReindex={handleReindex}
                    reindexing={reindexing}
                />

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
                                    No videos imported yet. Click &quot;All Videos&quot; to browse and import videos from this playlist.
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
                <DialogContent><Typography>This will remove &quot;{playlist?.title}&quot; from your library. Associated videos will remain in the database.</Typography></DialogContent>
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

