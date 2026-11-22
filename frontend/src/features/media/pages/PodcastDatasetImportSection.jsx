import React, { useState, useRef } from 'react';
import {
    Paper, Box, Typography, Button, Alert, AlertTitle,
    CircularProgress, Accordion, AccordionSummary,
    AccordionDetails, List, ListItem, ListItemText, Divider
} from '@mui/material';
import { Podcasts, FileUpload, CheckCircle, ExpandMore } from '@mui/icons-material';
import { useImportPodcastDataset } from '@/hooks/useUpload';
import AttributionBadge from '@/shared/AttributionBadge';
import StatTiles from '@/features/imports/pages/WebsiteImport/StatTiles';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import DemoDisabledArea from '@/features/demo/DemoDisabledArea';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';

const OUTLINED_BUTTON_SX = {
    borderColor: 'rgba(255, 255, 255, 0.7)',
    color: 'text.primary',
    '&:hover': {
        borderColor: 'rgba(255, 255, 255, 1)',
        backgroundColor: 'rgba(255, 255, 255, 0.05)',
    },
};

function PodcastDatasetImportSection() {
    const fileInputRef = useRef(null);
    const [file, setFile] = useState(null);
    const [error, setError] = useState('');
    const [result, setResult] = useState(null);

    const importMutation = useImportPodcastDataset();
    const importing = importMutation.isPending;

    const handleFileSelect = (event) => {
        const selectedFile = event.target.files[0];
        if (!selectedFile) return;

        const name = selectedFile.name.toLowerCase();
        if (name.endsWith('.csv')) {
            setFile(selectedFile);
            setError('');
            setResult(null);
        } else {
            setError('Please select a Listen Notes CSV export (.csv)');
            setFile(null);
        }
    };

    const handleImport = () => {
        if (!file) {
            setError('Please select a file first');
            return;
        }

        setError('');
        setResult(null);

        importMutation.mutate(file, {
            onSuccess: (data) => setResult(data),
            onError: (err) => {
                // A failed run still answers with the result body, which carries errorMessage.
                setError(
                    err.response?.data?.errorMessage ||
                    err.response?.data?.error ||
                    err.response?.data?.details ||
                    err.message ||
                    'Failed to import the dataset file. Please try again.'
                );
            },
        });
    };

    const handleReset = () => {
        setFile(null);
        setError('');
        setResult(null);
        importMutation.reset();
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const stats = result
        ? [
            { label: 'Total', value: result.totalProcessed ?? 0, color: 'text.primary' },
            { label: 'New shows', value: result.seriesCreatedCount ?? 0, color: 'success.main' },
            ...(result.fileKind === 'episodes'
                ? [{ label: 'New episodes', value: result.episodesCreatedCount ?? 0, color: 'success.main' }]
                : []),
            { label: 'Skipped', value: result.skippedCount ?? 0, color: 'text.secondary' },
            {
                label: 'Failed',
                value: result.failedCount ?? 0,
                color: result.failedCount > 0 ? 'warning.main' : 'text.secondary',
            },
        ]
        : [];

    return (
        <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
            <Box sx={{ textAlign: 'center', mb: 3 }}>
                <Podcasts sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                    Import Podcasts from a Listen Notes Dataset
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                    Upload a purchased Listen Notes export: the podcasts CSV (one show per row) or the
                    episodes CSV (one episode per row). Any set of the export&apos;s columns works, as long
                    as there is a <code>title</code> column.
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                    Shows come in unsubscribed. The next enrichment run looks each one up and, once it
                    finds the show&apos;s feed, replaces the Listen Notes details with the feed&apos;s. A show
                    or episode already in the library is skipped.
                </Typography>
                <AttributionBadge provider="listennotes" sx={{ display: 'flex', justifyContent: 'center' }} />
            </Box>

            <DemoDisabledArea title={DEMO_IMPORT_BLOCKED}>
                <Box sx={{ mb: 3 }}>
                    <input
                        ref={fileInputRef}
                        id="dataset-file-input"
                        type="file"
                        accept=".csv"
                        onChange={handleFileSelect}
                        style={{ display: 'none' }}
                    />
                    <label htmlFor="dataset-file-input">
                        <Button
                            variant="outlined"
                            component="span"
                            startIcon={<FileUpload />}
                            fullWidth
                            sx={{ mb: 2, ...OUTLINED_BUTTON_SX }}
                        >
                            Choose CSV File
                        </Button>
                    </label>

                    {file && (
                        <Alert severity="info" sx={{ mb: 2 }}>
                            <AlertTitle>File Selected</AlertTitle>
                            {file.name} ({(file.size / 1024).toFixed(1)} KB)
                        </Alert>
                    )}
                </Box>

                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                    <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                        <Button
                            variant="contained"
                            onClick={handleImport}
                            disabled={!file || importing}
                            startIcon={importing ? <CircularProgress size={20} /> : <Podcasts />}
                        >
                            {importing ? 'Importing...' : 'Import Dataset'}
                        </Button>
                    </DemoWriteGuard>
                    <Button
                        variant="outlined"
                        onClick={handleReset}
                        disabled={importing}
                        sx={OUTLINED_BUTTON_SX}
                    >
                        Reset
                    </Button>
                </Box>
            </DemoDisabledArea>

            {error && (
                <Alert severity="error" sx={{ mt: 3 }}>
                    <AlertTitle>Import Error</AlertTitle>
                    {error}
                </Alert>
            )}

            {result && (
                <Box sx={{ mt: 3 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                        <CheckCircle sx={{ color: 'success.main', mr: 1 }} />
                        <Typography variant="h6" sx={{ fontWeight: 600 }}>
                            Import Complete
                        </Typography>
                    </Box>

                    <StatTiles stats={stats} />

                    {result.warningMessage && (
                        <Alert severity="warning" sx={{ mb: 2 }}>
                            {result.warningMessage}
                        </Alert>
                    )}

                    {result.createdCount > 0 && (
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                            {result.reindexTriggered
                                ? 'The new items are being added to search.'
                                : 'The new items will appear in search after the next reindex.'}
                        </Typography>
                    )}

                    {result.failures && result.failures.length > 0 && (
                        <Accordion>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography sx={{ fontWeight: 600, color: 'warning.main' }}>
                                    Failed Rows ({result.failures.length})
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails>
                                <List dense>
                                    {result.failures.map((failure, index) => (
                                        <React.Fragment key={`${failure.title}::${failure.reason}`}>
                                            <ListItem>
                                                <ListItemText
                                                    primary={failure.title || 'Untitled row'}
                                                    secondary={failure.reason}
                                                />
                                            </ListItem>
                                            {index < result.failures.length - 1 && <Divider />}
                                        </React.Fragment>
                                    ))}
                                </List>
                            </AccordionDetails>
                        </Accordion>
                    )}
                </Box>
            )}
        </Paper>
    );
}

export default PodcastDatasetImportSection;
