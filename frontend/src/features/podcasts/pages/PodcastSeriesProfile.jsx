import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Typography, Button, Card, CardContent, Chip, Divider, CircularProgress, Alert, Accordion, AccordionSummary, AccordionDetails, List, Dialog, DialogTitle, DialogContent, DialogActions, Snackbar, ListItemButton, Tooltip } from '@mui/material';
import {
    OpenInNew, Sync, Delete, ExpandMore, Visibility,
    NotificationsActive, NotificationsNone, AutoFixHigh
} from '@mui/icons-material';
import MediaHeader from '@/features/media/MediaHeader';
import MediaInfoCard from '@/features/media/MediaInfoCard';
import MediaDetailAccordion from '@/features/media/MediaDetailAccordion';
import MixlistCarousel from '@/features/mixlists/MixlistCarousel';
import TopicsGenresSection from '@/features/media/TopicsGenresSection';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import {
    usePodcastSeries,
    useEpisodesBySeriesId,
    useSyncPodcastSeriesEpisodes,
    useDeletePodcastSeries,
    useSubscribeToPodcastSeries,
    useUnsubscribeFromPodcastSeries,
    useEnrichPodcastSeries,
} from '@/hooks/usePodcast';
import FeedEpisodeBrowserDialog from '@/features/podcasts/FeedEpisodeBrowserDialog';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
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

// Where the stored details came from, in the words shown under the action bar.
const METADATA_SOURCE_LABELS = {
    rss: "Details from the show's RSS feed",
    apple: 'Details from Apple Podcasts',
    podcastindex: 'Details from Podcast Index',
};

// Matches the API's first-sync episode count; only the first sync takes a batch of older episodes.
const FIRST_SYNC_EPISODE_COUNT = 25;

const syncHint = (series) =>
    series?.lastSyncDate
        ? 'Adds episodes released since the last sync. Older episodes are in All Episodes.'
        : `Adds the ${FIRST_SYNC_EPISODE_COUNT} newest episodes to your library. Older episodes are in All Episodes.`;

// A failed sync or enrich still answers with the result body, so prefer its message.
const resultErrorText = (err, fallback) =>
    err?.response?.data?.errorMessage || err?.response?.data?.error || fallback;

