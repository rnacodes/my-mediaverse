import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, Button, Card, CardContent, Alert, CircularProgress, Paper, Chip, Dialog, DialogTitle, DialogContent, DialogActions, List, ListItem, ListItemText, IconButton } from '@mui/material';
import { ArrowBack, CloudUpload, FileDownload, CheckCircle, Error, Delete, Preview } from '@mui/icons-material';
import Papa from 'papaparse';
import { importMixlists } from '@/api/mixlistService';

function ImportMixlistPage() {
    const [file, setFile] = useState(null);
    const [csvData, setCsvData] = useState([]);
    const [importing, setImporting] = useState(false);
    const [importResults, setImportResults] = useState(null);
    const [previewDialogOpen, setPreviewDialogOpen] = useState(false);
    const [selectedRow, setSelectedRow] = useState(null);
    const navigate = useNavigate();

    const handleFileUpload = (event) => {
        const uploadedFile = event.target.files[0];
        if (uploadedFile && uploadedFile.type === 'text/csv') {
            setFile(uploadedFile);
            parseCSV(uploadedFile);
        } else {
            alert('Please upload a valid CSV file.');
        }
    };

    const parseCSV = (file) => {
        Papa.parse(file, {
            header: true,
            skipEmptyLines: true,
            complete: (results) => {
                if (results.data && results.data.length > 0) {
                    setCsvData(results.data);
                } else {
                    alert('CSV file appears to be empty or invalid.');
                }
            },
            error: (error) => {
                console.error('CSV parsing error:', error);
                alert('Error parsing CSV file. Please check the file format.');
            }
        });
    };

    const handleImport = async () => {
        if (!csvData || csvData.length === 0) {
            alert('No data to import.');
            return;
        }

        setImporting(true);
        try {
            // Transform CSV data to match the expected DTO format
            const importData = csvData.map(row => ({
                name: row.Name || row.name || '',
                description: row.Description || row.description || '',
                thumbnail: row.Thumbnail || row.thumbnail || '',
                mediaItemIds: row.MediaItemIds || row.mediaItemIds || '',
                mediaItemTitles: row.MediaItemTitles || row.mediaItemTitles || '',
                mediaItemTypes: row.MediaItemTypes || row.mediaItemTypes || ''
            }));

            const response = await importMixlists(importData);
            setImportResults(response.data);
        } catch (error) {
            console.error('Import error:', error);
            const message = error.response?.data?.error || error.message || 'Import failed';
            alert(`Import failed: ${message}`);
        } finally {
            setImporting(false);
        }
    };

    const handlePreviewRow = (row, index) => {
        setSelectedRow({ ...row, index });
        setPreviewDialogOpen(true);
    };

    const handleRemoveRow = (index) => {
        setCsvData(prev => prev.filter((_, i) => i !== index));
    };

    const handleDownloadTemplate = () => {
        const templateData = [
            {
                Name: 'Sample Mixlist',
                Description: 'A sample mixlist description',
                Thumbnail: 'https://example.com/thumbnail.jpg',
                MediaItemIds: 'guid1;guid2;guid3',
                MediaItemTitles: 'Title 1;Title 2;Title 3',
                MediaItemTypes: 'Podcast;Book;Movie'
            }
        ];

        const csv = Papa.unparse(templateData);
        const blob = new Blob([csv], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'mixlist-import-template.csv';
        a.click();
        window.URL.revokeObjectURL(url);
    };

    const resetForm = () => {
        setFile(null);
        setCsvData([]);
        setImportResults(null);
    };

    return (
        <Box sx={{ p: 3, minHeight: '100vh' }}>
            {/* Header */}
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}>
                <IconButton onClick={() => navigate('/search?searchMode=mixlists')} sx={{ mr: 2 }}>
                    <ArrowBack />
                </IconButton>
                <Typography variant="h3" component="h1" sx={{ 
                    fontSize: '32px',
                    fontWeight: 'bold'
                }}>
                    Import Mixlists
                </Typography>
            </Box>

            {/* Instructions */}
            <Card sx={{ mb: 4 }}>
                <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                        How to Import Mixlists
                    </Typography>
                    <Typography variant="body1" paragraph>
                        Upload a CSV file containing mixlist data. The CSV should include the following columns:
                    </Typography>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                        {['Name', 'Description', 'Thumbnail', 'MediaItemIds', 'MediaItemTitles', 'MediaItemTypes'].map((header) => (
                            <Chip key={header} label={header} color="primary" variant="outlined" />
                        ))}
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                        • <strong>Name:</strong> Required. The name of the mixlist<br/>
                        • <strong>Description:</strong> Optional. Description of the mixlist<br/>
                        • <strong>Thumbnail:</strong> Optional. URL to the mixlist thumbnail<br/>
                        • <strong>MediaItemIds:</strong> Optional. Semicolon-separated list of media item GUIDs<br/>
                        • <strong>MediaItemTitles:</strong> Optional. Semicolon-separated list of media item titles<br/>
                        • <strong>MediaItemTypes:</strong> Optional. Semicolon-separated list of media item types
                    </Typography>
                </CardContent>
            </Card>

            {/* File Upload */}
            <Card sx={{ mb: 4 }}>
                <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                        Upload CSV File
                    </Typography>
                    
                    <Box sx={{ display: 'flex', gap: 2, mb: 3, alignItems: 'center' }}>
                        <Button
                            variant="contained"
                            component="label"
                            startIcon={<CloudUpload />}
                            sx={{ px: 3, py: 1.5 }}
                        >
                            Choose CSV File
                            <input
                                type="file"
                                hidden
                                accept=".csv"
                                onChange={handleFileUpload}
                            />
                        </Button>
                        
                        <Button
                            variant="outlined"
                            onClick={handleDownloadTemplate}
                            startIcon={<FileDownload />}
                            sx={{ px: 3, py: 1.5 }}
                        >
                            Download Template
                        </Button>
                    </Box>

                    {file && (
                        <Alert severity="info" sx={{ mb: 2 }}>
                            File selected: {file.name} ({csvData.length} rows)
                        </Alert>
                    )}
                </CardContent>
            </Card>

            {/* Data Preview */}
            {csvData.length > 0 && (
                <Card sx={{ mb: 4 }}>
                    <CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                            Data Preview ({csvData.length} rows)
                        </Typography>
                        
                        <Paper sx={{ maxHeight: 400, overflow: 'auto' }}>
                            <List>
                                {csvData.map((row, index) => (
                                    <ListItem
                                        key={`row-${row.Name || row.name || row.Description || index}`}
                                        divider
                                        secondaryAction={
                                            <Box sx={{ display: 'flex', gap: 1 }}>
                                                <IconButton
                                                    size="small"
                                                    onClick={() => handlePreviewRow(row, index)}
                                                >
                                                    <Preview />
                                                </IconButton>
                                                <IconButton
                                                    size="small"
                                                    color="error"
                                                    onClick={() => handleRemoveRow(index)}
                                                >
                                                    <Delete />
                                                </IconButton>
                                            </Box>
                                        }
                                    >
                                        <ListItemText
                                            primary={row.Name || row.name || 'Unnamed Mixlist'}
                                            secondary={`${row.Description || row.description || 'No description'} • ${row.MediaItemIds || row.mediaItemIds ? 'Has media items' : 'No media items'}`}
                                        />
                                    </ListItem>
                                ))}
                            </List>
                        </Paper>
                    </CardContent>
                </Card>
            )}

            {/* Import Button */}
            {csvData.length > 0 && (
                <Box sx={{ display: 'flex', gap: 2, mb: 4 }}>
                    <Button
                        variant="contained"
                        onClick={handleImport}
                        disabled={importing}
                        startIcon={importing ? <CircularProgress size={20} /> : <CloudUpload />}
                        sx={{ px: 4, py: 1.5, fontSize: '16px' }}
                    >
                        {importing ? 'Importing...' : `Import ${csvData.length} Mixlist${csvData.length !== 1 ? 's' : ''}`}
                    </Button>
                    
                    <Button
                        variant="outlined"
                        onClick={resetForm}
                        sx={{ px: 4, py: 1.5, fontSize: '16px' }}
                    >
                        Reset
                    </Button>
                </Box>
            )}

            {/* Import Results */}
            {importResults && (
                <Card sx={{ mb: 4 }}>
                    <CardContent>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                            Import Results
                        </Typography>
                        
                        <Alert severity={importResults.ErrorCount > 0 ? 'warning' : 'success'} sx={{ mb: 2 }}>
                            Successfully imported {importResults.SuccessCount} mixlist{importResults.SuccessCount !== 1 ? 's' : ''}
                            {importResults.ErrorCount > 0 && ` with ${importResults.ErrorCount} error${importResults.ErrorCount !== 1 ? 's' : ''}`}
                        </Alert>

                        {importResults.ImportedMixlists && importResults.ImportedMixlists.length > 0 && (
                            <Box sx={{ mb: 3 }}>
                                <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold' }}>
                                    Successfully Imported:
                                </Typography>
                                <List dense>
                                    {importResults.ImportedMixlists.map((mixlist) => (
                                        <ListItem key={`imp-${mixlist.Id}`}>
                                            <ListItemText
                                                primary={mixlist.Name}
                                                secondary={`ID: ${mixlist.Id} • ${mixlist.MediaItemCount} media items`}
                                            />
                                            <CheckCircle color="success" />
                                        </ListItem>
                                    ))}
                                </List>
                            </Box>
                        )}

                        {importResults.Errors && importResults.Errors.length > 0 && (
                            <Box>
                                <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', color: 'error.main' }}>
                                    Errors:
                                </Typography>
                                <List dense>
                                    {importResults.Errors.map((error) => (
                                        <ListItem key={`err-${error}`}>
                                            <ListItemText
                                                primary={error}
                                                sx={{ color: 'error.main' }}
                                            />
                                            <Error color="error" />
                                        </ListItem>
                                    ))}
                                </List>
                            </Box>
                        )}

                        <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
                            <Button
                                variant="contained"
                                onClick={() => navigate('/search?searchMode=mixlists')}
                                sx={{ px: 3, py: 1.5 }}
                            >
                                View All Mixlists
                            </Button>
                            
                            <Button
                                variant="outlined"
                                onClick={resetForm}
                                sx={{ px: 3, py: 1.5 }}
                            >
                                Import Another File
                            </Button>
                        </Box>
                    </CardContent>
                </Card>
            )}

            {/* Preview Dialog */}
            <Dialog
                open={previewDialogOpen}
                onClose={() => setPreviewDialogOpen(false)}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle>
                    Row Preview - Row {selectedRow?.index + 1}
                </DialogTitle>
                <DialogContent>
                    {selectedRow && (
                        <Box>
                            <Typography variant="h6" gutterBottom>
                                {selectedRow.Name || selectedRow.name || 'Unnamed Mixlist'}
                            </Typography>
                            
                            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary">Description:</Typography>
                                    <Typography>{selectedRow.Description || selectedRow.description || 'No description'}</Typography>
                                </Box>
                                
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary">Thumbnail:</Typography>
                                    <Typography>{selectedRow.Thumbnail || selectedRow.thumbnail || 'No thumbnail'}</Typography>
                                </Box>
                                
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary">Media Item IDs:</Typography>
                                    <Typography>{selectedRow.MediaItemIds || selectedRow.mediaItemIds || 'No media items'}</Typography>
                                </Box>
                                
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary">Media Item Titles:</Typography>
                                    <Typography>{selectedRow.MediaItemTitles || selectedRow.mediaItemTitles || 'No media items'}</Typography>
                                </Box>
                                
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary">Media Item Types:</Typography>
                                    <Typography>{selectedRow.MediaItemTypes || selectedRow.mediaItemTypes || 'No media items'}</Typography>
                                </Box>
                            </Box>
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setPreviewDialogOpen(false)}>Close</Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}

export default ImportMixlistPage;

