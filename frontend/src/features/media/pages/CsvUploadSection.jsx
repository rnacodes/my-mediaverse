import React, { useState, useRef } from 'react';
import {
    Paper, Box, Typography, Button, Alert, AlertTitle, CircularProgress,
    List, ListItem, ListItemButton, ListItemText, Divider, TextField, MenuItem,
} from '@mui/material';
import { CloudUpload, FileUpload, Upload } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useUploadCsv } from '@/hooks/useUpload';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import DemoDisabledArea from '@/features/demo/DemoDisabledArea';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';
import ImportResultPanel from '@/shared/ImportResultPanel';
import { CSV_MEDIA_TYPES, SHARED_COLUMNS, TYPE_COLUMNS } from './csvColumns';

const OUTLINED_BUTTON_SX = {
    borderColor: 'rgba(255, 255, 255, 0.7)',
    color: 'text.primary',
    '&:hover': {
        borderColor: 'rgba(255, 255, 255, 1)',
        backgroundColor: 'rgba(255, 255, 255, 0.05)',
    },
};

const RESULT_STATS = [
    { label: 'Created', key: 'createdCount', color: 'success.main' },
    { label: 'Skipped', key: 'skippedCount' },
    { label: 'Failed', key: 'failedCount', color: (value) => (value > 0 ? 'warning.main' : 'text.secondary') },
];

function ColumnList({ columns }) {
    return (
        <Box component="ul" sx={{ mt: 0, mb: 2, pl: 3 }}>
            {columns.map((column) => (
                <Typography component="li" variant="body2" key={column.name}>
                    <strong>{column.name}</strong>: {column.description}
                </Typography>
            ))}
        </Box>
    );
}

