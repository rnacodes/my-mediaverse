import React, { useState } from 'react';
import { Container, Paper, Typography, Button, Box, Alert, CircularProgress, Card, CardContent, Grid, Chip, TextField, Select, MenuItem, FormControl, InputLabel, Divider, List, ListItem, ListItemText, Switch, FormControlLabel } from '@mui/material';
import { Refresh as RefreshIcon, Search as SearchIcon, CheckCircle as CheckCircleIcon, Error as ErrorIcon, Info as InfoIcon } from '@mui/icons-material';
import {
  useRealTimeIndexingStatus, useSetRealTimeIndexingStatus,
  useTypesenseHealth, useTypesenseSearch,
  useTypesenseReindex, useTypesenseReindexMixlists, useReindexHighlights,
  useTypesenseResetMediaItems, useTypesenseResetMixlists, useResetHighlightsCollection,
  useTypesenseReindexNotes, useTypesenseResetNotesCollection,
} from '@/hooks/useTypesense';
import { useDuplicateArticles, useDeduplicateArticles } from '@/hooks/useArticle';
import { useSyncAllVaults, useNoteSyncStatus } from '@/hooks/useNote';
import { formatStatus } from '@/utils/formatters';

// Typesense/admin errors surface via data.message.
const errMsg = (error, fallback) =>
  error ? (error.response?.data?.message || error.message || fallback) : null;

