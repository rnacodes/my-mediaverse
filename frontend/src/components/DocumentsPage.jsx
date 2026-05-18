import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Container, Box, Typography, Grid, Button, CircularProgress,
    Alert, ToggleButton, ToggleButtonGroup, TextField,
    InputAdornment, Chip, Select, MenuItem, FormControl,
    InputLabel, Paper, Divider, IconButton, Tooltip,
    List, ListItem, ListItemText, Snackbar
} from '@mui/material';
import {
    ArrowBack, CloudDownload, ViewModule, ViewList,
    Search, FilterList, Archive, Sort, Refresh,
    Description, FolderOpen, Person, CalendarToday
} from '@mui/icons-material';
import { useAllDocuments, usePaperlessStatus, useSyncDocumentsFromPaperless } from '../hooks/useDocument';
import DocumentCard from './shared/DocumentCard';

function DocumentsPage() {
    const navigate = useNavigate();
    const [viewMode, setViewMode] = useState('grid');
    const [searchQuery, setSearchQuery] = useState('');
    const [filterDocumentType, setFilterDocumentType] = useState('all');
    const [filterArchived, setFilterArchived] = useState('all');
    const [sortBy, setSortBy] = useState('dateAdded');
    const [success, setSuccess] = useState('');
    const [overrideError, setOverrideError] = useState('');
    const [dismissedError, setDismissedError] = useState(false);

    const documentsQuery = useAllDocuments();
    const documents = documentsQuery.data ?? [];
    const loading = documentsQuery.isLoading;

    const paperlessStatusQuery = usePaperlessStatus();
    const paperlessStatus = paperlessStatusQuery.data ?? null;

    const syncMutation = useSyncDocumentsFromPaperless();
    const syncing = syncMutation.isPending;

    const queryError = !dismissedError && documentsQuery.error
        ? (documentsQuery.error.message || 'Failed to load documents')
        : '';
    const error = overrideError || queryError;

    const fetchDocuments = () => {
        setDismissedError(false);
        setOverrideError('');
        documentsQuery.refetch();
    };

    const documentTypes = [...new Set(documents.filter(d => d.documentType).map(d => d.documentType))].sort();

    const handleSyncFromPaperless = () => {
        setOverrideError('');
        syncMutation.mutate(undefined, {
            onSuccess: (result) => {
                if (result?.success) {
                    setSuccess(`Sync complete: ${result.documentsCreated} created, ${result.documentsUpdated} updated, ${result.documentsSkipped} skipped`);
                } else {
                    setOverrideError(result?.errorMessage || 'Sync failed');
                }
            },
            onError: (err) => {
                setOverrideError(err.response?.data?.error || err.message || 'Failed to sync from Paperless');
            },
        });
    };

    const filteredDocuments = useMemo(() => {
        let filtered = [...documents];

        if (searchQuery) {
            const query = searchQuery.toLowerCase();
            filtered = filtered.filter(doc =>
                doc.title?.toLowerCase().includes(query) ||
                doc.correspondent?.toLowerCase().includes(query) ||
                doc.documentType?.toLowerCase().includes(query) ||
                doc.description?.toLowerCase().includes(query) ||
                doc.originalFileName?.toLowerCase().includes(query)
            );
        }

        if (filterDocumentType !== 'all') {
            filtered = filtered.filter(doc => doc.documentType === filterDocumentType);
        }

        if (filterArchived !== 'all') {
            filtered = filtered.filter(doc => {
                switch (filterArchived) {
                    case 'archived':
                        return doc.isArchived;
                    case 'unarchived':
                        return !doc.isArchived;
                    default:
                        return true;
                }
            });
        }

        filtered.sort((a, b) => {
            switch (sortBy) {
                case 'dateAdded':
                    return new Date(b.dateAdded) - new Date(a.dateAdded);
                case 'dateAddedAsc':
                    return new Date(a.dateAdded) - new Date(b.dateAdded);
                case 'documentDate':
                    return new Date(b.documentDate || 0) - new Date(a.documentDate || 0);
                case 'title':
                    return (a.title || '').localeCompare(b.title || '');
                case 'correspondent':
                    return (a.correspondent || '').localeCompare(b.correspondent || '');
                case 'documentType':
                    return (a.documentType || '').localeCompare(b.documentType || '');
                case 'fileSize':
                    return (b.fileSizeBytes || 0) - (a.fileSizeBytes || 0);
                default:
                    return 0;
            }
        });

        return filtered;
    }, [documents, searchQuery, filterDocumentType, filterArchived, sortBy]);

    const getStatsChips = () => {
        const total = documents.length;
        const archived = documents.filter(d => d.isArchived).length;
        const byType = {};
        documents.forEach(d => {
            if (d.documentType) {
                byType[d.documentType] = (byType[d.documentType] || 0) + 1;
            }
        });
        const topTypes = Object.entries(byType)
            .sort((a, b) => b[1] - a[1])
            .slice(0, 3);

        return { total, archived, topTypes };
    };

    const stats = getStatsChips();

    if (loading) {
        return (
            <Box
                sx={{
                    minHeight: '100vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    py: 4,
                    px: 2
                }}
            >
                <Box sx={{
                    width: '100%',
                    maxWidth: '600px',
                    backgroundColor: 'background.paper',
                    borderRadius: '16px',
                    p: 4,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                    textAlign: 'center'
                }}>
                    <CircularProgress size={60} sx={{ mb: 2 }} />
                    <Typography variant="h6">Loading documents...</Typography>
                </Box>
            </Box>
        );
    }

    return (
        <Box
            sx={{
                minHeight: '100vh',
                py: 4,
                px: 2
            }}
        >
            <Container maxWidth="xl">
                <Box sx={{ mb: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
                    <Button
                        sx={{ color: 'white' }}
                        startIcon={<ArrowBack />}
                        onClick={() => navigate('/')}
                    >
                        Back to Home
                    </Button>

                    <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                        {paperlessStatus && (
                            <Chip
                                label={paperlessStatus.available ? 'Paperless Connected' : 'Paperless Disconnected'}
                                color={paperlessStatus.available ? 'success' : 'error'}
                                size="small"
                            />
                        )}
                        <Tooltip title="Refresh">
                            <IconButton onClick={fetchDocuments}>
                                <Refresh />
                            </IconButton>
                        </Tooltip>

                        <Button
                            variant="contained"
                            startIcon={syncing ? <CircularProgress size={20} color="inherit" /> : <CloudDownload />}
                            onClick={handleSyncFromPaperless}
                            disabled={syncing || !paperlessStatus?.available}
                        >
                            {syncing ? 'Syncing...' : 'Sync from Paperless'}
                        </Button>
                    </Box>
                </Box>

                <Paper
                    elevation={8}
                    sx={{
                        p: 4,
                        borderRadius: '16px',
                        backgroundColor: 'background.paper',
                        boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
                    }}
                >
                    <Typography variant="h4" gutterBottom fontWeight="bold">
                        My Documents
                    </Typography>

                    {/* Stats Chips */}
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 3 }}>
                        <Chip label={`Total: ${stats.total}`} icon={<Description />} color="default" />
                        <Chip label={`Archived: ${stats.archived}`} icon={<Archive />} color={stats.archived > 0 ? 'success' : 'default'} />
                        {stats.topTypes.map(([type, count]) => (
                            <Chip key={type} label={`${type}: ${count}`} variant="outlined" />
                        ))}
                    </Box>

                    <Divider sx={{ mb: 3 }} />

                    {error && (
                        <Alert severity="error" sx={{ mb: 3 }} onClose={() => { setOverrideError(''); setDismissedError(true); }}>
                            {error}
                        </Alert>
                    )}

                    {/* Filters and Search */}
                    <Grid container spacing={2} sx={{ mb: 3 }}>
                        <Grid item xs={12} md={4}>
                            <TextField
                                fullWidth
                                placeholder="Search documents..."
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                InputProps={{
                                    startAdornment: (
                                        <InputAdornment position="start">
                                            <Search />
                                        </InputAdornment>
                                    )
                                }}
                            />
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Document Type</InputLabel>
                                <Select
                                    value={filterDocumentType}
                                    onChange={(e) => setFilterDocumentType(e.target.value)}
                                    label="Document Type"
                                    startAdornment={<FilterList sx={{ ml: 1, mr: -0.5 }} />}
                                >
                                    <MenuItem value="all">All Types</MenuItem>
                                    {documentTypes.map(type => (
                                        <MenuItem key={type} value={type}>{type}</MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Archive Status</InputLabel>
                                <Select
                                    value={filterArchived}
                                    onChange={(e) => setFilterArchived(e.target.value)}
                                    label="Archive Status"
                                >
                                    <MenuItem value="all">All</MenuItem>
                                    <MenuItem value="archived">Archived Only</MenuItem>
                                    <MenuItem value="unarchived">Not Archived</MenuItem>
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Sort By</InputLabel>
                                <Select
                                    value={sortBy}
                                    onChange={(e) => setSortBy(e.target.value)}
                                    label="Sort By"
                                    startAdornment={<Sort sx={{ ml: 1, mr: -0.5 }} />}
                                >
                                    <MenuItem value="dateAdded">Newest First</MenuItem>
                                    <MenuItem value="dateAddedAsc">Oldest First</MenuItem>
                                    <MenuItem value="documentDate">Document Date</MenuItem>
                                    <MenuItem value="title">Title (A-Z)</MenuItem>
                                    <MenuItem value="correspondent">Correspondent</MenuItem>
                                    <MenuItem value="documentType">Document Type</MenuItem>
                                    <MenuItem value="fileSize">File Size</MenuItem>
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <ToggleButtonGroup
                                value={viewMode}
                                exclusive
                                onChange={(e, newMode) => newMode && setViewMode(newMode)}
                                fullWidth
                                sx={{ height: '56px' }}
                            >
                                <ToggleButton value="grid">
                                    <ViewModule />
                                </ToggleButton>
                                <ToggleButton value="list">
                                    <ViewList />
                                </ToggleButton>
                            </ToggleButtonGroup>
                        </Grid>
                    </Grid>

                    {/* Documents Display */}
                    {filteredDocuments.length === 0 ? (
                        <Box sx={{ textAlign: 'center', py: 8 }}>
                            <Description sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                            <Typography variant="h6" color="text.secondary" gutterBottom>
                                No documents found
                            </Typography>
                            <Typography variant="body2" color="text.secondary" paragraph>
                                {documents.length === 0
                                    ? "Sync documents from Paperless-ngx to get started"
                                    : "Try adjusting your filters or search query"}
                            </Typography>
                            {documents.length === 0 && paperlessStatus?.available && (
                                <Button
                                    variant="contained"
                                    startIcon={<CloudDownload />}
                                    onClick={handleSyncFromPaperless}
                                    disabled={syncing}
                                    sx={{ mt: 2 }}
                                >
                                    Sync from Paperless
                                </Button>
                            )}
                        </Box>
                    ) : (
                        <>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                Showing {filteredDocuments.length} of {documents.length} documents
                            </Typography>

                            {viewMode === 'grid' ? (
                                <Grid container spacing={3}>
                                    {filteredDocuments.map(document => (
                                        <Grid
                                            item
                                            xs={12}
                                            sm={6}
                                            md={4}
                                            lg={3}
                                            key={document.id}
                                        >
                                            <DocumentCard document={document} />
                                        </Grid>
                                    ))}
                                </Grid>
                            ) : (
                                <List sx={{ mt: 2 }}>
                                    {filteredDocuments.map((document, index) => (
                                        <React.Fragment key={document.id}>
                                            <ListItem
                                                sx={{
                                                    cursor: 'pointer',
                                                    '&:hover': {
                                                        backgroundColor: 'action.hover'
                                                    },
                                                    py: 2,
                                                    px: 2,
                                                    borderRadius: 1
                                                }}
                                                onClick={() => navigate(`/media/${document.id}`)}
                                            >
                                                <ListItemText
                                                    primary={
                                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5, flexWrap: 'wrap' }}>
                                                            <Description sx={{ color: 'text.secondary' }} />
                                                            <Typography variant="h6" component="div" sx={{ fontWeight: 600 }}>
                                                                {document.title}
                                                            </Typography>
                                                            {document.isArchived && (
                                                                <Archive sx={{ color: 'success.main', fontSize: 20 }} />
                                                            )}
                                                        </Box>
                                                    }
                                                    secondary={
                                                        <Box>
                                                            {document.description && (
                                                                <Typography
                                                                    variant="body2"
                                                                    sx={{
                                                                        mb: 1,
                                                                        overflow: 'hidden',
                                                                        textOverflow: 'ellipsis',
                                                                        display: '-webkit-box',
                                                                        WebkitLineClamp: 2,
                                                                        WebkitBoxOrient: 'vertical'
                                                                    }}
                                                                >
                                                                    {document.description}
                                                                </Typography>
                                                            )}
                                                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 1 }}>
                                                                {document.documentType && (
                                                                    <Chip
                                                                        label={document.documentType}
                                                                        size="small"
                                                                        icon={<FolderOpen />}
                                                                        color="primary"
                                                                    />
                                                                )}
                                                                {document.correspondent && (
                                                                    <Chip
                                                                        label={document.correspondent}
                                                                        size="small"
                                                                        icon={<Person />}
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                                {document.documentDate && (
                                                                    <Chip
                                                                        label={new Date(document.documentDate).toLocaleDateString()}
                                                                        size="small"
                                                                        icon={<CalendarToday />}
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                                {document.fileType && (
                                                                    <Chip
                                                                        label={document.fileType.toUpperCase()}
                                                                        size="small"
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                                {document.formattedFileSize && (
                                                                    <Chip
                                                                        label={document.formattedFileSize}
                                                                        size="small"
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                            </Box>
                                                        </Box>
                                                    }
                                                />
                                            </ListItem>
                                            {index < filteredDocuments.length - 1 && <Divider />}
                                        </React.Fragment>
                                    ))}
                                </List>
                            )}
                        </>
                    )}
                </Paper>
            </Container>

            {/* Success Snackbar */}
            <Snackbar
                open={!!success}
                autoHideDuration={6000}
                onClose={() => setSuccess('')}
                message={success}
            />
        </Box>
    );
}

export default DocumentsPage;
