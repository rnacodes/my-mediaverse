import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Container, Typography, Button, Grid, Card, CardContent, CardMedia,
    Box, CircularProgress, Chip, Fab, ButtonGroup, List, ListItem, ListItemText,
    ListItemSecondaryAction, IconButton, Divider, Checkbox, Toolbar,
    Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions,
    Snackbar, Alert
} from '@mui/material';
import { Add, FileDownload, Upload, ViewModule, ViewList, OpenInNew, Delete, CheckBox, CheckBoxOutlineBlank } from '@mui/icons-material';
import { getAllMixlists } from '../api/mixlistService';

function MixlistsPage() {
    const [mixlists, setMixlists] = useState([]);
    const [loading, setLoading] = useState(true);
    const [viewMode, setViewMode] = useState('card'); // 'card' or 'list'
    const [selectedItems, setSelectedItems] = useState(new Set());
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deleting, _setDeleting] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const navigate = useNavigate();

    useEffect(() => {
        const loadMixlists = async () => {
            try {
                console.log('Attempting to load mixlists...');
                const response = await getAllMixlists();
                console.log('Mixlists response:', response);
                console.log('Mixlists data:', response.data);
                // Use the actual mixlists from the main endpoint
                setMixlists(response.data);
            } catch (error) {
                console.error('Error loading mixlists:', error);
                console.error('Error details:', error.response?.data);
                console.error('Error status:', error.response?.status);
            } finally {
                setLoading(false);
            }
        };
        loadMixlists();
    }, []);

    const handleViewModeChange = (mode) => {
        setViewMode(mode);
    };

    const handleSelectItem = (itemId) => {
        const newSelected = new Set(selectedItems);
        if (newSelected.has(itemId)) {
            newSelected.delete(itemId);
        } else {
            newSelected.add(itemId);
        }
        setSelectedItems(newSelected);
    };

    const handleSelectAll = () => {
        const allIds = new Set(mixlists.map(item => item.id || item.Id));
        setSelectedItems(allIds);
    };

    const handleDeselectAll = () => {
        setSelectedItems(new Set());
    };

    const handleBulkDelete = async () => {
        // Note: This would require a bulk delete API endpoint for mixlists
        // For now, just show a message
        setSnackbar({ 
            open: true, 
            message: 'Bulk delete for mixlists is not yet implemented', 
            severity: 'info' 
        });
        setDeleteDialogOpen(false);
    };

    if (loading) {
        return (
            <Box sx={{ 
                display: 'flex', 
                flexDirection: 'column',
                justifyContent: 'center', 
                alignItems: 'center', 
                minHeight: '50vh',
                gap: 2,
                px: { xs: 2, sm: 3 }
            }}>
                <CircularProgress size={60} />
                <Typography 
                    variant="h6" 
                    color="text.secondary"
                    sx={{ fontSize: { xs: '1rem', sm: '1.25rem' } }}
                >
                    Loading mixlists...
                </Typography>
            </Box>
        );
    }

    const renderCardView = () => (
        <Grid container spacing={{ xs: 2, sm: 3 }}>
            {mixlists.map((mixlist) => {
                const id = mixlist.id || mixlist.Id;
                const name = mixlist.Name || mixlist.name || 'Unnamed Mixlist';
                const thumbnail = mixlist.Thumbnail || mixlist.thumbnail;
                const description = mixlist.Description || mixlist.description;
                const mediaCount = mixlist.MediaItems ? mixlist.MediaItems.length : (mixlist.mediaItems ? mixlist.mediaItems.length : 0);
                
                return (
                    <Grid item xs={12} sm={6} md={4} lg={3} key={id}>
                        <Card 
                            sx={{ 
                                height: '100%',
                                cursor: 'pointer',
                                transition: 'transform 0.2s',
                                display: 'flex',
                                flexDirection: 'column',
                                position: 'relative',
                                '&:hover': {
                                    transform: { xs: 'none', sm: 'translateY(-4px)' },
                                    boxShadow: '0 8px 24px rgba(0,0,0,0.3)'
                                },
                                '&:active': {
                                    transform: 'scale(0.98)'
                                }
                            }}
                            onClick={() => navigate(`/mixlist/${id}`)}
                        >
                            {thumbnail && (
                                <CardMedia
                                    component="img"
                                    sx={{ 
                                        height: { xs: 160, sm: 180, md: 200 },
                                        objectFit: 'cover'
                                    }}
                                    image={thumbnail}
                                    alt={name}
                                />
                            )}
                            <CardContent sx={{ 
                                flexGrow: 1, 
                                display: 'flex', 
                                flexDirection: 'column',
                                p: { xs: 2, sm: 2 }
                            }}>
                                <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, mb: 1 }}>
                                    <Checkbox
                                        checked={selectedItems.has(id)}
                                        onChange={(e) => {
                                            e.stopPropagation();
                                            handleSelectItem(id);
                                        }}
                                        onClick={(e) => e.preventDefault()}
                                        sx={{ 
                                            p: 0,
                                            width: { xs: 24, sm: 24 },
                                            height: { xs: 24, sm: 24 },
                                            mt: 0.5
                                        }}
                                    />
                                    <Typography variant="h6" component="h2" sx={{ 
                                        fontSize: { xs: '1rem', sm: '1.1rem', md: '1.125rem' },
                                        fontWeight: 'bold',
                                        flex: 1
                                    }}>
                                        {name}
                                    </Typography>
                                </Box>
                                
                                {description && (
                                    <Typography variant="body2" sx={{ 
                                        mb: 2,
                                        color: 'text.secondary',
                                        fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        display: '-webkit-box',
                                        WebkitLineClamp: 3,
                                        WebkitBoxOrient: 'vertical',
                                        lineHeight: 1.4
                                    }}>
                                        {description}
                                    </Typography>
                                )}
                                
                                <Box sx={{ mt: 'auto', display: 'flex', justifyContent: 'center' }}>
                                    <Typography variant="body2" sx={{ 
                                        color: '#888',
                                        fontWeight: 'medium',
                                        fontSize: { xs: '0.75rem', sm: '0.875rem' },
                                        backgroundColor: 'rgba(0,0,0,0.1)',
                                        px: { xs: 1.5, sm: 2 },
                                        py: 0.5,
                                        borderRadius: 1
                                    }}>
                                        {mediaCount} item{mediaCount !== 1 ? 's' : ''}
                                    </Typography>
                                </Box>
                            </CardContent>
                        </Card>
                    </Grid>
                );
            })}
        </Grid>
    );

    const renderListView = () => (
        <List sx={{ mt: { xs: 1, sm: 2 } }}>
            {mixlists.map((mixlist, index) => {
                const id = mixlist.id || mixlist.Id;
                const name = mixlist.Name || mixlist.name || 'Unnamed Mixlist';
                const description = mixlist.Description || mixlist.description;
                const mediaCount = mixlist.MediaItems ? mixlist.MediaItems.length : (mixlist.mediaItems ? mixlist.mediaItems.length : 0);
                
                return (
                    <React.Fragment key={id}>
                        <ListItem 
                            sx={{ 
                                px: { xs: 1, sm: 2 },
                                py: { xs: 1.5, sm: 2 },
                                flexDirection: { xs: 'column', sm: 'row' },
                                alignItems: { xs: 'flex-start', sm: 'center' },
                                '&:hover': {
                                    backgroundColor: 'action.hover'
                                },
                                '&:active': {
                                    backgroundColor: 'action.selected'
                                }
                            }}
                        >
                            <Checkbox
                                checked={selectedItems.has(id)}
                                onChange={() => handleSelectItem(id)}
                                sx={{ 
                                    mr: { xs: 0, sm: 2 },
                                    mb: { xs: 1, sm: 0 },
                                    width: { xs: 40, sm: 42 },
                                    height: { xs: 40, sm: 42 }
                                }}
                            />
                            <ListItemText
                                sx={{ 
                                    flex: 1,
                                    width: { xs: '100%', sm: 'auto' },
                                    cursor: 'pointer'
                                }}
                                onClick={() => navigate(`/mixlist/${id}`)}
                                primary={
                                    <Box sx={{ 
                                        display: 'flex', 
                                        flexDirection: { xs: 'column', sm: 'row' },
                                        alignItems: { xs: 'flex-start', sm: 'center' }, 
                                        gap: 1, 
                                        mb: 0.5 
                                    }}>
                                        <Typography 
                                            variant="h6" 
                                            component="div"
                                            sx={{ fontSize: { xs: '1rem', sm: '1.125rem', md: '1.25rem' } }}
                                        >
                                            {name}
                                        </Typography>
                                        <Chip 
                                            label={`${mediaCount} items`}
                                            size="small"
                                            sx={{ 
                                                fontSize: { xs: '0.7rem', sm: '0.75rem' },
                                                height: { xs: '22px', sm: '24px' }
                                            }}
                                        />
                                    </Box>
                                }
                                secondary={description}
                            />
                            <ListItemSecondaryAction sx={{ display: { xs: 'none', sm: 'block' } }}>
                                <IconButton 
                                    edge="end" 
                                    color="primary"
                                    onClick={() => navigate(`/mixlist/${id}`)}
                                    sx={{
                                        width: { sm: 44 },
                                        height: { sm: 44 }
                                    }}
                                >
                                    <OpenInNew />
                                </IconButton>
                            </ListItemSecondaryAction>
                        </ListItem>
                        {index < mixlists.length - 1 && <Divider />}
                    </React.Fragment>
                );
            })}
        </List>
    );

    return (
        <Box sx={{ p: { xs: 2, sm: 3 }, minHeight: '100vh' }}>
            {/* Header with View Toggle */}
            <Box sx={{ 
                mb: { xs: 2, sm: 3 }, 
                display: 'flex', 
                flexDirection: { xs: 'column', sm: 'row' },
                justifyContent: 'space-between', 
                alignItems: { xs: 'flex-start', sm: 'center' },
                gap: { xs: 2, sm: 0 }
            }}>
                <Box>
                    <Typography 
                        variant="h3" 
                        component="h1" 
                        gutterBottom 
                        sx={{ 
                            fontSize: { xs: '1.75rem', sm: '2rem', md: '2rem' },
                            fontWeight: 'bold'
                        }}
                    >
                        My Mixlists
                    </Typography>
                    <Typography 
                        variant="body1" 
                        color="text.secondary"
                        sx={{ fontSize: { xs: '0.875rem', sm: '1rem' } }}
                    >
                        {mixlists.length} mixlist{mixlists.length !== 1 ? 's' : ''} found
                        {selectedItems.size > 0 && ` (${selectedItems.size} selected)`}
                    </Typography>
                </Box>
                
                {/* View Mode Toggle */}
                <ButtonGroup 
                    variant="outlined" 
                    aria-label="view mode"
                    sx={{
                        width: { xs: '100%', sm: 'auto' },
                        '& .MuiButton-root': {
                            color: 'white',
                            borderColor: 'white',
                            '&:hover': {
                                borderColor: 'white',
                                backgroundColor: 'rgba(255, 255, 255, 0.08)'
                            },
                            '&.MuiButton-contained': {
                                color: 'background.default',
                                backgroundColor: 'white',
                                '&:hover': {
                                    backgroundColor: 'rgba(255, 255, 255, 0.9)'
                                }
                            }
                        }
                    }}
                >
                    <Button
                        onClick={() => handleViewModeChange('card')}
                        variant={viewMode === 'card' ? 'contained' : 'outlined'}
                        startIcon={<ViewModule />}
                        sx={{ 
                            flex: { xs: 1, sm: 'initial' },
                            fontSize: { xs: '0.875rem', sm: '1rem' },
                            minHeight: '44px'
                        }}
                    >
                        Cards
                    </Button>
                    <Button
                        onClick={() => handleViewModeChange('list')}
                        variant={viewMode === 'list' ? 'contained' : 'outlined'}
                        startIcon={<ViewList />}
                        sx={{ 
                            flex: { xs: 1, sm: 'initial' },
                            fontSize: { xs: '0.875rem', sm: '1rem' },
                            minHeight: '44px'
                        }}
                    >
                        List
                    </Button>
                </ButtonGroup>
            </Box>

            {/* Bulk Actions Toolbar */}
            {mixlists.length > 0 && (
                <Toolbar 
                    sx={{ 
                        mb: 2, 
                        bgcolor: 'background.paper',
                        borderRadius: 1,
                        px: { xs: 1, sm: 2 },
                        py: { xs: 1, sm: 1 },
                        display: 'flex',
                        flexDirection: { xs: 'column', sm: 'row' },
                        gap: { xs: 1, sm: 2 },
                        justifyContent: 'space-between',
                        alignItems: { xs: 'stretch', sm: 'center' }
                    }}
                >
                    <Box sx={{ 
                        display: 'flex', 
                        flexDirection: { xs: 'column', sm: 'row' },
                        gap: 1,
                        width: { xs: '100%', sm: 'auto' }
                    }}>
                        <Button
                            variant="outlined"
                            size="small"
                            onClick={handleSelectAll}
                            startIcon={<CheckBox />}
                            sx={{
                                color: 'white',
                                borderColor: 'white',
                                minHeight: '44px',
                                fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                '&:hover': {
                                    borderColor: 'white',
                                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                }
                            }}
                        >
                            Select All
                        </Button>
                        <Button
                            variant="outlined"
                            size="small"
                            onClick={handleDeselectAll}
                            startIcon={<CheckBoxOutlineBlank />}
                            disabled={selectedItems.size === 0}
                            sx={{
                                color: 'white',
                                borderColor: 'white',
                                minHeight: '44px',
                                fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                '&:hover': {
                                    borderColor: 'white',
                                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                },
                                '&.Mui-disabled': {
                                    borderColor: 'rgba(255, 255, 255, 0.3)',
                                    color: 'rgba(255, 255, 255, 0.3)'
                                }
                            }}
                        >
                            Deselect All
                        </Button>
                        <Button 
                            variant="outlined"
                            size="small"
                            onClick={() => navigate('/import-mixlist')}
                            sx={{ 
                                color: 'white',
                                borderColor: 'white',
                                minHeight: '44px',
                                fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                '&:hover': {
                                    borderColor: 'white',
                                    backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                }
                            }}
                        >
                            Import Mixlist
                        </Button>
                    </Box>
                    <Button
                        variant="contained"
                        color="error"
                        size="small"
                        onClick={() => setDeleteDialogOpen(true)}
                        startIcon={<Delete />}
                        disabled={selectedItems.size === 0}
                        sx={{
                            minHeight: '44px',
                            fontSize: { xs: '0.8rem', sm: '0.875rem' },
                            width: { xs: '100%', sm: 'auto' }
                        }}
                    >
                        Delete Selected ({selectedItems.size})
                    </Button>
                </Toolbar>
            )}

            {mixlists.length === 0 ? (
                <Box sx={{ textAlign: 'center', mt: { xs: 4, sm: 6, md: 8 }, px: { xs: 2, sm: 0 } }}>
                    <Typography 
                        variant="h5" 
                        sx={{ 
                            mb: 2, 
                            color: '#888',
                            fontSize: { xs: '1.25rem', sm: '1.5rem' }
                        }}
                    >
                        No mixlists yet
                    </Typography>
                    <Typography 
                        variant="body1" 
                        sx={{ 
                            mb: { xs: 3, sm: 4 }, 
                            color: '#666',
                            fontSize: { xs: '0.875rem', sm: '1rem' }
                        }}
                    >
                        Create your first mixlist to organize your media!
                    </Typography>
                    <Button 
                        variant="contained" 
                        size="large"
                        onClick={() => navigate('/create-mixlist', { state: { returnTo: '/mixlists' } })}
                        sx={{
                            fontSize: { xs: '0.875rem', sm: '1rem' },
                            px: { xs: 3, sm: 4 },
                            py: 1.5,
                            minHeight: '48px',
                            width: { xs: '100%', sm: 'auto' },
                            maxWidth: { xs: '400px', sm: 'none' }
                        }}
                    >
                        Create First Mixlist
                    </Button>
                </Box>
            ) : (
                viewMode === 'card' ? renderCardView() : renderListView()
            )}

            {/* Bulk Delete Confirmation Dialog */}
            <Dialog
                open={deleteDialogOpen}
                onClose={() => !deleting && setDeleteDialogOpen(false)}
            >
                <DialogTitle>Confirm Bulk Delete</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Are you sure you want to delete {selectedItems.size} mixlist{selectedItems.size !== 1 ? 's' : ''}? 
                        This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleting}>
                        Cancel
                    </Button>
                    <Button 
                        onClick={handleBulkDelete} 
                        color="error" 
                        variant="contained"
                        disabled={deleting}
                    >
                        {deleting ? 'Deleting...' : 'Delete'}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Snackbar for feedback */}
            <Snackbar 
                open={snackbar.open} 
                autoHideDuration={6000} 
                onClose={() => setSnackbar({ ...snackbar, open: false })}
            >
                <Alert 
                    onClose={() => setSnackbar({ ...snackbar, open: false })} 
                    severity={snackbar.severity}
                >
                    {snackbar.message}
                </Alert>
            </Snackbar>

            {/* Floating Action Button to create new mixlist */}
            <Fab 
                color="primary" 
                aria-label="create mixlist"
                sx={{ 
                    position: 'fixed', 
                    bottom: { xs: 16, sm: 24, md: 32 }, 
                    right: { xs: 16, sm: 24, md: 32 },
                    width: { xs: 56, sm: 56 },
                    height: { xs: 56, sm: 56 },
                    zIndex: 1000
                }}
                onClick={() => navigate('/create-mixlist', { state: { returnTo: '/mixlists' } })}
            >
                <Add sx={{ fontSize: { xs: 24, sm: 28 } }} />
            </Fab>
        </Box>
    );
}

export default MixlistsPage;
