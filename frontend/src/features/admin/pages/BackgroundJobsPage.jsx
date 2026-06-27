import React, { useState } from 'react';
import { Container, Paper, Typography, Button, Box, Alert, CircularProgress, Card, CardContent, Grid, Chip, Slider, Accordion, AccordionSummary, AccordionDetails } from '@mui/material';
import {
    Refresh as RefreshIcon,
    PlayArrow as PlayIcon,
    ExpandMore as ExpandMoreIcon,
    CheckCircle as CheckCircleIcon,
    Schedule as ScheduleIcon,
    MenuBook as BookIcon,
    Info as InfoIcon,
    Movie as MovieIcon,
    Podcasts as PodcastsIcon,
} from '@mui/icons-material';
import {
    useBookEnrichmentStatus, useRunBookEnrichment, useRunBookEnrichmentAll,
    useMovieTvEnrichmentStatus, useRunMovieEnrichment, useRunTvShowEnrichment, useRunMovieTvEnrichmentAll,
    usePodcastEnrichmentStatus, useRunPodcastEnrichment, useRunPodcastEnrichmentAll,
} from '@/hooks/useBackgroundJobs';

// Pull the API error message out of an axios error the same way across sections.
const errMsg = (error, fallback) =>
    error ? (error.response?.data?.error || error.message || fallback) : null;

