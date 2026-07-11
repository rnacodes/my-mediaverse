import React, { useState, useRef } from 'react';
import {
    Paper, Box, Typography, Button, Alert, AlertTitle,
    CircularProgress, Card, CardContent, Accordion, AccordionSummary,
    AccordionDetails, List, ListItem, ListItemText, Divider
} from '@mui/material';
import { Podcasts, FileUpload, CheckCircle, ExpandMore } from '@mui/icons-material';
import { useImportPodcastOpml } from '@/hooks/useUpload';

const OUTLINED_BUTTON_SX = {
    borderColor: 'rgba(255, 255, 255, 0.7)',
    color: 'text.primary',
    '&:hover': {
        borderColor: 'rgba(255, 255, 255, 1)',
        backgroundColor: 'rgba(255, 255, 255, 0.05)',
    },
};

function PodcastOpmlImportSection() {
    const fileInputRef = useRef(null);
    const [file, setFile] = useState(null);
    const [error, setError] = useState('');
    const [result, setResult] = useState(null);

    const importMutation = useImportPodcastOpml();
    const importing = importMutation.isPending;

    const handleFileSelect = (event) => {
        const selectedFile = event.target.files[0];
        if (!selectedFile) return;

        const name = selectedFile.name.toLowerCase();
        if (name.endsWith('.opml') || name.endsWith('.xml')) {
            setFile(selectedFile);
            setError('');
            setResult(null);
        } else {
            setError('Please select an OPML file (.opml or .xml)');
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
                console.error('OPML import error:', err);
                setError(
                    err.response?.data?.error ||
                    err.response?.data?.details ||
                    err.message ||
                    'Failed to import OPML file. Please try again.'
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
            { label: 'Total', value: result.total, color: 'text.primary' },
            { label: 'Imported', value: result.imported, color: 'success.main' },
            { label: 'Skipped', value: result.skipped, color: 'text.secondary' },
            {
                label: 'Failed',
                value: result.failed,
                color: result.failed > 0 ? 'warning.main' : 'text.secondary',
            },
        ]
        : [];

    return (
        <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
            <Box sx={{ textAlign: 'center', mb: 3 }}>
                <Podcasts sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                    Import Podcasts from OPML
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                    Upload an OPML export from your podcast app to import your subscriptions.
                    Feeds are added as lightweight entries and enriched automatically later.
                </Typography>
            </Box>

            <Box sx={{ mb: 3 }}>
                <input
                    ref={fileInputRef}
                    id="opml-file-input"
                    type="file"
                    accept=".opml,.xml"
                    onChange={handleFileSelect}
                    style={{ display: 'none' }}
                />
                <label htmlFor="opml-file-input">
                    <Button
                        variant="outlined"
                        component="span"
                        startIcon={<FileUpload />}
                        fullWidth
                        sx={{ mb: 2, ...OUTLINED_BUTTON_SX }}
                    >
                        Choose OPML File
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
                <Button
                    variant="contained"
                    onClick={handleImport}
                    disabled={!file || importing}
                    startIcon={importing ? <CircularProgress size={20} /> : <Podcasts />}
                >
                    {importing ? 'Importing...' : 'Import Podcasts'}
                </Button>
                <Button
                    variant="outlined"
                    onClick={handleReset}
                    disabled={importing}
                    sx={OUTLINED_BUTTON_SX}
                >
                    Reset
                </Button>
            </Box>

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

                    <Box
                        sx={{
                            display: 'flex',
                            gap: 2,
                            flexWrap: 'wrap',
                            mb: result.failures?.length ? 3 : 0,
                        }}
                    >
                        {stats.map((stat) => (
                            <Card key={stat.label} sx={{ flex: '1 1 120px', minWidth: 100 }}>
                                <CardContent sx={{ textAlign: 'center', py: 2 }}>
                                    <Typography variant="h4" sx={{ fontWeight: 700, color: stat.color }}>
                                        {stat.value ?? 0}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        {stat.label}
                                    </Typography>
                                </CardContent>
                            </Card>
                        ))}
                    </Box>

                    {result.failures && result.failures.length > 0 && (
                        <Accordion>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography sx={{ fontWeight: 600, color: 'warning.main' }}>
                                    Failed Feeds ({result.failures.length})
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails>
                                <List dense>
                                    {result.failures.map((failure, index) => (
                                        <React.Fragment key={`${failure.title}::${failure.reason}`}>
                                            <ListItem>
                                                <ListItemText
                                                    primary={failure.title || 'Untitled feed'}
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

export default PodcastOpmlImportSection;
