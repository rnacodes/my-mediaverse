import React, { useState, useRef } from 'react';
import { useUploadGoodreadsCsv } from '../hooks/useUpload';
import './GoodreadsUploadPage.css';

const GoodreadsUploadPage = () => {
  const [file, setFile] = useState(null);
  const [updateExisting, setUpdateExisting] = useState(true);
  const [error, setError] = useState(null);
  const fileInputRef = useRef(null);

  const uploadMutation = useUploadGoodreadsCsv();
  const loading = uploadMutation.isPending;
  const result = uploadMutation.data;

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
      uploadMutation.reset();
    }
  };

  const handleUpload = () => {
    if (!file) {
      setError('Please select a file first');
      return;
    }
    setError(null);
    uploadMutation.mutate(
      { file, updateExisting },
      {
        onError: (err) => {
          const errorMsg = err.response?.data?.error || err.response?.data?.details || err.message;
          setError(`Upload failed: ${errorMsg}`);
        },
      }
    );
  };

  const handleClear = () => {
    setFile(null);
    setError(null);
    uploadMutation.reset();
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

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
            disabled={loading || !file}
            className="btn btn-primary"
          >
            {loading ? 'Uploading...' : 'Upload & Import'}
          </button>
          <button
            onClick={handleClear}
            disabled={loading}
            className="btn btn-secondary"
          >
            Clear
          </button>
        </div>

        {loading && (
          <div className="progress-container">
            <div className="progress-bar">
              <div className="progress-fill" style={{ width: '0%' }} />
            </div>
            <p className="progress-text">Uploading...</p>
          </div>
        )}
      </section>

      {/* Results Section */}
      {result && (
        <section className="upload-section results-section">
          <h2>Import Results</h2>

          <div className="stats-grid">
            <div className="stat-card">
              <span className="stat-value">{result.totalProcessed}</span>
              <span className="stat-label">Total Processed</span>
            </div>
            <div className="stat-card success">
              <span className="stat-value">{result.successCount}</span>
              <span className="stat-label">Successful</span>
            </div>
            <div className="stat-card created">
              <span className="stat-value">{result.createdCount}</span>
              <span className="stat-label">Created</span>
            </div>
            <div className="stat-card updated">
              <span className="stat-value">{result.updatedCount}</span>
              <span className="stat-label">Updated</span>
            </div>
            {result.skippedCount > 0 && (
              <div className="stat-card skipped">
                <span className="stat-value">{result.skippedCount}</span>
                <span className="stat-label">Skipped</span>
              </div>
            )}
            {result.errorCount > 0 && (
              <div className="stat-card error">
                <span className="stat-value">{result.errorCount}</span>
                <span className="stat-label">Errors</span>
              </div>
            )}
          </div>

          {result.errors && result.errors.length > 0 && (
            <div className="errors-list">
              <h3>Errors</h3>
              <ul>
                {result.errors.slice(0, 10).map((err) => (
                  <li key={`err-${err}`}>{err}</li>
                ))}
                {result.errors.length > 10 && (
                  <li className="more-errors">...and {result.errors.length - 10} more errors</li>
                )}
              </ul>
            </div>
          )}

          {result.importedBooks && result.importedBooks.length > 0 && (
            <div className="imported-books">
              <h3>Imported Books ({result.importedBooks.length})</h3>
              <div className="books-list">
                {result.importedBooks.slice(0, 20).map((book) => (
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
                {result.importedBooks.length > 20 && (
                  <p className="more-books">...and {result.importedBooks.length - 20} more books</p>
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
            For very large libraries (thousands of books), consider splitting your CSV into smaller chunks
            or using the PowerShell batch import script available in the scripts folder.
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