const BackgroundJobsPage = () => {
    // ==========================================
    // Book Enrichment — status query + run mutations.
    // Each mutation's onSuccess invalidates the status query, so the "Refresh"
    // round-trip after a run happens automatically via the cache.
    // ==========================================
    const bookStatusQuery = useBookEnrichmentStatus();
    const enrichmentStatus = bookStatusQuery.data ?? null;
    const statusLoading = bookStatusQuery.isFetching;
    const statusError = errMsg(bookStatusQuery.error, 'Failed to fetch enrichment status');
    const fetchBookStatus = () => bookStatusQuery.refetch();

    const bookBatchMutation = useRunBookEnrichment();
    const bookAllMutation = useRunBookEnrichmentAll();
    const runningBatch = bookBatchMutation.isPending;
    const batchResult = bookBatchMutation.data ?? null;
    const batchError = errMsg(bookBatchMutation.error, 'Failed to run enrichment batch');
    const runningAll = bookAllMutation.isPending;
    const runAllResult = bookAllMutation.data ?? null;
    const runAllError = errMsg(bookAllMutation.error, 'Failed to run full enrichment');

    const [batchSize, setBatchSize] = useState(50);
    const [delayMs, setDelayMs] = useState(1000);
    const [maxBooks, setMaxBooks] = useState(500);
    const [pauseBetweenBatches, setPauseBetweenBatches] = useState(30);

    const handleRunBookBatch = () => {
        bookBatchMutation.mutate({ batchSize, delayBetweenCallsMs: delayMs });
    };

    const handleRunBookAll = () => {
        if (!window.confirm(`This will process up to ${maxBooks} books. This may take a while. Continue?`)) return;
        bookAllMutation.mutate({ batchSize, delayBetweenCallsMs: delayMs, maxBooks, pauseBetweenBatchesSeconds: pauseBetweenBatches });
    };

    const isBookRunning = runningBatch || runningAll;

    // ==========================================
    // Movie/TV Enrichment
    // ==========================================
    const movieTvStatusQuery = useMovieTvEnrichmentStatus();
    const movieTvStatus = movieTvStatusQuery.data ?? null;
    const movieTvStatusLoading = movieTvStatusQuery.isFetching;
    const movieTvStatusError = errMsg(movieTvStatusQuery.error, 'Failed to fetch Movie/TV status');
    const fetchMovieTvStatus = () => movieTvStatusQuery.refetch();

    const movieMutation = useRunMovieEnrichment();
    const tvShowMutation = useRunTvShowEnrichment();
    const movieTvAllMutation = useRunMovieTvEnrichmentAll();
    const runningMovies = movieMutation.isPending;
    const movieResult = movieMutation.data ?? null;
    const movieError = errMsg(movieMutation.error, 'Failed to run movie enrichment');
    const runningTvShows = tvShowMutation.isPending;
    const tvShowResult = tvShowMutation.data ?? null;
    const tvShowError = errMsg(tvShowMutation.error, 'Failed to run TV show enrichment');
    const runningMovieTvAll = movieTvAllMutation.isPending;
    const movieTvAllResult = movieTvAllMutation.data ?? null;
    const movieTvAllError = errMsg(movieTvAllMutation.error, 'Failed to run full Movie/TV enrichment');

    const [movieTvBatchSize, setMovieTvBatchSize] = useState(50);
    const [movieTvDelayMs, setMovieTvDelayMs] = useState(500);
    const [maxMovies, setMaxMovies] = useState(500);
    const [maxTvShows, setMaxTvShows] = useState(500);
    const [movieTvPause, setMovieTvPause] = useState(30);

    const handleRunMovies = () => {
        movieMutation.mutate({ batchSize: movieTvBatchSize, delayBetweenCallsMs: movieTvDelayMs });
    };

    const handleRunTvShows = () => {
        tvShowMutation.mutate({ batchSize: movieTvBatchSize, delayBetweenCallsMs: movieTvDelayMs });
    };

    const handleRunMovieTvAll = () => {
        if (!window.confirm(`This will process up to ${maxMovies} movies and ${maxTvShows} TV shows. Continue?`)) return;
        movieTvAllMutation.mutate({
            batchSize: movieTvBatchSize, delayBetweenCallsMs: movieTvDelayMs,
            maxMovies, maxTvShows, pauseBetweenBatchesSeconds: movieTvPause,
        });
    };

    const isMovieTvRunning = runningMovies || runningTvShows || runningMovieTvAll;

    // ==========================================
    // Podcast Enrichment
    // ==========================================
    const podcastStatusQuery = usePodcastEnrichmentStatus();
    const podcastStatus = podcastStatusQuery.data ?? null;
    const podcastStatusLoading = podcastStatusQuery.isFetching;
    const podcastStatusError = errMsg(podcastStatusQuery.error, 'Failed to fetch podcast status');
    const fetchPodcastStatus = () => podcastStatusQuery.refetch();

    const podcastBatchMutation = useRunPodcastEnrichment();
    const podcastAllMutation = useRunPodcastEnrichmentAll();
    const runningPodcastBatch = podcastBatchMutation.isPending;
    const podcastBatchResult = podcastBatchMutation.data ?? null;
    const podcastBatchError = errMsg(podcastBatchMutation.error, 'Failed to run podcast batch');
    const runningPodcastAll = podcastAllMutation.isPending;
    const podcastAllResult = podcastAllMutation.data ?? null;
    const podcastAllError = errMsg(podcastAllMutation.error, 'Failed to run full podcast enrichment');

    const [podcastBatchSize, setPodcastBatchSize] = useState(25);
    const [podcastDelayMs, setPodcastDelayMs] = useState(1500);
    const [maxPodcasts, setMaxPodcasts] = useState(100);
    const [podcastPause, setPodcastPause] = useState(60);

    const handleRunPodcastBatch = () => {
        podcastBatchMutation.mutate({ batchSize: podcastBatchSize, delayBetweenCallsMs: podcastDelayMs });
    };

    const handleRunPodcastAll = () => {
        if (!window.confirm(`This will process up to ${maxPodcasts} podcasts. ListenNotes has strict rate limits. Continue?`)) return;
        podcastAllMutation.mutate({
            batchSize: podcastBatchSize, delayBetweenCallsMs: podcastDelayMs,
            maxPodcasts, pauseBetweenBatchesSeconds: podcastPause,
        });
    };

    const isPodcastRunning = runningPodcastBatch || runningPodcastAll;

    // ==========================================
    // Shared helper: render error list
    // ==========================================
    const renderErrors = (errors) => {
        if (!errors || errors.length === 0) return null;
        return (
            <Alert severity="warning" sx={{ mt: 2 }}>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                    Errors ({errors.length}):
                </Typography>
                {errors.slice(0, 5).map((err) => (
                    <Typography key={`err-${err}`} variant="caption" display="block">{err}</Typography>
                ))}
                {errors.length > 5 && (
                    <Typography variant="caption">...and {errors.length - 5} more</Typography>
                )}
            </Alert>
        );
    };

    // ==========================================
    // Shared helper: render stat box
    // ==========================================
    const StatBox = ({ value, label, color = '#fcfafa' }) => (
        <Box sx={{ textAlign: 'center', p: 1, bgcolor: 'background.paper', borderRadius: 1 }}>
            <Typography variant="h4" sx={{ fontWeight: 'bold', color }}>{value}</Typography>
            <Typography variant="caption">{label}</Typography>
        </Box>
    );

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Typography variant="h3" gutterBottom sx={{ mb: 4, fontWeight: 'bold' }}>
                Background Jobs
            </Typography>
            <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                        Enrichment Services Coming Soon
                    </Typography>

            {/* ==========================================
                BOOK DESCRIPTION ENRICHMENT
               ========================================== */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                    <BookIcon sx={{ fontSize: 32 }} />
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                        Book Description Enrichment
                    </Typography>
                </Box>

                <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 3 }}>
                    Fetches book descriptions from Google Books for books that have an ISBN but no description.
                    HTML tags are automatically stripped from descriptions. Respects API rate limits.
                </Alert>

                <Card variant="outlined" sx={{ mb: 3 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                            <Typography variant="h6">Current Status</Typography>
                            <Button variant="contained" color="primary" size="small"
                                startIcon={statusLoading ? <CircularProgress size={16} /> : <RefreshIcon />}
                                onClick={fetchBookStatus} disabled={statusLoading || isBookRunning}
                                sx={{ color: '#fcfafa' }}>
                                Refresh
                            </Button>
                        </Box>
                        {statusError && <Alert severity="error" sx={{ mb: 2 }}>{statusError}</Alert>}
                        {enrichmentStatus && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                                <StatBox value={enrichmentStatus.booksNeedingEnrichment} label="Books Need Descriptions" />
                                {enrichmentStatus.booksNeedingEnrichment === 0 ? (
                                    <Chip icon={<CheckCircleIcon />} label="All books enriched!" color="success" sx={{ fontWeight: 'bold' }} />
                                ) : (
                                    <Chip icon={<ScheduleIcon />} label="Enrichment available" color="warning" sx={{ fontWeight: 'bold' }} />
                                )}
                            </Box>
                        )}
                    </CardContent>
                </Card>

                <Accordion defaultExpanded sx={{ mb: 3 }}>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                        <Typography variant="h6">Configuration</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Grid container spacing={3}>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Batch Size: <strong>{batchSize}</strong> books per batch</Typography>
                                <Slider value={batchSize} onChange={(e, v) => setBatchSize(v)} min={10} max={200} step={10}
                                    marks={[{ value: 10, label: '10' }, { value: 50, label: '50' }, { value: 100, label: '100' }, { value: 200, label: '200' }]}
                                    disabled={isBookRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>API Delay: <strong>{delayMs}ms</strong> between calls</Typography>
                                <Slider value={delayMs} onChange={(e, v) => setDelayMs(v)} min={500} max={3000} step={100}
                                    marks={[{ value: 500, label: '0.5s' }, { value: 1000, label: '1s' }, { value: 2000, label: '2s' }, { value: 3000, label: '3s' }]}
                                    disabled={isBookRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Max Books (Run All): <strong>{maxBooks}</strong></Typography>
                                <Slider value={maxBooks} onChange={(e, v) => setMaxBooks(v)} min={100} max={2000} step={100}
                                    marks={[{ value: 100, label: '100' }, { value: 500, label: '500' }, { value: 1000, label: '1000' }, { value: 2000, label: '2000' }]}
                                    disabled={isBookRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Pause Between Batches: <strong>{pauseBetweenBatches}s</strong></Typography>
                                <Slider value={pauseBetweenBatches} onChange={(e, v) => setPauseBetweenBatches(v)} min={10} max={120} step={10}
                                    marks={[{ value: 10, label: '10s' }, { value: 30, label: '30s' }, { value: 60, label: '60s' }, { value: 120, label: '120s' }]}
                                    disabled={isBookRunning} />
                            </Grid>
                        </Grid>
                    </AccordionDetails>
                </Accordion>

                <Grid container spacing={2} sx={{ mb: 3 }}>
                    <Grid item xs={12} md={6}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run Single Batch</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Process {batchSize} books and return immediately.</Typography>
                            <Button variant="contained" color="primary" fullWidth
                                startIcon={runningBatch ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunBookBatch} disabled={isBookRunning || enrichmentStatus?.booksNeedingEnrichment === 0}>
                                {runningBatch ? 'Running...' : `Run Batch (${batchSize} books)`}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                    <Grid item xs={12} md={6}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run All (Bulk)</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Process up to {maxBooks} books in multiple batches.</Typography>
                            <Button variant="contained" color="secondary" fullWidth
                                startIcon={runningAll ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunBookAll} disabled={isBookRunning || enrichmentStatus?.booksNeedingEnrichment === 0}>
                                {runningAll ? 'Running (this may take a while)...' : `Run All (up to ${maxBooks} books)`}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                </Grid>

                {batchError && <Alert severity="error" sx={{ mb: 2 }}><strong>Batch Run Failed:</strong> {batchError}</Alert>}
                {batchResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>Batch Run Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={3}><StatBox value={batchResult.totalProcessed} label="Processed" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={batchResult.enrichedCount} label="Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={batchResult.failedCount} label="Failed" color="error.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={batchResult.skippedCount} label="Skipped" color="text.secondary" /></Grid>
                        </Grid>
                        {renderErrors(batchResult.errors)}
                    </CardContent></Card>
                )}

                {runAllError && <Alert severity="error" sx={{ mb: 2 }}><strong>Full Run Failed:</strong> {runAllError}</Alert>}
                {runAllResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>Full Run Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={2}><StatBox value={runAllResult.totalProcessed} label="Processed" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={runAllResult.totalEnriched} label="Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={runAllResult.totalFailed} label="Failed" color="error.main" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={runAllResult.batchesRun} label="Batches" color="info.main" /></Grid>
                            <Grid item xs={12} sm={4}><StatBox value={runAllResult.remainingBooks} label="Still Remaining" color="warning.main" /></Grid>
                        </Grid>
                        {renderErrors(runAllResult.errors)}
                    </CardContent></Card>
                )}

                <Alert severity="info" icon={<ScheduleIcon />}>
                    <Typography variant="body2">
                        <strong>Scheduled Execution:</strong> For automated enrichment, set up a cron job to call <code>/api/bookenrichment/run-all</code> on a schedule.
                    </Typography>
                </Alert>
            </Paper>

            {/* ==========================================
                MOVIE/TV TMDB ENRICHMENT
               ========================================== */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                    <MovieIcon sx={{ fontSize: 32 }} />
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                        Movie/TV TMDB Enrichment
                    </Typography>
                </Box>

                <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 3 }}>
                    Fetches metadata from TMDB for movies and TV shows that don&apos;t have a TMDB ID.
                    Enriches titles, descriptions, posters, and other metadata.
                </Alert>

                <Card variant="outlined" sx={{ mb: 3 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                            <Typography variant="h6">Current Status</Typography>
                            <Button variant="contained" color="primary" size="small"
                                startIcon={movieTvStatusLoading ? <CircularProgress size={16} /> : <RefreshIcon />}
                                onClick={fetchMovieTvStatus} disabled={movieTvStatusLoading || isMovieTvRunning}
                                sx={{ color: '#fcfafa' }}>
                                Refresh
                            </Button>
                        </Box>
                        {movieTvStatusError && <Alert severity="error" sx={{ mb: 2 }}>{movieTvStatusError}</Alert>}
                        {movieTvStatus && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
                                <StatBox value={movieTvStatus.moviesNeedingEnrichment} label="Movies Need Enrichment" />
                                <StatBox value={movieTvStatus.tvShowsNeedingEnrichment} label="TV Shows Need Enrichment" />
                                {(movieTvStatus.moviesNeedingEnrichment === 0 && movieTvStatus.tvShowsNeedingEnrichment === 0) ? (
                                    <Chip icon={<CheckCircleIcon />} label="All enriched!" color="success" sx={{ fontWeight: 'bold' }} />
                                ) : (
                                    <Chip icon={<ScheduleIcon />} label="Enrichment available" color="warning" sx={{ fontWeight: 'bold' }} />
                                )}
                            </Box>
                        )}
                    </CardContent>
                </Card>

                <Accordion sx={{ mb: 3 }}>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                        <Typography variant="h6">Configuration</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Grid container spacing={3}>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Batch Size: <strong>{movieTvBatchSize}</strong> items per batch</Typography>
                                <Slider value={movieTvBatchSize} onChange={(e, v) => setMovieTvBatchSize(v)} min={10} max={200} step={10}
                                    marks={[{ value: 10, label: '10' }, { value: 50, label: '50' }, { value: 100, label: '100' }, { value: 200, label: '200' }]}
                                    disabled={isMovieTvRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>API Delay: <strong>{movieTvDelayMs}ms</strong> between calls</Typography>
                                <Slider value={movieTvDelayMs} onChange={(e, v) => setMovieTvDelayMs(v)} min={100} max={5000} step={100}
                                    marks={[{ value: 100, label: '0.1s' }, { value: 500, label: '0.5s' }, { value: 2000, label: '2s' }, { value: 5000, label: '5s' }]}
                                    disabled={isMovieTvRunning} />
                            </Grid>
                            <Grid item xs={12} md={4}>
                                <Typography gutterBottom>Max Movies: <strong>{maxMovies}</strong></Typography>
                                <Slider value={maxMovies} onChange={(e, v) => setMaxMovies(v)} min={0} max={5000} step={100}
                                    marks={[{ value: 0, label: '0' }, { value: 500, label: '500' }, { value: 2500, label: '2500' }, { value: 5000, label: '5000' }]}
                                    disabled={isMovieTvRunning} />
                            </Grid>
                            <Grid item xs={12} md={4}>
                                <Typography gutterBottom>Max TV Shows: <strong>{maxTvShows}</strong></Typography>
                                <Slider value={maxTvShows} onChange={(e, v) => setMaxTvShows(v)} min={0} max={5000} step={100}
                                    marks={[{ value: 0, label: '0' }, { value: 500, label: '500' }, { value: 2500, label: '2500' }, { value: 5000, label: '5000' }]}
                                    disabled={isMovieTvRunning} />
                            </Grid>
                            <Grid item xs={12} md={4}>
                                <Typography gutterBottom>Pause Between Batches: <strong>{movieTvPause}s</strong></Typography>
                                <Slider value={movieTvPause} onChange={(e, v) => setMovieTvPause(v)} min={10} max={120} step={10}
                                    marks={[{ value: 10, label: '10s' }, { value: 30, label: '30s' }, { value: 60, label: '60s' }, { value: 120, label: '120s' }]}
                                    disabled={isMovieTvRunning} />
                            </Grid>
                        </Grid>
                    </AccordionDetails>
                </Accordion>

                <Grid container spacing={2} sx={{ mb: 3 }}>
                    <Grid item xs={12} md={4}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run Movies</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Enrich {movieTvBatchSize} movies from TMDB.</Typography>
                            <Button variant="contained" color="primary" fullWidth
                                startIcon={runningMovies ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunMovies} disabled={isMovieTvRunning || movieTvStatus?.moviesNeedingEnrichment === 0}>
                                {runningMovies ? 'Running...' : 'Run Movies'}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                    <Grid item xs={12} md={4}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run TV Shows</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Enrich {movieTvBatchSize} TV shows from TMDB.</Typography>
                            <Button variant="contained" color="primary" fullWidth
                                startIcon={runningTvShows ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunTvShows} disabled={isMovieTvRunning || movieTvStatus?.tvShowsNeedingEnrichment === 0}>
                                {runningTvShows ? 'Running...' : 'Run TV Shows'}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                    <Grid item xs={12} md={4}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run All (Bulk)</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Process all movies and TV shows.</Typography>
                            <Button variant="contained" color="secondary" fullWidth
                                startIcon={runningMovieTvAll ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunMovieTvAll} disabled={isMovieTvRunning}>
                                {runningMovieTvAll ? 'Running...' : 'Run All'}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                </Grid>

                {movieError && <Alert severity="error" sx={{ mb: 2 }}><strong>Movie Enrichment Failed:</strong> {movieError}</Alert>}
                {movieResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>Movie Enrichment Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={3}><StatBox value={movieResult.totalProcessed} label="Processed" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={movieResult.enrichedCount} label="Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={movieResult.notFoundCount} label="Not Found" color="warning.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={movieResult.failedCount} label="Failed" color="error.main" /></Grid>
                        </Grid>
                        {renderErrors(movieResult.errors)}
                    </CardContent></Card>
                )}

                {tvShowError && <Alert severity="error" sx={{ mb: 2 }}><strong>TV Show Enrichment Failed:</strong> {tvShowError}</Alert>}
                {tvShowResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>TV Show Enrichment Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={3}><StatBox value={tvShowResult.totalProcessed} label="Processed" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={tvShowResult.enrichedCount} label="Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={tvShowResult.notFoundCount} label="Not Found" color="warning.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={tvShowResult.failedCount} label="Failed" color="error.main" /></Grid>
                        </Grid>
                        {renderErrors(tvShowResult.errors)}
                    </CardContent></Card>
                )}

                {movieTvAllError && <Alert severity="error" sx={{ mb: 2 }}><strong>Full Run Failed:</strong> {movieTvAllError}</Alert>}
                {movieTvAllResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>Full Movie/TV Enrichment Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={2}><StatBox value={movieTvAllResult.totalMoviesProcessed} label="Movies Processed" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={movieTvAllResult.totalMoviesEnriched} label="Movies Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={movieTvAllResult.totalTvShowsProcessed} label="TV Processed" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={movieTvAllResult.totalTvShowsEnriched} label="TV Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={movieTvAllResult.remainingMovies} label="Movies Left" color="warning.main" /></Grid>
                            <Grid item xs={6} sm={2}><StatBox value={movieTvAllResult.remainingTvShows} label="TV Left" color="warning.main" /></Grid>
                        </Grid>
                        {renderErrors(movieTvAllResult.errors)}
                    </CardContent></Card>
                )}

                <Alert severity="info" icon={<ScheduleIcon />}>
                    <Typography variant="body2">
                        <strong>Scheduled Execution:</strong> Call <code>/api/movietvenrichment/run-all</code> on a schedule for automated TMDB enrichment.
                    </Typography>
                </Alert>
            </Paper>

            {/* ==========================================
                PODCAST LISTENNOTES ENRICHMENT
               ========================================== */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                    <PodcastsIcon sx={{ fontSize: 32 }} />
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                        Podcast ListenNotes Enrichment
                    </Typography>
                </Box>

                <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 3 }}>
                    Fetches podcast metadata from ListenNotes for podcast series without an external ID.
                    ListenNotes has strict rate limits — use conservative settings.
                </Alert>

                <Card variant="outlined" sx={{ mb: 3 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                            <Typography variant="h6">Current Status</Typography>
                            <Button variant="contained" color="primary" size="small"
                                startIcon={podcastStatusLoading ? <CircularProgress size={16} /> : <RefreshIcon />}
                                onClick={fetchPodcastStatus} disabled={podcastStatusLoading || isPodcastRunning}
                                sx={{ color: '#fcfafa' }}>
                                Refresh
                            </Button>
                        </Box>
                        {podcastStatusError && <Alert severity="error" sx={{ mb: 2 }}>{podcastStatusError}</Alert>}
                        {podcastStatus && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                                <StatBox value={podcastStatus.podcastsNeedingEnrichment} label="Podcasts Need Enrichment" />
                                {podcastStatus.podcastsNeedingEnrichment === 0 ? (
                                    <Chip icon={<CheckCircleIcon />} label="All podcasts enriched!" color="success" sx={{ fontWeight: 'bold' }} />
                                ) : (
                                    <Chip icon={<ScheduleIcon />} label="Enrichment available" color="warning" sx={{ fontWeight: 'bold' }} />
                                )}
                            </Box>
                        )}
                    </CardContent>
                </Card>

                <Accordion sx={{ mb: 3 }}>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                        <Typography variant="h6">Configuration</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Grid container spacing={3}>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Batch Size: <strong>{podcastBatchSize}</strong> podcasts per batch</Typography>
                                <Slider value={podcastBatchSize} onChange={(e, v) => setPodcastBatchSize(v)} min={5} max={50} step={5}
                                    marks={[{ value: 5, label: '5' }, { value: 25, label: '25' }, { value: 50, label: '50' }]}
                                    disabled={isPodcastRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>API Delay: <strong>{podcastDelayMs}ms</strong> between calls</Typography>
                                <Slider value={podcastDelayMs} onChange={(e, v) => setPodcastDelayMs(v)} min={500} max={10000} step={500}
                                    marks={[{ value: 500, label: '0.5s' }, { value: 1500, label: '1.5s' }, { value: 5000, label: '5s' }, { value: 10000, label: '10s' }]}
                                    disabled={isPodcastRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Max Podcasts (Run All): <strong>{maxPodcasts}</strong></Typography>
                                <Slider value={maxPodcasts} onChange={(e, v) => setMaxPodcasts(v)} min={10} max={500} step={10}
                                    marks={[{ value: 10, label: '10' }, { value: 100, label: '100' }, { value: 250, label: '250' }, { value: 500, label: '500' }]}
                                    disabled={isPodcastRunning} />
                            </Grid>
                            <Grid item xs={12} md={6}>
                                <Typography gutterBottom>Pause Between Batches: <strong>{podcastPause}s</strong></Typography>
                                <Slider value={podcastPause} onChange={(e, v) => setPodcastPause(v)} min={30} max={180} step={10}
                                    marks={[{ value: 30, label: '30s' }, { value: 60, label: '60s' }, { value: 120, label: '120s' }, { value: 180, label: '180s' }]}
                                    disabled={isPodcastRunning} />
                            </Grid>
                        </Grid>
                    </AccordionDetails>
                </Accordion>

                <Grid container spacing={2} sx={{ mb: 3 }}>
                    <Grid item xs={12} md={6}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run Single Batch</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Process {podcastBatchSize} podcasts.</Typography>
                            <Button variant="contained" color="primary" fullWidth
                                startIcon={runningPodcastBatch ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunPodcastBatch} disabled={isPodcastRunning || podcastStatus?.podcastsNeedingEnrichment === 0}>
                                {runningPodcastBatch ? 'Running...' : `Run Batch (${podcastBatchSize} podcasts)`}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                    <Grid item xs={12} md={6}>
                        <Card variant="outlined"><CardContent>
                            <Typography variant="h6" gutterBottom>Run All (Bulk)</Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Process up to {maxPodcasts} podcasts.</Typography>
                            <Button variant="contained" color="secondary" fullWidth
                                startIcon={runningPodcastAll ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                                onClick={handleRunPodcastAll} disabled={isPodcastRunning || podcastStatus?.podcastsNeedingEnrichment === 0}>
                                {runningPodcastAll ? 'Running...' : `Run All (up to ${maxPodcasts})`}
                            </Button>
                        </CardContent></Card>
                    </Grid>
                </Grid>

                {podcastBatchError && <Alert severity="error" sx={{ mb: 2 }}><strong>Batch Failed:</strong> {podcastBatchError}</Alert>}
                {podcastBatchResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>Podcast Batch Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={3}><StatBox value={podcastBatchResult.totalProcessed} label="Processed" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={podcastBatchResult.enrichedCount} label="Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={podcastBatchResult.notFoundCount} label="Not Found" color="warning.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={podcastBatchResult.failedCount} label="Failed" color="error.main" /></Grid>
                        </Grid>
                        {renderErrors(podcastBatchResult.errors)}
                    </CardContent></Card>
                )}

                {podcastAllError && <Alert severity="error" sx={{ mb: 2 }}><strong>Full Run Failed:</strong> {podcastAllError}</Alert>}
                {podcastAllResult && (
                    <Card variant="outlined" sx={{ mb: 2, bgcolor: 'success.dark' }}><CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>Full Podcast Enrichment Complete</Typography>
                        <Grid container spacing={2}>
                            <Grid item xs={6} sm={3}><StatBox value={podcastAllResult.totalProcessed} label="Processed" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={podcastAllResult.totalEnriched} label="Enriched" color="success.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={podcastAllResult.batchesRun} label="Batches" color="info.main" /></Grid>
                            <Grid item xs={6} sm={3}><StatBox value={podcastAllResult.remainingPodcasts} label="Remaining" color="warning.main" /></Grid>
                        </Grid>
                        {renderErrors(podcastAllResult.errors)}
                    </CardContent></Card>
                )}

                <Alert severity="info" icon={<ScheduleIcon />}>
                    <Typography variant="body2">
                        <strong>Rate Limits:</strong> ListenNotes has strict rate limits. Use higher delays (1.5s+) and smaller batches to avoid throttling.
                    </Typography>
                </Alert>
            </Paper>

        </Container>
    );
};

export default BackgroundJobsPage;
