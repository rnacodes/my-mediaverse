import React, { useState, useEffect, useRef } from 'react';
import {
  getTraktStatus,
  startDeviceAuth,
  pollDeviceToken,
  disconnectTrakt,
  syncWatched,
  syncWatchlist,
  syncRatings,
  syncAll
} from '../api/traktService';
import './TraktSyncPage.css';

const TraktSyncPage = () => {
  const [connectionStatus, setConnectionStatus] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [syncResult, setSyncResult] = useState(null);

  // Device auth state
  const [deviceAuth, setDeviceAuth] = useState(null);
  const [polling, setPolling] = useState(false);
  const pollTimerRef = useRef(null);
  const pollExpiryRef = useRef(null);

  // Check connection status on mount
  useEffect(() => {
    checkStatus();
    return () => {
      if (pollTimerRef.current) clearInterval(pollTimerRef.current);
      if (pollExpiryRef.current) clearTimeout(pollExpiryRef.current);
    };
  }, []);

  const checkStatus = async () => {
    try {
      const response = await getTraktStatus();
      setConnectionStatus(response.data);
    } catch (err) {
      console.error('Error checking Trakt status:', err);
      setConnectionStatus({ connected: false });
    }
  };

  const handleStartAuth = async () => {
    setLoading(true);
    setError(null);
    setDeviceAuth(null);
    try {
      const response = await startDeviceAuth();
      const data = response.data;
      setDeviceAuth(data);
      startPolling(data.deviceCode, data.interval || 5, data.expiresIn || 600);
    } catch (err) {
      setError(`Failed to start authentication: ${err.response?.data?.details || err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const startPolling = (deviceCode, interval, expiresIn) => {
    setPolling(true);

    // Poll at the specified interval
    pollTimerRef.current = setInterval(async () => {
      try {
        const response = await pollDeviceToken(deviceCode);
        const data = response.data;

        if (data.status === 'authorized') {
          // Success - stop polling and refresh status
          stopPolling();
          setDeviceAuth(null);
          await checkStatus();
        } else if (data.status === 'failed') {
          stopPolling();
          setError(data.message || 'Authorization failed');
          setDeviceAuth(null);
        }
        // status === 'pending' -> continue polling
      } catch (err) {
        stopPolling();
        setError(`Authorization failed: ${err.response?.data?.message || err.message}`);
        setDeviceAuth(null);
      }
    }, interval * 1000);

    // Auto-expire after the timeout
    pollExpiryRef.current = setTimeout(() => {
      stopPolling();
      setError('Device code expired. Please try again.');
      setDeviceAuth(null);
    }, expiresIn * 1000);
  };

  const stopPolling = () => {
    setPolling(false);
    if (pollTimerRef.current) {
      clearInterval(pollTimerRef.current);
      pollTimerRef.current = null;
    }
    if (pollExpiryRef.current) {
      clearTimeout(pollExpiryRef.current);
      pollExpiryRef.current = null;
    }
  };

  const handleDisconnect = async () => {
    setLoading(true);
    setError(null);
    try {
      await disconnectTrakt();
      setConnectionStatus({ connected: false });
      setSyncResult(null);
    } catch (err) {
      setError(`Failed to disconnect: ${err.response?.data?.details || err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSync = async (syncFn, label) => {
    setLoading(true);
    setError(null);
    setSyncResult(null);
    try {
      const response = await syncFn();
      setSyncResult(response.data);
    } catch (err) {
      setError(`${label} failed: ${err.response?.data?.errors?.[0] || err.response?.data?.details || err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const isConnected = connectionStatus?.connected;

  return (
    <div className="trakt-sync-page">
      <div className="page-header">
        <h1 style={{ color: 'white' }}>Trakt Sync</h1>
        <p className="subtitle" style={{ color: 'white' }}>
          Sync your watch history, watchlist, and ratings from Trakt
        </p>
      </div>

      {error && (
        <div className="alert alert-error">
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* Connection Section */}
      <section className="sync-section">
        <h2>Connection Status</h2>

        {connectionStatus === null ? (
          <p>Checking connection...</p>
        ) : isConnected ? (
          <>
            <div className="connection-status connected">
              <span className="status-icon">{'\u2705'}</span>
              <span className="status-message">
                Connected as <span className="status-username">{connectionStatus.username || 'Unknown'}</span>
              </span>
            </div>
            <div style={{ marginTop: '1rem' }}>
              <button
                onClick={handleDisconnect}
                disabled={loading}
                className="btn btn-danger"
              >
                {loading ? 'Disconnecting...' : 'Disconnect'}
              </button>
            </div>
          </>
        ) : (
          <>
            <div className="connection-status disconnected">
              <span className="status-icon">{'\u274C'}</span>
              <span className="status-message">Not connected to Trakt</span>
            </div>

            {!deviceAuth && (
              <div style={{ marginTop: '1rem' }}>
                <button
                  onClick={handleStartAuth}
                  disabled={loading}
                  className="btn btn-primary"
                >
                  {loading ? 'Starting...' : 'Connect to Trakt'}
                </button>
              </div>
            )}

            {deviceAuth && (
              <div className="device-auth-panel">
                <p>Go to the following URL and enter the code:</p>
                <p>
                  <a
                    href={deviceAuth.verificationUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {deviceAuth.verificationUrl}
                  </a>
                </p>
                <div className="device-code-display">
                  {deviceAuth.userCode}
                </div>
                {polling && (
                  <div className="polling-indicator">
                    Waiting for authorization...
                  </div>
                )}
                <button
                  onClick={() => { stopPolling(); setDeviceAuth(null); }}
                  className="btn btn-secondary"
                  style={{ marginTop: '1rem' }}
                >
                  Cancel
                </button>
              </div>
            )}
          </>
        )}
      </section>

      {/* Sync Section - only visible when connected */}
      {isConnected && (
        <>
          <section className="sync-section">
            <h2>Sync Watch History</h2>
            <p>
              Import your watched movies and TV shows from Trakt, including episode-level tracking.
            </p>

            <div className="status-mapping-info">
              <strong>How watched items are synced:</strong>
              <ul>
                <li>
                  <span className="status-badge completed">Watched</span> &rarr; Completed
                </li>
                <li>
                  TV shows with partial episodes watched &rarr; Actively Exploring
                </li>
                <li>
                  New items not in your library are automatically created
                </li>
              </ul>
            </div>

            <button
              onClick={() => handleSync(syncWatched, 'Watch history sync')}
              disabled={loading}
              className="btn btn-primary"
            >
              {loading ? 'Syncing...' : 'Sync Watch History'}
            </button>
          </section>

          <section className="sync-section">
            <h2>Sync Watchlist</h2>
            <p>
              Import your Trakt watchlist. New items will be added to your library with &quot;Uncharted&quot; status.
              Existing items will not have their status changed.
            </p>

            <button
              onClick={() => handleSync(syncWatchlist, 'Watchlist sync')}
              disabled={loading}
              className="btn btn-primary"
            >
              {loading ? 'Syncing...' : 'Sync Watchlist'}
            </button>
          </section>

          <section className="sync-section">
            <h2>Sync Ratings</h2>
            <p>
              Import your Trakt ratings. Ratings are stored as the raw 1-10 Trakt value.
              Your app rating will only be set if you haven&apos;t rated the item yet.
            </p>

            <div className="status-mapping-info">
              <strong>Rating mapping (only applied if unrated):</strong>
              <ul>
                <li>Trakt 1-3 &rarr; Dislike</li>
                <li>Trakt 4-5 &rarr; Neutral</li>
                <li>Trakt 6-8 &rarr; Like</li>
                <li>Trakt 9-10 &rarr; Super Like</li>
              </ul>
            </div>

            <button
              onClick={() => handleSync(syncRatings, 'Ratings sync')}
              disabled={loading}
              className="btn btn-primary"
            >
              {loading ? 'Syncing...' : 'Sync Ratings'}
            </button>
          </section>

          <section className="sync-section">
            <h2>Sync All</h2>
            <p>
              Run all three syncs in sequence: watch history, watchlist, and ratings.
            </p>

            <button
              onClick={() => handleSync(syncAll, 'Full sync')}
              disabled={loading}
              className="btn btn-primary"
            >
              {loading ? 'Syncing All...' : 'Sync Everything'}
            </button>
          </section>

          {/* Sync Results */}
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
                {syncResult.moviesCreated > 0 && (
                  <div className="result-item">
                    <span className="result-label">Movies Created:</span>
                    <span className="result-value">{syncResult.moviesCreated}</span>
                  </div>
                )}
                {syncResult.moviesUpdated > 0 && (
                  <div className="result-item">
                    <span className="result-label">Movies Updated:</span>
                    <span className="result-value">{syncResult.moviesUpdated}</span>
                  </div>
                )}
                {syncResult.showsCreated > 0 && (
                  <div className="result-item">
                    <span className="result-label">Shows Created:</span>
                    <span className="result-value">{syncResult.showsCreated}</span>
                  </div>
                )}
                {syncResult.showsUpdated > 0 && (
                  <div className="result-item">
                    <span className="result-label">Shows Updated:</span>
                    <span className="result-value">{syncResult.showsUpdated}</span>
                  </div>
                )}
                {syncResult.episodesCreated > 0 && (
                  <div className="result-item">
                    <span className="result-label">Episodes Created:</span>
                    <span className="result-value">{syncResult.episodesCreated}</span>
                  </div>
                )}
                {syncResult.episodesUpdated > 0 && (
                  <div className="result-item">
                    <span className="result-label">Episodes Updated:</span>
                    <span className="result-value">{syncResult.episodesUpdated}</span>
                  </div>
                )}
                {syncResult.watchlistItemsProcessed > 0 && (
                  <div className="result-item">
                    <span className="result-label">Watchlist Items:</span>
                    <span className="result-value">{syncResult.watchlistItemsProcessed}</span>
                  </div>
                )}
                {syncResult.ratingsProcessed > 0 && (
                  <div className="result-item">
                    <span className="result-label">Ratings Processed:</span>
                    <span className="result-value">{syncResult.ratingsProcessed}</span>
                  </div>
                )}
              </div>

              {syncResult.errors && syncResult.errors.length > 0 && (
                <ul className="error-list">
                  {syncResult.errors.map((err) => (
                    <li key={`err-${err}`}>{err}</li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </>
      )}

      {/* How It Works */}
      <section className="info-box">
        <h3>How It Works</h3>
        <ul>
          <li><strong>Watch History:</strong> Imports watched movies and TV show episodes with play counts</li>
          <li><strong>Watchlist:</strong> Adds unwatched items to your library as &quot;Uncharted&quot;</li>
          <li><strong>Ratings:</strong> Stores your Trakt ratings and maps them to app ratings</li>
          <li><strong>Matching:</strong> Items are matched by TMDB ID, then by title + year</li>
          <li><strong>Safe:</strong> Existing status and ratings are never overwritten</li>
        </ul>
      </section>

      {/* Trakt attribution */}
      <div className="trakt-attribution">
      <a href="https://trakt.tv" target="_blank" rel="noopener noreferrer">
      <img src="/trakt-logo-dark.svg" alt="Trakt logo" style={{ height: '50px', width: 'auto' }} />
      </a>
      <br />
      <p style={{ fontSize: '20px' }}>  Powered by <a href="https://trakt.tv" target="_blank" rel="noopener noreferrer">Trakt</a></p>
      </div>
    </div>
  );
};

export default TraktSyncPage;
