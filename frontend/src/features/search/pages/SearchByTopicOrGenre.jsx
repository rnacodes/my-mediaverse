import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Typography, Box, Accordion, AccordionSummary, AccordionDetails, Chip, CircularProgress, Alert, Grid, Card, CardContent, Button, TextField, Dialog, DialogTitle, DialogContent, DialogActions, IconButton, DialogContentText } from '@mui/material';
import { ExpandMore, Topic as TopicIcon, Category as GenreIcon, Add as AddIcon, Delete as DeleteIcon, CloudUpload as UploadIcon, Edit as EditIcon, PlaylistAdd } from '@mui/icons-material';
import {
    useAllTopics,
    useAllGenres,
    useCreateTopic,
    useCreateGenre,
    useDeleteTopic,
    useDeleteGenre,
    useUpdateTopic,
    useUpdateGenre,
} from '@/hooks/useTopicGenre';

const sortByName = (items) =>
    [...items].sort((a, b) => (a.name || a.Name).localeCompare(b.name || b.Name));

function SearchByTopicOrGenre() {
    const [expanded, setExpanded] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');

    // Dialog states
    const [openTopicDialog, setOpenTopicDialog] = useState(false);
    const [openGenreDialog, setOpenGenreDialog] = useState(false);
    const [newTopicName, setNewTopicName] = useState('');
    const [newGenreName, setNewGenreName] = useState('');

    // Delete dialog states
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
    const [deleteTarget, setDeleteTarget] = useState(null);

    // Edit dialog states
    const [openEditDialog, setOpenEditDialog] = useState(false);
    const [editTarget, setEditTarget] = useState(null);
    const [editName, setEditName] = useState('');

    const navigate = useNavigate();

    const topicsQuery = useAllTopics();
    const genresQuery = useAllGenres();
    const topics = useMemo(() => sortByName(topicsQuery.data ?? []), [topicsQuery.data]);
    const genres = useMemo(() => sortByName(genresQuery.data ?? []), [genresQuery.data]);
    const loading = topicsQuery.isLoading || genresQuery.isLoading;

    const createTopicMutation = useCreateTopic();
    const createGenreMutation = useCreateGenre();
    const deleteTopicMutation = useDeleteTopic();
    const deleteGenreMutation = useDeleteGenre();
    const updateTopicMutation = useUpdateTopic();
    const updateGenreMutation = useUpdateGenre();

    const creating = createTopicMutation.isPending || createGenreMutation.isPending;
    const deleting = deleteTopicMutation.isPending || deleteGenreMutation.isPending;
    const editing = updateTopicMutation.isPending || updateGenreMutation.isPending;

    const handleAccordionChange = (panel) => (event, isExpanded) => {
        setExpanded(isExpanded ? panel : false);
    };

    const handleTopicClick = (topic) => {
        navigate(`/search?topics=${encodeURIComponent(topic.name || topic.Name)}&mediaType=all`);
    };

    const handleGenreClick = (genre) => {
        navigate(`/search?genres=${encodeURIComponent(genre.name || genre.Name)}&mediaType=all`);
    };

    const handleCreateTopic = () => {
        if (!newTopicName.trim()) {
            setError('Topic name cannot be empty');
            return;
        }

        setError('');
        setSuccess('');

        createTopicMutation.mutate(
            { name: newTopicName.trim() },
            {
                onSuccess: () => {
                    setSuccess(`Topic "${newTopicName}" created successfully!`);
                    setNewTopicName('');
                    setOpenTopicDialog(false);
                },
                onError: (err) => {
                    console.error('Error creating topic:', err);
                    setError(err.response?.data?.message || 'Failed to create topic');
                },
            }
        );
    };

    const handleCreateGenre = () => {
        if (!newGenreName.trim()) {
            setError('Genre name cannot be empty');
            return;
        }

        setError('');
        setSuccess('');

        createGenreMutation.mutate(
            { name: newGenreName.trim() },
            {
                onSuccess: () => {
                    setSuccess(`Genre "${newGenreName}" created successfully!`);
                    setNewGenreName('');
                    setOpenGenreDialog(false);
                },
                onError: (err) => {
                    console.error('Error creating genre:', err);
                    setError(err.response?.data?.message || 'Failed to create genre');
                },
            }
        );
    };

    const handleDeleteClick = (type, item) => {
        setDeleteTarget({
            type,
            id: item.id || item.Id,
            name: item.name || item.Name,
            mediaItemCount: item.mediaItemCount ?? (item.mediaItemIds || item.MediaItemIds || []).length
        });
        setOpenDeleteDialog(true);
    };

    const handleConfirmDelete = () => {
        if (!deleteTarget) return;

        setError('');
        setSuccess('');

        const mutation = deleteTarget.type === 'topic' ? deleteTopicMutation : deleteGenreMutation;
        const label = deleteTarget.type === 'topic' ? 'Topic' : 'Genre';

        mutation.mutate(deleteTarget.id, {
            onSuccess: () => {
                setSuccess(`${label} "${deleteTarget.name}" deleted successfully!`);
                setOpenDeleteDialog(false);
                setDeleteTarget(null);
            },
            onError: (err) => {
                console.error(`Error deleting ${deleteTarget.type}:`, err);
                const errorMessage = err.response?.data?.message || err.response?.data || `Failed to delete ${deleteTarget.type}`;
                setError(errorMessage);
            },
        });
    };

    const handleCancelDelete = () => {
        setOpenDeleteDialog(false);
        setDeleteTarget(null);
    };

    const handleEditClick = (type, item) => {
        setEditTarget({
            type,
            id: item.id || item.Id,
            name: item.name || item.Name
        });
        setEditName(item.name || item.Name);
        setOpenEditDialog(true);
    };

    const handleConfirmEdit = () => {
        if (!editTarget || !editName.trim()) return;

        setError('');
        setSuccess('');

        const trimmedName = editName.trim();
        const isTopic = editTarget.type === 'topic';
        const mutation = isTopic ? updateTopicMutation : updateGenreMutation;
        const idField = isTopic ? 'topicId' : 'genreId';
        const dataField = isTopic ? 'topicData' : 'genreData';
        const label = isTopic ? 'Topic' : 'Genre';

        mutation.mutate(
            { [idField]: editTarget.id, [dataField]: { name: trimmedName } },
            {
                onSuccess: () => {
                    setSuccess(`${label} renamed to "${trimmedName}" successfully!`);
                    setOpenEditDialog(false);
                    setEditTarget(null);
                    setEditName('');
                },
                onError: (err) => {
                    console.error(`Error updating ${editTarget.type}:`, err);
                    const errorMessage = err.response?.data?.message || err.response?.data || `Failed to update ${editTarget.type}`;
                    setError(errorMessage);
                },
            }
        );
    };

    const handleCancelEdit = () => {
        setOpenEditDialog(false);
        setEditTarget(null);
        setEditName('');
    };

    if (loading) {
        return (
            <Container maxWidth="lg">
                <Box sx={{ mt: 4, display: 'flex', justifyContent: 'center' }}>
                    <CircularProgress />
                </Box>
            </Container>
        );
    }

    return (
        <Container maxWidth="lg">
            <Box sx={{ mt: 4 }}>
                <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 1 }}>
                    📚 Topics & Genres Directory
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Browse, create, and manage all your topics and genres. Click any to see related media, or{' '}
                    <Button
                        variant="text"
                        onClick={() => navigate('/search')}
                        sx={{
                            p: 0,
                            minWidth: 'auto',
                            textTransform: 'none',
                            verticalAlign: 'baseline',
                            color: 'white',
                            fontSize: 'inherit',
                            '&:hover': {
                                color: 'primary.light',
                                backgroundColor: 'transparent'
                            }
                        }}
                    >
                        go to advanced search
                    </Button>
                    .
                </Typography>
                <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
                    <Button
                        variant="contained"
                        startIcon={<UploadIcon />}
                        onClick={() => navigate('/import-genres-topics')}
                        sx={{
                            backgroundColor: '#9c27b0',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: '#7b1fa2'
                            }
                        }}
                    >
                        Bulk Upload via CSV
                    </Button>
                    <Button
                        variant="contained"
                        startIcon={<PlaylistAdd />}
                        onClick={() => navigate('/create-mixlist', { state: { returnTo: '/search-by-topic-genre' } })}
                        sx={{
                            backgroundColor: '#362759',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: '#2a1e47'
                            }
                        }}
                    >
                        Create New Mixlist
                    </Button>
                </Box>
                
                {error && (
                    <Alert severity="error" sx={{ mb: 3, bgcolor: 'background.paper', color: 'white', '& .MuiAlert-icon': { color: '#f44336' } }} onClose={() => setError('')}>
                        {error}
                    </Alert>
                )}

                {success && (
                    <Alert severity="success" sx={{ mb: 3 }} onClose={() => setSuccess('')}>
                        {success}
                    </Alert>
                )}

                {/* Topics Section */}
                <Accordion 
                    expanded={expanded === 'topics'} 
                    onChange={handleAccordionChange('topics')}
                    sx={{ mb: 2 }}
                >
                    <AccordionSummary
                        expandIcon={<ExpandMore sx={{ color: 'white' }} />}
                        aria-controls="topics-content"
                        id="topics-header"
                        sx={{
                            backgroundColor: 'primary.main',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: 'primary.dark',
                            },
                            '& .MuiTypography-root': {
                                color: 'white',
                                fontWeight: 'bold'
                            }
                        }}
                    >
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                            <TopicIcon />
                            <Typography variant="h6">
                                Topics ({topics.length})
                            </Typography>
                            <Box sx={{ ml: 'auto' }}>
                                <IconButton
                                    size="small"
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        setOpenTopicDialog(true);
                                    }}
                                    sx={{ 
                                        color: 'white',
                                        '&:hover': {
                                            backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                        }
                                    }}
                                >
                                    <AddIcon />
                                </IconButton>
                            </Box>
                        </Box>
                    </AccordionSummary>
                    <AccordionDetails>
                        {topics.length === 0 ? (
                            <Typography color="text.secondary">
                                No topics found. Topics will appear here after you add media items with topics.
                            </Typography>
                        ) : (
                            <Grid container spacing={1}>
                                {topics.map((topic) => (
                                    <Grid item xs={12} sm={6} md={4} lg={3} key={topic.id || topic.Id}>
                                        <Card
                                            sx={{
                                                cursor: 'pointer',
                                                '&:hover': {
                                                    boxShadow: 'none'
                                                },
                                                transition: 'none',
                                                position: 'relative'
                                            }}
                                            onClick={() => handleTopicClick(topic)}
                                        >
                                            <Box sx={{ position: 'absolute', top: 4, right: 4, zIndex: 1, display: 'flex', gap: 0.5 }}>
                                                <IconButton
                                                    size="small"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleEditClick('topic', topic);
                                                    }}
                                                    sx={{
                                                        backgroundColor: '#9c27b0',
                                                        color: 'white',
                                                        '&:hover': {
                                                            backgroundColor: '#7b1fa2'
                                                        }
                                                    }}
                                                >
                                                    <EditIcon fontSize="small" />
                                                </IconButton>
                                                <IconButton
                                                    size="small"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleDeleteClick('topic', topic);
                                                    }}
                                                    sx={{
                                                        backgroundColor: '#9c27b0',
                                                        color: 'white',
                                                        '&:hover': {
                                                            backgroundColor: '#7b1fa2'
                                                        }
                                                    }}
                                                >
                                                    <DeleteIcon fontSize="small" />
                                                </IconButton>
                                            </Box>
                                            <CardContent sx={{ p: 2 }}>
                                                <Chip
                                                    label={topic.name || topic.Name}
                                                    color="primary"
                                                    variant="filled"
                                                    sx={{ 
                                                        width: '100%',
                                                        backgroundColor: 'primary.main',
                                                        color: 'white',
                                                        fontWeight: 'bold',
                                                        fontSize: '0.9rem',
                                                        '& .MuiChip-label': {
                                                            display: 'block',
                                                            whiteSpace: 'normal',
                                                            textAlign: 'center',
                                                            color: 'white'
                                                        },
                                                        '&:hover': {
                                                            backgroundColor: 'primary.dark'
                                                        }
                                                    }}
                                                />
                                            </CardContent>
                                        </Card>
                                    </Grid>
                                ))}
                            </Grid>
                        )}
                    </AccordionDetails>
                </Accordion>

                {/* Genres Section */}
                <Accordion 
                    expanded={expanded === 'genres'} 
                    onChange={handleAccordionChange('genres')}
                    sx={{ mb: 2 }}
                >
                    <AccordionSummary
                        expandIcon={<ExpandMore sx={{ color: 'white' }} />}
                        aria-controls="genres-content"
                        id="genres-header"
                        sx={{
                            backgroundColor: '#4b6aa2',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: '#3d5a8a',
                            },
                            '& .MuiTypography-root': {
                                color: 'white',
                                fontWeight: 'bold'
                            }
                        }}
                    >
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                            <GenreIcon />
                            <Typography variant="h6">
                                Genres ({genres.length})
                            </Typography>
                            <Box sx={{ ml: 'auto' }}>
                                <IconButton
                                    size="small"
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        setOpenGenreDialog(true);
                                    }}
                                    sx={{ 
                                        color: 'white',
                                        '&:hover': {
                                            backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                        }
                                    }}
                                >
                                    <AddIcon />
                                </IconButton>
                            </Box>
                        </Box>
                    </AccordionSummary>
                    <AccordionDetails>
                        {genres.length === 0 ? (
                            <Typography color="text.secondary">
                                No genres found. Genres will appear here after you add media items with genres.
                            </Typography>
                        ) : (
                            <Grid container spacing={1}>
                                {genres.map((genre) => (
                                    <Grid item xs={12} sm={6} md={4} lg={3} key={genre.id || genre.Id}>
                                        <Card
                                            sx={{
                                                cursor: 'pointer',
                                                '&:hover': {
                                                    boxShadow: 'none'
                                                },
                                                transition: 'none',
                                                position: 'relative'
                                            }}
                                            onClick={() => handleGenreClick(genre)}
                                        >
                                            <Box sx={{ position: 'absolute', top: 4, right: 4, zIndex: 1, display: 'flex', gap: 0.5 }}>
                                                <IconButton
                                                    size="small"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleEditClick('genre', genre);
                                                    }}
                                                    sx={{
                                                        backgroundColor: '#9c27b0',
                                                        color: 'white',
                                                        '&:hover': {
                                                            backgroundColor: '#7b1fa2'
                                                        }
                                                    }}
                                                >
                                                    <EditIcon fontSize="small" />
                                                </IconButton>
                                                <IconButton
                                                    size="small"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleDeleteClick('genre', genre);
                                                    }}
                                                    sx={{
                                                        backgroundColor: '#9c27b0',
                                                        color: 'white',
                                                        '&:hover': {
                                                            backgroundColor: '#7b1fa2'
                                                        }
                                                    }}
                                                >
                                                    <DeleteIcon fontSize="small" />
                                                </IconButton>
                                            </Box>
                                            <CardContent sx={{ p: 2 }}>
                                                <Chip
                                                    label={genre.name || genre.Name}
                                                    color="secondary"
                                                    variant="filled"
                                                    sx={{ 
                                                        width: '100%',
                                                        backgroundColor: '#4b6aa2',
                                                        color: 'white',
                                                        fontWeight: 'bold',
                                                        fontSize: '0.9rem',
                                                        '& .MuiChip-label': {
                                                            display: 'block',
                                                            whiteSpace: 'normal',
                                                            textAlign: 'center',
                                                            color: 'white'
                                                        },
                                                        '&:hover': {
                                                            backgroundColor: '#3d5a8a'
                                                        }
                                                    }}
                                                />
                                            </CardContent>
                                        </Card>
                                    </Grid>
                                ))}
                            </Grid>
                        )}
                    </AccordionDetails>
                </Accordion>

                {/* Create Topic Dialog */}
                <Dialog 
                    open={openTopicDialog} 
                    onClose={() => !creating && setOpenTopicDialog(false)}
                    maxWidth="sm"
                    fullWidth
                >
                    <DialogTitle>Create New Topic</DialogTitle>
                    <DialogContent>
                        <TextField
                            // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses name field on dialog open
                            autoFocus
                            margin="dense"
                            label="Topic Name"
                            type="text"
                            fullWidth
                            variant="outlined"
                            value={newTopicName}
                            onChange={(e) => setNewTopicName(e.target.value)}
                            disabled={creating}
                            onKeyPress={(e) => {
                                if (e.key === 'Enter' && !creating) {
                                    handleCreateTopic();
                                }
                            }}
                            sx={{ mt: 2 }}
                        />
                    </DialogContent>
                    <DialogActions>
                        <Button
                            onClick={() => setOpenTopicDialog(false)}
                            disabled={creating}
                            sx={{ color: 'white' }}
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleCreateTopic}
                            variant="contained"
                            disabled={creating || !newTopicName.trim()}
                            sx={{
                                backgroundColor: '#9c27b0',
                                color: 'white',
                                '&:hover': { backgroundColor: '#7b1fa2' }
                            }}
                        >
                            {creating ? 'Creating...' : 'Create'}
                        </Button>
                    </DialogActions>
                </Dialog>

                {/* Create Genre Dialog */}
                <Dialog 
                    open={openGenreDialog} 
                    onClose={() => !creating && setOpenGenreDialog(false)}
                    maxWidth="sm"
                    fullWidth
                >
                    <DialogTitle>Create New Genre</DialogTitle>
                    <DialogContent>
                        <TextField
                            // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses name field on dialog open
                            autoFocus
                            margin="dense"
                            label="Genre Name"
                            type="text"
                            fullWidth
                            variant="outlined"
                            value={newGenreName}
                            onChange={(e) => setNewGenreName(e.target.value)}
                            disabled={creating}
                            onKeyPress={(e) => {
                                if (e.key === 'Enter' && !creating) {
                                    handleCreateGenre();
                                }
                            }}
                            sx={{ mt: 2 }}
                        />
                    </DialogContent>
                    <DialogActions>
                        <Button
                            onClick={() => setOpenGenreDialog(false)}
                            disabled={creating}
                            sx={{ color: 'white' }}
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleCreateGenre}
                            variant="contained"
                            disabled={creating || !newGenreName.trim()}
                            sx={{
                                backgroundColor: '#9c27b0',
                                color: 'white',
                                '&:hover': { backgroundColor: '#7b1fa2' }
                            }}
                        >
                            {creating ? 'Creating...' : 'Create'}
                        </Button>
                    </DialogActions>
                </Dialog>

                {/* Delete Confirmation Dialog */}
                <Dialog 
                    open={openDeleteDialog} 
                    onClose={() => !deleting && handleCancelDelete()}
                    maxWidth="sm"
                    fullWidth
                >
                    <DialogTitle>
                        Delete {deleteTarget?.type === 'topic' ? 'Topic' : 'Genre'}?
                    </DialogTitle>
                    <DialogContent>
                        <DialogContentText>
                            Are you sure you want to delete <strong>&quot;{deleteTarget?.name}&quot;</strong>?
                        </DialogContentText>
                        {deleteTarget && deleteTarget.mediaItemCount > 0 && (
                            <Alert severity="warning" sx={{ mt: 2 }}>
                                This {deleteTarget.type} is currently attached to {deleteTarget.mediaItemCount} media item{deleteTarget.mediaItemCount !== 1 ? 's' : ''}. 
                                You may not be able to delete it. Consider removing it from all media items first.
                            </Alert>
                        )}
                        {deleteTarget && deleteTarget.mediaItemCount === 0 && (
                            <Alert severity="info" sx={{ mt: 2 }}>
                                This {deleteTarget.type} is not attached to any media items and can be safely deleted.
                            </Alert>
                        )}
                    </DialogContent>
                    <DialogActions>
                        <Button
                            onClick={handleCancelDelete}
                            disabled={deleting}
                            sx={{ color: 'white' }}
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleConfirmDelete}
                            variant="contained"
                            disabled={deleting}
                            sx={{
                                backgroundColor: '#9c27b0',
                                color: 'white',
                                '&:hover': { backgroundColor: '#7b1fa2' }
                            }}
                        >
                            {deleting ? 'Deleting...' : 'Delete'}
                        </Button>
                    </DialogActions>
                </Dialog>

                {/* Edit Dialog */}
                <Dialog
                    open={openEditDialog}
                    onClose={() => !editing && handleCancelEdit()}
                    maxWidth="sm"
                    fullWidth
                >
                    <DialogTitle>
                        Rename {editTarget?.type === 'topic' ? 'Topic' : 'Genre'}
                    </DialogTitle>
                    <DialogContent>
                        <DialogContentText sx={{ mb: 2 }}>
                            Enter a new name for <strong>&quot;{editTarget?.name}&quot;</strong>:
                        </DialogContentText>
                        <TextField
                            // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses name field on rename-dialog open
                            autoFocus
                            margin="dense"
                            label={editTarget?.type === 'topic' ? 'Topic Name' : 'Genre Name'}
                            type="text"
                            fullWidth
                            variant="outlined"
                            value={editName}
                            onChange={(e) => setEditName(e.target.value)}
                            disabled={editing}
                            onKeyPress={(e) => {
                                if (e.key === 'Enter' && !editing) {
                                    handleConfirmEdit();
                                }
                            }}
                        />
                    </DialogContent>
                    <DialogActions>
                        <Button
                            onClick={handleCancelEdit}
                            disabled={editing}
                            sx={{ color: 'white' }}
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleConfirmEdit}
                            variant="contained"
                            disabled={editing || !editName.trim() || editName.trim() === editTarget?.name}
                            sx={{
                                backgroundColor: '#9c27b0',
                                color: 'white',
                                '&:hover': { backgroundColor: '#7b1fa2' }
                            }}
                        >
                            {editing ? 'Saving...' : 'Save'}
                        </Button>
                    </DialogActions>
                </Dialog>
            </Box>
        </Container>
    );
}

export default SearchByTopicOrGenre;
