import { lazy, Suspense } from 'react';
import { BrowserRouter as Router, Route, Routes, Link, useLocation } from 'react-router-dom';
import { ErrorBoundary } from 'react-error-boundary';
import { ThemeProvider, CssBaseline, Typography, Button, Box } from '@mui/material';

// --- Eager imports: providers, route guards, chrome (always rendered) ---
import { AuthProvider } from './contexts/AuthProvider';
import { DemoAdminProvider } from './contexts/DemoAdminProvider';
import { DemoReadOnlyProvider } from './contexts/DemoReadOnlyProvider';
import ConditionalProtectedRoute from './features/auth/ConditionalProtectedRoute';
import DemoReadOnlyDialog from './components/shared/DemoReadOnlyDialog';
import { theme } from './components/shared/DesignSystem';
import ResponsiveNavigation from './components/shared/ResponsiveNavigation';
import Footer from './components/shared/Footer';
import LoadingSpinner from './components/shared/LoadingSpinner';

// --- Eager route components: bundled into the main chunk for instant nav ---
import HomePage from './components/HomePage';
import LoginPage from './features/auth/pages/LoginPage';
import AddMediaForm from './components/AddMediaForm';
import AllMedia from './components/AllMedia';
import MixlistsPage from './components/MixlistsPage';
import CreateMixlistForm from './components/CreateMixlistForm';
import MixlistProfilePage from './components/MixlistProfilePage';
import MediaProfilePage from './components/MediaProfilePage';
import EditMediaForm from './components/EditMediaForm';
import EditMixlistForm from './components/EditMixlistForm';
import ImportMediaPage from './components/ImportMedia';
import ImportGenresTopicsPage from './components/ImportGenresTopicsPage';
import SearchByTopicOrGenre from './components/SearchByTopicOrGenre';
import Search from './components/Search';
import UploadMediaPage from './components/UploadMediaPage';
import YouTubeCallback from './features/videos/pages/YouTubeCallback';
import ReadwiseSyncPage from './components/ReadwiseSyncPage';
import TraktSyncPage from './components/TraktSyncPage';
import HighlightLinkingPage from './components/HighlightLinkingPage';
import ArticlesPage from './components/ArticlesPage';
import DocumentsPage from './components/DocumentsPage';
import SourceDirectoryPage from './components/SourceDirectoryPage';
import YouTubeChannelList from './components/YouTubeChannelList';
import YouTubeChannelProfile from './components/YouTubeChannelProfile';
import YouTubePlaylistProfile from './components/YouTubePlaylistProfile';
import PodcastSeriesProfile from './components/PodcastSeriesProfile';
import TvShowProfile from './components/TvShowProfile';
import CleanupManagementPage from './components/CleanupManagementPage';
import WebsiteImportPage from './components/WebsiteImportPage';
import WebsitesPage from './components/WebsitesPage';
import GoodreadsUploadPage from './components/GoodreadsUploadPage';
import NoteProfilePage from './components/NoteProfilePage';
import HighlightProfilePage from './components/HighlightProfilePage';
import ScriptExecutionPage from './components/ScriptExecutionPage';
import NotesListingPage from './components/NotesListingPage';
import AiAdminPage from './components/AiAdminPage';
import SearchByVibePage from './components/SearchByVibePage';
import DemoUnlockPage from './components/DemoUnlockPage';
import DemoDataUploadPage from './components/DemoDataUploadPage';

// --- Lazy: heavy + infrequently-visited routes. Kept out of the main chunk.
// DemoPage (113 kB, separate user path) + admin/import maintenance pages. ---
const DemoPage = lazy(() => import('./components/DemoPage'));
const ImportMixlistPage = lazy(() => import('./components/ImportMixlistPage'));
const TypesenseAdminPage = lazy(() => import('./components/TypesenseAdminPage'));
const BackgroundJobsPage = lazy(() => import('./components/BackgroundJobsPage'));

function RouteErrorFallback({ error, resetErrorBoundary }) {
  return (
    <Box role="alert" sx={{ p: 4, maxWidth: 600, mx: 'auto', textAlign: 'center' }}>
      <Typography variant="h5" gutterBottom>This page hit an error</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        You can try this page again, or navigate elsewhere using the menu above.
      </Typography>
      <Box component="pre" sx={{ textAlign: 'left', bgcolor: 'rgba(255,255,255,0.05)', p: 2, borderRadius: 1, overflow: 'auto', fontSize: '0.85rem', mb: 2 }}>
        {error?.message || 'Unknown error'}
      </Box>
      <Button variant="contained" onClick={resetErrorBoundary}>Try again</Button>
    </Box>
  );
}

