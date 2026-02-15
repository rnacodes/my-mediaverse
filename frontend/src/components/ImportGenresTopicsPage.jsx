import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent, TextField,
    Alert, CircularProgress, Paper, Divider, Chip,
    Tab, Tabs, List, ListItem, ListItemText
} from '@mui/material';
import {
    ArrowBack, CloudUpload, FileDownload, CheckCircle,
    Error, Info
} from '@mui/icons-material';
import { importGenresFromCsv, importTopicsFromCsv } from '../api/topicGenreService';

function ImportGenresTopicsPage() {
    const [activeTab, setActiveTab] = useState(0); // 0 for Genres, 1 for Topics
    const [file, setFile] = useState(null);
    const [importing, setImporting] = useState(false);
    const [importResults, setImportResults] = useState(null);
    const navigate = useNavigate();

    const handleTabChange = (event, newValue) => {
        setActiveTab(newValue);
        setFile(null);
        setImportResults(null);
    };

    const handleFileUpload = (event) => {
        const uploadedFile = event.target.files[0];
        if (uploadedFile && uploadedFile.name.toLowerCase().endsWith('.csv')) {
            setFile(uploadedFile);
            setImportResults(null);
        } else {
            alert('Please upload a valid CSV file.');
        }
    };

    const handleImport = async () => {
        if (!file) {
            alert('Please select a CSV file to import.');
            return;
        }

        setImporting(true);
        try {
            let result;
            if (activeTab === 0) {
                // Import Genres
                const response = await importGenresFromCsv(file);
                result = response.data;
            } else {
                // Import Topics
                const response = await importTopicsFromCsv(file);
                result = response.data;
            }
            setImportResults(result);
        } catch (error) {
            console.error('Import error:', error);
            const errorMessage = error.response?.data?.message || error.message || 'Import failed';
            alert(`Import failed: ${errorMessage}`);
        } finally {
            setImporting(false);
        }
    };

    const downloadSampleCsv = () => {
        const csvContent = "Name\nAction\nAdventure\nComedy\nDrama\nFantasy\nHorror\nMystery\nRomance\nScience Fiction\nThriller";
        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = activeTab === 0 ? 'sample-genres.csv' : 'sample-topics.csv';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
    };

    const getImportType = () => activeTab === 0 ? 'Genres' : 'Topics';

    return (
        <Box sx={{ p: 3, maxWidth: 1200, margin: '0 auto' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                <Button
                    startIcon={<ArrowBack />}
                    onClick={() => navigate(-1)}
                    sx={{ mr: 2 }}
                >
                    Back
                </Button>
                <Typography variant="h4">
                    Import {getImportType()}
                </Typography>
            </Box>

            <Card sx={{ mb: 3 }}>
                <CardContent>
                    <Tabs
                        value={activeTab}
                        onChange={handleTabChange}
                        sx={{
                            mb: 3,
                            '& .MuiTab-root': {
                                color: 'rgba(255, 255, 255, 0.7)',
                                '&.Mui-selected': {
                                    color: 'white'
                                }
                            },
                            '& .MuiTabs-indicator': {
                                backgroundColor: 'white'
                            }
                        }}
                    >
                        <Tab label="Import Genres" />
                        <Tab label="Import Topics" />
                    </Tabs>

                    <Alert severity="info" sx={{ mb: 3 }}>
                        <Typography variant="body2">
                            <strong>CSV Format Requirements:</strong>
                        </Typography>
                        <Typography variant="body2">
                            • The CSV file must have a header row with a "Name" column
                        </Typography>
                        <Typography variant="body2">
                            • Each row should contain one {activeTab === 0 ? 'genre' : 'topic'} name
                        </Typography>
                        <Typography variant="body2">
                            • Duplicate names will be skipped automatically
                        </Typography>
                        <Typography variant="body2">
                            • GUIDs will be automatically generated for new entries
                        </Typography>
                    </Alert>

                    <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
                        <Button
                            variant="contained"
                            startIcon={<FileDownload />}
                            onClick={downloadSampleCsv}
                            sx={{
                                backgroundColor: 'white',
                                color: 'black',
                                '&:hover': {
                                    backgroundColor: 'rgba(255, 255, 255, 0.9)'
                                }
                            }}
                        >
                            Download Sample CSV
                        </Button>

                        <Button
                            variant="contained"
                            component="label"
                            startIcon={<CloudUpload />}
                            sx={{
                                backgroundColor: 'white',
                                color: 'black',
                                '&:hover': {
                                    backgroundColor: 'rgba(255, 255, 255, 0.9)'
                                }
                            }}
                        >
                            Choose CSV File
                            <input
                                type="file"
                                accept=".csv"
                                hidden
                                onChange={handleFileUpload}
                            />
                        </Button>
                    </Box>

                    {file && (
                        <Alert severity="success" sx={{ mb: 3 }}>
                            <Typography variant="body2">
                                File selected: <strong>{file.name}</strong>
                            </Typography>
                        </Alert>
                    )}

                    <Divider sx={{ my: 3 }} />

                    <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                        <Button
                            variant="contained"
                            onClick={handleImport}
                            disabled={!file || importing}
                            startIcon={importing ? <CircularProgress size={20} color="inherit" /> : <CloudUpload />}
                            sx={{
                                backgroundColor: 'white',
                                color: 'black',
                                '&:hover': {
                                    backgroundColor: 'rgba(255, 255, 255, 0.9)'
                                },
                                '&.Mui-disabled': {
                                    backgroundColor: 'rgba(255, 255, 255, 0.5)',
                                    color: 'rgba(0, 0, 0, 0.5)'
                                }
                            }}
                        >
                            {importing ? `Importing ${getImportType()}...` : `Import ${getImportType()}`}
                        </Button>
                    </Box>
                </CardContent>
            </Card>

            {importResults && (
                <Card>
                    <CardContent>
                        <Typography variant="h5" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <CheckCircle color="success" />
                            Import Results
                        </Typography>

                        <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
                            <Chip
                                label={`Total Processed: ${importResults.totalProcessed}`}
                                color="default"
                                variant="outlined"
                            />
                            <Chip
                                icon={<CheckCircle />}
                                label={`Imported: ${importResults.successCount}`}
                                color="success"
                            />
                            <Chip
                                icon={<Info />}
                                label={`Skipped: ${importResults.skippedCount}`}
                                color="warning"
                            />
                            {importResults.errorCount > 0 && (
                                <Chip
                                    icon={<Error />}
                                    label={`Errors: ${importResults.errorCount}`}
                                    color="error"
                                />
                            )}
                        </Box>

                        {importResults.imported && importResults.imported.length > 0 && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="h6" gutterBottom>
                                    Successfully Imported ({importResults.imported.length}):
                                </Typography>
                                <Paper sx={{ maxHeight: 200, overflow: 'auto', p: 2 }}>
                                    <List dense>
                                        {importResults.imported.map((item, index) => (
                                            <ListItem key={index}>
                                                <ListItemText
                                                    primary={item.name}
                                                    secondary={`ID: ${item.id}`}
                                                />
                                            </ListItem>
                                        ))}
                                    </List>
                                </Paper>
                            </Box>
                        )}

                        {importResults.skipped && importResults.skipped.length > 0 && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="h6" gutterBottom>
                                    Skipped ({importResults.skipped.length}):
                                </Typography>
                                <Paper sx={{ maxHeight: 200, overflow: 'auto', p: 2 }}>
                                    <List dense>
                                        {importResults.skipped.map((message, index) => (
                                            <ListItem key={index}>
                                                <ListItemText primary={message} />
                                            </ListItem>
                                        ))}
                                    </List>
                                </Paper>
                            </Box>
                        )}

                        {importResults.errors && importResults.errors.length > 0 && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="h6" gutterBottom color="error">
                                    Errors ({importResults.errors.length}):
                                </Typography>
                                <Paper sx={{ maxHeight: 200, overflow: 'auto', p: 2, bgcolor: '#ffebee' }}>
                                    <List dense>
                                        {importResults.errors.map((error, index) => (
                                            <ListItem key={index}>
                                                <ListItemText primary={error} />
                                            </ListItem>
                                        ))}
                                    </List>
                                </Paper>
                            </Box>
                        )}

                        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                            <Button
                                variant="outlined"
                                onClick={() => {
                                    setFile(null);
                                    setImportResults(null);
                                }}
                            >
                                Import Another File
                            </Button>
                            <Button
                                variant="contained"
                                onClick={() => navigate(-1)}
                            >
                                Done
                            </Button>
                        </Box>
                    </CardContent>
                </Card>
            )}
        </Box>
    );
}

export default ImportGenresTopicsPage;


