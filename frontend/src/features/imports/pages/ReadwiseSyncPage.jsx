import React, { useState } from 'react';
import {
  useValidateReadwiseConnection,
  useReadwiseSyncAll,
  useReadwiseFetchArticleContent,
} from '@/hooks/useReadwise';
import { useCleanHighlightText } from '@/hooks/useHighlight';
import './ReadwiseSyncPage.css';

const ReadwiseSyncPage = () => {
  const [syncResult, setSyncResult] = useState(null);
  const [fetchResult, setFetchResult] = useState(null);
  const [error, setError] = useState(null);

  // Connection validation: query gated to button click (enabled:false + refetch()).
  const validateQuery = useValidateReadwiseConnection({ enabled: false, retry: false });
  const connectionStatus = validateQuery.data
    ?? (validateQuery.error
        ? { connected: false, message: validateQuery.error.response?.data?.message || 'Connection failed' }
        : null);

  // Sync / fetch mutations (single shared busy flag, as before)
  const syncMutation = useReadwiseSyncAll();
  const fetchMutation = useReadwiseFetchArticleContent();
  const loading = validateQuery.isFetching || syncMutation.isPending || fetchMutation.isPending;

  // Highlight cleanup mutation — its result/pending drive the UI directly
  const cleanMutation = useCleanHighlightText();
  const cleaningHighlights = cleanMutation.isPending;
  const cleanResult = cleanMutation.data;

  const handleCleanHighlightText = () => {
    setError(null);
    cleanMutation.mutate(undefined, {
      onError: (err) => setError(`Cleanup failed: ${err.response?.data?.details || err.message}`),
    });
  };

  const handleValidateConnection = async () => {
    setError(null);
    const result = await validateQuery.refetch();
    if (result.error) {
      const errorDetails = result.error.response?.data?.details || result.error.response?.data?.message || result.error.message;
      setError(`Connection validation failed: ${errorDetails}`);
    } else if (result.data && !result.data.connected && result.data.details) {
      setError(result.data.details);
    }
  };

  const handleSync = (incremental = true) => {
    setError(null);
    setSyncResult(null);
    syncMutation.mutate(incremental, {
      onSuccess: (response) => setSyncResult(response.data),
      onError: (err) => setError(`Sync failed: ${err.response?.data?.details || err.message}`),
    });
  };

  const handleFetchContent = (batchSize, recentOnly = false) => {
    setError(null);
    setFetchResult(null);
    fetchMutation.mutate({ batchSize, recentOnly }, {
      onSuccess: (response) => setFetchResult(response.data),
      onError: (err) => setError(`Content fetch failed: ${err.response?.data?.details || err.message}`),
    });
  };

  const formatDuration = (duration) => {
    if (!duration) return 'N/A';
    const match = duration.match(/(\d+):(\d+):(\d+)/);
    if (!match) return duration;
    const [, hours, minutes, seconds] = match;
    if (parseInt(hours) > 0) return `${hours}h ${minutes}m ${seconds}s`;
    if (parseInt(minutes) > 0) return `${minutes}m ${seconds}s`;
    return `${seconds}s`;
  };

  return (
    <div className="readwise-sync-page">
      <div className="page-header">
        <h1 style={{ color: 'white' }}>Readwise Sync</h1>
        <p className="subtitle" style={{ color: 'white' }}>
          Sync your articles and highlights from Readwise Reader
        </p>
      </div>

      {error && (
        <div className="alert alert-error">
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* Connection Status Section */}
      <section className="sync-section">
        <h2>Connection Status</h2>
        <p>Verify your Readwise API connection before syncing</p>

        <button
          onClick={handleValidateConnection}
          disabled={loading}
          className="btn btn-secondary"
        >
          {loading ? 'Validating...' : 'Validate Connection'}
        </button>

        {connectionStatus && (
          <div className={`connection-status ${connectionStatus.connected ? 'connected' : 'disconnected'}`}>
            <span className="status-icon">{connectionStatus.connected ? '\u2705' : '\u274C'}</span>
            <span className="status-message">{connectionStatus.message}</span>
          </div>
        )}
      </section>

      {/* Sync Section */}
      <section className="sync-section">
        <h2>Sync Articles & Highlights</h2>
        <p>
          Imports article metadata from Reader and highlights from Readwise.
          Updates reading progress and status, and links highlights to articles automatically.
        </p>

        <div className="status-mapping-info">
          <strong>Reader status is synced to your library:</strong>
          <ul>
            <li><span className="status-badge uncharted">New / Later / Feed</span> &rarr; To Be Explored</li>
            <li><span className="status-badge completed">Archive</span> &rarr; Completed</li>
          </ul>
        </div>

        <div className="sync-buttons">
          <button
            onClick={() => handleSync(false)}
            disabled={loading}
            className="btn btn-primary"
          >
            {loading ? 'Syncing...' : 'Full Sync'}
          </button>
          <button
            onClick={() => handleSync(true)}
            disabled={loading}
            className="btn btn-secondary"
          >
            {loading ? 'Syncing...' : 'Sync Last 7 Days'}
          </button>
        </div>

        <div className="info-note">
          Use this regularly to keep your library in sync. Fast and lightweight.
        </div>

        {syncResult && (
          <div className={`sync-result ${syncResult.success ? 'success' : 'error'}`}>
            <h3>Sync Results</h3>
            <div className="result-grid">
              <div className="result-item">
                <span className="result-label">Status:</span>
                <span className="result-value">
                  {syncResult.success ? '\u2705 Success' : '\u274C Failed'}
                </span>
              </div>
              <div className="result-item">
                <span className="result-label">Articles Created:</span>
                <span className="result-value">{syncResult.articlesCreated}</span>
              </div>
              <div className="result-item">
                <span className="result-label">Articles Updated:</span>
                <span className="result-value">{syncResult.articlesUpdated}</span>
              </div>
              <div className="result-item">
                <span className="result-label">Highlights Created:</span>
                <span className="result-value">{syncResult.highlightsCreated}</span>
              </div>
              <div className="result-item">
                <span className="result-label">Highlights Updated:</span>
                <span className="result-value">{syncResult.highlightsUpdated}</span>
              </div>
              <div className="result-item">
                <span className="result-label">Highlights Linked:</span>
                <span className="result-value">{syncResult.highlightsLinked}</span>
              </div>
              {syncResult.duration && (
                <div className="result-item">
                  <span className="result-label">Duration:</span>
                  <span className="result-value">{formatDuration(syncResult.duration)}</span>
                </div>
              )}
            </div>
            {syncResult.errorMessage && (
              <div className="alert alert-error" style={{ marginTop: '1rem', marginBottom: 0 }}>
                <strong>Error:</strong> {syncResult.errorMessage}
              </div>
            )}
            {syncResult.warningMessage && (
              <div className="alert alert-warning" style={{ marginTop: '1rem', marginBottom: 0 }}>
                <strong>Warning:</strong> {syncResult.warningMessage}
              </div>
            )}
          </div>
        )}
      </section>

      {/* Fetch Content Section */}
      <section className="sync-section">
        <h2>Fetch Article Content (Archival)</h2>
        <p>
          Downloads the full HTML content for articles you&apos;ve archived in Reader.
          This creates a permanent local copy for archival purposes.
        </p>

        <div className="info-note warning">
          <strong>Note:</strong> Only fetches Completed/Archived articles without content.
          Articles are processed in order - clicking again fetches the next batch.
        </div>

        <div className="sync-buttons">
          <button
            onClick={() => handleFetchContent(25, false)}
            disabled={loading}
            className="btn btn-primary"
          >
            {loading ? 'Fetching...' : 'Fetch 25'}
          </button>
          <button
            onClick={() => handleFetchContent(50, false)}
            disabled={loading}
            className="btn btn-secondary"
          >
            {loading ? 'Fetching...' : 'Fetch 50'}
          </button>
          <button
            onClick={() => handleFetchContent(50, true)}
            disabled={loading}
            className="btn btn-secondary"
          >
            {loading ? 'Fetching...' : 'Fetch Recently Synced (7 days)'}
          </button>
        </div>

        <div className="info-note">
          This is slower - content is fetched one article at a time from Reader&apos;s servers.
        </div>

        {fetchResult && (
          <div className={`sync-result ${fetchResult.fetchedCount > 0 ? 'success' : ''}`}>
            <h3>Fetch Results</h3>
            <div className="result-grid">
              <div className="result-item">
                <span className="result-label">Fetched:</span>
                <span className="result-value">{fetchResult.fetchedCount} article{fetchResult.fetchedCount === 1 ? '' : 's'}</span>
              </div>
              {fetchResult.message && (
                <div className="result-item full-width">
                  <span className="result-label">Message:</span>
                  <span className="result-value">{fetchResult.message}</span>
                </div>
              )}
            </div>
          </div>
        )}
      </section>

      {/* How It Works Section */}
      <section className="info-box">
        <h3>How It Works</h3>
        <ul>
          <li><strong>Sync:</strong> Updates metadata & status from Readwise (fast)</li>
          <li><strong>Fetch:</strong> Downloads full article text for archival (slow)</li>
          <li><strong>Archive = Content:</strong> Only archived articles get full content saved</li>
          <li><strong>Recommended:</strong> Run Sync regularly; Fetch when you want to archive</li>
        </ul>
      </section>

      {/* Maintenance Section */}
      <section className="sync-section">
        <h2>Maintenance</h2>
        <p>
          Clean highlight text to remove any CSS or HTML formatting that may have been accidentally stored.
        </p>

        <button
          onClick={handleCleanHighlightText}
          disabled={cleaningHighlights || loading}
          className="btn btn-secondary"
        >
          {cleaningHighlights ? 'Cleaning...' : 'Clean Highlight Text'}
        </button>

        {cleanResult && (
          <div className="sync-result success" style={{ marginTop: '1rem' }}>
            <div className="result-grid">
              <div className="result-item">
                <span className="result-label">Highlights Cleaned:</span>
                <span className="result-value">{cleanResult.cleanedCount}</span>
              </div>
              <div className="result-item full-width">
                <span className="result-label">Status:</span>
                <span className="result-value">{cleanResult.message}</span>
              </div>
            </div>
          </div>
        )}
      </section>

    </div>
  );
};

export default ReadwiseSyncPage;
