import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, IconButton, CircularProgress, Alert, Tooltip,
    Dialog, DialogTitle, DialogContent, DialogActions,
    Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper
} from '@mui/material';
import { Add, CheckCircle, Refresh, Close as CloseIcon } from '@mui/icons-material';
import {
    usePodcastFeedEpisodes,
    useRefreshPodcastFeedEpisodes,
    useImportPodcastEpisodeFromFeed,
} from '@/hooks/usePodcast';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';

const PAGE_SIZE = 20;

const formatDuration = (seconds) => {
    if (!seconds) return 'N/A';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    return h > 0 ? `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}` : `${m}:${s.toString().padStart(2, '0')}`;
};

const errorText = (err, fallback) => err?.response?.data?.error || fallback;

// A feed item is identified by its guid, or by its audio URL when it has none.
const itemKey = (item) => item.guid || item.audioUrl || item.title;

/**
 * Browses every episode in the series' feed, a page at a time, and imports
 * single episodes into the library. The server marks each row with
 * existingEpisodeId when that episode is already stored.
 */
function FeedEpisodeBrowserDialog({ open, onClose, seriesId, onSnackbar }) {
    const navigate = useNavigate();
    const [importingKey, setImportingKey] = useState(null);

    const feedQuery = usePodcastFeedEpisodes(seriesId, { limit: PAGE_SIZE }, { enabled: open && !!seriesId });
    const refreshFeed = useRefreshPodcastFeedEpisodes();
    const importEpisode = useImportPodcastEpisodeFromFeed();

    const pages = feedQuery.data?.pages ?? [];
    const items = pages.flatMap((page) => page.items);
    const feedItemCount = pages.length > 0 ? pages[pages.length - 1].feedItemCount : 0;
    const refreshing = refreshFeed.isPending;

    const handleImport = (item) => {
        const key = itemKey(item);
        setImportingKey(key);
        importEpisode.mutate(
            { seriesId, guid: item.guid || undefined, audioUrl: item.guid ? undefined : item.audioUrl },
            {
                onSuccess: ({ status }) => onSnackbar?.({
                    open: true,
                    message: status === 201
                        ? `Successfully imported "${item.title}"!`
                        : `"${item.title}" is already in your library.`,
                    severity: status === 201 ? 'success' : 'info',
                }),
                onError: (err) => onSnackbar?.({
                    open: true,
                    message: errorText(err, 'Failed to import episode'),
                    severity: 'error',
                }),
                onSettled: () => setImportingKey(null),
            },
        );
    };

    const handleRefresh = () => {
        refreshFeed.mutate(seriesId, {
            onError: (err) => onSnackbar?.({
                open: true,
                message: errorText(err, 'Failed to refresh the feed'),
                severity: 'error',
            }),
        });
    };

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                All Episodes
                <Box>
                    <Tooltip title="Re-read the feed">
                        <span>
                            <IconButton onClick={handleRefresh} size="small" disabled={refreshing || feedQuery.isLoading} aria-label="Refresh feed">
                                <Refresh />
                            </IconButton>
                        </span>
                    </Tooltip>
                    <IconButton onClick={onClose} size="small" aria-label="Close"><CloseIcon /></IconButton>
                </Box>
            </DialogTitle>
            <DialogContent dividers>
                {feedQuery.isLoading || refreshing ? (
                    <Box textAlign="center" py={4}><CircularProgress /><Typography sx={{ mt: 2 }}>Reading the feed...</Typography></Box>
                ) : feedQuery.isError ? (
                    <Alert severity="error">
                        {errorText(feedQuery.error, "Couldn't read this show's feed. Try again later.")}
                    </Alert>
                ) : items.length === 0 ? (
                    <Alert severity="info">This show&apos;s feed has no episodes.</Alert>
                ) : (
                    <>
                        <TableContainer component={Paper} sx={{ maxHeight: 400 }}>
                            <Table stickyHeader size="small">
                                <TableHead>
                                    <TableRow>
                                        <TableCell>Status</TableCell>
                                        <TableCell>Episode Title</TableCell>
                                        <TableCell>Published</TableCell>
                                        <TableCell>Length</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {items.map((item) => {
                                        const key = itemKey(item);
                                        return (
                                            <TableRow key={key} hover>
                                                <TableCell>
                                                    {item.existingEpisodeId ? (
                                                        <Tooltip title="In your library — open it">
                                                            <IconButton
                                                                onClick={() => navigate(`/podcast-episode/${item.existingEpisodeId}`)}
                                                                aria-label={`View ${item.title}`}
                                                            >
                                                                <CheckCircle color="success" />
                                                            </IconButton>
                                                        </Tooltip>
                                                    ) : item.importable ? (
                                                        <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                                            <IconButton
                                                                onClick={() => handleImport(item)}
                                                                disabled={importingKey !== null}
                                                                aria-label={`Import ${item.title}`}
                                                            >
                                                                {importingKey === key ? <CircularProgress size={20} /> : <Add />}
                                                            </IconButton>
                                                        </DemoWriteGuard>
                                                    ) : (
                                                        <Tooltip title="This feed item has no guid or audio link, so it can't be imported">
                                                            <span>—</span>
                                                        </Tooltip>
                                                    )}
                                                </TableCell>
                                                <TableCell sx={{ fontWeight: 500 }}>{item.title || 'Untitled episode'}</TableCell>
                                                <TableCell>{item.publishedAt ? new Date(item.publishedAt).toLocaleDateString() : 'N/A'}</TableCell>
                                                <TableCell>{formatDuration(item.durationSeconds)}</TableCell>
                                            </TableRow>
                                        );
                                    })}
                                </TableBody>
                            </Table>
                        </TableContainer>
                        <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <Typography variant="caption" color="text.secondary">
                                Showing {items.length} of {feedItemCount} episodes in the feed
                            </Typography>
                            {feedQuery.hasNextPage && (
                                <Button
                                    size="small"
                                    variant="contained"
                                    onClick={() => feedQuery.fetchNextPage()}
                                    disabled={feedQuery.isFetchingNextPage}
                                >
                                    {feedQuery.isFetchingNextPage ? 'Loading...' : `Load ${PAGE_SIZE} More`}
                                </Button>
                            )}
                        </Box>
                    </>
                )}
            </DialogContent>
            <DialogActions><Button onClick={onClose} sx={{ color: '#fcfafa' }}>Close</Button></DialogActions>
        </Dialog>
    );
}

export default FeedEpisodeBrowserDialog;
