import React, { useState, useRef } from 'react';
import { useUploadGoodreadsCsv } from '@/hooks/useUpload';
import { splitCsvIntoChunks } from '@/utils/csvChunking';
import './GoodreadsUploadPage.css';

// Books are imported in sequential chunks so large Goodreads exports don't
// overwhelm a single request. The backend is idempotent (books are matched by
// ISBN or Title+Author), so a chunk that fails can be safely re-uploaded.
const ROWS_PER_CHUNK = 200;

const emptySummary = () => ({
  totalProcessed: 0,
  successCount: 0,
  createdCount: 0,
  updatedCount: 0,
  skippedCount: 0,
  errorCount: 0,
  errors: [],
  importedBooks: [],
});

const GoodreadsUploadPage = () => {
  const [file, setFile] = useState(null);
  const [updateExisting, setUpdateExisting] = useState(true);
  const [error, setError] = useState(null);
  const [importing, setImporting] = useState(false);
  const [progress, setProgress] = useState(null); // { rowsProcessed, totalRows, chunksDone, totalChunks }
  const [summary, setSummary] = useState(null); // aggregated GoodreadsImportResult shape
  const [failedChunks, setFailedChunks] = useState([]);
  const fileInputRef = useRef(null);

  const uploadMutation = useUploadGoodreadsCsv();

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile) {
      if (!selectedFile.name.endsWith('.csv')) {
        setError('Please select a CSV file');
        setFile(null);
        return;
      }
      setFile(selectedFile);
      setError(null);
      setSummary(null);
      setProgress(null);
      setFailedChunks([]);
      uploadMutation.reset();
    }
  };

  const handleUpload = async () => {
    if (!file) {
      setError('Please select a file first');
      return;
    }

    setError(null);
    setSummary(null);
    setProgress(null);
    setFailedChunks([]);
    uploadMutation.reset();

    let chunks;
    try {
      const text = await file.text();
      chunks = splitCsvIntoChunks(text, ROWS_PER_CHUNK);
    } catch (readErr) {
      setError(`Could not read the CSV file: ${readErr.message}`);
      return;
    }

    if (chunks.length === 0) {
      setError('The CSV file has no book rows to import.');
      return;
    }

    const totalRows = chunks.reduce((sum, chunk) => sum + chunk.rowCount, 0);
    const agg = emptySummary();
    const failed = [];
    let rowsProcessed = 0;

    setImporting(true);
    setProgress({ rowsProcessed: 0, totalRows, chunksDone: 0, totalChunks: chunks.length });

    for (let i = 0; i < chunks.length; i += 1) {
      const chunk = chunks[i];
      const chunkFile = new File([chunk.csv], file.name, { type: 'text/csv' });

      try {
        // Chunks must upload sequentially, so awaiting inside the loop is intentional.
        const data = await uploadMutation.mutateAsync({
          file: chunkFile,
          updateExisting,
          chunkIndex: i,
          totalChunks: chunks.length,
        });

        // Chunked responses are wrapped as { chunkIndex, totalChunks, result };
        // fall back to the bare result for safety.
        const result = data?.result ?? data;
        agg.totalProcessed += result.totalProcessed ?? 0;
        agg.successCount += result.successCount ?? 0;
        agg.createdCount += result.createdCount ?? 0;
        agg.updatedCount += result.updatedCount ?? 0;
        agg.skippedCount += result.skippedCount ?? 0;
        agg.errorCount += result.errorCount ?? 0;
        if (Array.isArray(result.errors)) {
          agg.errors.push(...result.errors);
        }
        if (Array.isArray(result.importedBooks)) {
          agg.importedBooks.push(...result.importedBooks);
        }
      } catch (chunkErr) {
        const reason =
          chunkErr.response?.data?.error ||
          chunkErr.response?.data?.details ||
          chunkErr.message;
        failed.push({
          chunkIndex: i,
          startRow: chunk.startRow,
          endRow: chunk.endRow,
          reason,
        });
      }

      rowsProcessed += chunk.rowCount;
      setSummary({ ...agg });
      setFailedChunks([...failed]);
      setProgress({
        rowsProcessed,
        totalRows,
        chunksDone: i + 1,
        totalChunks: chunks.length,
      });
    }

    setImporting(false);
  };

  const handleClear = () => {
    setFile(null);
    setError(null);
    setSummary(null);
    setProgress(null);
    setFailedChunks([]);
    uploadMutation.reset();
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const progressPercent =
    progress && progress.totalRows > 0
      ? Math.round((progress.rowsProcessed / progress.totalRows) * 100)
      : 0;

  return (
    <div className="goodreads-upload-page">
      <div className="page-header">
        <h1>Import from Goodreads</h1>
        <p className="subtitle">
          Upload your Goodreads library export to import books
        </p>
      </div>

      {error && (
        <div className="alert alert-error">
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* Instructions Section */}
      <section className="upload-section">
        <h2>How to Export from Goodreads</h2>
        <ol className="instructions-list">
          <li>Go to <a href="https://www.goodreads.com/review/import" target="_blank" rel="noopener noreferrer">Goodreads Export</a></li>
          <li>Click &quot;Export Library&quot; at the top of the page</li>
          <li>Wait for the export to complete (this may take a few minutes for large libraries)</li>
          <li>Download the CSV file when ready</li>
          <li>Upload the CSV file below</li>
        </ol>
      </section>

      {/* Upload Section */}
      <section className="upload-section">
        <h2>Upload CSV File</h2>

        <div className="file-input-container">
          <input
            type="file"
            accept=".csv"
            onChange={handleFileChange}
            ref={fileInputRef}
            className="file-input"
            id="goodreads-file"
            disabled={importing}
          />
          <label htmlFor="goodreads-file" className="file-input-label">
            {file ? file.name : 'Choose a CSV file...'}
          </label>
        </div>

        <div className="checkbox-container">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={updateExisting}
              onChange={(e) => setUpdateExisting(e.target.checked)}
              disabled={importing}
            />
            <span>Update existing books on match</span>
          </label>
          <p className="checkbox-help">
            When enabled, books that already exist (matched by ISBN or Title+Author) will be updated with new data from Goodreads.
          </p>
        </div>

        <div className="button-group">
          <button
            onClick={handleUpload}
            disabled={importing || !file}
            className="btn btn-primary"
          >
            {importing ? 'Importing...' : 'Upload & Import'}
          </button>
          <button
            onClick={handleClear}
            disabled={importing}
            className="btn btn-secondary"
          >
            Clear
          </button>
        </div>

        {progress && (
          <div className="progress-container">
            <div className="progress-bar">
              <div className="progress-fill" style={{ width: `${progressPercent}%` }} />
            </div>
            <p className="progress-text">
              {importing ? 'Importing' : 'Imported'} {progress.rowsProcessed} of {progress.totalRows} rows
              {' '}(chunk {progress.chunksDone} of {progress.totalChunks})
            </p>
          </div>
        )}
      </section>

      {/* Results Section */}
      {summary && (
        <section className="upload-section results-section">
          <h2>Import Results</h2>

          <div className="stats-grid">
            <div className="stat-card">
              <span className="stat-value">{summary.totalProcessed}</span>
              <span className="stat-label">Total Processed</span>
            </div>
            <div className="stat-card success">
              <span className="stat-value">{summary.successCount}</span>
              <span className="stat-label">Successful</span>
            </div>
            <div className="stat-card created">
              <span className="stat-value">{summary.createdCount}</span>
              <span className="stat-label">Created</span>
            </div>
            <div className="stat-card updated">
              <span className="stat-value">{summary.updatedCount}</span>
              <span className="stat-label">Updated</span>
            </div>
            {summary.skippedCount > 0 && (
              <div className="stat-card skipped">
                <span className="stat-value">{summary.skippedCount}</span>
                <span className="stat-label">Skipped</span>
              </div>
            )}
            {summary.errorCount > 0 && (
              <div className="stat-card error">
                <span className="stat-value">{summary.errorCount}</span>
                <span className="stat-label">Errors</span>
              </div>
            )}
          </div>

          {failedChunks.length > 0 && (
            <div className="errors-list failed-chunks">
              <h3>Failed Chunks ({failedChunks.length})</h3>
              <ul>
                {failedChunks.map((fc) => (
                  <li key={`chunk-${fc.chunkIndex}`}>
                    Rows {fc.startRow}&ndash;{fc.endRow}: {fc.reason}
                  </li>
                ))}
              </ul>
              <p className="failed-chunks-help">
                These chunks were skipped. The rest of your library still imported.
                You can safely re-upload the same file &mdash; existing books are matched
                and updated, not duplicated.
              </p>
            </div>
          )}

          {summary.errors && summary.errors.length > 0 && (
            <div className="errors-list">
              <h3>Errors</h3>
              <ul>
                {summary.errors.slice(0, 10).map((err) => (
                  <li key={`err-${err}`}>{err}</li>
                ))}
                {summary.errors.length > 10 && (
                  <li className="more-errors">...and {summary.errors.length - 10} more errors</li>
                )}
              </ul>
            </div>
          )}

          {summary.importedBooks && summary.importedBooks.length > 0 && (
            <div className="imported-books">
              <h3>Imported Books ({summary.importedBooks.length})</h3>
              <div className="books-list">
                {summary.importedBooks.slice(0, 20).map((book) => (
                  <div key={book.id} className={`book-item ${book.wasUpdated ? 'updated' : 'created'}`}>
                    {book.thumbnail && (
                      <img src={book.thumbnail} alt={book.title} className="book-thumbnail" />
                    )}
                    <div className="book-info">
                      <span className="book-title">{book.title}</span>
                      <span className="book-author">by {book.author}</span>
                      <span className={`book-status ${book.wasUpdated ? 'updated' : 'created'}`}>
                        {book.wasUpdated ? 'Updated' : 'Created'}
                      </span>
                    </div>
                  </div>
                ))}
                {summary.importedBooks.length > 20 && (
                  <p className="more-books">...and {summary.importedBooks.length - 20} more books</p>
                )}
              </div>
            </div>
          )}
        </section>
      )}

      {/* Info Section */}
      <section className="upload-section info-section">
        <h2>About Goodreads Import</h2>
        <div className="info-content">
          <h3>What Gets Imported</h3>
          <ul>
            <li>Title, Author, ISBN</li>
            <li>Your rating and the average community rating</li>
            <li>Reading status (to-read, currently-reading, read)</li>
            <li>Publisher and publication years</li>
            <li>Date read and date added</li>
            <li>Your review</li>
            <li>Bookshelves (as tags)</li>
            <li>Book format (paperback, hardcover, etc.)</li>
          </ul>

          <h3>Deduplication</h3>
          <p>
            Books are matched first by ISBN, then by Title + Author combination.
            If a match is found and &quot;Update existing books&quot; is enabled, the existing book will be updated with the new data.
          </p>

          <h3>Large Libraries</h3>
          <p>
            Large exports are uploaded in batches of {ROWS_PER_CHUNK} books at a time,
            with progress shown above. If a batch fails, the rest still import and the
            failed rows are reported so you can re-upload.
          </p>

          <h3>Book Descriptions</h3>
          <p>
            Book descriptions are not included in Goodreads exports. A background service runs periodically
            (every 48 hours) to automatically fetch descriptions from Google Books for books that have an ISBN.
            This process runs in batches to respect API rate limits, so it may take some time for all
            descriptions to be populated.
          </p>
        </div>
      </section>
    </div>
  );
};

export default GoodreadsUploadPage;
