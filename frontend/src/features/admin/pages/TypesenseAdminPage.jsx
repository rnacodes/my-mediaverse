import React from 'react';
import { Container, Paper, Typography, Button, Box, Alert, CircularProgress, Card, CardContent, Grid, Chip, List, ListItem, ListItemText } from '@mui/material';
import { Refresh as RefreshIcon, Search as SearchIcon, CheckCircle as CheckCircleIcon, Error as ErrorIcon, Info as InfoIcon } from '@mui/icons-material';
import {
  useTypesenseHealth,
  useTypesenseReindex, useTypesenseReindexMixlists, useReindexHighlights,
  useTypesenseResetMediaItems, useTypesenseResetMixlists, useResetHighlightsCollection,
  useTypesenseReindexNotes, useTypesenseResetNotesCollection,
} from '@/hooks/useTypesense';
import { useDuplicateArticles, useDeduplicateArticles } from '@/hooks/useArticle';
import { useSyncAllVaults, useNoteSyncStatus } from '@/hooks/useNote';

// Typesense/admin errors surface via data.message.
const errMsg = (error, fallback) =>
  error ? (error.response?.data?.message || error.message || fallback) : null;

const TypesenseAdminPage = () => {
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

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h3" gutterBottom sx={{ mb: 4, fontWeight: 'bold' }}>
        Typesense Administration
      </Typography>

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

        {/* Obsidian vault sync status */}
        {noteSyncStatus && (
          <Card variant="outlined" sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 1 }}>
                Obsidian Sync Configuration
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

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Sync Notes from Vaults
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

          <Grid item xs={12} md={6}>
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

        {/* Vault Sync Results */}
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

        {/* Notes Reindex Results */}
        {reindexNotesError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Notes Reindex Failed:</strong> {reindexNotesError}
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

          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Reset Notes
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                  Deletes and recreates the notes collection. All indexed notes will be removed from search.
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

        {resetNotesError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Notes Reset Failed:</strong> {resetNotesError}
          </Alert>
        )}

        {resetNotesResult && (
          <Alert severity="success" sx={{ mt: 2 }}>
            <strong>Reset Complete:</strong> {resetNotesResult.message || 'Notes collection has been reset successfully.'}
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

    </Container>
  );
};

export default TypesenseAdminPage;