function CsvUploadSection() {
    const navigate = useNavigate();
    const fileInputRef = useRef(null);
    const [mediaType, setMediaType] = useState(CSV_MEDIA_TYPES[0].value);
    const [file, setFile] = useState(null);
    const [uploadResult, setUploadResult] = useState(null);
    const [error, setError] = useState('');

    const uploadCsvMutation = useUploadCsv();
    const uploading = uploadCsvMutation.isPending;

    const typeLabel = CSV_MEDIA_TYPES.find((type) => type.value === mediaType).label;
    const typeColumns = TYPE_COLUMNS[mediaType];

    const handleFileSelect = (event) => {
        const selectedFile = event.target.files[0];
        if (!selectedFile) return;

        if (selectedFile.name.toLowerCase().endsWith('.csv')) {
            setFile(selectedFile);
            setError('');
            setUploadResult(null);
        } else {
            setError('Please select a CSV file');
            setFile(null);
        }
    };

    const handleUpload = () => {
        if (!file) {
            setError('Please select a file first');
            return;
        }

        setError('');
        setUploadResult(null);

        uploadCsvMutation.mutate(
            { file, mediaType },
            {
                onSuccess: (data) => setUploadResult(data),
                onError: (err) => {
                    // A crashed upload still answers with the result body, which carries errorMessage.
                    setError(
                        err.response?.data?.errorMessage ||
                        err.response?.data?.error ||
                        err.message ||
                        'Failed to upload file. Please try again.'
                    );
                },
            }
        );
    };

    const resetUpload = () => {
        setFile(null);
        setUploadResult(null);
        setError('');
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const skipped = uploadResult?.skipped ?? [];
    const importedItems = uploadResult?.importedItems ?? [];

    return (
        <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
            <Box sx={{ textAlign: 'center', mb: 3 }}>
                <Upload sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                    Upload a CSV File
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Pick a media type, then upload a CSV file with a header row. Only <strong>Title</strong> is
                    required (a video also needs <strong>Link</strong>). Every other column is optional, and
                    columns that are not listed below are ignored.
                </Typography>
            </Box>

            <TextField
                select
                fullWidth
                label="Media type"
                value={mediaType}
                onChange={(event) => setMediaType(event.target.value)}
                disabled={uploading}
                sx={{ mb: 2 }}
            >
                {CSV_MEDIA_TYPES.map((type) => (
                    <MenuItem key={type.value} value={type.value}>{type.label}</MenuItem>
                ))}
            </TextField>

            <DemoDisabledArea title={DEMO_IMPORT_BLOCKED}>
                <Box sx={{ mb: 3 }}>
                    <input
                        ref={fileInputRef}
                        id="csv-file-input"
                        type="file"
                        accept=".csv"
                        onChange={handleFileSelect}
                        style={{ display: 'none' }}
                    />
                    <label htmlFor="csv-file-input">
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

                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', mb: 3 }}>
                    <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                        <Button
                            variant="contained"
                            onClick={handleUpload}
                            disabled={!file || uploading}
                            startIcon={uploading ? <CircularProgress size={20} /> : <CloudUpload />}
                        >
                            {uploading ? 'Uploading...' : 'Upload CSV'}
                        </Button>
                    </DemoWriteGuard>
                    <Button
                        variant="outlined"
                        onClick={resetUpload}
                        disabled={uploading}
                        sx={OUTLINED_BUTTON_SX}
                    >
                        Reset
                    </Button>
                </Box>
            </DemoDisabledArea>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }}>
                    <AlertTitle>Upload Error</AlertTitle>
                    {error}
                </Alert>
            )}

            {uploadResult && (
                <Box sx={{ mb: 3 }}>
                    <ImportResultPanel
                        result={{
                            ...uploadResult,
                            warnings: uploadResult.warningMessage ? [uploadResult.warningMessage] : [],
                        }}
                        title="Upload complete"
                        failedTitle="Upload failed"
                        stats={RESULT_STATS}
                        testId="csv-upload-result"
                    />

                    {skipped.length > 0 && (
                        <Box sx={{ mb: 3 }} data-testid="csv-skipped-rows">
                            <Typography variant="subtitle1" gutterBottom>
                                Skipped rows ({skipped.length})
                            </Typography>
                            <List dense>
                                {skipped.map((message) => (
                                    <ListItem key={message}>
                                        <ListItemText primary={message} />
                                    </ListItem>
                                ))}
                            </List>
                        </Box>
                    )}

                    {importedItems.length > 0 && (
                        <Box data-testid="csv-imported-items">
                            <Typography variant="subtitle1" gutterBottom>
                                Added to the library ({importedItems.length})
                            </Typography>
                            <List dense>
                                {importedItems.map((item, index) => (
                                    <React.Fragment key={item.id}>
                                        <ListItem disablePadding>
                                            <ListItemButton onClick={() => navigate(`/media/${item.id}`)}>
                                                <ListItemText primary={item.title} secondary={item.mediaType} />
                                            </ListItemButton>
                                        </ListItem>
                                        {index < importedItems.length - 1 && <Divider />}
                                    </React.Fragment>
                                ))}
                            </List>
                        </Box>
                    )}
                </Box>
            )}

            <Divider sx={{ mb: 3 }} />

            <Typography variant="subtitle1" gutterBottom>
                Columns for every media type
            </Typography>
            <ColumnList columns={SHARED_COLUMNS} />

            <Typography variant="subtitle1" gutterBottom>
                {typeLabel} columns
            </Typography>
            {typeColumns.note && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    {typeColumns.note}
                </Typography>
            )}
            <ColumnList columns={typeColumns.columns} />

            <Typography variant="subtitle1" gutterBottom>
                Formatting tips
            </Typography>
            <Box component="ul" sx={{ mt: 0, mb: 0, pl: 3 }}>
                <Typography component="li" variant="body2">Put quotes around text that contains commas.</Typography>
                <Typography component="li" variant="body2">For TRUE / FALSE columns, any capitalization works.</Typography>
                <Typography component="li" variant="body2">Write dates like 2024-01-15 or 01/15/2024.</Typography>
                <Typography component="li" variant="body2">
                    Separate several topics or genres in one cell with semicolons. They are stored in lowercase.
                </Typography>
                <Typography component="li" variant="body2">
                    A row for an item that is already in the library is skipped, not duplicated.
                </Typography>
            </Box>
        </Paper>
    );
}

export default CsvUploadSection;