const TypesenseAdminPage = () => {
  // ----- Real-time indexing toggle (status query + toggle mutation) -----
  // 401s while unauthenticated just leave the toggle at its default (enabled).
  const realTimeIndexingQuery = useRealTimeIndexingStatus({ retry: false });
  const realTimeIndexing = realTimeIndexingQuery.data?.enabled ?? true;
  const toggleIndexingMutation = useSetRealTimeIndexingStatus();
  const realTimeIndexingLoading = toggleIndexingMutation.isPending;
  const realTimeIndexingError = errMsg(toggleIndexingMutation.error, 'Failed to toggle real-time indexing');
  const handleToggleRealTimeIndexing = (event) => {
    toggleIndexingMutation.mutate(event.target.checked);
  };

  // ----- Health -----
  const healthQuery = useTypesenseHealth({ retry: false });
  const healthStatus = healthQuery.data ?? null;
  const healthLoading = healthQuery.isFetching;
  const healthError = errMsg(healthQuery.error, 'Failed to check health');
  const checkHealth = () => healthQuery.refetch();

  // ----- Bulk reindex mutations -----
  const reindexMutation = useTypesenseReindex();
  const reindexing = reindexMutation.isPending;
  const reindexResult = reindexMutation.data ?? null;
  const reindexError = errMsg(reindexMutation.error, 'Failed to reindex media items');
  const handleReindex = () => reindexMutation.mutate();

  const reindexMixlistsMutation = useTypesenseReindexMixlists();
  const reindexingMixlists = reindexMixlistsMutation.isPending;
  const reindexMixlistsResult = reindexMixlistsMutation.data ?? null;
  const reindexMixlistsError = errMsg(reindexMixlistsMutation.error, 'Failed to reindex mixlists');
  const handleReindexMixlists = () => reindexMixlistsMutation.mutate();

  const reindexHighlightsMutation = useReindexHighlights();
  const reindexingHighlights = reindexHighlightsMutation.isPending;
  const reindexHighlightsResult = reindexHighlightsMutation.data ?? null;
  const reindexHighlightsError = errMsg(reindexHighlightsMutation.error, 'Failed to reindex highlights');
  const handleReindexHighlights = () => reindexHighlightsMutation.mutate();

  // ----- Reset collections (media + mixlists share one result/error panel) -----
  // Reset the sibling mutation before firing so only the latest result shows.
  const resetMediaMutation = useTypesenseResetMediaItems();
  const resetMixlistsMutation = useTypesenseResetMixlists();
  const resetting = resetMediaMutation.isPending || resetMixlistsMutation.isPending;
  const resetResult = resetMediaMutation.data ?? resetMixlistsMutation.data ?? null;
  const resetError = resetMediaMutation.error
    ? errMsg(resetMediaMutation.error, 'Failed to reset media items collection')
    : errMsg(resetMixlistsMutation.error, 'Failed to reset mixlists collection');
  const handleResetMediaItems = () => {
    if (!window.confirm('⚠️ WARNING: This will delete ALL media items from the search index! This action cannot be undone. Continue?')) return;
    resetMixlistsMutation.reset();
    resetMediaMutation.mutate();
  };
  const handleResetMixlists = () => {
    if (!window.confirm('⚠️ WARNING: This will delete ALL mixlists from the search index! This action cannot be undone. Continue?')) return;
    resetMediaMutation.reset();
    resetMixlistsMutation.mutate();
  };

  const resetHighlightsMutation = useResetHighlightsCollection();
  const resettingHighlights = resetHighlightsMutation.isPending;
  const resetHighlightsResult = resetHighlightsMutation.data ?? null;
  const resetHighlightsError = errMsg(resetHighlightsMutation.error, 'Failed to reset highlights collection');
  const handleResetHighlights = () => {
    if (!window.confirm('WARNING: This will delete ALL highlights from the search index! This action cannot be undone. Continue?')) return;
    resetHighlightsMutation.mutate();
  };

  // ----- Article deduplication (find = on-demand query, merge = mutation) -----
  const dupQuery = useDuplicateArticles({ enabled: false, retry: false });
  const duplicates = dupQuery.data ?? null;
  const findingDuplicates = dupQuery.isFetching;
  const dedupMutation = useDeduplicateArticles();
  const deduplicating = dedupMutation.isPending;
  const deduplicationResult = dedupMutation.data?.data ?? null;
  const deduplicationError = errMsg(dupQuery.error, 'Failed to find duplicate articles')
    || errMsg(dedupMutation.error, 'Failed to deduplicate articles');
  const handleFindDuplicates = () => {
    dedupMutation.reset();
    dupQuery.refetch();
  };
  const handleDeduplicate = () => {
    if (!window.confirm('⚠️ This will merge duplicate articles based on normalized URLs. Articles with the same URL will be combined into a single entry. Continue?')) return;
    dedupMutation.mutate(undefined, {
      onSuccess: (response) => {
        if (response?.data?.success) dupQuery.refetch();
      },
    });
  };

  // ----- Obsidian notes (sync-status query + sync/reindex/reset mutations) -----
  // Sync mutation invalidates noteKeys.all, so the status card refreshes itself.
  const noteSyncStatusQuery = useNoteSyncStatus({ retry: false });
  const noteSyncStatus = noteSyncStatusQuery.data ?? null;

  const syncNotesMutation = useSyncAllVaults();
  const syncingNotes = syncNotesMutation.isPending;
  const syncNotesResult = syncNotesMutation.data ?? null;
  const syncNotesError = errMsg(syncNotesMutation.error, 'Failed to sync notes from vaults');
  const handleSyncNotes = () => syncNotesMutation.mutate();

  const reindexNotesMutation = useTypesenseReindexNotes();
  const reindexingNotes = reindexNotesMutation.isPending;
  const reindexNotesResult = reindexNotesMutation.data ?? null;
  const reindexNotesError = errMsg(reindexNotesMutation.error, 'Failed to reindex notes');
  const handleReindexNotes = () => reindexNotesMutation.mutate();

  const resetNotesMutation = useTypesenseResetNotesCollection();
  const resettingNotes = resetNotesMutation.isPending;
  const resetNotesResult = resetNotesMutation.data ?? null;
  const resetNotesError = errMsg(resetNotesMutation.error, 'Failed to reset notes collection');
  const handleResetNotes = () => {
    if (!window.confirm('WARNING: This will delete ALL notes from the search index! This action cannot be undone. Continue?')) return;
    resetNotesMutation.mutate();
  };

  // ----- Search testing (button-triggered query via a submitted mirror state) -----
  const [searchQuery, setSearchQuery] = useState('');
  const [searchType, setSearchType] = useState('all');
  const [submittedSearch, setSubmittedSearch] = useState(null);
  const [searchValidationError, setSearchValidationError] = useState(null);
  const searchTestQuery = useTypesenseSearch(submittedSearch?.query, submittedSearch?.type ?? 'all', 1, 20);
  const searchResults = submittedSearch ? (searchTestQuery.data ?? null) : null;
  const searchLoading = searchTestQuery.isFetching;
  const searchError = searchValidationError || errMsg(searchTestQuery.error, 'Search failed');
  const handleSearchTest = () => {
    if (!searchQuery.trim()) {
      setSearchValidationError('Please enter a search query');
      return;
    }
    setSearchValidationError(null);
    setSubmittedSearch({ query: searchQuery, type: searchType });
  };

  const mediaTypes = [
    { value: 'all', label: 'All Types' },
    { value: 'Book', label: 'Books' },
    { value: 'Article', label: 'Articles' },
    { value: 'Movie', label: 'Movies' },
    { value: 'TVShow', label: 'TV Shows' },
    { value: 'Video', label: 'Videos' },
    { value: 'Podcast', label: 'Podcasts' },
    { value: 'Website', label: 'Websites' },
  ];

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h3" gutterBottom sx={{ mb: 4, fontWeight: 'bold' }}>
        Typesense Administration
      </Typography>

      {/* Real-Time Indexing Toggle */}
      <Paper elevation={3} sx={{ p: 3, mb: 3, border: !realTimeIndexing ? '2px solid #f44336' : 'none' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            Real-Time Indexing
          </Typography>
          <FormControlLabel
            control={
              <Switch
                checked={realTimeIndexing}
                onChange={handleToggleRealTimeIndexing}
                disabled={realTimeIndexingLoading}
                color="success"
              />
            }
            label={realTimeIndexingLoading ? 'Updating...' : (realTimeIndexing ? 'Enabled' : 'Paused')}
            labelPlacement="start"
          />
        </Box>

        {realTimeIndexing ? (
          <Alert severity="info" icon={<InfoIcon />}>
            Media items are indexed in Typesense immediately after each create, update, or delete. Toggle this off before bulk imports to avoid hundreds of individual index operations.
          </Alert>
        ) : (
          <Alert severity="warning" icon={<ErrorIcon />}>
            Real-time indexing is <strong>paused</strong>. New media items will NOT appear in search results until you run a bulk reindex below. This setting resets to enabled when the server restarts.
          </Alert>
        )}

        {realTimeIndexingError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Toggle Failed:</strong> {realTimeIndexingError}
          </Alert>
        )}
      </Paper>

      {/* Health Status Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            System Health
          </Typography>
          <Button
            variant="contained"
            startIcon={<RefreshIcon />}
            onClick={checkHealth}
            disabled={healthLoading}
            sx={{
              backgroundColor: '#fcfafa',
              color: '#1B1B1B',
              '&:hover': {
                backgroundColor: '#e0e0e0'
              }
            }}
          >
            Refresh
          </Button>
        </Box>

        {healthLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
            <CircularProgress />
          </Box>
        )}

        {healthError && (
          <Alert severity="error" icon={<ErrorIcon />} sx={{ mb: 2 }}>
            <strong>Health Check Failed:</strong> {healthError}
          </Alert>
        )}

        {healthStatus && (
          <Card variant="outlined" sx={{ bgcolor: 'success.light', color: 'success.contrastText' }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CheckCircleIcon sx={{ fontSize: 40, color: 'success.main' }} />
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                    {healthStatus.status?.toUpperCase() || 'HEALTHY'}
                  </Typography>
                  <Typography variant="body2">
                    {healthStatus.message || 'Typesense integration is operational.'}
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        )}
      </Paper>

      {/* Bulk Reindex Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Bulk Reindex
        </Typography>
        
        <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 2 }}>
          Reindex syncs data from your database to Typesense. Use this after adding or modifying content, or if search results seem out of sync.
        </Alert>

        <Grid container spacing={2}>
          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reindex Media Items
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Syncs all media items (books, articles, videos, etc.) from your database to the search index.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={reindexing ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleReindex}
                  disabled={reindexing}
                  fullWidth
                >
                  {reindexing ? 'Reindexing...' : 'Reindex Media Items'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reindex Mixlists
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Syncs all mixlists and their associated topics/genres to the search index.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={reindexingMixlists ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleReindexMixlists}
                  disabled={reindexingMixlists}
                  fullWidth
                >
                  {reindexingMixlists ? 'Reindexing...' : 'Reindex Mixlists'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reindex Highlights
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Syncs all Readwise highlights from your database to the search index.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={reindexingHighlights ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleReindexHighlights}
                  disabled={reindexingHighlights}
                  fullWidth
                >
                  {reindexingHighlights ? 'Reindexing...' : 'Reindex Highlights'}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Media Items Reindex Results */}
        {reindexError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Media Items Reindex Failed:</strong> {reindexError}
          </Alert>
        )}

        {reindexResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: '#fcfafa' }}>
                ✓ Media Items Reindex Complete
              </Typography>
              
              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>
                  {reindexResult.indexed_count || reindexResult.indexedCount || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Media Items Indexed
                </Typography>
              </Box>

              {reindexResult.message && (
                <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic' }}>
                  {reindexResult.message}
                </Typography>
              )}
            </CardContent>
          </Card>
        )}

        {/* Mixlists Reindex Results */}
        {reindexMixlistsError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Mixlists Reindex Failed:</strong> {reindexMixlistsError}
          </Alert>
        )}

        {reindexMixlistsResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: '#fcfafa' }}>
                ✓ Mixlists Reindex Complete
              </Typography>
              
              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>
                  {reindexMixlistsResult.indexed_count || reindexMixlistsResult.indexedCount || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Mixlists Indexed
                </Typography>
              </Box>

              {reindexMixlistsResult.message && (
                <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic' }}>
                  {reindexMixlistsResult.message}
                </Typography>
              )}
            </CardContent>
          </Card>
        )}

        {/* Highlights Reindex Results */}
        {reindexHighlightsError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Highlights Reindex Failed:</strong> {reindexHighlightsError}
          </Alert>
        )}

        {reindexHighlightsResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: '#fcfafa' }}>
                Highlights Reindex Complete
              </Typography>

              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>
                  {reindexHighlightsResult.indexed_count || reindexHighlightsResult.indexedCount || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Highlights Indexed
                </Typography>
              </Box>

              {reindexHighlightsResult.message && (
                <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic' }}>
                  {reindexHighlightsResult.message}
                </Typography>
              )}
            </CardContent>
          </Card>
        )}
      </Paper>

      {/* Reset Collections Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Reset Collections
        </Typography>
        
        <Alert severity="warning" icon={<ErrorIcon />} sx={{ mb: 2 }}>
          <strong>⚠️ WARNING:</strong> Resetting will permanently delete all data from the Typesense collection and recreate it empty. 
          Use this when you need to completely clear old data (e.g., after clearing the database). 
          This is different from reindexing, which syncs data from the database.
        </Alert>

        <Grid container spacing={2}>
          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reset Media Items
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Deletes and recreates the media_items collection. All indexed media will be removed from search.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={resetting ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleResetMediaItems}
                  disabled={resetting}
                  fullWidth
                >
                  {resetting ? 'Resetting...' : 'Reset Media Items Collection'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reset Mixlists
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Deletes and recreates the mixlists collection. All indexed mixlists will be removed from search.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={resetting ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleResetMixlists}
                  disabled={resetting}
                  fullWidth
                >
                  {resetting ? 'Resetting...' : 'Reset Mixlists Collection'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reset Highlights
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Deletes and recreates the highlights collection. All indexed highlights will be removed from search.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={resettingHighlights ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleResetHighlights}
                  disabled={resettingHighlights}
                  fullWidth
                >
                  {resettingHighlights ? 'Resetting...' : 'Reset Highlights Collection'}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {resetError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Reset Failed:</strong> {resetError}
          </Alert>
        )}

        {resetResult && (
          <Alert severity="success" sx={{ mt: 2 }}>
            <strong>✓ Reset Complete:</strong> {resetResult.message || 'Collection has been reset successfully.'}
          </Alert>
        )}

        {resetHighlightsError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Highlights Reset Failed:</strong> {resetHighlightsError}
          </Alert>
        )}

        {resetHighlightsResult && (
          <Alert severity="success" sx={{ mt: 2 }}>
            <strong>Reset Complete:</strong> {resetHighlightsResult.message || 'Highlights collection has been reset successfully.'}
          </Alert>
        )}
      </Paper>

      {/* Article Deduplication Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Article Deduplication
        </Typography>
        
        <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 2 }}>
          Find and merge duplicate articles. Articles with the same normalized URL will be combined into a single article, preserving all metadata.
        </Alert>

        <Grid container spacing={2}>
          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Find Duplicates
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Scan for articles with matching URLs (after normalization). This is a preview only - no changes will be made.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={findingDuplicates ? <CircularProgress size={20} color="inherit" /> : <SearchIcon />}
                  onClick={handleFindDuplicates}
                  disabled={findingDuplicates || deduplicating}
                  fullWidth
                >
                  {findingDuplicates ? 'Scanning...' : 'Find Duplicates'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Merge Duplicates
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Automatically merge duplicate articles. The most complete article will be kept, with data from duplicates merged in.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={deduplicating ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleDeduplicate}
                  disabled={deduplicating || findingDuplicates}
                  fullWidth
                >
                  {deduplicating ? 'Merging...' : 'Merge Duplicates'}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Duplicates Found Results */}
        {duplicates && (
          <Card variant="outlined" sx={{ mt: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                Duplicate Scan Results
              </Typography>
              
              <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                <Chip 
                  label={`${duplicates.count} Duplicate Groups`} 
                  color="primary" 
                  sx={{ fontWeight: 'bold' }}
                />
                <Chip 
                  label={`${duplicates.totalDuplicates} Articles to Merge`} 
                  color="warning" 
                  sx={{ fontWeight: 'bold' }}
                />
              </Box>

              {duplicates.count > 0 ? (
                <Alert severity="warning" sx={{ mb: 2 }}>
                  Found {duplicates.totalDuplicates} duplicate article{duplicates.totalDuplicates !== 1 ? 's' : ''} across {duplicates.count} URL{duplicates.count !== 1 ? 's' : ''}. Click &quot;Merge Duplicates&quot; to combine them.
                </Alert>
              ) : (
                <Alert severity="success">
                  No duplicate articles found! Your article library is clean.
                </Alert>
              )}

              {duplicates.groups && duplicates.groups.length > 0 && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
                    Duplicate Groups (showing first 5):
                  </Typography>
                  <List>
                    {duplicates.groups.slice(0, 5).map((group) => (
                      <ListItem key={`dup-${group.normalizedUrl}`} divider>
                        <ListItemText
                          primary={group.normalizedUrl}
                          secondary={`${group.articles.length} articles with this URL`}
                        />
                      </ListItem>
                    ))}
                  </List>
                  {duplicates.groups.length > 5 && (
                    <Typography variant="caption" color="textSecondary" sx={{ mt: 1, display: 'block' }}>
                      ...and {duplicates.groups.length - 5} more group{duplicates.groups.length - 5 !== 1 ? 's' : ''}
                    </Typography>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        )}

        {/* Deduplication Results */}
        {deduplicationError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Deduplication Failed:</strong> {deduplicationError}
          </Alert>
        )}

        {deduplicationResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: '#fcfafa' }}>
                ✓ Deduplication Complete
              </Typography>
              
              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>
                  {deduplicationResult.mergedCount || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Articles Merged into {deduplicationResult.groupCount || 0} Primary Article{deduplicationResult.groupCount !== 1 ? 's' : ''}
                </Typography>
              </Box>

              {deduplicationResult.duration && (
                <Typography variant="body2" sx={{ mt: 2, textAlign: 'center', fontStyle: 'italic' }}>
                  Completed in {Math.round(deduplicationResult.duration.totalSeconds || 0)} seconds
                </Typography>
              )}
            </CardContent>
          </Card>
        )}
      </Paper>

      {/* Obsidian Notes Sync Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Obsidian Notes
        </Typography>

        <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 2 }}>
          Sync notes from your Quartz-published Obsidian vaults and index them in Typesense for search.
        </Alert>

        {/* Sync Status */}
        {noteSyncStatus && (
          <Card variant="outlined" sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 1 }}>
                Sync Configuration
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                <Chip
                  label={noteSyncStatus.backgroundSyncEnabled ? 'Background Sync Enabled' : 'Background Sync Disabled'}
                  color={noteSyncStatus.backgroundSyncEnabled ? 'success' : 'default'}
                  size="small"
                />
                {noteSyncStatus.generalVaultConfigured && (
                  <Chip label="General Vault Configured" color="primary" size="small" />
                )}
                {noteSyncStatus.programmingVaultConfigured && (
                  <Chip label="Programming Vault Configured" color="primary" size="small" />
                )}
                {noteSyncStatus.lastSyncTime && (
                  <Chip label={`Last Sync: ${new Date(noteSyncStatus.lastSyncTime).toLocaleString()}`} size="small" variant="outlined" />
                )}
              </Box>
            </CardContent>
          </Card>
        )}

        <Grid container spacing={2}>
          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Sync from Vaults
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Import notes from your configured Quartz vaults. New notes are added, existing notes are updated if content changed.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={syncingNotes ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleSyncNotes}
                  disabled={syncingNotes}
                  fullWidth
                >
                  {syncingNotes ? 'Syncing...' : 'Sync All Vaults'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reindex Notes
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Reindex all notes from the database to Typesense. Use this if search results are out of sync.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={reindexingNotes ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleReindexNotes}
                  disabled={reindexingNotes}
                  fullWidth
                >
                  {reindexingNotes ? 'Reindexing...' : 'Reindex Notes'}
                </Button>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reset Notes Collection
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Delete and recreate the notes collection in Typesense. All indexed notes will be removed.
                </Typography>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={resettingNotes ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
                  onClick={handleResetNotes}
                  disabled={resettingNotes}
                  fullWidth
                >
                  {resettingNotes ? 'Resetting...' : 'Reset Notes Collection'}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Sync Results */}
        {syncNotesError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Sync Failed:</strong> {syncNotesError}
          </Alert>
        )}

        {syncNotesResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: '#fcfafa' }}>
                Sync Complete
              </Typography>

              <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                {syncNotesResult.results && syncNotesResult.results.map((result, index) => (
                  <Box key={`sync-${result.vaultName || index}`} sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1, minWidth: 120 }}>
                    <Typography variant="subtitle2" color="textSecondary">
                      {result.vaultName || `Vault ${index + 1}`}
                    </Typography>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', color: 'white' }}>
                      {result.importedCount || 0}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      Imported/Updated
                    </Typography>
                  </Box>
                ))}
              </Box>

              {syncNotesResult.message && (
                <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic' }}>
                  {syncNotesResult.message}
                </Typography>
              )}
            </CardContent>
          </Card>
        )}

        {/* Reindex Results */}
        {reindexNotesError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Reindex Failed:</strong> {reindexNotesError}
          </Alert>
        )}

        {reindexNotesResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: '#fcfafa' }}>
                Notes Reindex Complete
              </Typography>

              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>
                  {reindexNotesResult.indexed_count || reindexNotesResult.indexedCount || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Notes Indexed
                </Typography>
              </Box>

              {reindexNotesResult.message && (
                <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic' }}>
                  {reindexNotesResult.message}
                </Typography>
              )}
            </CardContent>
          </Card>
        )}

        {/* Reset Results */}
        {resetNotesError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Reset Failed:</strong> {resetNotesError}
          </Alert>
        )}

        {resetNotesResult && (
          <Alert severity="success" sx={{ mt: 2 }}>
            <strong>Reset Complete:</strong> {resetNotesResult.message || 'Notes collection has been reset successfully.'}
          </Alert>
        )}
      </Paper>

      {/* Search Testing Section */}
      <Paper elevation={3} sx={{ p: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Search Testing
        </Typography>
        
        <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 2 }}>
          Test your Typesense search functionality by entering a query below. This uses the same search API as your application.
        </Alert>

        <Grid container spacing={2} sx={{ mb: 2 }}>
          <Grid item xs={12} md={8}>
            <TextField
              fullWidth
              label="Search Query"
              variant="outlined"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={(e) => {
                if (e.key === 'Enter') {
                  handleSearchTest();
                }
              }}
              placeholder="Enter search terms..."
              InputLabelProps={{
                sx: { color: 'white' }
              }}
            />
          </Grid>

          <Grid item xs={12} md={4}>
            <FormControl fullWidth>
              <InputLabel>Media Type</InputLabel>
              <Select
                value={searchType}
                label="Media Type"
                onChange={(e) => setSearchType(e.target.value)}
              >
                {mediaTypes.map((type) => (
                  <MenuItem key={type.value} value={type.value}>
                    {type.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
        </Grid>

        <Button
          variant="contained"
          color="error"
          startIcon={searchLoading ? <CircularProgress size={20} color="inherit" /> : <SearchIcon />}
          onClick={handleSearchTest}
          disabled={searchLoading || !searchQuery.trim()}
        >
          {searchLoading ? 'Searching...' : 'Test Search'}
        </Button>

        {searchError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Search Failed:</strong> {searchError}
          </Alert>
        )}

        {searchResults && (
          <Card variant="outlined" sx={{ mt: 2 }}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                  Search Results
                </Typography>
                <Chip
                  label={`${searchResults.found || 0} results`}
                  color="primary"
                  size="small"
                />
              </Box>

              <Divider sx={{ mb: 2 }} />

              {searchResults.hits && searchResults.hits.length > 0 ? (
                <List>
                  {searchResults.hits.slice(0, 10).map((hit, index) => {
                    const doc = hit.document;
                    return (
                      <ListItem
                        key={doc.id || index}
                        sx={{
                          border: '1px solid',
                          borderColor: 'divider',
                          borderRadius: 1,
                          mb: 1,
                          flexDirection: 'column',
                          alignItems: 'flex-start',
                        }}
                      >
                        <Box sx={{ width: '100%', display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                          <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                            {doc.title}
                          </Typography>
                          <Chip label={doc.media_type} size="small" color="secondary" />
                        </Box>

                        {doc.description && (
                          <Typography variant="body2" color="textSecondary" sx={{ mb: 1 }}>
                            {doc.description.substring(0, 150)}
                            {doc.description.length > 150 ? '...' : ''}
                          </Typography>
                        )}

                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          {doc.author && (
                            <Chip label={`Author: ${doc.author}`} size="small" variant="outlined" />
                          )}
                          {doc.director && (
                            <Chip label={`Director: ${doc.director}`} size="small" variant="outlined" />
                          )}
                          {doc.creator && (
                            <Chip label={`Creator: ${doc.creator}`} size="small" variant="outlined" />
                          )}
                          {doc.status && (
                            <Chip label={formatStatus(doc.status)} size="small" variant="outlined" />
                          )}
                          {doc.rating && (
                            <Chip label={doc.rating} size="small" variant="outlined" color="primary" />
                          )}
                          {hit.text_match && (
                            <Chip
                              label={`Match Score: ${Math.round(hit.text_match / 1000000)}`}
                              size="small"
                              color="info"
                            />
                          )}
                        </Box>

                        {(doc.topics && doc.topics.length > 0) && (
                          <Box sx={{ mt: 1 }}>
                            <Typography variant="caption" color="textSecondary">
                              Topics: {doc.topics.join(', ')}
                            </Typography>
                          </Box>
                        )}

                        {(doc.genres && doc.genres.length > 0) && (
                          <Box sx={{ mt: 0.5 }}>
                            <Typography variant="caption" color="textSecondary">
                              Genres: {doc.genres.join(', ')}
                            </Typography>
                          </Box>
                        )}
                      </ListItem>
                    );
                  })}
                </List>
              ) : (
                <Alert severity="info">
                  No results found for &quot;{searchQuery}&quot;
                </Alert>
              )}

              {searchResults.hits && searchResults.hits.length > 10 && (
                <Typography variant="caption" color="textSecondary" sx={{ mt: 2, display: 'block', textAlign: 'center' }}>
                  Showing first 10 of {searchResults.found} results
                </Typography>
              )}
            </CardContent>
          </Card>
        )}
      </Paper>
    </Container>
  );
};

export default TypesenseAdminPage;

