/* eslint-disable react-refresh/only-export-components -- entry file: RootErrorFallback is co-located intentionally and never hot-reloaded */
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ErrorBoundary } from 'react-error-boundary'
import './index.css'
import App from './App.jsx'

function RootErrorFallback({ error }) {
  return (
    <div role="alert" style={{ padding: '2rem', maxWidth: 600, margin: '4rem auto', textAlign: 'center', fontFamily: 'sans-serif' }}>
      <h1 style={{ fontSize: '1.5rem', marginBottom: '0.5rem' }}>Something went wrong</h1>
      <p style={{ color: '#666', marginBottom: '1rem' }}>An unexpected error occurred. Reloading the page usually fixes it.</p>
      <pre style={{ textAlign: 'left', background: '#f5f5f5', padding: '1rem', borderRadius: 4, overflow: 'auto', fontSize: '0.85rem' }}>
        {error?.message || 'Unknown error'}
      </pre>
      <button
        type="button"
        onClick={() => window.location.reload()}
        style={{ marginTop: '1rem', padding: '0.5rem 1.25rem', fontSize: '1rem', cursor: 'pointer' }}
      >
        Reload page
      </button>
    </div>
  )
}

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <ErrorBoundary FallbackComponent={RootErrorFallback} onError={(error, info) => console.error('Top-level error:', error, info)}>
      <App />
    </ErrorBoundary>
  </StrictMode>,
)