function PodcastSeriesProfile() {
    const [currentMixlists, setCurrentMixlists] = useState([]);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [deleteConfirmDialog, setDeleteConfirmDialog] = useState(false);
    const [viewAllEpisodesDialog, setViewAllEpisodesDialog] = useState(false);
    const [refreshConfirmDialog, setRefreshConfirmDialog] = useState(false);
    const [refreshKey, setRefreshKey] = useState(0);

    const { id } = useParams();
    const navigate = useNavigate();
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

    const seriesQuery = usePodcastSeries(id);
    const series = seriesQuery.data ?? null;

    const episodesQuery = useEpisodesBySeriesId(id);
    const episodes = useMemo(() => {
        const list = episodesQuery.data ?? [];
        return [...list].sort((a, b) => {
            if (a.episodeNumber && b.episodeNumber) return b.episodeNumber - a.episodeNumber;
            if (a.releaseDate && b.releaseDate) return new Date(b.releaseDate) - new Date(a.releaseDate);
            return new Date(b.dateAdded) - new Date(a.dateAdded);
        });
    }, [episodesQuery.data]);

    const mixlistsQuery = useAllMixlists();
    const availableMixlistsFromQuery = useMemo(() => mixlistsQuery.data ?? [], [mixlistsQuery.data]);
    const [availableMixlists, setAvailableMixlists] = useState([]);
    useEffect(() => { setAvailableMixlists(availableMixlistsFromQuery); }, [availableMixlistsFromQuery]);

    const loading = seriesQuery.isLoading || episodesQuery.isLoading;

    const syncMutation = useSyncPodcastSeriesEpisodes();
    const syncing = syncMutation.isPending;
    const demoWriteBlocked = useDemoWriteBlocked();
    const deleteMutation = useDeletePodcastSeries();
    const subscribeMutation = useSubscribeToPodcastSeries();
    const unsubscribeMutation = useUnsubscribeFromPodcastSeries();
    const subscriptionPending = subscribeMutation.isPending || unsubscribeMutation.isPending;
    const enrichMutation = useEnrichPodcastSeries();
    const enriching = enrichMutation.isPending;

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
            seriesQuery.refetch();
            episodesQuery.refetch();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [refreshKey]);

    // Surface load errors.
    useEffect(() => {
        if (seriesQuery.error) {
            setSnackbar({ open: true, message: `Failed to load podcast series: ${seriesQuery.error.response?.data?.message || seriesQuery.error.message}`, severity: 'error' });
        }
    }, [seriesQuery.error]);

    // Derive currentMixlists.
    useEffect(() => {
        if (!series) return;
        const mixlistIds = series.mixlistIds || [];
        if (mixlistIds.length > 0 && availableMixlistsFromQuery.length > 0) {
            const seriesMixlists = mixlistIds
                .map(mixlistId => availableMixlistsFromQuery.find(m => m.id === mixlistId))
                .filter(Boolean);
            setCurrentMixlists(seriesMixlists);
        } else {
            setCurrentMixlists([]);
        }
    }, [series, availableMixlistsFromQuery]);

    const handleToggleSubscription = () => {
        const subscribed = !!series?.isSubscribed;
        const mutation = subscribed ? unsubscribeMutation : subscribeMutation;
        mutation.mutate(id, {
            onSuccess: () => setSnackbar({
                open: true,
                message: subscribed ? 'Unsubscribed. New episodes will no longer sync.' : 'Subscribed! New episodes sync with your subscriptions.',
                severity: 'success'
            }),
            onError: () => setSnackbar({ open: true, message: subscribed ? 'Failed to unsubscribe' : 'Failed to subscribe', severity: 'error' }),
        });
    };

    const handleSync = () => {
        syncMutation.mutate(id, {
            // The sync hook's mutationFn returns response.data, so `data` here is that payload.
            onSuccess: (data) => {
                const created = data?.createdCount || 0;
                const backlog = data?.backlogCount || 0;
                const parts = [`Synced! ${created} new ${created === 1 ? 'episode' : 'episodes'} added.`];
                if (backlog > 0) parts.push(`${backlog} older ${backlog === 1 ? 'episode is' : 'episodes are'} available in All Episodes.`);
                if (data?.warningMessage) parts.push(data.warningMessage);
                setSnackbar({ open: true, message: parts.join(' '), severity: data?.warningMessage ? 'warning' : 'success' });
            },
            onError: (error) => setSnackbar({ open: true, message: resultErrorText(error, 'Failed to sync episodes'), severity: 'error' }),
        });
    };

    const handleEnrich = (force) => {
        setRefreshConfirmDialog(false);
        enrichMutation.mutate({ seriesId: id, force }, {
            onSuccess: (data) => {
                let message = 'Nothing to update.';
                let severity = 'info';
                if (data?.enrichedCount > 0) {
                    message = "Details updated from the show's feed.";
                    severity = 'success';
                } else if (data?.unchangedCount > 0) {
                    message = 'Already up to date — the feed had nothing new.';
                } else if (data?.notFoundCount > 0) {
                    message = "Couldn't find a feed for this show. Add its RSS feed URL and try again.";
                    severity = 'warning';
                } else if (data?.failedCount > 0) {
                    message = data?.warningMessage || "Couldn't read this show's feed. It will be retried later.";
                    severity = 'warning';
                } else if (data?.warningMessage) {
                    message = data.warningMessage;
                }
                setSnackbar({ open: true, message, severity });
            },
            onError: (error) => setSnackbar({ open: true, message: resultErrorText(error, 'Failed to update details from the feed'), severity: 'error' }),
        });
    };

    const handleDelete = () => {
        deleteMutation.mutate(id, {
            onSuccess: () => {
                setSnackbar({ open: true, message: 'Podcast series deleted', severity: 'success' });
                setTimeout(() => navigate('/'), 1500);
            },
            onError: () => setSnackbar({ open: true, message: 'Failed to delete podcast series', severity: 'error' }),
        });
        setDeleteConfirmDialog(false);
    };

    const getExternalLink = () => {
        if (series?.applePodcastsId) {
            return { label: 'Apple Podcasts', url: `https://podcasts.apple.com/podcast/id${series.applePodcastsId}` };
        }
        return series?.link ? { label: 'Website', url: series.link } : null;
    };

    // A successful delete drops the series from the cache, so without this the page would
    // flash "not found" (and lose the snackbar) for the moment before it navigates away.
    if (deleteMutation.isSuccess) return <Box p={3}><Alert severity="success">Podcast series deleted</Alert></Box>;
    if (loading) return <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh"><CircularProgress /></Box>;
    if (!series) return <Box p={3}><Alert severity="error">Podcast series not found</Alert></Box>;

    const externalLink = getExternalLink();

    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', py: { xs: 2, sm: 4 }, px: { xs: 1, sm: 2 } }}>
            <Box sx={{ width: '100%', maxWidth: '900px', backgroundColor: 'background.paper', borderRadius: { xs: '8px', sm: '16px' }, p: { xs: 2, sm: 3, md: 4 }, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>
                {/* Header with back button, reindex, and edit buttons */}
                <MediaHeader
                    title={series.title}
                    mediaId={id}
                    onReindex={handleReindex}
                    reindexing={reindexing}
                />

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
                    {externalLink && <Button variant="contained" size="small" startIcon={<OpenInNew />} href={externalLink.url} target="_blank" rel="noopener noreferrer">{externalLink.label}</Button>}
                    <DemoWriteGuard>
                        <Button
                            variant={series.isSubscribed ? 'outlined' : 'contained'}
                            size="small"
                            startIcon={series.isSubscribed ? <NotificationsActive /> : <NotificationsNone />}
                            onClick={handleToggleSubscription}
                            disabled={subscriptionPending}
                            sx={series.isSubscribed ? { color: '#fcfafa', borderColor: '#fcfafa' } : undefined}
                        >
                            {series.isSubscribed ? 'Unsubscribe' : 'Subscribe'}
                        </Button>
                    </DemoWriteGuard>
                    {/* The demo guard brings its own tooltip, so the hint stays out of its way there. */}
                    <Tooltip title={demoWriteBlocked ? '' : syncHint(series)}>
                        <span style={{ display: 'inline-flex' }}>
                            <DemoWriteGuard>
                                <Button variant="contained" size="small" startIcon={<Sync />} onClick={handleSync} disabled={syncing}>{syncing ? <CircularProgress size={20} /> : 'Sync'}</Button>
                            </DemoWriteGuard>
                        </span>
                    </Tooltip>
                    <Button variant="contained" size="small" startIcon={<Visibility />} onClick={() => setViewAllEpisodesDialog(true)}>All Episodes</Button>
                    <DemoWriteGuard>
                        <Button
                            variant="contained"
                            size="small"
                            startIcon={<AutoFixHigh />}
                            onClick={() => (series.enrichedAt ? setRefreshConfirmDialog(true) : handleEnrich(false))}
                            disabled={enriching}
                        >
                            {enriching ? <CircularProgress size={20} /> : series.enrichedAt ? 'Refresh from feed' : 'Enrich now'}
                        </Button>
                    </DemoWriteGuard>
                    <Button variant="contained" size="small" startIcon={<Delete />} onClick={() => setDeleteConfirmDialog(true)} color="error">Delete</Button>
                </Box>
                {METADATA_SOURCE_LABELS[series.metadataSource] && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: -2, mb: 3 }}>
                        {METADATA_SOURCE_LABELS[series.metadataSource]}
                    </Typography>
                )}

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
                                            {ep.status && <Chip label={formatStatus(ep.status)} size="small" sx={{ bgcolor: getStatusColor(ep.status), color: 'white' }} />}
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

            <FeedEpisodeBrowserDialog
                open={viewAllEpisodesDialog}
                onClose={() => setViewAllEpisodesDialog(false)}
                seriesId={id}
                onSnackbar={setSnackbar}
            />

            {/* Refresh-from-feed Dialog */}
            <Dialog open={refreshConfirmDialog} onClose={() => setRefreshConfirmDialog(false)}>
                <DialogTitle>Refresh from feed?</DialogTitle>
                <DialogContent><Typography>This re-reads the show&apos;s feed and overwrites the stored details (description, publisher, artwork, genres) with what the feed says.</Typography></DialogContent>
                <DialogActions>
                    <Button onClick={() => setRefreshConfirmDialog(false)} sx={{ color: '#fcfafa' }}>Cancel</Button>
                    <Button onClick={() => handleEnrich(true)} variant="contained">Refresh</Button>
                </DialogActions>
            </Dialog>

            {/* Delete Dialog */}
            <Dialog open={deleteConfirmDialog} onClose={() => setDeleteConfirmDialog(false)}>
                <DialogTitle>Delete Series?</DialogTitle>
                <DialogContent><Typography>This will remove &quot;{series?.title}&quot; and all its imported episodes.</Typography></DialogContent>
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