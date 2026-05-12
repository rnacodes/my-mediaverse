import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Container,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  Chip,
  Button,
  ButtonGroup,
  List,
  ListItem,
  ListItemText,
  CircularProgress,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Snackbar,
  Alert,
  InputAdornment,
} from '@mui/material';
import {
  ViewModule,
  ViewList,
  Search as SearchIcon,
  Note as NoteIcon,
  Folder as FolderIcon,
} from '@mui/icons-material';
import { searchNotes } from '../api/noteService';

// Vault color mapping
const vaultColors = {
  general: '#4caf50',
  programming: '#2196f3',
};

function NotesListingPage() {
  const [notes, setNotes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('card');
  const [selectedVault, setSelectedVault] = useState('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [searching, setSearching] = useState(false);
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });

  useEffect(() => {
    fetchNotes();
  }, [selectedVault]);

  const fetchNotes = async () => {
    try {
      setLoading(true);
      setError(null);

      // Use Typesense search with wildcard query for faster initial load
      const filter = selectedVault !== 'all' ? `vault_name:${selectedVault}` : null;
      const response = await searchNotes('*', filter, 1, 250);

      if (response && response.hits) {
        // Transform Typesense results to note format
        const fetchedNotes = response.hits.map(hit => ({
          id: hit.document.id,
          title: hit.document.title,
          vaultName: hit.document.vault_name || hit.document.vaultName,
          description: hit.document.description,
          tags: hit.document.tags || [],
          dateImported: hit.document.date_imported || hit.document.dateImported,
        }));
        setNotes(fetchedNotes);
      } else if (response && Array.isArray(response)) {
        setNotes(response);
      } else {
        setNotes([]);
      }
    } catch (error) {
      console.error('Failed to fetch notes:', error);
      setError(`Failed to load notes: ${error.response?.data?.message || error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      fetchNotes();
      return;
    }

    try {
      setSearching(true);
      setError(null);

      const filter = selectedVault !== 'all' ? `vault_name:${selectedVault}` : null;
      const response = await searchNotes(searchQuery, filter, 1, 100);

      if (response && response.hits) {
        // Transform Typesense results to note format
        const searchedNotes = response.hits.map(hit => ({
          id: hit.document.id,
          title: hit.document.title,
          vaultName: hit.document.vault_name || hit.document.vaultName,
          description: hit.document.description,
          tags: hit.document.tags || [],
          dateImported: hit.document.date_imported || hit.document.dateImported,
        }));
        setNotes(searchedNotes);
      } else if (response && Array.isArray(response)) {
        setNotes(response);
      } else {
        setNotes([]);
      }
    } catch (error) {
      console.error('Failed to search notes:', error);
      setSnackbar({
        open: true,
        message: `Search failed: ${error.response?.data?.message || error.message}`,
        severity: 'error'
      });
    } finally {
      setSearching(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const handleClearSearch = () => {
    setSearchQuery('');
    fetchNotes();
  };

  const getVaultColor = (vaultName) => {
    return vaultColors[vaultName?.toLowerCase()] || '#9e9e9e';
  };

  const truncateDescription = (text, maxLength = 150) => {
    if (!text) return '';
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  };

  const renderCardView = () => (
    <Grid container spacing={3} sx={{ mt: 2 }}>
      {notes.map((note) => (
        <Grid item xs={12} sm={6} md={4} key={note.id}>
          <Card
            component={Link}
            to={`/note/${note.id}`}
            sx={{
              textDecoration: 'none',
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              '&:hover': {
                transform: 'translateY(-2px)',
                boxShadow: 4
              },
              transition: 'all 0.2s ease-in-out'
            }}
          >
            <CardContent sx={{ flexGrow: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, mb: 1 }}>
                <NoteIcon sx={{ color: 'text.secondary', mt: 0.5 }} />
                <Box sx={{ flexGrow: 1 }}>
                  <Typography variant="h6" component="div" sx={{ fontWeight: 'bold', lineHeight: 1.3 }}>
                    {note.title}
                  </Typography>
                </Box>
              </Box>

              <Box sx={{ display: 'flex', gap: 1, mb: 1, flexWrap: 'wrap' }}>
                <Chip
                  icon={<FolderIcon />}
                  label={note.vaultName}
                  size="small"
                  sx={{
                    bgcolor: getVaultColor(note.vaultName),
                    color: 'white',
                    fontWeight: 'bold'
                  }}
                />
              </Box>

              {(note.description || note.aiDescription) && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  {truncateDescription(note.description || note.aiDescription)}
                </Typography>
              )}

              {note.tags && note.tags.length > 0 && (
                <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                  {note.tags.slice(0, 4).map((tag) => (
                    <Chip
                      key={`tag-${tag}`}
                      label={tag}
                      size="small"
                      variant="outlined"
                      sx={{ fontSize: '0.7rem' }}
                    />
                  ))}
                  {note.tags.length > 4 && (
                    <Chip
                      label={`+${note.tags.length - 4}`}
                      size="small"
                      variant="outlined"
                      sx={{ fontSize: '0.7rem' }}
                    />
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );

  const renderListView = () => (
    <List sx={{ mt: 2 }}>
      {notes.map((note) => (
        <ListItem
          key={note.id}
          component={Link}
          to={`/note/${note.id}`}
          sx={{
            border: '1px solid',
            borderColor: 'divider',
            borderRadius: 1,
            mb: 1,
            textDecoration: 'none',
            '&:hover': {
              bgcolor: 'action.hover'
            }
          }}
        >
          <NoteIcon sx={{ mr: 2, color: 'text.secondary' }} />
          <ListItemText
            primary={
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                  {note.title}
                </Typography>
                <Chip
                  label={note.vaultName}
                  size="small"
                  sx={{
                    bgcolor: getVaultColor(note.vaultName),
                    color: 'white',
                    fontSize: '0.7rem'
                  }}
                />
              </Box>
            }
            secondary={
              <Box>
                {(note.description || note.aiDescription) && (
                  <Typography variant="body2" color="text.secondary" component="span">
                    {truncateDescription(note.description || note.aiDescription, 200)}
                  </Typography>
                )}
                {note.tags && note.tags.length > 0 && (
                  <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 0.5 }}>
                    {note.tags.slice(0, 6).map((tag) => (
                      <Chip
                        key={`tag-${tag}`}
                        label={tag}
                        size="small"
                        variant="outlined"
                        sx={{ fontSize: '0.65rem' }}
                      />
                    ))}
                  </Box>
                )}
              </Box>
            }
          />
        </ListItem>
      ))}
    </List>
  );

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '50vh' }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h3" gutterBottom sx={{ mb: 4, fontWeight: 'bold' }}>
        Notes
      </Typography>

      {/* Search and Filter Bar */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField
          placeholder="Search notes..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          onKeyPress={handleKeyPress}
          sx={{ flexGrow: 1, minWidth: 200 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon />
              </InputAdornment>
            ),
          }}
        />

        <Button
          variant="contained"
          onClick={handleSearch}
          disabled={searching}
          startIcon={searching ? <CircularProgress size={20} color="inherit" /> : <SearchIcon />}
        >
          Search
        </Button>

        {searchQuery && (
          <Button variant="outlined" onClick={handleClearSearch}>
            Clear
          </Button>
        )}

        <FormControl sx={{ minWidth: 150 }}>
          <InputLabel>Vault</InputLabel>
          <Select
            value={selectedVault}
            label="Vault"
            onChange={(e) => setSelectedVault(e.target.value)}
          >
            <MenuItem value="all">All Vaults</MenuItem>
            <MenuItem value="general">General</MenuItem>
            <MenuItem value="programming">Programming</MenuItem>
          </Select>
        </FormControl>

        <ButtonGroup>
          <Button
            variant={viewMode === 'card' ? 'contained' : 'outlined'}
            onClick={() => setViewMode('card')}
          >
            <ViewModule />
          </Button>
          <Button
            variant={viewMode === 'list' ? 'contained' : 'outlined'}
            onClick={() => setViewMode('list')}
          >
            <ViewList />
          </Button>
        </ButtonGroup>
      </Box>

      {/* Results Count */}
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {notes.length} note{notes.length !== 1 ? 's' : ''} found
        {selectedVault !== 'all' && ` in ${selectedVault} vault`}
        {searchQuery && ` matching "${searchQuery}"`}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {notes.length === 0 && !loading && !error && (
        <Box sx={{ textAlign: 'center', py: 8 }}>
          <NoteIcon sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
          <Typography variant="h6" color="text.secondary">
            No notes found
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {searchQuery
              ? 'Try adjusting your search query'
              : 'Sync your Obsidian vaults to import notes'}
          </Typography>
        </Box>
      )}

      {notes.length > 0 && (viewMode === 'card' ? renderCardView() : renderListView())}

      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
      >
        <Alert onClose={() => setSnackbar({ ...snackbar, open: false })} severity={snackbar.severity}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
}

export default NotesListingPage;
