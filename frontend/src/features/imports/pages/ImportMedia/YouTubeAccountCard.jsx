import React, { useEffect, useState } from 'react';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import {
    Box, Card, CardContent, Typography, Button, Chip, Alert, CircularProgress,
    Table, TableBody, TableCell, TableHead, TableRow
} from '@mui/material';
import {
    useYouTubeAuthStatus, useStartYouTubeAuth, useDisconnectYouTube,
    useMyYouTubePlaylists, useImportMyYouTubePlaylist
} from '@/hooks/useYoutube';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';

// What the API callback reports when Google sends the browser back (?youtube=...).
const RETURN_MESSAGES = {
    connected: { message: 'YouTube account connected.', severity: 'success' },
    denied: { message: 'YouTube access was not granted.', severity: 'info' },
    error: { message: 'Connecting YouTube failed. Please try again.', severity: 'error' },
};

const PRIVACY_COLORS = { private: 'warning', unlisted: 'info', public: 'success' };

const formatDate = (value) => new Date(value).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });

const errorText = (err, fallback) => err?.response?.data?.error || err?.message || fallback;

// Connects the owner's YouTube account (Google sign-in) and lists their playlists, private ones
// included, for import. Hidden when the server has no OAuth configuration (the demo).
function YouTubeAccountCard({ onSnackbar, goToUrl = (url) => window.location.assign(url) }) {
    const [searchParams, setSearchParams] = useSearchParams();
    const [importingId, setImportingId] = useState(null);

    const { data: status, isLoading: statusLoading } = useYouTubeAuthStatus();
    const connected = Boolean(status?.configured && status?.connected);
    const playlistsQuery = useMyYouTubePlaylists({ enabled: connected });
    const startAuth = useStartYouTubeAuth();
    const disconnect = useDisconnectYouTube();
    const importPlaylist = useImportMyYouTubePlaylist();

    const returnOutcome = searchParams.get('youtube');
    useEffect(() => {
        if (!returnOutcome) return;
        const message = RETURN_MESSAGES[returnOutcome];
        if (message) onSnackbar?.({ open: true, ...message });
        const next = new URLSearchParams(searchParams);
        next.delete('youtube');
        setSearchParams(next, { replace: true });
    }, [returnOutcome]); // eslint-disable-line react-hooks/exhaustive-deps

    if (statusLoading || !status?.configured) return null;

    const handleConnect = async () => {
        try {
            goToUrl(await startAuth.mutateAsync());
        } catch (err) {
            onSnackbar?.({ open: true, message: errorText(err, 'Could not start the YouTube sign-in.'), severity: 'error' });
        }
    };

    const handleDisconnect = async () => {
        try {
            await disconnect.mutateAsync();
            onSnackbar?.({ open: true, message: 'YouTube account disconnected.', severity: 'info' });
        } catch (err) {
            onSnackbar?.({ open: true, message: errorText(err, 'Could not disconnect YouTube.'), severity: 'error' });
        }
    };

    const handleImport = async (playlist) => {
        setImportingId(playlist.playlistId);
        try {
            const { sync } = await importPlaylist.mutateAsync(playlist.playlistId);
            onSnackbar?.({
                open: true,
                severity: 'success',
                message: `"${playlist.title}" imported: ${sync.createdCount} new videos, ${sync.linkedCount} already in your library.`,
            });
        } catch (err) {
            onSnackbar?.({ open: true, message: errorText(err, `Importing "${playlist.title}" failed.`), severity: 'error' });
        } finally {
            setImportingId(null);
        }
    };

    const reconnectOverdue = status.reconnectBy && new Date(status.reconnectBy) < new Date();

    return (
        <Card sx={{ mb: 3 }} data-testid="youtube-account-card">
            <CardContent>
                <Typography variant="h6" gutterBottom>Your YouTube account</Typography>

                {!connected ? (
                    <Box>
                        <Typography variant="body2" sx={{ mb: 2 }}>
                            Connect your account to list all of your playlists, private ones included.
                        </Typography>
                        <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                            <WhiteOutlineButton onClick={handleConnect} disabled={startAuth.isPending}>
                                {startAuth.isPending ? 'Opening Google...' : 'Connect YouTube'}
                            </WhiteOutlineButton>
                        </DemoWriteGuard>
                    </Box>
                ) : (
                    <Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap', mb: 2 }}>
                            <Typography variant="body2">
                                Connected{status.channelTitle ? ` as ${status.channelTitle}` : ''}
                                {status.reconnectBy ? ` · reconnect by ${formatDate(status.reconnectBy)}` : ''}
                            </Typography>
                            <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                <WhiteOutlineButton size="small" onClick={handleConnect} disabled={startAuth.isPending}>
                                    Reconnect
                                </WhiteOutlineButton>
                            </DemoWriteGuard>
                            <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                <WhiteOutlineButton size="small" onClick={handleDisconnect} disabled={disconnect.isPending}>
                                    Disconnect
                                </WhiteOutlineButton>
                            </DemoWriteGuard>
                        </Box>

                        {reconnectOverdue && (
                            <Alert severity="warning" sx={{ mb: 2 }}>
                                Google ends this connection seven days after you connect. Reconnect to keep private playlists in sync.
                            </Alert>
                        )}

                        {playlistsQuery.isLoading && <CircularProgress size={24} />}
                        {playlistsQuery.isError && (
                            <Alert severity="error">
                                {errorText(playlistsQuery.error, 'Could not list your playlists.')}
                            </Alert>
                        )}
                        {playlistsQuery.data && (
                            playlistsQuery.data.length === 0 ? (
                                <Typography variant="body2">This account has no playlists.</Typography>
                            ) : (
                                <Table size="small" aria-label="My YouTube playlists">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Playlist</TableCell>
                                            <TableCell>Privacy</TableCell>
                                            <TableCell align="right">Videos</TableCell>
                                            <TableCell align="right" />
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {playlistsQuery.data.map((playlist) => (
                                            <TableRow key={playlist.playlistId}>
                                                <TableCell>{playlist.title}</TableCell>
                                                <TableCell>
                                                    <Chip
                                                        size="small"
                                                        label={playlist.privacyStatus ?? 'unknown'}
                                                        color={PRIVACY_COLORS[playlist.privacyStatus] ?? 'default'}
                                                    />
                                                </TableCell>
                                                <TableCell align="right">{playlist.itemCount ?? '—'}</TableCell>
                                                <TableCell align="right">
                                                    {playlist.importedId ? (
                                                        <Button
                                                            size="small"
                                                            component={RouterLink}
                                                            to={`/youtube-playlist/${playlist.importedId}`}
                                                            sx={{ color: 'inherit', textTransform: 'none' }}
                                                        >
                                                            In library
                                                        </Button>
                                                    ) : (
                                                        <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                                            <WhiteOutlineButton
                                                                size="small"
                                                                onClick={() => handleImport(playlist)}
                                                                disabled={importingId !== null}
                                                            >
                                                                {importingId === playlist.playlistId ? 'Importing...' : 'Import'}
                                                            </WhiteOutlineButton>
                                                        </DemoWriteGuard>
                                                    )}
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            )
                        )}
                    </Box>
                )}
            </CardContent>
        </Card>
    );
}

export default YouTubeAccountCard;