function RoutedContent() {
  const location = useLocation();
  return (
    <ErrorBoundary
      FallbackComponent={RouteErrorFallback}
      resetKeys={[location.pathname]}
      onError={(error) => console.error('Route error:', error)}
    >
      <Suspense fallback={<LoadingSpinner fullScreen message="Loading page..." />}>
        <Routes>
            {/* Public routes - always accessible */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/demo" element={<DemoPage />} />

            {/* Protected routes - require login in production, open in demo */}
            <Route path="/" element={
              <ConditionalProtectedRoute><HomePage /></ConditionalProtectedRoute>
            } />
            <Route path="/add-media" element={
              <ConditionalProtectedRoute><AddMediaForm /></ConditionalProtectedRoute>
            } />
            <Route path="/all-media" element={
              <ConditionalProtectedRoute><AllMedia /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlists" element={
              <ConditionalProtectedRoute><MixlistsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlist/:id" element={
              <ConditionalProtectedRoute><MixlistProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlist/:id/edit" element={
              <ConditionalProtectedRoute><EditMixlistForm /></ConditionalProtectedRoute>
            } />
            <Route path="/create-mixlist" element={
              <ConditionalProtectedRoute><CreateMixlistForm /></ConditionalProtectedRoute>
            } />
            <Route path="/import-media" element={
              <ConditionalProtectedRoute><ImportMediaPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-mixlist" element={
              <ConditionalProtectedRoute><ImportMixlistPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-genres-topics" element={
              <ConditionalProtectedRoute><ImportGenresTopicsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/upload-media" element={
              <ConditionalProtectedRoute><UploadMediaPage /></ConditionalProtectedRoute>
            } />
            <Route path="/upload-goodreads" element={
              <ConditionalProtectedRoute><GoodreadsUploadPage /></ConditionalProtectedRoute>
            } />
            <Route path="/search-by-topic-genre" element={
              <ConditionalProtectedRoute><SearchByTopicOrGenre /></ConditionalProtectedRoute>
            } />
            <Route path="/search" element={
              <ConditionalProtectedRoute><Search /></ConditionalProtectedRoute>
            } />
            <Route path="/media/:id" element={
              <ConditionalProtectedRoute><MediaProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/media/:id/edit" element={
              <ConditionalProtectedRoute><EditMediaForm /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube/callback" element={
              <ConditionalProtectedRoute><YouTubeCallback /></ConditionalProtectedRoute>
            } />
            <Route path="/readwise-sync" element={
              <ConditionalProtectedRoute><ReadwiseSyncPage /></ConditionalProtectedRoute>
            } />
            <Route path="/trakt-sync" element={
              <ConditionalProtectedRoute><TraktSyncPage /></ConditionalProtectedRoute>
            } />
            <Route path="/highlight-linking" element={
              <ConditionalProtectedRoute><HighlightLinkingPage /></ConditionalProtectedRoute>
            } />
            <Route path="/articles" element={
              <ConditionalProtectedRoute><ArticlesPage /></ConditionalProtectedRoute>
            } />
            <Route path="/documents" element={
              <ConditionalProtectedRoute><DocumentsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/sources" element={
              <ConditionalProtectedRoute><SourceDirectoryPage /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube-channels" element={
              <ConditionalProtectedRoute><YouTubeChannelList /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube-channel/:id" element={
              <ConditionalProtectedRoute><YouTubeChannelProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube-playlist/:id" element={
              <ConditionalProtectedRoute><YouTubePlaylistProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/podcast-series/:id" element={
              <ConditionalProtectedRoute><PodcastSeriesProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/tv-show/:id" element={
              <ConditionalProtectedRoute><TvShowProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/podcast-episode/:id" element={
              <ConditionalProtectedRoute><MediaProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/cleanup" element={
              <ConditionalProtectedRoute><CleanupManagementPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-website" element={
              <ConditionalProtectedRoute><WebsiteImportPage /></ConditionalProtectedRoute>
            } />
            <Route path="/websites" element={
              <ConditionalProtectedRoute><WebsitesPage /></ConditionalProtectedRoute>
            } />
            <Route path="/typesense-admin" element={
              <ConditionalProtectedRoute><TypesenseAdminPage /></ConditionalProtectedRoute>
            } />
            <Route path="/background-jobs" element={
              <ConditionalProtectedRoute><BackgroundJobsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/script-execution" element={
              <ConditionalProtectedRoute><ScriptExecutionPage /></ConditionalProtectedRoute>
            } />
            <Route path="/note/:id" element={
              <ConditionalProtectedRoute><NoteProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/notes" element={
              <ConditionalProtectedRoute><NotesListingPage /></ConditionalProtectedRoute>
            } />
            <Route path="/highlight/:id" element={
              <ConditionalProtectedRoute><HighlightProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/ai-admin" element={
              <ConditionalProtectedRoute><AiAdminPage /></ConditionalProtectedRoute>
            } />
            <Route path="/search-by-vibe" element={
              <ConditionalProtectedRoute><SearchByVibePage /></ConditionalProtectedRoute>
            } />
            <Route path="/demo-unlock" element={
              <ConditionalProtectedRoute><DemoUnlockPage /></ConditionalProtectedRoute>
            } />
            <Route path="/upload-demo-data" element={
              <ConditionalProtectedRoute><DemoDataUploadPage /></ConditionalProtectedRoute>
            } />
          {/* Catch-all route for 404 */}
          <Route path="*" element={
            <div style={{ padding: '2rem', textAlign: 'center' }}>
              <Typography variant="h4">Page Not Found</Typography>
              <Typography variant="body1">The page you&apos;re looking for doesn&apos;t exist.</Typography>
              <Button component={Link} to="/" variant="contained" sx={{ mt: 2 }}>
                Go Home
              </Button>
            </div>
          } />
        </Routes>
      </Suspense>
    </ErrorBoundary>
  );
}

function App() {
  return (
    // The ThemeProvider wraps the entire application
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Router>
        <AuthProvider>
          <DemoAdminProvider>
          <DemoReadOnlyProvider>
          <DemoReadOnlyDialog />
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              minHeight: '100vh'
            }}
          >
            {/* Responsive Navigation Component */}
            <ResponsiveNavigation />

            {/* Routes without outer container - each component handles its own layout */}
            <Box component="main" sx={{ flexGrow: 1 }}>
              <RoutedContent />
            </Box>

            {/* Footer Component */}
            <Footer />
          </Box>
          </DemoReadOnlyProvider>
          </DemoAdminProvider>
        </AuthProvider>
      </Router>
    </ThemeProvider>
  );
}

export default App;
