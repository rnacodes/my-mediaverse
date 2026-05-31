
import React, { useState, useEffect } from 'react';
import { Container, Paper, Typography, Button, Box, Alert, CircularProgress, Grid, Chip, Switch, FormControlLabel, TextField, LinearProgress, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Collapse, IconButton } from '@mui/material';
import {
    Refresh as RefreshIcon,
    PlayArrow as PlayIcon,
    Stop as StopIcon,
    CheckCircle as CheckCircleIcon,
    Error as ErrorIcon,
    Schedule as ScheduleIcon,
    Code as CodeIcon,
    Description as DescriptionIcon,
    Storage as StorageIcon,
    ExpandMore as ExpandMoreIcon,
    ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import {
    useScriptRunnerHealth,
    useScriptJobs,
    useScriptJob,
    useRunNormalizeNotes,
    useRunNormalizeVault,
    useCancelScriptJob,
} from '@/hooks/useScriptExecution';

const TERMINAL_STATUSES = ['completed', 'failed', 'cancelled'];

const ScriptExecutionPage = () => {
    // Health (query-driven; preserve the original {status:'unavailable'} shape on error).
    // retry: false — the Python service is often simply not running; don't delay the error.
    const healthQuery = useScriptRunnerHealth({ retry: false });
    const healthStatus = healthQuery.data
        ?? (healthQuery.error ? { status: 'unavailable', error: healthQuery.error.message } : null);
    const healthLoading = healthQuery.isFetching;
    const refetchHealth = () => healthQuery.refetch();

    // Jobs list
    const jobsQuery = useScriptJobs(20);
    const jobs = jobsQuery.data?.jobs ?? [];
    const jobsLoading = jobsQuery.isFetching;
    const refetchJobs = () => jobsQuery.refetch();

    // Normalize Notes options
    const [notesRunning, setNotesRunning] = useState(false);
    const [notesDryRun, setNotesDryRun] = useState(true);
    const [notesVerbose, setNotesVerbose] = useState(false);

    // Normalize Vault options
    const [vaultRunning, setVaultRunning] = useState(false);
    const [vaultDryRun, setVaultDryRun] = useState(true);
    const [vaultVerbose, setVaultVerbose] = useState(false);
    const [vaultPath, setVaultPath] = useState('');
    const [vaultUseAI, setVaultUseAI] = useState(false);
    const [vaultBackup, setVaultBackup] = useState(true);

    // Expanded logs state
    const [expandedLogs, setExpandedLogs] = useState({});

    // Active job: poll via refetchInterval until a terminal status, then stop.
    const [activeJobId, setActiveJobId] = useState(null);
    const [activeJobProgress, setActiveJobProgress] = useState(null);
    const activeJobQuery = useScriptJob(activeJobId, {
        enabled: !!activeJobId,
        refetchInterval: (query) =>
            TERMINAL_STATUSES.includes(query.state.data?.status) ? false : 2000,
    });

    // Mutations
    const runNotesMutation = useRunNormalizeNotes();
    const runVaultMutation = useRunNormalizeVault();
    const cancelMutation = useCancelScriptJob();

    // Mirror polled job data into local progress + handle the terminal transition.
    // Keeping the side-effect in an effect (rather than the queryFn) is intentional
    // per the Phase 4 plan: setActiveJobId(null) leaves activeJobProgress showing the
    // final state, exactly as the old setInterval poller did.
    useEffect(() => {
        const job = activeJobQuery.data;
        if (!job) return;
        setActiveJobProgress(job);
        if (TERMINAL_STATUSES.includes(job.status)) {
            setActiveJobId(null);
            setNotesRunning(false);
            setVaultRunning(false);
            refetchJobs();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [activeJobQuery.data]);

    const handleRunNormalizeNotes = () => {
        setNotesRunning(true);
        runNotesMutation.mutate(
            { dryRun: notesDryRun, verbose: notesVerbose },
            {
                onSuccess: (job) => {
                    setActiveJobId(job.job_id);
                    setActiveJobProgress(job);
                },
                onError: (error) => {
                    alert('Failed to start job: ' + (error.response?.data?.error || error.message));
                    setNotesRunning(false);
                },
            }
        );
    };

    const handleRunNormalizeVault = () => {
        if (!vaultPath.trim()) {
            alert('Please enter a vault path');
            return;
        }
        setVaultRunning(true);
        runVaultMutation.mutate(
            {
                dryRun: vaultDryRun,
                verbose: vaultVerbose,
                vaultPath: vaultPath,
                useAI: vaultUseAI,
                backup: vaultBackup,
            },
            {
                onSuccess: (job) => {
                    setActiveJobId(job.job_id);
                    setActiveJobProgress(job);
                },
                onError: (error) => {
                    alert('Failed to start job: ' + (error.response?.data?.error || error.message));
                    setVaultRunning(false);
                },
            }
        );
    };

    const handleCancelJob = (jobId) => {
        cancelMutation.mutate(jobId, {
            onSuccess: () => {
                setActiveJobId(null);
                setNotesRunning(false);
                setVaultRunning(false);
                refetchJobs();
            },
            onError: (error) => {
                alert('Failed to cancel job: ' + error.message);
            },
        });
    };

    const toggleLogs = (jobId) => {
        setExpandedLogs(prev => ({
            ...prev,
            [jobId]: !prev[jobId]
        }));
    };

    const getStatusChip = (status) => {
        const statusConfig = {
            pending: { color: 'default', icon: <ScheduleIcon fontSize="small" /> },
            running: { color: 'primary', icon: <CircularProgress size={14} /> },
            completed: { color: 'success', icon: <CheckCircleIcon fontSize="small" /> },
            failed: { color: 'error', icon: <ErrorIcon fontSize="small" /> },
            cancelled: { color: 'warning', icon: <StopIcon fontSize="small" /> }
        };
        const config = statusConfig[status] || statusConfig.pending;
        return <Chip label={status} color={config.color} size="small" icon={config.icon} />;
    };

    const formatDateTime = (dateStr) => {
        if (!dateStr) return '-';
        return new Date(dateStr).toLocaleString();
    };

    const isRunning = notesRunning || vaultRunning;
    const isServiceHealthy = healthStatus?.status === 'healthy';

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Typography variant="h3" gutterBottom sx={{ mb: 4, fontWeight: 'bold' }}>
                Script Execution
            </Typography>

            {/* Health Status */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <CodeIcon sx={{ fontSize: 28 }} />
                        <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                            Script Runner Service
                        </Typography>
                    </Box>
                    <Button
                        variant="contained"
                        color="primary"
                        startIcon={healthLoading ? <CircularProgress size={16} /> : <RefreshIcon />}
                        onClick={refetchHealth}
                        disabled={healthLoading}
                        sx={{ color: '#fcfafa' }}
                    >
                        Refresh
                    </Button>
                </Box>

                {healthStatus && (
                    <Alert severity={isServiceHealthy ? 'success' : 'error'}>
                        <strong>Status:</strong> {healthStatus.status}
                        {healthStatus.database_connected !== undefined && (
                            <> | <strong>Database:</strong> {healthStatus.database_connected ? 'Connected' : 'Disconnected'}</>
                        )}
                        {healthStatus.error && <> | <strong>Error:</strong> {healthStatus.error}</>}
                    </Alert>
                )}

                {!healthStatus && !healthLoading && (
                    <Alert severity="warning">
                        Click &quot;Refresh&quot; to check service status. Make sure the Python FastAPI service is running on port 8001.
                    </Alert>
                )}
            </Paper>

            {/* Active Job Progress */}
            {activeJobProgress && (
                <Paper elevation={3} sx={{ p: 3, mb: 3, bgcolor: 'primary.dark' }}>
                    <Typography variant="h6" gutterBottom sx={{ color: 'white' }}>
                        Active Job: {activeJobProgress.script_type}
                    </Typography>
                    <Box sx={{ mb: 2 }}>
                        {getStatusChip(activeJobProgress.status)}
                    </Box>
                    {activeJobProgress.progress && (
                        <>
                            <LinearProgress
                                variant="determinate"
                                value={activeJobProgress.progress.total > 0
                                    ? (activeJobProgress.progress.processed / activeJobProgress.progress.total) * 100
                                    : 0}
                                sx={{ mb: 1, height: 10, borderRadius: 5 }}
                            />
                            <Typography variant="body2" sx={{ color: 'white' }}>
                                Processed: {activeJobProgress.progress.processed} / {activeJobProgress.progress.total}
                                {activeJobProgress.progress.current_item && (
                                    <> - {activeJobProgress.progress.current_item}</>
                                )}
                            </Typography>
                        </>
                    )}
                    {activeJobProgress.status === 'running' && (
                        <Button
                            variant="outlined"
                            color="error"
                            startIcon={<StopIcon />}
                            onClick={() => handleCancelJob(activeJobProgress.job_id)}
                            sx={{ mt: 2 }}
                        >
                            Cancel
                        </Button>
                    )}
                </Paper>
            )}

            {/* Normalize Notes Script */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <StorageIcon sx={{ fontSize: 32 }} />
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                        Standardize Notes (Database)
                    </Typography>
                </Box>

                <Alert severity="info" sx={{ mb: 2 }}>
                    Standardizes notes in the PostgreSQL database: fixes empty content, generates descriptions from content,
                    converts tags to lowercase, and ensures source URLs are valid.
                </Alert>

                <Grid container spacing={2} sx={{ mb: 2 }}>
                    <Grid item>
                        <FormControlLabel
                            control={
                                <Switch
                                    checked={notesDryRun}
                                    onChange={(e) => setNotesDryRun(e.target.checked)}
                                    disabled={isRunning}
                                />
                            }
                            label="Dry Run (preview only)"
                        />
                    </Grid>
                    <Grid item>
                        <FormControlLabel
                            control={
                                <Switch
                                    checked={notesVerbose}
                                    onChange={(e) => setNotesVerbose(e.target.checked)}
                                    disabled={isRunning}
                                />
                            }
                            label="Verbose Output"
                        />
                    </Grid>
                </Grid>

                <Button
                    variant="contained"
                    color="primary"
                    startIcon={notesRunning ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                    onClick={handleRunNormalizeNotes}
                    disabled={isRunning || !isServiceHealthy}
                >
                    {notesRunning ? 'Running...' : 'Run Standardize Notes'}
                </Button>
            </Paper>

            {/* Normalize Vault Script */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <DescriptionIcon sx={{ fontSize: 32 }} />
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                        Standardize Obsidian Vault (Files)
                    </Typography>
                </Box>

                <Alert severity="info" sx={{ mb: 2 }}>
                    Standardizes markdown files in an Obsidian vault: converts inline #tags to frontmatter,
                    converts tags to lowercase, adds titles from filenames, and generates descriptions.
                </Alert>

                <TextField
                    fullWidth
                    label="Vault Path"
                    value={vaultPath}
                    onChange={(e) => setVaultPath(e.target.value)}
                    placeholder="/path/to/obsidian/vault"
                    sx={{ mb: 2 }}
                    disabled={isRunning}
                    helperText="Full path to your Obsidian vault directory"
                />

                <Grid container spacing={2} sx={{ mb: 2 }}>
                    <Grid item xs={6} sm={3}>
                        <FormControlLabel
                            control={
                                <Switch
                                    checked={vaultDryRun}
                                    onChange={(e) => setVaultDryRun(e.target.checked)}
                                    disabled={isRunning}
                                />
                            }
                            label="Dry Run"
                        />
                    </Grid>
                    <Grid item xs={6} sm={3}>
                        <FormControlLabel
                            control={
                                <Switch
                                    checked={vaultVerbose}
                                    onChange={(e) => setVaultVerbose(e.target.checked)}
                                    disabled={isRunning}
                                />
                            }
                            label="Verbose"
                        />
                    </Grid>
                    <Grid item xs={6} sm={3}>
                        <FormControlLabel
                            control={
                                <Switch
                                    checked={vaultBackup}
                                    onChange={(e) => setVaultBackup(e.target.checked)}
                                    disabled={isRunning}
                                />
                            }
                            label="Create Backup"
                        />
                    </Grid>
                    <Grid item xs={6} sm={3}>
                        <FormControlLabel
                            control={
                                <Switch
                                    checked={vaultUseAI}
                                    onChange={(e) => setVaultUseAI(e.target.checked)}
                                    disabled={isRunning}
                                />
                            }
                            label="Use AI for Descriptions"
                        />
                    </Grid>
                </Grid>

                <Button
                    variant="contained"
                    color="primary"
                    startIcon={vaultRunning ? <CircularProgress size={20} color="inherit" /> : <PlayIcon />}
                    onClick={handleRunNormalizeVault}
                    disabled={isRunning || !isServiceHealthy || !vaultPath.trim()}
                    sx={{ color: '#fcfafa' }}
                >
                    {vaultRunning ? 'Running...' : 'Run Standardize Vault'}
                </Button>
            </Paper>

            {/* Job History */}
            <Paper elevation={3} sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                        Job History
                    </Typography>
                    <Button
                        variant="contained"
                        color="primary"
                        startIcon={jobsLoading ? <CircularProgress size={16} /> : <RefreshIcon />}
                        onClick={refetchJobs}
                        disabled={jobsLoading}
                        sx={{ color: '#fcfafa' }}
                    >
                        Refresh
                    </Button>
                </Box>

                <TableContainer sx={{ maxHeight: 500, overflow: 'auto' }}>
                    <Table size="small" stickyHeader>
                        <TableHead>
                            <TableRow>
                                <TableCell width={40} sx={{ fontSize: '0.8rem' }}></TableCell>
                                <TableCell sx={{ fontSize: '0.8rem', fontWeight: 'bold' }}>Script</TableCell>
                                <TableCell sx={{ fontSize: '0.8rem', fontWeight: 'bold' }}>Status</TableCell>
                                <TableCell sx={{ fontSize: '0.8rem', fontWeight: 'bold' }}>Started</TableCell>
                                <TableCell sx={{ fontSize: '0.8rem', fontWeight: 'bold' }}>Progress</TableCell>
                                <TableCell sx={{ fontSize: '0.8rem', fontWeight: 'bold' }}>Result</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {jobs.map((job) => (
                                <React.Fragment key={job.job_id}>
                                    <TableRow>
                                        <TableCell>
                                            <IconButton
                                                size="small"
                                                onClick={() => toggleLogs(job.job_id)}
                                            >
                                                {expandedLogs[job.job_id] ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                                            </IconButton>
                                        </TableCell>
                                        <TableCell sx={{ fontSize: '0.8rem' }}>{job.script_type}</TableCell>
                                        <TableCell sx={{ fontSize: '0.8rem' }}>{getStatusChip(job.status)}</TableCell>
                                        <TableCell sx={{ fontSize: '0.8rem' }}>{formatDateTime(job.started_at)}</TableCell>
                                        <TableCell sx={{ fontSize: '0.8rem' }}>
                                            {job.progress?.processed || 0} / {job.progress?.total || 0}
                                        </TableCell>
                                        <TableCell sx={{ fontSize: '0.8rem' }}>
                                            {job.error_message ? (
                                                <Typography color="error" variant="body2" sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                                    {job.error_message}
                                                </Typography>
                                            ) : job.result ? (
                                                <Typography color="success.main" variant="body2">
                                                    {job.result.modified !== undefined
                                                        ? `${job.result.modified} modified`
                                                        : job.result.description_generated !== undefined
                                                            ? `${job.result.description_generated} descriptions`
                                                            : 'Success'}
                                                </Typography>
                                            ) : '-'}
                                        </TableCell>
                                    </TableRow>
                                    <TableRow>
                                        <TableCell colSpan={6} sx={{ py: 0 }}>
                                            <Collapse in={expandedLogs[job.job_id]} timeout="auto" unmountOnExit>
                                                <Box sx={{ p: 2, bgcolor: 'background.default', borderRadius: 1, my: 1 }}>
                                                    <Typography variant="subtitle2" gutterBottom>
                                                        Job ID: {job.job_id}
                                                    </Typography>
                                                    {job.completed_at && (
                                                        <Typography variant="body2" color="text.secondary">
                                                            Completed: {formatDateTime(job.completed_at)}
                                                        </Typography>
                                                    )}
                                                    {job.result && (
                                                        <Box sx={{ mt: 1 }}>
                                                            <Typography variant="subtitle2">Result:</Typography>
                                                            <pre style={{ fontSize: '0.75rem', margin: 0, overflow: 'auto', maxHeight: 200 }}>
                                                                {JSON.stringify(job.result, null, 2)}
                                                            </pre>
                                                        </Box>
                                                    )}
                                                    {job.logs && job.logs.length > 0 && (
                                                        <Box sx={{ mt: 1 }}>
                                                            <Typography variant="subtitle2">Logs ({job.logs.length}):</Typography>
                                                            <Box sx={{ maxHeight: 150, overflow: 'auto', bgcolor: 'grey.900', p: 1, borderRadius: 1, fontSize: '0.75rem', fontFamily: 'monospace' }}>
                                                                {job.logs.slice(-20).map((log, idx) => (
                                                                    // eslint-disable-next-line react/no-array-index-key -- log lines are non-unique strings; positional key is intentional for an append-only log
                                                                    <div key={`log-${idx}`}>{log}</div>
                                                                ))}
                                                            </Box>
                                                        </Box>
                                                    )}
                                                </Box>
                                            </Collapse>
                                        </TableCell>
                                    </TableRow>
                                </React.Fragment>
                            ))}
                            {jobs.length === 0 && (
                                <TableRow>
                                    <TableCell colSpan={6} align="center">
                                        {jobsLoading ? 'Loading...' : 'No jobs found. Run a script to see job history.'}
                                    </TableCell>
                                </TableRow>
                            )}
                        </TableBody>
                    </Table>
                </TableContainer>
            </Paper>

            {/* Help */}
            <Alert severity="info" sx={{ mt: 3 }}>
                <Typography variant="body2">
                    <strong>Getting Started:</strong> Make sure the Python FastAPI service is running:
                </Typography>
                <Typography variant="body2" component="div" sx={{ mt: 1, fontFamily: 'monospace', bgcolor: 'background.default', p: 1, borderRadius: 1 }}>
                    cd scripts<br />
                    pip install -r requirements.txt<br />
                    python -m uvicorn api.main:app --port 8001
                </Typography>
            </Alert>
        </Container>
    );
};

export default ScriptExecutionPage;
