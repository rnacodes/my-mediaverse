import React, { useState, useEffect } from 'react';
import { validateReadwiseConnection, syncAll as syncReadwiseAll, fetchArticleContent as fetchReadwiseContent } from '../api/readwiseService';
import { getUnlinkedHighlights, updateHighlight, cleanHighlightText } from '../api/highlightService';
import { getAllBooks } from '../api/bookService';
import { getAllArticles } from '../api/articleService';
import './ReadwiseSyncPage.css';

const ReadwiseSyncPage = () => {
  const [syncResult, setSyncResult] = useState(null);
  const [fetchResult, setFetchResult] = useState(null);
  const [connectionStatus, setConnectionStatus] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Unlinked highlights state
  const [unlinkedHighlights, setUnlinkedHighlights] = useState([]);
  const [unlinkedLoading, setUnlinkedLoading] = useState(false);
  const [books, setBooks] = useState([]);
  const [articles, setArticles] = useState([]);
  const [expandedHighlight, setExpandedHighlight] = useState(null);
  const [expandedText, setExpandedText] = useState(new Set()); // Track which highlights have expanded text
  const [searchQuery, setSearchQuery] = useState('');
  const [linkingId, setLinkingId] = useState(null);
  const [linkSuccess, setLinkSuccess] = useState(null);
  const [cleaningHighlights, setCleaningHighlights] = useState(false);
  const [cleanResult, setCleanResult] = useState(null);

  const handleCleanHighlightText = async () => {
    setCleaningHighlights(true);
    setCleanResult(null);
    setError(null);
    try {
      const result = await cleanHighlightText();
      setCleanResult(result);
      // Reload highlights to show cleaned text
      await loadUnlinkedHighlights();
    } catch (err) {
      setError(`Cleanup failed: ${err.response?.data?.details || err.message}`);
    } finally {
      setCleaningHighlights(false);
    }
  };

  const handleValidateConnection = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await validateReadwiseConnection();
      setConnectionStatus(response.data);

      if (!response.data.connected && response.data.details) {
        setError(response.data.details);
      }
    } catch (err) {
      const errorDetails = err.response?.data?.details || err.response?.data?.message || err.message;
      setError(`Connection validation failed: ${errorDetails}`);
      setConnectionStatus({
        connected: false,
        message: err.response?.data?.message || 'Connection failed',
        details: errorDetails
      });
    } finally {
      setLoading(false);
    }
  };

  const handleSync = async (incremental = true) => {
    setLoading(true);
    setError(null);
    setSyncResult(null);
    try {
      const response = await syncReadwiseAll(incremental);
      setSyncResult(response.data);
    } catch (err) {
      setError(`Sync failed: ${err.response?.data?.details || err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleFetchContent = async (batchSize, recentOnly = false) => {
    setLoading(true);
    setError(null);
    setFetchResult(null);
    try {
      const response = await fetchReadwiseContent(batchSize, recentOnly);
      setFetchResult(response.data);
    } catch (err) {
      setError(`Content fetch failed: ${err.response?.data?.details || err.message}`);
    } finally {
      setLoading(false);
    }
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

  // Load unlinked highlights and media on mount
  useEffect(() => {
    loadUnlinkedHighlights();
    loadMediaOptions();
  }, []);

  const loadUnlinkedHighlights = async () => {
    setUnlinkedLoading(true);
    try {
      const highlights = await getUnlinkedHighlights();
      setUnlinkedHighlights(highlights);
    } catch (err) {
      console.error('Error loading unlinked highlights:', err);
    } finally {
      setUnlinkedLoading(false);
    }
  };

  const loadMediaOptions = async () => {
    try {
      const [booksRes, articlesRes] = await Promise.all([
        getAllBooks(),
        getAllArticles()
      ]);
      // getAllBooks returns axios response, getAllArticles returns data directly
      setBooks(booksRes.data || []);
      setArticles(Array.isArray(articlesRes) ? articlesRes : (articlesRes.data || []));
    } catch (err) {
      console.error('Error loading media options:', err);
    }
  };

  const handleLinkHighlight = async (highlightId, mediaType, mediaId) => {
    setLinkingId(highlightId);
    setLinkSuccess(null);
    try {
      const highlight = unlinkedHighlights.find(h => h.id === highlightId);
      if (!highlight) return;

      const updateData = {
        text: highlight.text,
        note: highlight.note,
        tags: highlight.tags || [],
        articleId: mediaType === 'article' ? mediaId : null,
        bookId: mediaType === 'book' ? mediaId : null
      };

      await updateHighlight(highlightId, updateData);
      setLinkSuccess(`Highlight linked to ${mediaType} successfully!`);
      setExpandedHighlight(null);
      setSearchQuery('');

      // Refresh unlinked highlights
      await loadUnlinkedHighlights();
    } catch (err) {
      setError(`Failed to link highlight: ${err.message}`);
    } finally {
      setLinkingId(null);
    }
  };

  const filteredBooks = books.filter(book =>
    searchQuery.length > 0 && (
      book.title?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      book.author?.toLowerCase().includes(searchQuery.toLowerCase())
    )
  ).slice(0, 10);

  const filteredArticles = articles.filter(article =>
    searchQuery.length > 0 && (
      article.title?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      article.author?.toLowerCase().includes(searchQuery.toLowerCase())
    )
  ).slice(0, 10);

  const TEXT_CUTOFF = 200;

  const truncateText = (text, maxLength = TEXT_CUTOFF) => {
    if (!text || text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  };

  const isTextLong = (text) => text && text.length > TEXT_CUTOFF;

  const toggleTextExpansion = (highlightId) => {
    setExpandedText(prev => {
      const newSet = new Set(prev);
      if (newSet.has(highlightId)) {
        newSet.delete(highlightId);
      } else {
        newSet.add(highlightId);
      }
      return newSet;
    });
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

      {/* Unlinked Highlights Section */}
      <section className="sync-section unlinked-highlights-section">
        <h2>Manage Unlinked Highlights</h2>
        <p>
          These highlights are not linked to any book or article in your library.
          Link them manually to see them on the media profile pages.
        </p>

        {linkSuccess && (
          <div className="alert alert-success">
            {linkSuccess}
          </div>
        )}

        <button
          onClick={loadUnlinkedHighlights}
          disabled={unlinkedLoading}
          className="btn btn-secondary"
          style={{ marginBottom: '1rem' }}
        >
          {unlinkedLoading ? 'Loading...' : 'Refresh List'}
        </button>

        {unlinkedLoading ? (
          <div className="loading-state">Loading unlinked highlights...</div>
        ) : unlinkedHighlights.length === 0 ? (
          <div className="empty-state">
            <p>No unlinked highlights found. All highlights are linked to books or articles.</p>
          </div>
        ) : (
          <>
            <div className="unlinked-count">
              <strong>{unlinkedHighlights.length}</strong> highlight{unlinkedHighlights.length !== 1 ? 's' : ''} need manual linking
            </div>

            <div className="unlinked-highlights-list">
              {unlinkedHighlights.map((highlight) => (
                <div key={highlight.id} className="highlight-card">
                  <div className="highlight-text">
                    &quot;{expandedText.has(highlight.id) ? highlight.text : truncateText(highlight.text)}&quot;
                    {isTextLong(highlight.text) && (
                      <button
                        className="read-more-btn"
                        onClick={() => toggleTextExpansion(highlight.id)}
                      >
                        {expandedText.has(highlight.id) ? 'Show Less' : 'Read More'}
                      </button>
                    )}
                  </div>
                  <div className="highlight-meta">
                    {highlight.title && (
                      <span className="meta-item">
                        <strong>From:</strong> {highlight.title}
                        {highlight.author && ` by ${highlight.author}`}
                      </span>
                    )}
                    {highlight.category && (
                      <span className="meta-badge">{highlight.category}</span>
                    )}
                    {highlight.highlightedAt && (
                      <span className="meta-date">
                        {new Date(highlight.highlightedAt).toLocaleDateString()}
                      </span>
                    )}
                  </div>

                  {expandedHighlight === highlight.id ? (
                    <div className="link-panel">
                      <input
                        type="text"
                        placeholder="Search books or articles..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="search-input"
                        // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses search input when link panel expands
                        autoFocus
                      />

                      {searchQuery.length > 0 && (
                        <div className="search-results">
                          {filteredBooks.length > 0 && (
                            <div className="result-section">
                              <h4>Books</h4>
                              {filteredBooks.map((book) => (
                                <button
                                  key={book.id}
                                  className="result-item"
                                  onClick={() => handleLinkHighlight(highlight.id, 'book', book.id)}
                                  disabled={linkingId === highlight.id}
                                >
                                  <span className="result-title">{book.title}</span>
                                  {book.author && <span className="result-author">by {book.author}</span>}
                                </button>
                              ))}
                            </div>
                          )}

                          {filteredArticles.length > 0 && (
                            <div className="result-section">
                              <h4>Articles</h4>
                              {filteredArticles.map((article) => (
                                <button
                                  key={article.id}
                                  className="result-item"
                                  onClick={() => handleLinkHighlight(highlight.id, 'article', article.id)}
                                  disabled={linkingId === highlight.id}
                                >
                                  <span className="result-title">{article.title}</span>
                                  {article.author && <span className="result-author">by {article.author}</span>}
                                </button>
                              ))}
                            </div>
                          )}

                          {filteredBooks.length === 0 && filteredArticles.length === 0 && (
                            <div className="no-results">No matches found</div>
                          )}
                        </div>
                      )}

                      <button
                        className="btn btn-secondary btn-sm"
                        onClick={() => {
                          setExpandedHighlight(null);
                          setSearchQuery('');
                        }}
                        style={{ marginTop: '0.5rem' }}
                      >
                        Cancel
                      </button>
                    </div>
                  ) : (
                    <button
                      className="btn btn-primary btn-sm"
                      onClick={() => setExpandedHighlight(highlight.id)}
                    >
                      Link to Media
                    </button>
                  )}
                </div>
              ))}
            </div>
          </>
        )}
      </section>
    </div>
  );
};

export default ReadwiseSyncPage;
